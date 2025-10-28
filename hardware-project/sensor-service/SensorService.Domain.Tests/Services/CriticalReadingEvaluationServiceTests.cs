using Moq;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Services;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Tests.Builders;

namespace SensorService.Domain.Tests.Services;

public class CriticalReadingEvaluationServiceTests
{
    private readonly Mock<IVariableRepository> _variableRepositoryMock;
    private readonly CriticalReadingEvaluationService _sut;

    public CriticalReadingEvaluationServiceTests()
    {
        _variableRepositoryMock = new Mock<IVariableRepository>();
        _sut = new CriticalReadingEvaluationService(_variableRepositoryMock.Object);
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithNoManualVariables_ReturnsEmptyResult()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;
        var readings = new List<Reading>();
        var sensors = new List<Sensor>();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable>());

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.Should().NotBeNull();
        result.Esp32Id.Should().Be(esp32Id);
        result.Timestamp.Should().Be(timestamp);
        result.ManualReadings.Should().BeEmpty();
        result.ContextualReadings.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithManualVariable_AndReadingOutOfRange_CreatesAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithUnit("")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var phReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(5.0) // Below optimal min
            .WithTimestamp(timestamp)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { phReading };
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var alert = result.ManualReadings.First();
        alert.Code.Should().Be("PH");
        alert.Name.Should().Be("pH");
        alert.Value.Should().Be(5.0);
        alert.IsAlert.Should().BeTrue();
        alert.Threshold.Should().Contain("6.0");
        alert.Threshold.Should().Contain("7.5");
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithManualVariable_AndReadingInRange_NoAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var phReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(6.8) // Within range
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { phReading };
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var reading = result.ManualReadings.First();
        reading.Code.Should().Be("PH");
        reading.Value.Should().Be(6.8);
        reading.IsAlert.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithManualVariable_AndNoReadings_CreatesAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading>(); // No readings
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var alert = result.ManualReadings.First();
        alert.Code.Should().Be("PH");
        alert.Value.Should().Be(double.NaN);
        alert.IsAlert.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithManualVariable_AndNoSensors_CreatesConfigurationAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading>();
        var sensors = new List<Sensor>(); // No sensors configured

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var alert = result.ManualReadings.First();
        alert.Code.Should().Be("PH");
        alert.Value.Should().Be(double.NaN);
        alert.IsAlert.Should().BeTrue();
        alert.Threshold.Should().Be("Sin sensores configurados");
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithMultipleReadings_UsesLatestReading()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var olderReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(5.0)
            .WithTimestamp(timestamp.AddMinutes(-10))
            .Build();

        var newerReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(6.8) // Latest reading is in range
            .WithTimestamp(timestamp)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { olderReading, newerReading };
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var reading = result.ManualReadings.First();
        reading.Value.Should().Be(6.8); // Should use the newer reading
        reading.IsAlert.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithAutomaticVariables_AndManualVariables_AddsContextualReadings()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        // Manual variable (required for processing to continue)
        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var phReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(6.8)
            .Build();

        // Automatic variable (for contextual reading)
        var tempVariable = new VariableBuilder()
            .WithCode("T_AMB")
            .WithName("Temperatura Ambiente")
            .WithUnit("°C")
            .WithOptimalMin(15.0)
            .WithOptimalMax(25.0)
            .WithRegulationType(RegulationType.Automatic)
            .Build();

        var tempSensor = new SensorBuilder()
            .WithCode("DHT22-01")
            .WithVariables(new List<string> { "T_AMB" })
            .Build();

        var tempReading = new ReadingBuilder()
            .WithSensorCode("DHT22-01")
            .WithVariableCode("T_AMB")
            .WithValue(22.5)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable> { tempVariable });

        var readings = new List<Reading> { phReading, tempReading };
        var sensors = new List<Sensor> { phSensor, tempSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ContextualReadings.Should().HaveCount(1);
        var contextual = result.ContextualReadings.First();
        contextual.Name.Should().Be("Temperatura Ambiente");
        contextual.Value.Should().Be(22.5);
        contextual.Unit.Should().Be("°C");

        // Also verify manual reading was processed
        result.ManualReadings.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithNaNValue_CreatesAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var nanReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(double.NaN)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { nanReading };
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var alert = result.ManualReadings.First();
        alert.IsAlert.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithInfinityValue_CreatesAlert()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithName("pH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var phSensor = new SensorBuilder()
            .WithCode("PH-01")
            .WithVariables(new List<string> { "PH" })
            .Build();

        var infinityReading = new ReadingBuilder()
            .WithSensorCode("PH-01")
            .WithVariableCode("PH")
            .WithValue(double.PositiveInfinity)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { phVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { infinityReading };
        var sensors = new List<Sensor> { phSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var alert = result.ManualReadings.First();
        alert.IsAlert.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithVariableNoOptimalMax_FormatsThresholdCorrectly()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var ecVariable = new VariableBuilder()
            .WithCode("EC")
            .WithName("Conductividad Eléctrica")
            .WithUnit("µS/cm")
            .WithOptimalMin(100.0)
            .WithOptimalMax(null) // No max value
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var ecSensor = new SensorBuilder()
            .WithCode("EC-01")
            .WithVariables(new List<string> { "EC" })
            .Build();

        var ecReading = new ReadingBuilder()
            .WithSensorCode("EC-01")
            .WithVariableCode("EC")
            .WithValue(150.0)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable> { ecVariable });

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable>());

        var readings = new List<Reading> { ecReading };
        var sensors = new List<Sensor> { ecSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ManualReadings.Should().HaveCount(1);
        var reading = result.ManualReadings.First();
        reading.Threshold.Should().Contain("≥");
        reading.Threshold.Should().Contain("100.0");
        reading.Threshold.Should().Contain("µS/cm");
    }

    [Fact]
    public async Task EvaluateCriticalReadingsAsync_WithAutomaticVariable_AndInvalidValue_DoesNotAddContextualReading()
    {
        // Arrange
        var esp32Id = "esp32-01";
        var timestamp = DateTime.UtcNow;

        var tempVariable = new VariableBuilder()
            .WithCode("T_AMB")
            .WithName("Temperatura")
            .WithUnit("°C")
            .WithRegulationType(RegulationType.Automatic)
            .Build();

        var tempSensor = new SensorBuilder()
            .WithCode("DHT22-01")
            .WithVariables(new List<string> { "T_AMB" })
            .Build();

        var invalidReading = new ReadingBuilder()
            .WithSensorCode("DHT22-01")
            .WithVariableCode("T_AMB")
            .WithValue(double.NaN) // Invalid value
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Manual, default))
            .ReturnsAsync(new List<Variable>());

        _variableRepositoryMock
            .Setup(x => x.GetByRegulationTypeAsync(RegulationType.Automatic, default))
            .ReturnsAsync(new List<Variable> { tempVariable });

        var readings = new List<Reading> { invalidReading };
        var sensors = new List<Sensor> { tempSensor };

        // Act
        var result = await _sut.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors);

        // Assert
        result.ContextualReadings.Should().BeEmpty();
    }
}
