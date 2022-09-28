using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VRC.OSCQuery
{
    public class OSCQueryService : IDisposable
    {
        // Constants
        public const int DefaultPortHttp = 8080;
        public const int DefaultPortOsc = 9000;
        public const string DefaultServerName = "OSCQueryService";

        // Services
        private static readonly string _localOscUdpServiceName = $"{Attributes.SERVICE_OSC_UDP}.local";
        private static readonly string _localOscJsonServiceName = $"{Attributes.SERVICE_OSCJSON_TCP}.local";

        // Zeroconf
        private ServiceProfile _zeroconfService;
        private ServiceProfile _oscService;
        private MulticastService _mdns;
        private ServiceDiscovery _discovery;
        public event Action<ServiceProfile> OnProfileAdded;
        public event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        public event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;

        // Store discovered services
        private HashSet<OSCQueryServiceProfile> _oscQueryServices = new HashSet<OSCQueryServiceProfile>();
        private HashSet<OSCQueryServiceProfile> _oscServices = new HashSet<OSCQueryServiceProfile>();

        // HTTP Server
        HttpListener _listener;
        private bool _shouldProcessHttp;
        
        // Misc
        private OSCQueryRootNode _rootNode;
        private HostInfo _hostInfo;
        public static ILogger<OSCQueryService> Logger;
        private readonly HashSet<string> _matchedNames = new HashSet<string>() { 
            _localOscUdpServiceName, _localOscJsonServiceName
        };

        public OSCQueryService(string serverName, ILogger<OSCQueryService> logger = null)
        { 
            Logger = logger ?? new NullLogger<OSCQueryService>();
            Initialize(serverName); 
            RefreshServices();
        }
        
        /// <summary>
        /// Creates an OSCQueryService which can track OSC endpoints in the enclosing program as well as find other OSCQuery-compatible services on the link-local network
        /// </summary>
        /// <param name="serverName">Server name to use, default is "OSCQueryService"</param>
        /// <param name="httpPort">TCP port on which to serve OSCQuery info, default is 8080</param>
        /// <param name="oscPort">UDP Port at which the OSC Server can be reached, default is 9000</param>
        /// <param name="logger">Optional logger which will be used for logs generated within this class. Will log to Null if not set.</param>
        public OSCQueryService(string serverName = DefaultServerName, int httpPort = DefaultPortHttp, int oscPort = DefaultPortOsc, ILogger<OSCQueryService> logger = null)
        {
            Logger = logger ?? new NullLogger<OSCQueryService>();
            Initialize(serverName);
            StartOSCQueryService(serverName, httpPort);
            AdvertiseOSCService(serverName, oscPort);
            RefreshServices();
        }

        public void Initialize(string serverName = DefaultServerName)
        {
            // Create HostInfo object
            _hostInfo = new HostInfo()
            {
                name = serverName,
            };
            
            _mdns = new MulticastService();
            _mdns.UseIpv6 = false;
            _mdns.IgnoreDuplicateMessages = true;

            _discovery = new ServiceDiscovery(_mdns);
            
            // Query for OSC and OSCQuery services on every network interface
            _mdns.NetworkInterfaceDiscovered += (s, e) =>
            {
                RefreshServices();
            };
            
            // Callback invoked when the above query is answered
            _mdns.AnswerReceived += OnRemoteServiceInfo;
            _mdns.Start();
        }

        public void StartOSCQueryService(string serverName, int httpPort = -1)
        {
            BuildRootResponse();
            
            // Use the provided port or grab a new one
            httpPort = httpPort == -1 ? Extensions.GetAvailableTcpPort() : httpPort;
            
            // Advertise OSCJSON service
            _zeroconfService = new ServiceProfile(serverName, Attributes.SERVICE_OSCJSON_TCP, (ushort)httpPort, new[] { IPAddress.Loopback });
            _discovery.Advertise(_zeroconfService);
            Logger.LogInformation($"Advertising TCP Service {serverName} as {Attributes.SERVICE_OSCJSON_TCP} on {httpPort}");

            // Create and start HTTPListener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{httpPort}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{httpPort}/");
            _listener.Start();
            Task.Run(() => HttpListenerLoop());
            _shouldProcessHttp = true;
        }

        public void AdvertiseOSCService(string serverName, int oscPort = -1)
        {
            _hostInfo.oscPort = oscPort;
            _hostInfo.oscIP = IPAddress.Loopback.ToString();
            _oscService = new ServiceProfile(serverName, Attributes.SERVICE_OSC_UDP, (ushort)oscPort, new[] { IPAddress.Loopback });
            _discovery.Advertise(_oscService);
            Logger.LogInformation($"Advertising OSC Service {serverName} as {Attributes.SERVICE_OSC_UDP} on {oscPort}");
        }

        public void RefreshServices()
        {
            _mdns.SendQuery(_localOscUdpServiceName);
            _mdns.SendQuery(_localOscJsonServiceName);
        }

        /// <summary>
        /// Callback invoked when an mdns Service provides information about itself 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs">Event Data with info from queried Service</param>
        private void OnRemoteServiceInfo(object sender, MessageEventArgs eventArgs)
        {
            var response = eventArgs.Message;
            
            try
            {
                // Check whether this service matches OSCJSON or OSC services for which we're looking
                bool hasMatch = response.Answers.Any(record => _matchedNames.Contains(record?.CanonicalName));
                if (!hasMatch)
                {
                    return;
                }
                
                // Get the name and SRV Record of the service
                var name = response.Answers.First(r => _matchedNames.Contains(r?.CanonicalName)).CanonicalName;
                var srvRecord = response.AdditionalRecords.OfType<SRVRecord>().FirstOrDefault();
                if (srvRecord == default)
                {
                    Logger.LogWarning($"Found the matching service {name}, but it doesn't have an SRVRecord, can't proceed.");
                    return;
                }
                
                // Get the rest of the items we need to track this service
                var port = srvRecord.Port;
                var domainName = srvRecord.Name.Labels;
                var instanceName = domainName[0];

                var serviceName = string.Join(".", domainName.Skip(1).SkipLast(1));
                var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
                var profile = new ServiceProfile(instanceName, serviceName, srvRecord.Port, ips);

                // If this is an OSC service, add it to the OSC collection
                if (name.CompareTo(_localOscUdpServiceName) == 0 && profile != _oscService)
                {
                    // Make sure there's not already a service with the same name
                    if (!_oscServices.Any(p => p.name == profile.InstanceName))
                    {
                        var p = new OSCQueryServiceProfile(instanceName, ips.First(), port, OSCQueryServiceProfile.ServiceType.OSC);
                        _oscServices.Add(p);
                        OnProfileAdded?.Invoke(profile);
                        OnOscServiceAdded?.Invoke(p);
                        Logger.LogInformation($"Found match {name} on port {port}");
                    }
                }
                // If this is an OSCQuery service, add it to the OSCQuery collection
                else if (name.CompareTo(_localOscJsonServiceName) == 0 && (_zeroconfService != null && profile.FullyQualifiedName != _zeroconfService.FullyQualifiedName))
                {
                    // Make sure there's not already a service with the same name
                    if (!_oscQueryServices.Any(p => p.name == profile.InstanceName))
                    {
                        var p = new OSCQueryServiceProfile(instanceName, ips.First(), port, OSCQueryServiceProfile.ServiceType.OSCQuery);
                        _oscQueryServices.Add(p);
                        OnProfileAdded?.Invoke(profile);
                        OnOscQueryServiceAdded?.Invoke(p);
                        Logger.LogInformation($"Found match {name} on port {port}");
                    }
                    else
                    {
                        Logger.LogInformation($"Not triggering service added for {name} because it's already tracked");
                    }
                }
            }
            catch (Exception e)
            {
                // Using a non-error log level because we may have just found a non-matching service
                Logger.LogInformation($"Could not parse answer from {eventArgs.RemoteEndPoint}: {e.Message}");
            }
        }

        public HashSet<OSCQueryServiceProfile> GetOSCQueryServices() => _oscQueryServices;
        public HashSet<OSCQueryServiceProfile> GetOSCServices() => _oscServices;
        
        public void SetValue(string address, string value)
        {
            var target = _rootNode.GetNodeWithPath(address);
            if (target == null)
            {
                // add this node
                target = _rootNode.AddNode(new OSCQueryNode(address));
            }
            
            target.Value = value;
        }

        /// <summary>
        /// Process and responds to incoming HTTP queries
        /// </summary>
        private async Task HttpListenerLoop()
        {
            while (_shouldProcessHttp)
            {
                // Wait until next request
                var context = await _listener.GetContextAsync();
                
                if (context.Request.RawUrl.Contains(Attributes.HOST_INFO))
                {
                    try
                    {
                        // Serve Host Info for requests with "HOST_INFO" in them
                        var hostInfoString = _hostInfo.ToString();
                        
                        // Send Response
                        context.Response.Headers.Add("pragma:no-cache");
                
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = hostInfoString.Length;
                        using (var sw = new StreamWriter(context.Response.OutputStream))
                        {
                            await sw.WriteAsync(hostInfoString);
                            await sw.FlushAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Could not construct and send Host Info: {e.Message}");
                    }
                }
                else if (context.Request.RawUrl.Contains("favicon.ico"))
                {
                    // ignore for now, could send favicon if we want to be fancy
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    string err = "favicon not supported";
                    context.Response.ContentLength64 = err.Length;
                    using (var sw = new StreamWriter(context.Response.OutputStream))
                    {
                        await sw.WriteAsync(err);
                        await sw.FlushAsync();
                    }
                }
                else
                {
                    var path = context.Request.Url.LocalPath;
                    var matchedNode = _rootNode.GetNodeWithPath(path);
                    if (matchedNode == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        string err = "OSC Path not found";
                        context.Response.ContentLength64 = err.Length;
                        using (var sw = new StreamWriter(context.Response.OutputStream))
                        {
                            await sw.WriteAsync(err);
                            await sw.FlushAsync();
                        }
                    }
                    else
                    {
                        var stringResponse = matchedNode.ToString();
                    
                        // Send Response
                        context.Response.Headers.Add("pragma:no-cache");
                
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = stringResponse.Length;
                        using (var sw = new StreamWriter(context.Response.OutputStream))
                        {
                            await sw.WriteAsync(stringResponse);
                            await sw.FlushAsync();
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Registers the info for an OSC path.
        /// </summary>
        /// <param name="name">String used for building JSON tree</param>
        /// <param name="accessValues">Enum - 0: NoValue, 1: ReadOnly 2:WriteOnly 3:ReadWrite</param>
        /// <param name="path">Full OSC path to entry</param>
        /// <param name="getter">Function which can return the current value for the entry</param>
        /// <param name="description">Optional longer string to use when displaying a label for the entry</param>
        /// <returns></returns>
        public bool AddEndpoint(string path, string oscTypeString, Attributes.AccessValues accessValues, string initialValue = null,
            string description = "")
        {
            if (_rootNode.GetNodeWithPath(path) != null)
            {
                Logger.LogWarning($"Path already exists, skipping: {path}");
                return false;
            }
            
            _rootNode.AddNode(new OSCQueryNode(path)
            {
                Access = accessValues,
                Description = description,
                OscType = oscTypeString,
                Value = initialValue
            });
            
            return true;
        }
        
        public bool AddEndpoint<T>(string path, Attributes.AccessValues accessValues, string initialValue = null, string description = "")
        {
            var oscType = Attributes.OSCTypeFor(typeof(T));
            if (string.IsNullOrWhiteSpace(oscType))
            {
                Logger.LogError($"Could not add {path} to OSCQueryService because type {typeof(T)} is not supported.");
                return false;
            }

            return AddEndpoint(path, oscType, accessValues, initialValue, description);
        }

        /// <summary>
        /// Removes the data for a given OSC path, including its value getter if it has one
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveEndpoint(string path)
        {
            // Exit early if no matching path is found
            if (_rootNode == null || _rootNode.GetNodeWithPath(path) == null)
            {
                Logger.LogError($"No endpoint found for {path}");
                return false;
            }

            _rootNode.RemoveNode(path);

            return true;
        }
        
        /// <summary>
        /// Constructs the response the server will use for HOST_INFO queries
        /// </summary>
        void BuildRootResponse()
        {
            _rootNode = new OSCQueryRootNode()
            {
                Access = Attributes.AccessValues.NoValue,
                Description = "root node"
            };
        }

        public void Dispose()
        {
            _shouldProcessHttp = false;
            
            // HttpListener teardown
            if (_listener != null)
            {
                if (_listener.IsListening)
                    _listener.Stop();
                
                _listener.Close();
            }
            
            // Service Teardown
            _discovery.Dispose();
            _mdns.Stop();
        }

        ~OSCQueryService()
        {
           Dispose();
        }
    }

}