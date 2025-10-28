using HydroEspinaca.Shared.Enums;
using SensorService.Application.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Application.Tests.Builders;

namespace SensorService.Application.Tests.UseCases;

public class GenerateAlertsUseCaseTests
{
    private readonly Mock<IVariableRepository> _variableRepositoryMock;
    private readonly Mock<IAlertCalculationService> _alertCalculationServiceMock;
    private readonly Mock<ISensorAlertRepository> _sensorAlertRepositoryMock;
    private readonly Mock<IAlertResolutionService> _alertResolutionServiceMock;
    private readonly GenerateAlertsUseCase _sut;

    public GenerateAlertsUseCaseTests()
    {
        _variableRepositoryMock = new Mock<IVariableRepository>();
        _alertCalculationServiceMock = new Mock<IAlertCalculationService>();
        _sensorAlertRepositoryMock = new Mock<ISensorAlertRepository>();
        _alertResolutionServiceMock = new Mock<IAlertResolutionService>();

        _sut = new GenerateAlertsUseCase(
            _variableRepositoryMock.Object,
            _alertCalculationServiceMock.Object,
            _sensorAlertRepositoryMock.Object,
            _alertResolutionServiceMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithOutOfRangeReading_CreatesNewAlert()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var reading = new ReadingBuilder()
            .WithVariableCode("PH")
            .WithValue(5.0)
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var alert = new SensorAlert
        {
            VariableCode = "PH",
            Value = 5.0,
            Timestamp = timestamp
        };

        _variableRepositoryMock
            .Setup(x => x.GetByCodeAsync("PH"))
            .ReturnsAsync(variable);

        _alertCalculationServiceMock
            .Setup(x => x.CalculateOutOfRangeAlert(reading, variable, timestamp))
            .Returns(alert);

        _sensorAlertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync("PH"))
            .ReturnsAsync((SensorAlert?)null);

        // Act
        var result = await _sut.ExecuteAsync(new[] { reading }, timestamp);

        // Assert
        var alerts = result.ToList();
        alerts.Should().HaveCount(1);
        alerts[0].VariableCode.Should().Be("PH");
        alerts[0].Value.Should().Be(5.0);
    }

    [Fact]
    public async Task ExecuteAsync_WithOutOfRangeReading_AndExistingAlert_UpdatesExistingAlert()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var reading = new ReadingBuilder()
            .WithVariableCode("PH")
            .WithValue(5.2)
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var newAlert = new SensorAlert
        {
            VariableCode = "PH",
            Value = 5.2,
            Timestamp = timestamp
        };

        var existingAlert = new SensorAlert
        {
            VariableCode = "PH",
            Value = 5.0,
            Timestamp = timestamp.AddMinutes(-10),
            LastSeen = timestamp.AddMinutes(-10)
        };
        existingAlert.SetId("existing-alert-id");

        _variableRepositoryMock
            .Setup(x => x.GetByCodeAsync("PH"))
            .ReturnsAsync(variable);

        _alertCalculationServiceMock
            .Setup(x => x.CalculateOutOfRangeAlert(reading, variable, timestamp))
            .Returns(newAlert);

