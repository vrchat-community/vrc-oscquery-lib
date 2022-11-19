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
    }
}
    