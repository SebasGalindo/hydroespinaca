using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using HydroEspinaca.Shared.DTOs.Analytics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB repository implementation for routine command execution records.
/// </summary>
public class MongoRoutineCommandRepository : IRoutineCommandRepository
{
    private readonly BaseMongoRepository<RoutineCommand, RoutineCommandDocument> _baseRepo;
    private readonly TimeZoneInfo _colombiaTz;

    public MongoRoutineCommandRepository(MongoDbContext ctx,
                                         IEntityMapper<RoutineCommand, RoutineCommandDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<RoutineCommand, RoutineCommandDocument>(ctx.Database, "routine_commands", mapper);
        _colombiaTz = TryGetColombiaTimeZone();
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

    public async Task<int> DeleteRunningCommandsAsync(string? esp32Id = null)
    {
        var filterBuilder = Builders<RoutineCommandDocument>.Filter;
        FilterDefinition<RoutineCommandDocument> filter;

        if (esp32Id != null)
        {
            // Delete only RUNNING commands for the specific ESP32
            filter = filterBuilder.And(
                filterBuilder.Eq(x => x.StatusGeneral, HydroEspinaca.Shared.Enums.RoutineCommandStatus.RUNNING),
                filterBuilder.Eq(x => x.Esp32Id, esp32Id)
            );
        }
        else
        {
            // Delete all RUNNING commands
            filter = filterBuilder.Eq(x => x.StatusGeneral, HydroEspinaca.Shared.Enums.RoutineCommandStatus.RUNNING);
        }

        var result = await _baseRepo.DeleteManyAsync(filter);
        return (int)result.DeletedCount;
    }

    public async Task<ActuatorAnalyticsData> GetActuatorAnalyticsAsync(ActuatorAnalyticsRequest request)
    {
        // Ensure dates are properly marked as UTC (they come from frontend already in UTC)
        var startUtc = request.StartDate.Kind == DateTimeKind.Utc
            ? request.StartDate
            : DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        var endUtc = request.EndDate.Kind == DateTimeKind.Utc
            ? request.EndDate
            : DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

        // 1. Get Timeline data
        var timelineData = await GetTimelineDataAsync(startUtc, endUtc, request.View);

        // 2. Get Total Duration by Actuator
        var totalDurationData = await GetTotalDurationByActuatorAsync(startUtc, endUtc);

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
            "hourly" => "%Y-%m-%dT%H:00:00.000", // Hour precision
            "daily" => "%Y-%m-%dT%H:00:00.000",  // Hour precision for detailed view
            "weekly" => "%Y-W%V", // ISO week format
            "monthly" => "%Y-%m-01T00:00:00.000", // First day of month
            _ => "%Y-%m-%dT00:00:00.000"
        };

        var tzId = _colombiaTz.Id; // e.g., "America/Bogota"

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
                        { "date", "$FinishedAt" },
                        { "timezone", tzId }
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
                        { "dateString", "$timestamp" },
                        // Interpret the truncated string in Colombia time so the resulting Date (UTC instant) aligns to local boundaries
                        { "timezone", tzId }
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

        var results = await _baseRepo.AggregateToBsonAsync(pipeline);

        // Convert timestamps from UTC to Colombia time for frontend during mapping
        var timelineData = results.Select(doc =>
        {
            var utcTimestamp = doc["timestamp"].ToUniversalTime();
            var colombiaTimestamp = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, _colombiaTz);

            return new TimelineData
            {
                Timestamp = colombiaTimestamp,
                ActuatorCode = doc["actuatorCode"].AsString,
                TotalDurationSeconds = doc["totalDurationSeconds"].ToDouble(),
                ActivationCount = doc["activationCount"].ToInt32()
            };
        }).ToList();

