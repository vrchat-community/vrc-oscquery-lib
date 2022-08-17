using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Makaretu.Dns;
using Newtonsoft.Json.Linq;

namespace VRC.OSCQuery
{
    public class OSCQueryService : IDisposable
    {
        private const int DefaultPortHttp = 8080;
        private const int DefaultPortOsc = 9000;
        private const string DefaultServerName = "OSCQueryService";

        public int httpPort, oscPort;
        public string serverName;
        
        HttpListener _listener;

        private HostInfo _hostInfo;
        private ServiceProfile _zeroconfService;
        private ServiceProfile _oscService;
        private MulticastService _mdns;
        private ServiceDiscovery _discovery;

        private HashSet<ServiceProfile> _oscQueryServices = new();
        private HashSet<ServiceProfile> _oscServices = new();

        // Track getters and setters to respond to incoming value queries / updates
        private Dictionary<string, Func<dynamic>> _getters = new();
        private Dictionary<string, JObject> _oscPaths = new();

        private string _localOscUdpServiceName = $"{Attributes.SERVICE_OSC_UDP}.local";
        private string _localOscJsonServiceName = $"{Attributes.SERVICE_OSCJSON_TCP}.local";

        public OSCQueryService(string serverName = DefaultServerName, int httpPort = DefaultPortHttp, int oscPort = DefaultPortOsc)
        {
            try
            {
                this.serverName = serverName;
                this.httpPort = httpPort;
                this.oscPort = oscPort;

                _hostInfo = new HostInfo()
                {
                    name = serverName,
                    oscPort = oscPort,
                };
                
                _oscService = new ServiceProfile(serverName, Attributes.SERVICE_OSC_UDP, (ushort)oscPort);
                _zeroconfService = new ServiceProfile(serverName, Attributes.SERVICE_OSCJSON_TCP, (ushort)httpPort);

                _mdns = new MulticastService();
                _discovery = new ServiceDiscovery(_mdns);
                
                _discovery.Advertise(_oscService);
                _discovery.Advertise(_zeroconfService);
                
                _mdns.NetworkInterfaceDiscovered += (s, e) =>
                {
                    _mdns.SendQuery(_localOscUdpServiceName);
                    _mdns.SendQuery(_localOscJsonServiceName);
                };
                
                _mdns.AnswerReceived += MdnsOnAnswerReceived;

                _mdns.Start();
                
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{httpPort}/");
                _listener.Start();

                // RefreshOtherServices();
                
                Task.Run(() => HttpListenerLoop());
                _dorunrun = true;
            }
            catch (Exception e)
            {
                _dorunrun = false;
                Console.Error.Write(e.Message);
                // throw;
            }

            BuildRootResponse();
        }

        private void MdnsOnAnswerReceived(object? sender, MessageEventArgs eventArgs)
        {
            var matchedNames = new HashSet<string>()
            {
                _localOscUdpServiceName, _localOscJsonServiceName
            };

            var response = eventArgs.Message;
            
            try
            {
                var name = response.Answers.First(r => matchedNames.Contains(r?.CanonicalName)).CanonicalName;
                var srvRecord = response.AdditionalRecords.OfType<SRVRecord>().First();
                var port = srvRecord.Port;
                var domainName = srvRecord.Name.Labels;
                var instanceName = domainName[0];
                var serviceName = string.Join(".", domainName.Skip(1).SkipLast(1));
                var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
                var profile = new ServiceProfile(instanceName, serviceName, srvRecord.Port, ips);

                if (!_oscServices.Any(p => p.FullyQualifiedName == profile.FullyQualifiedName))
                {
                    _oscServices.Add(profile);
                    OnProfileAdded?.Invoke(profile);
                    Trace.WriteLine($"Found match {name} on port {port}");
                }
                
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception parsing answer for {response.Answers.First().Name}: {e.Message}");
            }
        }

        public event Action<ServiceProfile> OnProfileAdded;

        private bool _dorunrun = true;
        
        private async Task HttpListenerLoop()
        {
            while (_dorunrun)
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
                        Console.Error.WriteLine(e);
                    }
                }
                else
                {
                    var path = context.Request.Url.LocalPath;
                    if (_oscPaths.TryGetValue(path, out JObject match))
                    {
                        if(match.ContainsKey(Attributes.VALUE))
                        {
                            match[Attributes.VALUE] = GetValueFor(path);
                        }
                        var stringResponse = match.ToString();
                
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
                    else
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
                }
            }
        }

        public void AddEndpoint<T>(string name, Attributes.AccessValues accessValues, string path, Func<dynamic> getter = null!, string description = "")
        {
            var oscType = Attributes.OSCTypeFor(typeof(T));
            if (string.IsNullOrWhiteSpace(oscType))
            {
                Console.Error.WriteLine($"Could not add {name} to OSCQueryService because type {typeof(T)} is not supported.");
                return;
            }

            var jObject = new JObject() // need to check for existing or add differently
            {
                { Attributes.DESCRIPTION, string.IsNullOrWhiteSpace(description) ? name : description },
                { Attributes.FULL_PATH, path },
                { Attributes.ACCESS, (int)accessValues},
                { Attributes.TYPE, oscType}, // NEED TO HANDLE DIFFERENT TYPES
                { Attributes.VALUE, 0},
            };
            
            _oscPaths.Add(path, jObject);
            
            // Add to root object so it can be returned for queries
            ((JObject)_rootObject[Attributes.CONTENTS]).Add(name, jObject);

            if (getter != null)
            {
                _getters.Add(path, getter);
            }
        }

        public dynamic GetValueFor(string name)
        {
            if (_getters.TryGetValue(name, out var getter))
            {
                return getter.Invoke();
            }

            return null;
        }

        private JObject _rootObject;

        void BuildRootResponse()
        {
            _rootObject = new JObject()
            {
                { Attributes.ACCESS, (int)Attributes.AccessValues.NoValue },
                { Attributes.FULL_PATH, "/" },
                { Attributes.CONTENTS, new JObject()}
            };
            
            _oscPaths.Add("/", _rootObject);
        }

        public void Dispose()
        {
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