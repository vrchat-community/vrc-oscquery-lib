using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VRC.OSCQuery
{
    public static class Extensions
    {
        private static readonly HttpClient _client = new HttpClient();
        
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
        {
            var queue = new Queue<T>();

            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (queue.Count == count)
                    {
                        do
                        {
                            yield return queue.Dequeue();
                            queue.Enqueue(e.Current);
                        } while (e.MoveNext());
                    }
                    else
                    {
                        queue.Enqueue(e.Current);
                    }
                }
            }
        }
    
        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
        
        public static int GetAvailableTcpPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
        
        public static int GetAvailableUdpPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

        public static async Task<OSCQueryRootNode> GetOSCTree(IPAddress ip, int port)
        {
            var response = await _client.GetAsync($"http://{ip}:{port}/");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var oscTreeString = await response.Content.ReadAsStringAsync();
            var oscTree = OSCQueryRootNode.FromString(oscTreeString);
            
            return oscTree;
        }

        public static async Task<HostInfo> GetHostInfo(IPAddress address, int port)
        {
            var response = await _client.GetAsync($"http://{address}:{port}?{Attributes.HOST_INFO}");
            var hostInfoString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HostInfo>(hostInfoString);
        }
        
        public static async Task ServeStaticFile(string path, string mimeType, HttpListenerContext context)
        {
            using (var targetFile = File.OpenRead(path))
            {
                context.Response.ContentType =mimeType;
                context.Response.StatusCode = 200;
                context.Response.ContentLength64 = targetFile.Length;
                await targetFile.CopyToAsync(context.Response.OutputStream);
                await context.Response.OutputStream.FlushAsync();
            }
        }
    }
}