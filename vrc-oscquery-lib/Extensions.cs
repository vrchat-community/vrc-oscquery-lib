using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace VRC.OSCQuery
{
    public static class Extensions
    {
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
    }
}