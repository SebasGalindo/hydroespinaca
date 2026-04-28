using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Infrastructure.Persistence;

public class MongoAlertDeliveryLogRepository : IAlertDeliveryLogRepository
{
    private readonly IMongoCollection<AlertDeliveryLogDocument> _collection;

    public MongoAlertDeliveryLogRepository(IMongoDatabase database)
    {
        const string CollectionName = "alert_delivery_log";

        EnsureTimeSeriesCollectionAsync(database, CollectionName).GetAwaiter().GetResult();

        _collection = database.GetCollection<AlertDeliveryLogDocument>(CollectionName);
    }

    public async Task InsertAsync(AlertDeliveryLog log, CancellationToken ct = default)
    {
        var doc = new AlertDeliveryLogDocument
        {
            FuzzySystemId = log.FuzzySystemId,
            AlertType = log.AlertType,
            ForecastDate = log.ForecastDate.Date,
            UserId = log.UserId,
            SentAt = log.SentAt,
        };

        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task<bool> ExistsAsync(
        string fuzzySystemId,
        string alertType,
        DateTime forecastDate,
        string userId,
        CancellationToken ct = default)
    {
        var forecastDay = forecastDate.Date;
        var filter = Builders<AlertDeliveryLogDocument>.Filter.And(
            Builders<AlertDeliveryLogDocument>.Filter.Eq(d => d.FuzzySystemId, fuzzySystemId),
            Builders<AlertDeliveryLogDocument>.Filter.Eq(d => d.AlertType, alertType),
            Builders<AlertDeliveryLogDocument>.Filter.Eq(d => d.ForecastDate, forecastDay),
            Builders<AlertDeliveryLogDocument>.Filter.Eq(d => d.UserId, userId)
        );

        return await _collection.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
    }

    private static async Task EnsureTimeSeriesCollectionAsync(IMongoDatabase database, string collectionName)
    {
        var existing = await (await database.ListCollectionNamesAsync()).ToListAsync();
        if (existing.Contains(collectionName))
            return;

        await database.CreateCollectionAsync(collectionName, new CreateCollectionOptions
        {
            TimeSeriesOptions = new TimeSeriesOptions("sentAt", "fuzzySystemId", TimeSeriesGranularity.Hours)
        });
    }
}

[BsonIgnoreExtraElements]
internal class AlertDeliveryLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("fuzzySystemId")]
    public string FuzzySystemId { get; set; } = string.Empty;

    [BsonElement("alertType")]
    public string AlertType { get; set; } = string.Empty;

    [BsonElement("forecastDate")]
    public DateTime ForecastDate { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; }
}
