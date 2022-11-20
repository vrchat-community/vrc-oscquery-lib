using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class OSCQueryServiceBuilder
    {
        private readonly OSCQueryService _service = new OSCQueryService();
        public OSCQueryService Build() => _service;
        
        public OSCQueryServiceBuilder WithTcpPort(int port)
        {
            //set tcp port for service
            _service.TcpPort = port;
            return this;
        }
        
        public OSCQueryServiceBuilder WithOscPort(int port)
        {
            _service.OscPort = port;
            return this;
        }

        public OSCQueryServiceBuilder WithHostIP(IPAddress address)
        {
            _service.HostIP = address;
            return this;
        }

        public OSCQueryServiceBuilder StartHttpServer()
        {
            _service.StartHttpServer();
            return this;
        }

        public OSCQueryServiceBuilder WithServiceName(string name)
        {
            _service.ServerName = name;
            return this;
        }

        public OSCQueryServiceBuilder WithLogger(ILogger<OSCQueryService> logger)
        {
            OSCQueryService.Logger = logger;
            return this;
        }

        public OSCQueryServiceBuilder WithMiddleware(Func<HttpListenerContext, Action, Task> middleware)
        {
            _service.AddMiddleware(middleware);
            return this;
        }

        public OSCQueryServiceBuilder WithDiscovery(IDiscovery d)
        {
            _service.SetDiscovery(d);
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSC()
        {
            _service.AdvertiseOSCService(_service.ServerName, _service.OscPort);
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSCQuery()
        {
            _service.AdvertiseOSCQueryService(_service.ServerName, _service.TcpPort);
            return this;
        }
    }
}
    