using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HydroEspinaca.Shared.Mongo;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.Database);
    }
}
