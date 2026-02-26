using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Tests.Entities;

public class WeatherAlertTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var alert = new WeatherAlert
        {
            FuzzySystemId = "fuzzy-1",
            AlertType = "extreme_heat"
        };

        alert.Id.Should().BeEmpty();
        alert.Severity.Should().Be("warning");
        alert.Title.Should().BeEmpty();
        alert.Message.Should().BeEmpty();
        alert.Recommendation.Should().BeEmpty();
        alert.GovernmentAlert.Should().BeFalse();
        alert.NotifiedUsers.Should().BeEmpty();
    }

    [Fact]
    public void SetId_UpdatesId()
    {
        var alert = new WeatherAlert
        {
            FuzzySystemId = "fuzzy-1",
            AlertType = "extreme_heat"
        };
        alert.SetId("alert-123");
        alert.Id.Should().Be("alert-123");
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var alert = new WeatherAlert
        {
            FuzzySystemId = "fuzzy-1",
            AlertType = "heavy_rain",
            Severity = "critical",
            Title = "Heavy Rain Alert",
            Message = "Heavy rain expected",
            Recommendation = "Reduce water pump",
            ForecastDatetime = now,
            ForecastValue = 15.5,
            ForecastCondition = "rain",
            GovernmentAlert = true,
            CreatedAt = now
        };

        alert.FuzzySystemId.Should().Be("fuzzy-1");
        alert.AlertType.Should().Be("heavy_rain");
        alert.Severity.Should().Be("critical");
        alert.Title.Should().Be("Heavy Rain Alert");
        alert.ForecastValue.Should().Be(15.5);
        alert.ForecastCondition.Should().Be("rain");
        alert.GovernmentAlert.Should().BeTrue();
    }

    [Fact]
    public void CreatedAt_DefaultsCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var alert = new WeatherAlert
        {
            FuzzySystemId = "fuzzy-1",
            AlertType = "extreme_heat"
        };
        var after = DateTime.UtcNow;

        alert.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ForecastValue_CanBeNull()
    {
        var alert = new WeatherAlert
        {
            FuzzySystemId = "fuzzy-1",
            AlertType = "thunderstorm",
            ForecastValue = null,
            ForecastCondition = null
        };

        alert.ForecastValue.Should().BeNull();
        alert.ForecastCondition.Should().BeNull();
    }
}
