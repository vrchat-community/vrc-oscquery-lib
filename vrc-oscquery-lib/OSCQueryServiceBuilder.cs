using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class OSCQueryServiceBuilder
    {
        private readonly OSCQueryService _service = new OSCQueryService();
        public OSCQueryService Build()
        {
            if (!_customStartup)
            {
                WithDefaults();
            }
            return _service;
        }

        // flag to know whether the user has set something custom
        private bool _customStartup = false;

        /// <summary>
        /// Starts HTTP Server, Advertises OSCQuery & OSC, Uses default library for Network Discovery
        /// </summary>
        /// <returns>OSCQueryServiceBuilder for Fluent construction</returns>
        public OSCQueryServiceBuilder WithDefaults()
        {
            _customStartup = true;
            StartHttpServer();
            WithDiscovery(new MeaModDiscovery());
            AdvertiseOSCQuery();
            AdvertiseOSC();
            return this;
        }
        
        public OSCQueryServiceBuilder WithTcpPort(int port)
        {
            _customStartup = true;
            _service.TcpPort = port;
            return this;
        }
        
        public OSCQueryServiceBuilder WithUdpPort(int port)
        {
            _customStartup = true;
            _service.OscPort = port;
            return this;
        }

        public OSCQueryServiceBuilder WithHostIP(IPAddress address)
        {
            _customStartup = true;
            _service.HostIP = address;
            
            // Set the OSC IP to the host IP if it's not already set
            if(Equals(_service.OscIP, IPAddress.Loopback))
                _service.OscIP = address;
            
            return this;
        }
        
        public OSCQueryServiceBuilder WithOscIP(IPAddress address)
        {
            _customStartup = true;
            _service.OscIP = address;
            return this;
        }

        public OSCQueryServiceBuilder StartHttpServer()
        {
            _customStartup = true;
            _service.StartHttpServer();
            return this;
        }

        public OSCQueryServiceBuilder WithServiceName(string name)
        {
            _customStartup = true;
            _service.ServerName = name;
            return this;
        }

        public OSCQueryServiceBuilder WithLogger(ILogger<OSCQueryService> logger)
        {
            _customStartup = true;
            OSCQueryService.Logger = logger;
            return this;
        }

        public OSCQueryServiceBuilder WithMiddleware(Func<HttpListenerContext, Action, Task> middleware)
        {
            _customStartup = true;
            _service.AddMiddleware(middleware);
            return this;
        }

        public OSCQueryServiceBuilder WithDiscovery(IDiscovery d)
        {
            _customStartup = true;
            _service.SetDiscovery(d);
            return this;
        }

        public OSCQueryServiceBuilder AddListenerForServiceType(Action<OSCQueryServiceProfile> listener, OSCQueryServiceProfile.ServiceType type)
        {
            _customStartup = true;
            switch (type)
            {
                case OSCQueryServiceProfile.ServiceType.OSC:
                    _service.OnOscServiceAdded += listener;
                    break;
                case OSCQueryServiceProfile.ServiceType.OSCQuery:
                    _service.OnOscQueryServiceAdded += listener;
                    break;
            }
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSC()
        {
            _customStartup = true;
            _service.AdvertiseOSCService(_service.ServerName, _service.OscPort);
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSCQuery()
        {
            _customStartup = true;
            _service.AdvertiseOSCQueryService(_service.ServerName, _service.TcpPort);
            return this;
        }
    }
}