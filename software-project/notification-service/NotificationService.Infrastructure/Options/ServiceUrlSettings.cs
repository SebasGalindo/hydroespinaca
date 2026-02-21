namespace NotificationService.Infrastructure.Options;

/// <summary>
/// Configuration for internal microservice URLs used by the DailySummaryDataAggregator.
/// </summary>
public class ServiceUrlSettings
{
    public string SensorServiceUrl { get; set; } = "http://sensor-service:8080";
    public string ActuatorServiceUrl { get; set; } = "http://actuator-service:8080";
    public string FuzzyServiceUrl { get; set; } = "http://fuzzy-service:8080";
    public string WeatherServiceUrl { get; set; } = "http://weather-service:8080";
}