        return timelineData;
    }

    private async Task<List<TotalDurationData>> GetTotalDurationByActuatorAsync(DateTime startDate, DateTime endDate)
    {
        // Ensure dates are properly marked as UTC
        var startUtc = startDate.Kind == DateTimeKind.Utc
            ? startDate
            : DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var endUtc = endDate.Kind == DateTimeKind.Utc
            ? endDate
            : DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        // We want the time each actuator was actually ON during [startUtc, endUtc].
        // Sum only the portion of each command that falls inside the requested window.
        // Overlap formula:  max(0, min(FinishedAt, endUtc) - max(CreatedAt, startUtc))
        var pipeline = new BsonDocument[]
        {
            // Stage 1: Any command that overlaps the window (started before end AND finished after start).
            // Includes RUNNING commands (FinishedAt is null) by treating null as endUtc.
            new BsonDocument("$match", new BsonDocument
            {
                { "CreatedAt", new BsonDocument("$lt", endUtc) },
                { "$or", new BsonArray
                    {
                        new BsonDocument("FinishedAt", new BsonDocument("$gt", startUtc)),
                        new BsonDocument("FinishedAt", BsonNull.Value)
                    }
                }
            }),
            // Stage 2: Compute the overlap (in seconds) of each command with [startUtc, endUtc]
            new BsonDocument("$addFields", new BsonDocument
            {
                { "effectiveStart", new BsonDocument("$max", new BsonArray { "$CreatedAt", startUtc }) },
                { "effectiveEnd", new BsonDocument("$min", new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$FinishedAt", endUtc }),
                        endUtc
                    })
                }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "overlapSeconds", new BsonDocument("$max", new BsonArray
                    {
                        0,
                        new BsonDocument("$divide", new BsonArray
                        {
                            new BsonDocument("$subtract", new BsonArray { "$effectiveEnd", "$effectiveStart" }),
                            1000 // ms → s
                        })
                    })
                }
            }),
            // Stage 3: Drop commands whose overlap is zero (no real activity inside the window)
            new BsonDocument("$match", new BsonDocument
            {
                { "overlapSeconds", new BsonDocument("$gt", 0) }
            }),
            // Stage 4: Group by ActuatorCode summing the clipped duration
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$ActuatorCode" },
                { "totalDurationSeconds", new BsonDocument("$sum", "$overlapSeconds") },
                { "activationCount", new BsonDocument("$sum", 1) }
            }),
            // Stage 5: Project to final structure
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "actuatorCode", "$_id" },
                { "totalDurationSeconds", 1 },
                { "activationCount", 1 }
            }),
            // Stage 6: Sort by totalDurationSeconds descending
            new BsonDocument("$sort", new BsonDocument
            {
                { "totalDurationSeconds", -1 }
            })
        };

        var results = await _baseRepo.AggregateToBsonAsync(pipeline);

        return results.Select(doc => new TotalDurationData
        {
            ActuatorCode = doc["actuatorCode"].AsString,
            TotalDurationSeconds = doc["totalDurationSeconds"].ToDouble(),
            ActivationCount = doc["activationCount"].ToInt32()
        }).ToList();
    }

    private List<ActiveTimeProportionData> CalculateActiveTimeProportion(List<TotalDurationData> totalDurationData)
    {
        var totalSeconds = totalDurationData.Sum(x => x.TotalDurationSeconds);

        // Use tolerance for floating-point comparison
        const double tolerance = 1e-9;
        if (Math.Abs(totalSeconds) < tolerance)
            return new List<ActiveTimeProportionData>();

        return totalDurationData.Select(item => new ActiveTimeProportionData
        {
            ActuatorCode = item.ActuatorCode,
            Percentage = Math.Round((item.TotalDurationSeconds / totalSeconds) * 100, 2)
        }).ToList();
    }

    public async Task<List<TimelineData>> GetRawTimelineDataAsync(DateTime startDate, DateTime endDate)
    {
        // Ensure dates are properly marked as UTC
        var startUtc = startDate.Kind == DateTimeKind.Utc
            ? startDate
            : DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var endUtc = endDate.Kind == DateTimeKind.Utc
            ? endDate
            : DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        var pipeline = new BsonDocument[]
        {
            // Stage 1: Match documents within date range and with FINISHED status
            new BsonDocument("$match", new BsonDocument
            {
                { "FinishedAt", new BsonDocument
                    {
                        { "$gte", startUtc },
                        { "$lte", endUtc }
                    }
                },
                { "StatusGeneral", "FINISHED" }
            }),
            // Stage 2: Project required fields
            new BsonDocument("$project", new BsonDocument
            {
                { "ActuatorCode", 1 },
                { "CommandId", 1 },
                { "CreatedAt", 1 },
                { "TotalDurationSeconds", 1 },
                { "StatusGeneral", 1 }
            }),
            // Stage 3: Sort by CreatedAt ascending
            new BsonDocument("$sort", new BsonDocument
            {
                { "CreatedAt", 1 }
            })
        };

        var results = await _baseRepo.AggregateToBsonAsync(pipeline);

        // Convert timestamps from UTC to Colombia time for frontend during mapping
        var timelineData = results.Select(doc =>
        {
            var utcTimestamp = doc["CreatedAt"].ToUniversalTime();
            var colombiaTimestamp = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, _colombiaTz);

            return new TimelineData
            {
                Timestamp = colombiaTimestamp,
                ActuatorCode = doc["ActuatorCode"].AsString,
                TotalDurationSeconds = doc["TotalDurationSeconds"].ToDouble(),
                ActivationCount = 1 // Each document represents one activation
            };
        }).ToList();

        return timelineData;
    }

    private static TimeZoneInfo TryGetColombiaTimeZone()
    {
        // Linux containers use IANA time zones
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); } catch { }

        // Windows fallback
        try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); } catch { }

        // As a last resort, use UTC (will keep behavior stable but without shift)
        return TimeZoneInfo.Utc;
    }
}