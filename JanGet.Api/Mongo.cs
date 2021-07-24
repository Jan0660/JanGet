using System.Collections.Generic;
using System.Threading.Tasks;
using JanGet.ApiClient;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace JanGet.Api
{
    public static class Mongo
    {
        public static IMongoCollection<Package> PackageCollection;
        public static IMongoCollection<File> FileCollection;
        public static MongoClient Client;
        public static IMongoDatabase Database;

        public static void Connect()
        {
            Client = new(Program.Config.MongoUrl);
            Database = Client.GetDatabase(Program.Config.DatabaseName);
            PackageCollection = Database.GetCollection<Package>("packages");
            FileCollection = Database.GetCollection<File>("files");
        }

        public static async Task UpdatePackageAsync(Package pkg)
        {
            if (await Mongo.PackageCollection.CountDocumentsAsync(p => p.Name == pkg.Name) == 0)
                await Mongo.PackageCollection.InsertOneAsync(pkg);
            else
                await Mongo.PackageCollection.ReplaceOneAsync(p => p.Name == pkg.Name, pkg);
        }
    }
}