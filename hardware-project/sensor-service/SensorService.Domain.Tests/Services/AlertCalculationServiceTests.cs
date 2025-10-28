using SensorService.Domain.Services;
using SensorService.Domain.Entities;
using SensorService.Domain.Tests.Builders;

namespace SensorService.Domain.Tests.Services;

public class AlertCalculationServiceTests
{
    private readonly AlertCalculationService _sut;

    public AlertCalculationServiceTests()
    {
        _sut = new AlertCalculationService();
    }

    #region CalculateOutOfRangeAlert Tests

    [Fact]
    public void CalculateOutOfRangeAlert_WithValueBelowOptimalMin_ReturnsAlert()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(5.0)
            .WithVariableCode("PH")
            .WithSensorCode("SEN-01")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithPhysicalMin(0)
            .WithPhysicalMax(14)
            .Build();

        var timestamp = DateTime.UtcNow;

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, timestamp);

        // Assert
        result.Should().NotBeNull();
        result!.VariableCode.Should().Be("PH");
        result.Value.Should().Be(5.0);
        result.Message.Should().Contain("5");
        result.Message.Should().Contain("6");
        result.Message.Should().Contain("7.5");
        result.Timestamp.Should().Be(timestamp);
        result.LastSeen.Should().Be(timestamp);
        result.Acknowledged.Should().BeFalse();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithValueAboveOptimalMax_ReturnsAlert()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(8.5)
            .WithVariableCode("PH")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        var timestamp = DateTime.UtcNow;

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, timestamp);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(8.5);
        result.Message.Should().Contain("8.5");
        result.Message.Should().Contain("7.5");
        result.Acknowledged.Should().BeFalse();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithValueWithinOptimalRange_ReturnsNull()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(6.8)
            .WithVariableCode("PH")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, DateTime.UtcNow);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithValueAtOptimalMin_ReturnsNull()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(6.0)
            .Build();

        var variable = new VariableBuilder()
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, DateTime.UtcNow);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithValueAtOptimalMax_ReturnsNull()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(7.5)
            .Build();

        var variable = new VariableBuilder()
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, DateTime.UtcNow);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithNoOptimalMax_AndValueAboveMin_ReturnsNull()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(100.0)
            .WithVariableCode("EC")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("EC")
            .WithOptimalMin(50.0)
            .WithOptimalMax(null) // No upper limit
            .Build();

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, DateTime.UtcNow);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOutOfRangeAlert_WithNoOptimalMax_AndValueBelowMin_ReturnsAlert()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(30.0)
            .WithVariableCode("EC")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("EC")
            .WithOptimalMin(50.0)
            .WithOptimalMax(null)
            .Build();

        // Act
        var result = _sut.CalculateOutOfRangeAlert(reading, variable, DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result!.Message.Should().Contain("30");
        result.Message.Should().Contain("50");
        result.Message.Should().NotContain("rango óptimo");
        result.Message.Should().Contain("mínimo óptimo");
    }

    #endregion

    #region IsValueWithinOptimalRange Tests

    [Theory]
    [InlineData(6.0, true)]  // At min
    [InlineData(7.5, true)]  // At max
    [InlineData(6.8, true)]  // Within range
    [InlineData(5.9, false)] // Below min
    [InlineData(7.6, false)] // Above max
    public void IsValueWithinOptimalRange_WithVariousValues_ReturnsExpectedResult(double value, bool expected)
    {
        // Arrange
        var variable = new VariableBuilder()
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        // Act
        var result = _sut.IsValueWithinOptimalRange(value, variable);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValueWithinOptimalRange_WithNoMaxValue_AndValueAboveMin_ReturnsTrue()
    {
        // Arrange
        var variable = new VariableBuilder()
            .WithOptimalMin(50.0)
            .WithOptimalMax(null)
            .Build();

        // Act
        var result = _sut.IsValueWithinOptimalRange(1000.0, variable);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValueWithinOptimalRange_WithNoMaxValue_AndValueBelowMin_ReturnsFalse()
    {
        // Arrange
        var variable = new VariableBuilder()
            .WithOptimalMin(50.0)
            .WithOptimalMax(null)
            .Build();

        // Act
        var result = _sut.IsValueWithinOptimalRange(30.0, variable);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
