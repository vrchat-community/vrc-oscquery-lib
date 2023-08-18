using System;
using System.Net;

namespace VRC.OSCQuery
{
    public class OSCQueryServiceProfile : IEquatable<OSCQueryServiceProfile>
    {
        public int port;
        public string name;
        public IPAddress address;
        public ServiceType serviceType;

        public enum ServiceType
        {
            Unknown, OSCQuery, OSC
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

        public bool Equals(OSCQueryServiceProfile other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return port == other.port && name == other.name && Equals(address, other.address) && serviceType == other.serviceType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OSCQueryServiceProfile)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = port;
                hashCode = (hashCode * 397) ^ (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (address != null ? address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)serviceType;
                return hashCode;
            }
        }
    }
}