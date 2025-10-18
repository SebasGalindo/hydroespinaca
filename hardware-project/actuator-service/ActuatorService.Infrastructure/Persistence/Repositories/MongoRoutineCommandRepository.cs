using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoRoutineCommandRepository : IRoutineCommandRepository
{
    private readonly BaseMongoRepository<RoutineCommand, RoutineCommandDocument> _baseRepo;
    private readonly IMongoCollection<RoutineCommandDocument> _collection;

    public MongoRoutineCommandRepository(MongoDbContext ctx,
                                         IEntityMapper<RoutineCommand, RoutineCommandDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<RoutineCommand, RoutineCommandDocument>(ctx.Database, "routine_commands", mapper);
        _collection = ctx.Database.GetCollection<RoutineCommandDocument>("routine_commands");
    }

    public async Task<RoutineCommand?> GetByCommandIdAsync(string commandId)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.CommandId, commandId);
        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<RoutineCommand?> GetRunningByActuatorCodeAsync(string actuatorCode)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.And(
            Builders<RoutineCommandDocument>.Filter.Eq(x => x.ActuatorCode, actuatorCode),
            Builders<RoutineCommandDocument>.Filter.Eq(x => x.StatusGeneral, HydroEspinaca.Shared.Enums.RoutineCommandStatus.RUNNING)
        );
        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<List<RoutineCommand>> GetByActuatorCodeAsync(string actuatorCode)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.ActuatorCode, actuatorCode);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<List<RoutineCommand>> GetAllAsync()
    {
        return await _baseRepo.FindManyAsync(FilterDefinition<RoutineCommandDocument>.Empty);
    }

    public async Task<List<RoutineCommand>> GetByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task AddAsync(RoutineCommand routineCommand)
        => _baseRepo.CreateAsync(routineCommand);

    public Task UpdateAsync(RoutineCommand routineCommand)
        => _baseRepo.UpdateAsync(routineCommand);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);

    public async Task<long> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Lt(x => x.CreatedAt, cutoffDate);
        var result = await _baseRepo.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<ActuatorAnalyticsData> GetActuatorAnalyticsAsync(DateTime startDate, DateTime endDate, string view)
    {
        // 1. Get Timeline data
        var timelineData = await GetTimelineDataAsync(startDate, endDate, view);

        // 2. Get Total Duration by Actuator
        var totalDurationData = await GetTotalDurationByActuatorAsync(startDate, endDate);

        // 3. Get Active Time Proportion
        var activeTimeProportionData = CalculateActiveTimeProportion(totalDurationData);

        return new ActuatorAnalyticsData
        {
            Timeline = timelineData,
            TotalDurationByActuator = totalDurationData,
            ActiveTimeProportion = activeTimeProportionData
        };
    }

    private async Task<List<TimelineData>> GetTimelineDataAsync(DateTime startDate, DateTime endDate, string view)
    {
        var dateFormat = view.ToLower() switch
        {
            "hourly" => "%Y-%m-%dT%H:00:00.000Z",
            "daily" => "%Y-%m-%dT00:00:00.000Z",
            "weekly" => "%Y-W%V",
            "monthly" => "%Y-%m-01T00:00:00.000Z",
            _ => "%Y-%m-%dT00:00:00.000Z"
        };

        var pipeline = new BsonDocument[]
        {
            // Stage 1: Match documents within date range and with FINISHED status
            // Use FinishedAt instead of CreatedAt to filter by completion date
            new BsonDocument("$match", new BsonDocument
            {
                { "FinishedAt", new BsonDocument
                    {
                        { "$gte", startDate },
                        { "$lte", endDate }
                    }
                },
                { "StatusGeneral", "FINISHED" }
            }),
            // Stage 2: Project with truncated timestamp based on FinishedAt
            new BsonDocument("$project", new BsonDocument
            {
                { "ActuatorCode", 1 },
                { "TotalDurationSeconds", 1 },
                { "FinishedAt", 1 },
                { "truncatedDate", new BsonDocument("$dateToString", new BsonDocument
                    {
                        { "format", dateFormat },
                        { "date", "$FinishedAt" }
                    })
                }
            }),
            // Stage 3: Group by truncated date and ActuatorCode
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "actuatorCode", "$ActuatorCode" },
                        { "truncatedDate", "$truncatedDate" }
                    }
                },
                { "totalDurationSeconds", new BsonDocument("$sum", "$TotalDurationSeconds") },
                { "activationCount", new BsonDocument("$sum", 1) },
                { "timestamp", new BsonDocument("$first", "$truncatedDate") }
            }),
            // Stage 4: Project to final structure
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "actuatorCode", "$_id.actuatorCode" },
                { "totalDurationSeconds", 1 },
                { "activationCount", 1 },
                { "timestamp", new BsonDocument("$dateFromString", new BsonDocument
                    {
                        { "dateString", "$timestamp" }
                    })
                }
            }),
            // Stage 5: Sort by timestamp and actuatorCode
            new BsonDocument("$sort", new BsonDocument
            {
                { "timestamp", 1 },
                { "actuatorCode", 1 }
            })
        };

        var cursor = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

        return cursor.Select(doc => new TimelineData
        {
            Timestamp = doc["timestamp"].ToUniversalTime(),
            ActuatorCode = doc["actuatorCode"].AsString,
            TotalDurationSeconds = doc["totalDurationSeconds"].ToDouble(),
            ActivationCount = doc["activationCount"].ToInt32()
        }).ToList();
    }

    private async Task<List<TotalDurationData>> GetTotalDurationByActuatorAsync(DateTime startDate, DateTime endDate)
    {
        var pipeline = new BsonDocument[]
        {
            // Stage 1: Match documents within date range and with FINISHED status
            // Use FinishedAt instead of CreatedAt to filter by completion date
            new BsonDocument("$match", new BsonDocument
            {
                { "FinishedAt", new BsonDocument
                    {
                        { "$gte", startDate },
                        { "$lte", endDate }
                    }
                },
                { "StatusGeneral", "FINISHED" }
            }),
            // Stage 2: Group by ActuatorCode
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$ActuatorCode" },
                { "totalDurationSeconds", new BsonDocument("$sum", "$TotalDurationSeconds") },
                { "activationCount", new BsonDocument("$sum", 1) }
            }),
            // Stage 3: Project to final structure
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "actuatorCode", "$_id" },
                { "totalDurationSeconds", 1 },
                { "activationCount", 1 }
            }),
            // Stage 4: Sort by totalDurationSeconds descending
            new BsonDocument("$sort", new BsonDocument
            {
                { "totalDurationSeconds", -1 }
            })
        };

        var cursor = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

        return cursor.Select(doc => new TotalDurationData
        {
            ActuatorCode = doc["actuatorCode"].AsString,
            TotalDurationSeconds = doc["totalDurationSeconds"].ToDouble(),
            ActivationCount = doc["activationCount"].ToInt32()
        }).ToList();
    }

    private List<ActiveTimeProportionData> CalculateActiveTimeProportion(List<TotalDurationData> totalDurationData)
    {
        var totalSeconds = totalDurationData.Sum(x => x.TotalDurationSeconds);

        if (totalSeconds == 0)
            return new List<ActiveTimeProportionData>();

        return totalDurationData.Select(item => new ActiveTimeProportionData
        {
            ActuatorCode = item.ActuatorCode,
            Percentage = Math.Round((item.TotalDurationSeconds / totalSeconds) * 100, 2)
        }).ToList();
    }
}