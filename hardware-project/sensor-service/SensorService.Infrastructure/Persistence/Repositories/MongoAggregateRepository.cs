using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Exceptions;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoAggregateRepository : IAggregateRepository
{
    private readonly BaseMongoRepository<Aggregate, AggregateDocument> _baseRepo;

    public MongoAggregateRepository(IConfiguration config, IEntityMapper<Aggregate, AggregateDocument> mapper)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);

        _baseRepo = new BaseMongoRepository<Aggregate, AggregateDocument>(
            db, "aggregates", mapper
        );
    }

    public async Task CreateAsync(Aggregate aggregate)
    {
        await _baseRepo.CreateAsync(aggregate);
    }

    public async Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to)
    {
        var filter = Builders<AggregateDocument>.Filter.And(
            Builders<AggregateDocument>.Filter.Eq(x => x.SensorCode, sensorCode),
            Builders<AggregateDocument>.Filter.Eq(x => x.VariableCode, variableCode),
            Builders<AggregateDocument>.Filter.Gte(x => x.Timestamp, from),
            Builders<AggregateDocument>.Filter.Lte(x => x.Timestamp, to)
        );

        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorCode, string variableCode, DateTime timestamp)
    {
        var filter = Builders<AggregateDocument>.Filter.And(
            Builders<AggregateDocument>.Filter.Eq(x => x.SensorCode, sensorCode),
            Builders<AggregateDocument>.Filter.Eq(x => x.VariableCode, variableCode),
            Builders<AggregateDocument>.Filter.Eq(x => x.Timestamp, timestamp)
        );

        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<Dictionary<string, List<Aggregate>>> GetEnvironmentalAggregatesAsync(DateTime startDate, DateTime endDate, string view)
    {
        var dateFormat = view.ToLower() switch
        {
            "hourly" => "%Y-%m-%dT%H:00:00.000Z", // Hour precision
            "daily" => "%Y-%m-%dT%H:00:00.000Z",  // Hour precision for boxplot calculation
            "weekly" => "%Y-W%V", // ISO week format
            "monthly" => "%Y-%m-01T00:00:00.000Z", // First day of month
            _ => "%Y-%m-%dT00:00:00.000Z"
        };

        var pipeline = new BsonDocument[]
        {
            // Stage 1: Match documents within date range
            new BsonDocument("$match", new BsonDocument
            {
                { "timestamp", new BsonDocument
                    {
                        { "$gte", startDate },
                        { "$lte", endDate }
                    }
                }
            }),
            // Stage 2: Project with truncated timestamp
            new BsonDocument("$project", new BsonDocument
            {
                { "variableCode", 1 },
                { "avg", 1 },
                { "min", 1 },
                { "max", 1 },
                { "count", 1 },
                { "timestamp", 1 },
                { "truncatedDate", new BsonDocument("$dateToString", new BsonDocument
                    {
                        { "format", dateFormat },
                        { "date", "$timestamp" }
                    })
                }
            }),
            // Stage 3: Group by variableCode and truncated date
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "variableCode", "$variableCode" },
                        { "truncatedDate", "$truncatedDate" }
                    }
                },
                { "avg", new BsonDocument("$avg", "$avg") },
                { "min", new BsonDocument("$min", "$min") },
                { "max", new BsonDocument("$max", "$max") },
                { "count", new BsonDocument("$sum", "$count") },
                { "timestamp", new BsonDocument("$first", "$truncatedDate") }
            }),
            // Stage 4: Project to match Aggregate structure
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "sensorCode", "" },
                { "variableCode", "$_id.variableCode" },
                { "avg", 1 },
                { "min", 1 },
                { "max", 1 },
                { "count", 1 },
                { "timestamp", new BsonDocument("$dateFromString", new BsonDocument
                    {
                        { "dateString", "$timestamp" }
                    })
                }
            }),
            // Stage 5: Sort by variableCode and timestamp
            new BsonDocument("$sort", new BsonDocument
            {
                { "variableCode", 1 },
                { "timestamp", 1 }
            })
        };

        var results = await _baseRepo.AggregateAsync(pipeline);

        // Group results by variableCode
        var grouped = results
            .GroupBy(a => a.VariableCode)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.Timestamp).ToList()
            );

        return grouped;
    }
}
