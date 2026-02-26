using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Tests.Entities;

public class WeatherAlertConfigTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var config = new WeatherAlertConfig
        {
            FuzzySystemId = "fuzzy-1"
        };

        config.Id.Should().BeEmpty();
        config.FuzzySystemName.Should().BeEmpty();
        config.Alerts.Should().BeEmpty();
        config.IsActive.Should().BeTrue();
        config.CreatedBy.Should().BeEmpty();
        config.UpdatedBy.Should().BeEmpty();
    }

    [Fact]
    public void SetId_UpdatesId()
    {
        var config = new WeatherAlertConfig { FuzzySystemId = "fuzzy-1" };
        config.SetId("config-123");
        config.Id.Should().Be("config-123");
    }

    [Fact]
    public void UpdateTimestamp_SetsUserAndTime()
    {
        var config = new WeatherAlertConfig { FuzzySystemId = "fuzzy-1" };
        var before = DateTime.UtcNow;

        config.UpdateTimestamp("user-42");

        config.UpdatedBy.Should().Be("user-42");
        config.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void CreatedAt_DefaultsCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var config = new WeatherAlertConfig { FuzzySystemId = "fuzzy-1" };
        var after = DateTime.UtcNow;

        config.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Alerts_CanBePopulated()
    {
        var config = new WeatherAlertConfig
        {
            FuzzySystemId = "fuzzy-1",
            Alerts = new List<AlertThreshold>
            {
                new() { Type = "extreme_heat", Enabled = true, ThresholdValue = 35, Comparison = "gt", Recommendation = "Cool down" },
                new() { Type = "extreme_cold", Enabled = false, ThresholdValue = 5, Comparison = "lt", Recommendation = "Heat up" }
            }
        };

        config.Alerts.Should().HaveCount(2);
        config.Alerts[0].Type.Should().Be("extreme_heat");
        config.Alerts[1].Enabled.Should().BeFalse();
    }
}
