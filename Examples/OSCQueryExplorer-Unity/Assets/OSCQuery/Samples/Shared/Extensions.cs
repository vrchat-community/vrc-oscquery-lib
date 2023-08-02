using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace VRC.OSCQuery.Samples.Shared
{
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }
        
        public static IPAddress GetLocalIPAddress()
        {
            // Android can always serve on the non-loopback address
#if UNITY_ANDROID
            return GetLocalIPAddressNonLoopback();
#else
            // Windows can only serve TCP on the loopback address, but can serve UDP on the non-loopback address
            return IPAddress.Loopback;
#endif
        }
        
        public static IPAddress GetLocalIPAddressNonLoopback()
        {
            // Get the host name of the local machine
            string hostName = Dns.GetHostName();

            // Get the IP address of the first IPv4 network interface found on the local machine
            foreach (IPAddress ip in Dns.GetHostEntry(hostName).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return null;
        }

    }
}