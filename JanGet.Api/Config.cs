namespace JanGet.Api
{
    public class Config
    {
        public string MongoUrl { get; set; }
        public string DatabaseName { get; set; } = "janget";
        public string MasterToken { get; set; }
        public string ArchDatabaseName { get; set; }
    }
}