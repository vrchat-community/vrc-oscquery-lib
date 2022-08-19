using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VRC.OSCQuery
{
    
    public class OSCQueryRootNode : OSCQueryNode
    {
        private Dictionary<string, OSCQueryNode> _pathLookup;

        public OSCQueryRootNode()
        {
            // Initialize path lookup with self reference
            _pathLookup = new()
            {
                {"/", this}
            };
            
        }
        public OSCQueryNode? GetNodeWithPath(string path)
        {
            if (_pathLookup.TryGetValue(path, out OSCQueryNode node))
            {
                return node;
            }
            
            return null;
        }
        
        public OSCQueryNode AddNode(string name, OSCQueryNode node)
        {
            if (Contents == null)
            {
                Contents = new Dictionary<string, OSCQueryNode>();
            }
            else if (Contents.ContainsKey(name))
            {
                OSCQueryService.Logger.LogWarning($"Child node {name} already exists on {FullPath}, you need to remove the existing entry first");
                return null;
            }

            // Add to contents
            Contents.Add(name, node);
            
            // Todo: handle case where this full path already exists
            _pathLookup.Add(node.FullPath, node);
            
            return node;
        }
    }
    public class OSCQueryNode
    {
        // Empty Constructor for Json Serialization
        public OSCQueryNode(){}

        public OSCQueryNode(string fullPath)
        {
            FullPath = fullPath;
        }
        
        [JsonProperty(Attributes.DESCRIPTION)]
        public string Description;

        [JsonProperty(Attributes.FULL_PATH)] public string FullPath;

        [JsonProperty(Attributes.ACCESS)]
        public Attributes.AccessValues Access;

        [JsonProperty(Attributes.CONTENTS)]
        public Dictionary<string, OSCQueryNode> Contents;

        [JsonProperty(Attributes.TYPE)]
        public string OscType;

        [JsonProperty(Attributes.VALUE)] 
        public string? Value;

        [JsonIgnore]
        public Func<string?>? valueGetter;

        public void RefreshValue(HashSet<OSCQueryNode> visited = null)
        {
            visited = visited ?? new HashSet<OSCQueryNode>();
            if (Contents != null)
            {
                foreach (OSCQueryNode node in Contents.Values)
                {
                    if (!visited.Contains(node))
                    {
                        node.RefreshValue(visited);
                        visited.Add(node);
                    }
                }
            }
            Value = valueGetter?.Invoke();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}