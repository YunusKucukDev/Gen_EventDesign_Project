using MongoDB.Driver;
using Shared.Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Concrete
{
    public class MongoDbService : IMongoDbService
    {
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            IMongoDatabase database = GetDatabase();
            return database.GetCollection<T>(collectionName);
        }

        public IMongoDatabase GetDatabase(string databaseName = "ProductDB", string connectionString = "mongodb://localhost:27017")
        {
            MongoClient client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        }
    }
}
