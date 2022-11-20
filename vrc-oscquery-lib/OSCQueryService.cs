using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VRC.OSCQuery
{
    public class OSCQueryService : IDisposable
    {
        #region Fluent Pattern Implementation

        public OSCQueryService() {} // Need to have this empty constructor for the Builder
        
        public int TcpPort { get; set; } = DefaultPortHttp;

        public int OscPort
        {
            get => HostInfo.oscPort;
            set => HostInfo.oscPort = value;
        }

        public string ServerName {
            get => HostInfo.name;
            set => HostInfo.name = value;
        } 
        
        public IPAddress HostIP { get; set; } = IPAddress.Loopback;
        
        public static ILogger<OSCQueryService> Logger { get; set; } = new NullLogger<OSCQueryService>();

        public HostInfo HostInfo
        {
            get
            {
                if (_hostInfo == null)
                {
                    BuildHostInfo();
                }
                return _hostInfo;
            }
        }

        public OSCQueryRootNode RootNode
        {
            get
            {
                if (_rootNode == null)
                {
                    BuildRootNode();
                }

                return _rootNode;
            }
        }

        private void BuildHostInfo()
        {
            // Create HostInfo object
            _hostInfo = new HostInfo()
            {
                name = DefaultServerName,
                oscPort = DefaultPortOsc,
                oscIP = IPAddress.Loopback.ToString()
            };
        }

        public void AddMiddleware(Func<HttpListenerContext, Action, Task> middleware)
        {
            _http.AddMiddleware(middleware);
        }
        
        public void SetDiscovery(IDiscovery discovery)
        {
            _discovery = discovery;
            _discovery.OnOscQueryServiceAdded += profile => OnOscQueryServiceAdded?.Invoke(profile);
            _discovery.OnOscServiceAdded += profile => OnOscServiceAdded?.Invoke(profile);
        }

        #endregion
        
        // Constants
        public const int DefaultPortHttp = 8080;
        public const int DefaultPortOsc = 9000;
        public const string DefaultServerName = "OSCQueryService";

        // Services
        public static readonly string _localOscUdpServiceName = $"{Attributes.SERVICE_OSC_UDP}.local";
        public static readonly string _localOscJsonServiceName = $"{Attributes.SERVICE_OSCJSON_TCP}.local";
        
        public static readonly HashSet<string> MatchedNames = new HashSet<string>() { 
            _localOscUdpServiceName, _localOscJsonServiceName
        };

        private IDiscovery _discovery;

        #region Wrapped Calls for Discovery Service

        public event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        public event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;
        public HashSet<OSCQueryServiceProfile> GetOSCQueryServices() => _discovery.GetOSCQueryServices();
        public HashSet<OSCQueryServiceProfile> GetOSCServices() => _discovery.GetOSCServices();

        #endregion

        // HTTP Server
        OSCQueryHttpServer _http;

        // Misc
        private OSCQueryRootNode _rootNode;
        private HostInfo _hostInfo;
        
        /// <summary>
        /// Creates an OSCQueryService which can track OSC endpoints in the enclosing program as well as find other OSCQuery-compatible services on the link-local network
        /// </summary>
        /// <param name="serverName">Server name to use, default is "OSCQueryService"</param>
        /// <param name="httpPort">TCP port on which to serve OSCQuery info, default is 8080</param>
        /// <param name="oscPort">UDP Port at which the OSC Server can be reached, default is 9000</param>
        /// <param name="logger">Optional logger which will be used for logs generated within this class. Will log to Null if not set.</param>
        /// <param name="middleware">Optional set of middleware to be injected into the HTTP server. Middleware will be executed in the order they are passed in.</param>
        [Obsolete("Use the Fluent Interface so we can remove this constructor", false)]
        public OSCQueryService(string serverName = DefaultServerName, int httpPort = DefaultPortHttp, int oscPort = DefaultPortOsc, ILogger<OSCQueryService> logger = null, params Func<HttpListenerContext, Action, Task>[] middleware)
        {
            if (logger != null) Logger = logger;

            OscPort = oscPort;
            TcpPort = httpPort;
            
            Initialize(serverName);
            StartOSCQueryService(serverName, httpPort, middleware);
            if (oscPort != DefaultPortOsc)
            {
                AdvertiseOSCService(serverName, oscPort);
            }
            RefreshServices();
        }

        [Obsolete("Use the Fluent Interface so we can remove this function", false)]
        public void Initialize(string serverName = DefaultServerName)
        {
            ServerName = serverName;
            SetDiscovery(new MeaModDiscovery(Logger));
        }

        [Obsolete("Use the Fluent Interface instead of this combo function", false)]
        public void StartOSCQueryService(string serverName, int httpPort = -1, params Func<HttpListenerContext, Action, Task>[] middleware)
        {
            ServerName = serverName;
            
            // Use the provided port or grab a new one
            TcpPort = httpPort == -1 ? Extensions.GetAvailableTcpPort() : httpPort;

            // Add all provided middleware
            if (middleware != null)
            {
                foreach (var newMiddleware in middleware)
                {
                    AddMiddleware(newMiddleware);
                }
            }
            
            AdvertiseOSCQueryService(serverName, TcpPort);
            StartHttpServer();
        }

        public void StartHttpServer()
        {
            _http = new OSCQueryHttpServer(this, Logger);
        }
        
        public void AdvertiseOSCQueryService(string serviceName, int port = DefaultPortHttp)
        {
            _discovery.Advertise(new OSCQueryServiceProfile(serviceName, HostIP, port, OSCQueryServiceProfile.ServiceType.OSCQuery));
        }

        public void AdvertiseOSCService(string serviceName, int port = DefaultPortOsc)
        {
            _discovery.Advertise(new OSCQueryServiceProfile(serviceName, HostIP, port, OSCQueryServiceProfile.ServiceType.OSC));
        }

        public void RefreshServices()
        {
            _discovery.RefreshServices();
        }
        
        public void SetValue(string address, string value)
        {
            var target = RootNode.GetNodeWithPath(address);
            if (target == null)
            {
                // add this node
                target = RootNode.AddNode(new OSCQueryNode(address));
            }
            target.Value = value;
        }

        /// <summary>
        /// Registers the info for an OSC path.
        /// </summary>
        /// <param name="path">Full OSC path to entry</param>
        /// <param name="oscTypeString">String representation of OSC type(s)</param>
        /// <param name="accessValues">Enum - 0: NoValue, 1: ReadOnly 2:WriteOnly 3:ReadWrite</param>
        /// <param name="initialValue">Starting value for param in string form</param>
        /// <param name="description">Optional longer string to use when displaying a label for the entry</param>
        /// <returns></returns>
        public bool AddEndpoint(string path, string oscTypeString, Attributes.AccessValues accessValues, string initialValue = null,
            string description = "")
        {
            // Exit early if path does not start with slash
            if (!path.StartsWith("/"))
            {
                Logger.LogError($"An OSC path must start with a '/', your path {path} does not.");
                return false;
            }
            
            if (RootNode.GetNodeWithPath(path) != null)
            {
                Logger.LogWarning($"Path already exists, skipping: {path}");
                return false;
            }
            
            RootNode.AddNode(new OSCQueryNode(path)
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
            var typeExists = Attributes.OSCTypeFor(typeof(T), out string oscType);

            if (typeExists) return AddEndpoint(path, oscType, accessValues, initialValue, description);
            
            Logger.LogError($"Could not add {path} to OSCQueryService because type {typeof(T)} is not supported.");
            return false;
        }

        /// <summary>
        /// Removes the data for a given OSC path, including its value getter if it has one
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveEndpoint(string path)
        {
            // Exit early if no matching path is found
            if (RootNode.GetNodeWithPath(path) == null)
            {
                Logger.LogWarning($"No endpoint found for {path}");
                return false;
            }

            RootNode.RemoveNode(path);

            return true;
        }
        
        /// <summary>
        /// Constructs the response the server will use for HOST_INFO queries
        /// </summary>
        private void BuildRootNode()
        {
            _rootNode = new OSCQueryRootNode()
            {
                Access = Attributes.AccessValues.NoValue,
                Description = "root node",
                FullPath = "/",
            };
        }

        public void Dispose()
        {
            _http?.Dispose();
            _discovery?.Dispose();

            GC.SuppressFinalize(this);
        }

        ~OSCQueryService()
        {
           Dispose();
        }
    }

}