        _sensorAlertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync("PH"))
            .ReturnsAsync(existingAlert);

        // Act
        var result = await _sut.ExecuteAsync(new[] { reading }, timestamp);

        // Assert
        result.Should().BeEmpty(); // No new alerts created

        _sensorAlertRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<SensorAlert>(a =>
                a.Id == "existing-alert-id" &&
                a.LastSeen == timestamp &&
                a.LatestValue == 5.2
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithReadingInRange_ResolvesExistingAlert()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var reading = new ReadingBuilder()
            .WithVariableCode("PH")
            .WithValue(6.8)
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByCodeAsync("PH"))
            .ReturnsAsync(variable);

        _alertCalculationServiceMock
            .Setup(x => x.CalculateOutOfRangeAlert(reading, variable, timestamp))
            .Returns((SensorAlert?)null); // No alert

        // Act
        var result = await _sut.ExecuteAsync(new[] { reading }, timestamp);

        // Assert
        result.Should().BeEmpty();

        _alertResolutionServiceMock.Verify(
            x => x.ResolveOutOfRangeAlertsAsync(reading, variable, timestamp),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentVariable_SkipsReading()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var reading = new ReadingBuilder()
            .WithVariableCode("UNKNOWN_VAR")
            .WithValue(5.0)
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByCodeAsync("UNKNOWN_VAR"))
            .ReturnsAsync((Variable?)null);

        // Act
        var result = await _sut.ExecuteAsync(new[] { reading }, timestamp);

        // Assert
        result.Should().BeEmpty();

        _alertCalculationServiceMock.Verify(
            x => x.CalculateOutOfRangeAlert(It.IsAny<Reading>(), It.IsAny<Variable>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithAutomaticVariable_SkipsProcessing()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var reading = new ReadingBuilder()
            .WithVariableCode("T_AMB")
            .WithValue(22.5)
            .Build();

        var variable = new VariableBuilder()
            .WithCode("T_AMB")
            .WithRegulationType(RegulationType.Automatic) // Automatic, not Manual
            .Build();

        _variableRepositoryMock
            .Setup(x => x.GetByCodeAsync("T_AMB"))
            .ReturnsAsync(variable);

        // Act
        var result = await _sut.ExecuteAsync(new[] { reading }, timestamp);

        // Assert
        result.Should().BeEmpty();

        _alertCalculationServiceMock.Verify(
            x => x.CalculateOutOfRangeAlert(It.IsAny<Reading>(), It.IsAny<Variable>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleReadings_ProcessesAllManualVariables()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        var phReading = new ReadingBuilder()
            .WithVariableCode("PH")
            .WithValue(5.0)
            .Build();

        var ecReading = new ReadingBuilder()
            .WithVariableCode("EC")
            .WithValue(1.2)
            .Build();

        var tempReading = new ReadingBuilder()
            .WithVariableCode("T_AMB")
            .WithValue(22.5)
            .Build();

        var phVariable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var ecVariable = new VariableBuilder()
            .WithCode("EC")
            .WithOptimalMin(1.8)
            .WithOptimalMax(2.3)
            .WithRegulationType(RegulationType.Manual)
            .Build();

        var tempVariable = new VariableBuilder()
            .WithCode("T_AMB")
            .WithRegulationType(RegulationType.Automatic)
            .Build();

        var phAlert = new SensorAlert { VariableCode = "PH", Value = 5.0, Timestamp = timestamp };
        var ecAlert = new SensorAlert { VariableCode = "EC", Value = 1.2, Timestamp = timestamp };

        _variableRepositoryMock.Setup(x => x.GetByCodeAsync("PH")).ReturnsAsync(phVariable);
        _variableRepositoryMock.Setup(x => x.GetByCodeAsync("EC")).ReturnsAsync(ecVariable);
        _variableRepositoryMock.Setup(x => x.GetByCodeAsync("T_AMB")).ReturnsAsync(tempVariable);

        _alertCalculationServiceMock
            .Setup(x => x.CalculateOutOfRangeAlert(phReading, phVariable, timestamp))
            .Returns(phAlert);

        _alertCalculationServiceMock
            .Setup(x => x.CalculateOutOfRangeAlert(ecReading, ecVariable, timestamp))
            .Returns(ecAlert);

        _sensorAlertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((SensorAlert?)null);

        // Act
        var result = await _sut.ExecuteAsync(new[] { phReading, ecReading, tempReading }, timestamp);

        // Assert
        var alerts = result.ToList();
        alerts.Should().HaveCount(2);
        alerts.Should().Contain(a => a.VariableCode == "PH");
        alerts.Should().Contain(a => a.VariableCode == "EC");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyReadings_ReturnsEmptyList()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var readings = Enumerable.Empty<Reading>();

        // Act
        var result = await _sut.ExecuteAsync(readings, timestamp);

        // Assert
        result.Should().BeEmpty();
    }
}
