using BffService.Domain.DTOs;
using FluentAssertions;
using Xunit;

namespace BffService.Domain.Tests.DTOs;

public class AggregateDataPointTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var dataPoint = new AggregateDataPoint
        {
            Timestamp = timestamp,
            Avg = 25.5,
            Min = 10.0,
            Max = 40.0,
            Count = 100
        };

        // Assert
        dataPoint.Timestamp.Should().Be(timestamp);
        dataPoint.Avg.Should().Be(25.5);
        dataPoint.Min.Should().Be(10.0);
        dataPoint.Max.Should().Be(40.0);
        dataPoint.Count.Should().Be(100);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var dataPoint = new AggregateDataPoint();

        // Act
        dataPoint.Timestamp = DateTime.UtcNow;
        dataPoint.Avg = 30.0;
        dataPoint.Min = 15.0;
        dataPoint.Max = 45.0;
        dataPoint.Count = 50;

        // Assert
        dataPoint.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dataPoint.Avg.Should().Be(30.0);
        dataPoint.Min.Should().Be(15.0);
        dataPoint.Max.Should().Be(45.0);
        dataPoint.Count.Should().Be(50);
    }
}

public class AggregateSummaryTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var summary = new AggregateSummary
        {
            Min = 5.0,
            Max = 50.0,
            Avg = 27.5,
            Count = 200
        };

        // Assert
        summary.Min.Should().Be(5.0);
        summary.Max.Should().Be(50.0);
        summary.Avg.Should().Be(27.5);
        summary.Count.Should().Be(200);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var summary = new AggregateSummary();

        // Act
        summary.Min = 10.0;
        summary.Max = 60.0;
        summary.Avg = 35.0;
        summary.Count = 150;

        // Assert
        summary.Min.Should().Be(10.0);
        summary.Max.Should().Be(60.0);
        summary.Avg.Should().Be(35.0);
        summary.Count.Should().Be(150);
    }
}

public class AggregateTrendPointTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var trendPoint = new AggregateTrendPoint
        {
            Timestamp = timestamp,
            Avg = 22.5
        };

        // Assert
        trendPoint.Timestamp.Should().Be(timestamp);
        trendPoint.Avg.Should().Be(22.5);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var trendPoint = new AggregateTrendPoint();

        // Act
        trendPoint.Timestamp = DateTime.UtcNow;
        trendPoint.Avg = 28.5;

        // Assert
        trendPoint.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        trendPoint.Avg.Should().Be(28.5);
    }
}

public class AggregateVariabilityPointTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var variabilityPoint = new AggregateVariabilityPoint
        {
            Timestamp = timestamp,
            Min = 10.0,
            Q1 = 20.0,
            Median = 30.0,
            Q3 = 40.0,
            Max = 50.0,
            Count = 100
        };

        // Assert
        variabilityPoint.Timestamp.Should().Be(timestamp);
        variabilityPoint.Min.Should().Be(10.0);
        variabilityPoint.Q1.Should().Be(20.0);
        variabilityPoint.Median.Should().Be(30.0);
        variabilityPoint.Q3.Should().Be(40.0);
        variabilityPoint.Max.Should().Be(50.0);
        variabilityPoint.Count.Should().Be(100);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var variabilityPoint = new AggregateVariabilityPoint();

        // Act
        variabilityPoint.Timestamp = DateTime.UtcNow;
        variabilityPoint.Min = 5.0;
        variabilityPoint.Q1 = 15.0;
        variabilityPoint.Median = 25.0;
        variabilityPoint.Q3 = 35.0;
        variabilityPoint.Max = 45.0;
        variabilityPoint.Count = 80;

        // Assert
        variabilityPoint.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        variabilityPoint.Min.Should().Be(5.0);
        variabilityPoint.Q1.Should().Be(15.0);
        variabilityPoint.Median.Should().Be(25.0);
        variabilityPoint.Q3.Should().Be(35.0);
        variabilityPoint.Max.Should().Be(45.0);
        variabilityPoint.Count.Should().Be(80);
    }
}

public class EnvironmentalAggregateResponseTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange
        var summary = new AggregateSummary { Min = 10, Max = 30, Avg = 20, Count = 100 };
        var trend = new List<AggregateTrendPoint>();
        var variability = new List<AggregateVariabilityPoint>();

        // Act
        var response = new EnvironmentalAggregateResponse
        {
            VariableCode = "TEMP",
            VariableName = "Temperature",
            Summary = summary,
            Trend = trend,
            Variability = variability
        };

        // Assert
        response.VariableCode.Should().Be("TEMP");
        response.VariableName.Should().Be("Temperature");
        response.Summary.Should().BeSameAs(summary);
        response.Trend.Should().BeSameAs(trend);
        response.Variability.Should().BeSameAs(variability);
    }
}

public class EnvironmentalAggregatesResponseTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange
        var variables = new List<EnvironmentalAggregateResponse>
        {
            new() { VariableCode = "TEMP", VariableName = "Temperature" },
            new() { VariableCode = "HUM", VariableName = "Humidity" }
        };

        // Act
        var response = new EnvironmentalAggregatesResponse
        {
            Variables = variables
        };

        // Assert
        response.Variables.Should().HaveCount(2);
        response.Variables.Should().BeSameAs(variables);
    }
}

public class FuzzyRuleSummaryDtoTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var dto = new FuzzyRuleSummaryDto
        {
            Id = "rule-1",
            Name = "Rule 1",
            Description = "Test rule description"
        };

        // Assert
        dto.Id.Should().Be("rule-1");
        dto.Name.Should().Be("Rule 1");
        dto.Description.Should().Be("Test rule description");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var dto = new FuzzyRuleSummaryDto();

        // Act
        dto.Id = "rule-42";
        dto.Name = "Updated Rule";
        dto.Description = "Updated description";

        // Assert
        dto.Id.Should().Be("rule-42");
        dto.Name.Should().Be("Updated Rule");
        dto.Description.Should().Be("Updated description");
    }
}

public class SystemStatusDtoTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var dto = new SystemStatusDto();

        // Assert
        dto.Readings.Should().NotBeNull();
        dto.JobStatus.Should().NotBeNull();
        dto.Stats.Should().NotBeNull();
        dto.InternalRoutines.Should().NotBeNull();
        dto.InternalRoutines.Should().BeEmpty();
        dto.Weather.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var dto = new SystemStatusDto();
        var weather = new WeatherDto { Temperature = 25.5 };

        // Act
        dto.Weather = weather;

        // Assert
        dto.Weather.Should().BeSameAs(weather);
        dto.Weather.Temperature.Should().Be(25.5);
    }

    [Fact]
    public void InternalRoutines_ShouldBeModifiable()
    {
        // Arrange
        var dto = new SystemStatusDto();

        // Act
        dto.InternalRoutines.Should().BeEmpty();

        // Assert - just verify it's initialized and can be used
        dto.InternalRoutines.Should().NotBeNull();
    }
}

public class WeatherDtoTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var dto = new WeatherDto
        {
            Temperature = 25.5,
            FeelsLike = 26.0,
            Humidity = 60,
            Main = "Clear",
            Description = "Clear sky",
            Icon = "01d",
            WindSpeed = 5.5,
            Cloudiness = 10,
            Rain1h = 0.0,
            Sunrise = DateTime.UtcNow.AddHours(-6),
            Sunset = DateTime.UtcNow.AddHours(6),
            LastUpdate = DateTime.UtcNow
        };

        // Assert
        dto.Temperature.Should().Be(25.5);
        dto.FeelsLike.Should().Be(26.0);
        dto.Humidity.Should().Be(60);
        dto.Main.Should().Be("Clear");
        dto.Description.Should().Be("Clear sky");
        dto.Icon.Should().Be("01d");
        dto.WindSpeed.Should().Be(5.5);
        dto.Cloudiness.Should().Be(10);
        dto.Rain1h.Should().Be(0.0);
        dto.Sunrise.Should().BeCloseTo(DateTime.UtcNow.AddHours(-6), TimeSpan.FromSeconds(1));
        dto.Sunset.Should().BeCloseTo(DateTime.UtcNow.AddHours(6), TimeSpan.FromSeconds(1));
        dto.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var dto = new WeatherDto();

        // Act
        dto.Temperature = 30.0;
        dto.FeelsLike = 32.0;
        dto.Humidity = 70;
        dto.Main = "Rain";
        dto.Description = "Light rain";
        dto.Icon = "10d";
        dto.WindSpeed = 10.0;
        dto.Cloudiness = 80;
        dto.Rain1h = 5.0;
        dto.Sunrise = DateTime.UtcNow.AddHours(-5);
        dto.Sunset = DateTime.UtcNow.AddHours(7);
        dto.LastUpdate = DateTime.UtcNow;

        // Assert
        dto.Temperature.Should().Be(30.0);
        dto.FeelsLike.Should().Be(32.0);
        dto.Humidity.Should().Be(70);
        dto.Main.Should().Be("Rain");
        dto.Description.Should().Be("Light rain");
        dto.Icon.Should().Be("10d");
        dto.WindSpeed.Should().Be(10.0);
        dto.Cloudiness.Should().Be(80);
        dto.Rain1h.Should().Be(5.0);
    }
}
