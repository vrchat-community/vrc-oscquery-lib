using System.Collections.Generic;
using Newtonsoft.Json;

namespace VRC.OSCQuery
{
    public class HostInfo
    {
        [JsonProperty(Keys.NAME)]
        public string name;

        [JsonProperty(Keys.EXTENSIONS)] public Dictionary<string, bool> extensions = new Dictionary<string, bool>()
        {
            { Attributes.ACCESS, true },
            { Attributes.CLIPMODE, false },
            { Attributes.RANGE, true },
            { Attributes.TYPE, true },
            { Attributes.VALUE, true },
        };
        
        [JsonProperty(Keys.OSC_IP)]
        public string oscIP;
        
        [JsonProperty(Keys.OSC_PORT)]
        public int oscPort = OSCQueryService.DefaultPortOsc;

        [JsonProperty(Keys.OSC_TRANSPORT)] 
        public string oscTransport = Keys.OSC_TRANSPORT_UDP;

        /// <summary>
        /// Empty Constructor required for JSON Serialization
        /// </summary>
        public HostInfo()
        {
            
        }

        public override string ToString()
        {
            var result = JsonConvert.SerializeObject(this);
            return result;
        }

        public class Keys
        {
            public const string NAME = "NAME";
            public const string EXTENSIONS = "EXTENSIONS";
            public const string OSC_IP = "OSC_IP";
            public const string OSC_PORT = "OSC_PORT";
            public const string OSC_TRANSPORT = "OSC_TRANSPORT";
            public const string OSC_TRANSPORT_UDP = "UDP";
        }
    }
}