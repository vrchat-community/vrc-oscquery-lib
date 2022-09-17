using System.Net;

namespace VRC.OSCQuery
{
    public class OSCServiceProfile
    {
        public int port;
        public string name;
        public IPAddress address;

        public OSCServiceProfile(string name, IPAddress address, int port)
        {
            this.name = name;
            this.address = address;
            this.port = port;
        }
    }

    public class OSCQueryServiceProfile : OSCServiceProfile
    {
        public OSCQueryServiceProfile(string name, IPAddress address, int port) : base(name, address, port)
        {
        }
    }
}