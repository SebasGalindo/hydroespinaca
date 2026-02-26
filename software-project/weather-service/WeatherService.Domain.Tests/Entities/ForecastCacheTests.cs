using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Tests.Entities;

public class ForecastCacheTests
{
    [Fact]
    public void Constructor_SetsDefaultId()
    {
        var cache = new ForecastCache();
        cache.Id.Should().Be("latest_forecast");
    }

    [Fact]
    public void SetId_UpdatesId()
    {
        var cache = new ForecastCache();
        cache.SetId("custom-id");
        cache.Id.Should().Be("custom-id");
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var now = DateTime.UtcNow;
        var cache = new ForecastCache
        {
            FetchedAt = now,
            ExpiresAt = now.AddMinutes(30)
        };

        cache.FetchedAt.Should().Be(now);
        cache.ExpiresAt.Should().Be(now.AddMinutes(30));
    }

    [Fact]
    public void Collections_DefaultToEmpty()
    {
        var cache = new ForecastCache();
        cache.Hourly.Should().NotBeNull().And.BeEmpty();
        cache.Daily.Should().NotBeNull().And.BeEmpty();
        cache.GovernmentAlerts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Current_DefaultsToNewInstance()
    {
        var cache = new ForecastCache();
        cache.Current.Should().NotBeNull();
    }
}
