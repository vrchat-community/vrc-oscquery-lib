using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VRC.OSCQuery
{
    
    public class OSCQueryRootNode : OSCQueryNode
    {
        private Dictionary<string, OSCQueryNode> _pathLookup;

        public OSCQueryRootNode()
        {
            // Initialize path lookup with self reference
            _pathLookup = new Dictionary<string, OSCQueryNode>()
            {
                {"/", this}
            };
            
        }
        public OSCQueryNode GetNodeWithPath(string path)
        {
            if (_pathLookup.TryGetValue(path, out OSCQueryNode node))
            {
                return node;
            }
            
            return null;
        }
        
        public OSCQueryNode AddNode(OSCQueryNode node)
        {
            // Todo: parse path and figure out which sub-node to add it to
            var parent = GetNodeWithPath(node.ParentPath);
            if (parent == null)
            {
                parent = AddNode(new OSCQueryNode(node.ParentPath));
            }
            if (parent.Contents == null)
            {
                parent.Contents = new Dictionary<string, OSCQueryNode>();
            }
            else if (parent.Contents.ContainsKey(node.Name))
            {
                OSCQueryService.Logger.Warn($"Child node {node.Name} already exists on {FullPath}, you need to remove the existing entry first");
                return null;
            }

            // Add to contents
            parent.Contents.Add(node.Name, node);
            
            // Todo: handle case where this full path already exists, but I don't think it should ever happen
            _pathLookup.Add(node.FullPath, node);
            
            return node;
        }

        public bool RemoveNode(string path)
        {
            if(_pathLookup.TryGetValue(path, out OSCQueryNode node))
            {
                var parent = GetNodeWithPath(node.ParentPath);
                if (parent != null && parent.Contents != null)
                {
                    if (parent.Contents.ContainsKey(node.Name))
                    {
                        parent.Contents.Remove(node.Name);
                        return true;
                    }
                }
            }

            return false;
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
        public string Value;

        [JsonIgnore]
        public Func<string> valueGetter;

        [JsonIgnore] 
        public string ParentPath {
            get
            {
                int length = Math.Max(1, FullPath.LastIndexOf("/"));
                return FullPath.Substring(0, length);
            }
            
        }

        [JsonIgnore]
        public string Name => FullPath.Substring(FullPath.LastIndexOf('/')+1);

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