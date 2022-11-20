using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class OSCQueryServiceBuilder
    {
        private OSCQueryService _service = new OSCQueryService();
        public OSCQueryService Build() => _service;
        
        public OSCQueryServiceBuilder WithTcpPort(int port)
        {
            //set tcp port for service
            _service.TcpPort = port;
            return this;
        }
        
        public OSCQueryServiceBuilder WithOscPort(int port)
        {
            //set osc port for service
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
            _service.Initialize();
            return this;
        }

        public OSCQueryServiceBuilder WithMiddleware(Func<HttpListenerContext, Action, Task> middleware)
        {
            _service.AddMiddleware(middleware);
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSC(string serviceName, int port = -1)
        {
            _service.AdvertiseOSCService(serviceName, port);
            return this;
        }

        public OSCQueryServiceBuilder AdvertiseOSCQuery(string serviceName, int port = -1)
        {
            _service.AdvertiseOSCQueryService(serviceName, port);
            return this;
        }
    }
}
    