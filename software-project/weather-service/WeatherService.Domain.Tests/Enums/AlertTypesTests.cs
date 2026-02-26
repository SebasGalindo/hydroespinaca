using WeatherService.Domain.Enums;

namespace WeatherService.Domain.Tests.Enums;

public class AlertTypesTests
{
    [Theory]
    [InlineData("extreme_heat")]
    [InlineData("extreme_cold")]
    [InlineData("high_humidity")]
    [InlineData("low_humidity")]
    [InlineData("heavy_rain")]
    [InlineData("thunderstorm")]
    [InlineData("high_cloudiness")]
    [InlineData("strong_wind")]
    [InlineData("extreme_uv")]
    public void All_ContainsExpectedType(string alertType)
    {
        AlertTypes.All.Should().Contain(alertType);
    }

    [Fact]
    public void All_HasExactlyNineTypes()
    {
        AlertTypes.All.Should().HaveCount(9);
    }

    [Fact]
    public void Constants_MatchExpectedValues()
    {
        AlertTypes.ExtremeHeat.Should().Be("extreme_heat");
        AlertTypes.ExtremeCold.Should().Be("extreme_cold");
        AlertTypes.HighHumidity.Should().Be("high_humidity");
        AlertTypes.LowHumidity.Should().Be("low_humidity");
        AlertTypes.HeavyRain.Should().Be("heavy_rain");
        AlertTypes.Thunderstorm.Should().Be("thunderstorm");
        AlertTypes.HighCloudiness.Should().Be("high_cloudiness");
        AlertTypes.StrongWind.Should().Be("strong_wind");
        AlertTypes.ExtremeUv.Should().Be("extreme_uv");
    }

    [Fact]
    public void Government_IsNotInAllArray()
    {
        AlertTypes.Government.Should().Be("government");
        AlertTypes.All.Should().NotContain("government");
    }
}
