using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace VRC.OSCQuery
{
    public class OSCQueryService : IDisposable
    {
        // Constants
        public const int DefaultPortHttp = 8080;
        public const int DefaultPortOsc = 9000;
        public const string DefaultServerName = "OSCQueryService";

        // Services
        private readonly string _localOscUdpServiceName = $"{Attributes.SERVICE_OSC_UDP}.local";
        private readonly string _localOscJsonServiceName = $"{Attributes.SERVICE_OSCJSON_TCP}.local";

        // Zeroconf
        private ServiceProfile _zeroconfService;
        private ServiceProfile _oscService;
        private MulticastService _mdns;
        private ServiceDiscovery _discovery;
        public event Action<ServiceProfile> OnProfileAdded;

        // Store discovered services
        private HashSet<ServiceProfile> _oscQueryServices = new HashSet<ServiceProfile>();
        private HashSet<ServiceProfile> _oscServices = new HashSet<ServiceProfile>();

        // HTTP Server
        HttpListener _listener;
        private bool _shouldProcessHttp;
        
        // Misc
        private OSCQueryRootNode _rootNode;
        private JObject _rootObject;
        private HostInfo _hostInfo;
        public static ILogger Logger;
        private readonly HashSet<string> _matchedNames;

        /// <summary>
        /// Creates an OSCQueryService which can track OSC endpoints in the enclosing program as well as find other OSCQuery-compatible services on the link-local network
        /// </summary>
        /// <param name="serverName">Server name to use, default is "OSCQueryService"</param>
        /// <param name="httpPort">TCP port on which to serve OSCQuery info, default is 8080</param>
        /// <param name="oscPort">UDP Port at which the OSC Server can be reached, default is 9000</param>
        /// <param name="logger">Optional logger which will be used for logs generated within this class. Will log to Null if not set.</param>
        public OSCQueryService(string serverName = DefaultServerName, int httpPort = DefaultPortHttp, int oscPort = DefaultPortOsc, ILogger logger = null)
        {
            // Construct hashset for services to track
            _matchedNames = new HashSet<string>() { 
                _localOscUdpServiceName, _localOscJsonServiceName
            };
            
            // Set up logging
            OSCQueryService.Logger = logger ?? NullLogger.Instance;
            
            try
            {
                // Create HostInfo object
                _hostInfo = new HostInfo()
                {
                    name = serverName,
                    oscPort = oscPort,
                };
                
                // Set up and Advertise OSC and ZeroConf profiles
                _oscService = new ServiceProfile(serverName, Attributes.SERVICE_OSC_UDP, (ushort)oscPort);
                _zeroconfService = new ServiceProfile(serverName, Attributes.SERVICE_OSCJSON_TCP, (ushort)httpPort);

                _mdns = new MulticastService();
                _discovery = new ServiceDiscovery(_mdns);
                
                _discovery.Advertise(_oscService);
                _discovery.Advertise(_zeroconfService);
                
                // Query for OSC and OSCQuery services on every network interface
                _mdns.NetworkInterfaceDiscovered += (s, e) =>
                {
                    _mdns.SendQuery(_localOscUdpServiceName);
                    _mdns.SendQuery(_localOscJsonServiceName);
                };
                // Callback invoked when the above query is answered
                _mdns.AnswerReceived += OnRemoteServiceInfo;

                _mdns.Start();
                
                // Create and start HTTPListener
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{httpPort}/");
                _listener.Start();
                Task.Run(() => HttpListenerLoop());
                _shouldProcessHttp = true;
            }
            catch (Exception e)
            {
                _shouldProcessHttp = false;
                Logger.LogError($"Could not start OSCQuery service: {e.Message}");
            }

            BuildRootResponse();
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
                // Doing lots of LINQ-y stuff here and catching exceptions below for unmatched services. Open to other ideas!
                var name = response.Answers.First(r => _matchedNames.Contains(r?.CanonicalName)).CanonicalName;
                var srvRecord = response.AdditionalRecords.OfType<SRVRecord>().First();
                var port = srvRecord.Port;
                var domainName = srvRecord.Name.Labels;
                var instanceName = domainName[0];
                var serviceName = string.Join(".", domainName.Skip(1).SkipLast(1));
                var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
                var profile = new ServiceProfile(instanceName, serviceName, srvRecord.Port, ips);

                // If this is an OSC service, add it to the OSC collection
                if (name.CompareTo(_localOscUdpServiceName) == 0 && profile != _oscService)
                {
                    if (!_oscServices.Any(p => p.FullyQualifiedName == profile.FullyQualifiedName))
                    {
                        _oscServices.Add(profile);
                        OnProfileAdded?.Invoke(profile);
                        Logger.LogInformation($"Found match {name} on port {port}");
                    }
                }
                // If this is an OSCQuery service, add it to the OSCQuery collection
                else if (name.CompareTo(_localOscJsonServiceName) == 0 && profile.FullyQualifiedName != _zeroconfService.FullyQualifiedName)
                {
                    if (!_oscQueryServices.Any(p => p.FullyQualifiedName == profile.FullyQualifiedName))
                    {
                        _oscQueryServices.Add(profile);
                        OnProfileAdded?.Invoke(profile);
                        Logger.LogInformation($"Found match {name} on port {port}");
                    }
                }
            }
            catch (Exception e)
            {
                // Using a non-error log level because we may have just found a non-matching service
                Logger.LogInformation($"Could not parse answer from {eventArgs.RemoteEndPoint}: {e.Message}");
            }
        }

        public HashSet<ServiceProfile> GetOSCQueryServices() => _oscQueryServices;
        public HashSet<ServiceProfile> GetOSCServices() => _oscServices;

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
                    return;
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
                        return;
                    }
                    
                    matchedNode.RefreshValue();
                    
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

        /// <summary>
        /// Registers the info for an OSC path.
        /// </summary>
        /// <param name="name">String used for building JSON tree</param>
        /// <param name="accessValues">Enum - 0: NoValue, 1: ReadOnly 2:WriteOnly 3:ReadWrite</param>
        /// <param name="path">Full OSC path to entry</param>
        /// <param name="getter">Function which can return the current value for the entry</param>
        /// <param name="description">Optional longer string to use when displaying a label for the entry</param>
        /// <typeparam name="T">The System.Type for the entry, will be converted to OSCType</typeparam>
        /// <returns></returns>
        public bool AddEndpoint<T>(string path, Attributes.AccessValues accessValues, Func<string> getter = null, string description = "")
        {
            var oscType = Attributes.OSCTypeFor(typeof(T));
            if (string.IsNullOrWhiteSpace(oscType))
            {
                Logger.LogError($"Could not add {path} to OSCQueryService because type {typeof(T)} is not supported.");
                return false;
            }
            
            _rootNode.AddNode(new OSCQueryNode(path)
            {
                Access = accessValues,
                Description = description,
                OscType = oscType,
                valueGetter = getter
            });
            
            return true;
        }

        /// <summary>
        /// Removes the data for a given OSC path, including its value getter if it has one
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveEndpoint(string path)
        {

            var node = _rootNode.GetNodeWithPath(path);
            // Exit early if no matching path is found
            if (node == null)
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