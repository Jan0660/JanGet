using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace JanGet.ApiClient
{
    [BsonIgnoreExtraElements]
    public class Package
    {
        public string Name { get; set; }
        public List<Resource> Resources { get; set; }

        public Package()
        {
        }

        public Package(string name) => (Name, Resources) = (name, new());
    }

    [BsonIgnoreExtraElements]
    public class Resource
    {
        public string Platform { get; set; }
        public string Type { get; set; }
        public string GitUrl { get; set; }
        public string File { get; set; }
        public string SigFile { get; set; }
    }
}