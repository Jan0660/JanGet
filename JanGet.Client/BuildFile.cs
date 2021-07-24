using System.Collections.Generic;
using JanGet.ApiClient;

namespace JanGet.Client
{
    public class BuildFile
    {
        public string Name { get; set; }
        public List<BuildResource> Resources { get; set; }
    }

    public class BuildResource : Resource
    {
        public List<string> Build { get; set; }
    }
}