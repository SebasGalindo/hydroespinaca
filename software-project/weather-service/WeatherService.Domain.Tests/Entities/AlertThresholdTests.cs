using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Tests.Entities;

public class AlertThresholdTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var threshold = new AlertThreshold { Type = "extreme_heat" };

        threshold.Enabled.Should().BeTrue();
        threshold.ThresholdValue.Should().BeNull();
        threshold.Comparison.Should().BeNull();
        threshold.Recommendation.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var threshold = new AlertThreshold
        {
            Type = "heavy_rain",
            Enabled = true,
            ThresholdValue = 10.0,
            Comparison = "gt",
            Recommendation = "Reduce water pump frequency"
        };

        threshold.Type.Should().Be("heavy_rain");
        threshold.Enabled.Should().BeTrue();
        threshold.ThresholdValue.Should().Be(10.0);
        threshold.Comparison.Should().Be("gt");
        threshold.Recommendation.Should().Be("Reduce water pump frequency");
    }

    [Fact]
    public void ThresholdValue_CanBeNull_ForThunderstorm()
    {
        var threshold = new AlertThreshold
        {
            Type = "thunderstorm",
            Enabled = true,
            ThresholdValue = null,
            Comparison = null
        };

        threshold.ThresholdValue.Should().BeNull();
        threshold.Comparison.Should().BeNull();
    }
}
