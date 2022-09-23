using System.Net.Sockets;
using OscCore;

namespace VRC.OSCQuery.Samples.Chatbox
{
    public class OscClientPlus : OscClient
    {
        /// <summary>Send a message with a string and a bool</summary>
        public void Send(string address, string message, bool value)
        {
            string boolTag = value ? "T" : "F";
            m_Writer.Reset();
            m_Writer.Write(address);
            string typeTags = $",s{boolTag}";
            m_Writer.Write(typeTags);
            m_Writer.Write(message);
            m_Socket.Send(m_Writer.Buffer, m_Writer.Length, SocketFlags.None);
        }
        
        public OscClientPlus(string ipAddress, int port) : base(ipAddress, port)
        {
        }
    }
}