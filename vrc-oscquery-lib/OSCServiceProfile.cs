using System.Net;

namespace VRC.OSCQuery
{
    public class OSCQueryServiceProfile
    {
        public int port;
        public string name;
        public IPAddress address;
        public ServiceType serviceType;

        public enum ServiceType
        {
            OSCQuery, OSC
        }

        public string GetServiceTypeString()
        {
            switch (serviceType)
            {
                case ServiceType.OSC:
                    return Attributes.SERVICE_OSC_UDP;
                case ServiceType.OSCQuery:
                    return Attributes.SERVICE_OSCJSON_TCP;
                default:
                    return "UNKNOWN";
            }
        }

        public OSCQueryServiceProfile(string name, IPAddress address, int port, ServiceType serviceType)
        {
            this.name = name;
            this.address = address;
            this.port = port;
            this.serviceType = serviceType;
        }
    }
}