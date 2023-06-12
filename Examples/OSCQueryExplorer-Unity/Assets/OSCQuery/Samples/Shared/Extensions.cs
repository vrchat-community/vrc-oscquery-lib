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
#if UNITY_ANDROID
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
#else
            return IPAddress.Loopback;
#endif
        }

    }
}