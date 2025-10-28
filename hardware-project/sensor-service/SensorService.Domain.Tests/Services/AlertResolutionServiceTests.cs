using Moq;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Services;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Tests.Builders;

namespace SensorService.Domain.Tests.Services;

public class AlertResolutionServiceTests
{
    private readonly Mock<ISensorAlertRepository> _alertRepositoryMock;
    private readonly Mock<IAlertCalculationService> _alertCalculationServiceMock;
    private readonly Mock<ILogger<AlertResolutionService>> _loggerMock;
    private readonly AlertResolutionService _sut;

    public AlertResolutionServiceTests()
    {
        _alertRepositoryMock = new Mock<ISensorAlertRepository>();
        _alertCalculationServiceMock = new Mock<IAlertCalculationService>();
        _loggerMock = new Mock<ILogger<AlertResolutionService>>();

        _sut = new AlertResolutionService(
            _alertRepositoryMock.Object,
            _alertCalculationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ResolveOutOfRangeAlertsAsync_WhenValueStillOutOfRange_DoesNotResolveAlert()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(5.0)
            .WithVariableCode("PH")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .WithOptimalMin(6.0)
            .WithOptimalMax(7.5)
            .Build();

        _alertCalculationServiceMock
            .Setup(x => x.IsValueWithinOptimalRange(5.0, variable))
            .Returns(false);

        // Act
        await _sut.ResolveOutOfRangeAlertsAsync(reading, variable, DateTime.UtcNow);

        // Assert
        _alertRepositoryMock.Verify(
            x => x.GetActiveByVariableCodeAsync(It.IsAny<string>()),
            Times.Never
        );
        _alertRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<SensorAlert>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ResolveOutOfRangeAlertsAsync_WhenValueBackInRange_AndActiveAlertExists_ResolvesAlert()
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

        var activeAlert = new SensorAlert
        {
            VariableCode = "PH",
            Value = 5.0,
            Acknowledged = false,
            ResolvedAt = null
        };
        activeAlert.SetId("alert123");

        var timestamp = new DateTime(2025, 10, 25, 10, 30, 0, DateTimeKind.Utc);

        _alertCalculationServiceMock
            .Setup(x => x.IsValueWithinOptimalRange(6.8, variable))
            .Returns(true);

        _alertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync("PH"))
            .ReturnsAsync(activeAlert);

        // Act
        await _sut.ResolveOutOfRangeAlertsAsync(reading, variable, timestamp);

        // Assert
        _alertRepositoryMock.Verify(
            x => x.GetActiveByVariableCodeAsync("PH"),
            Times.Once
        );

        _alertRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<SensorAlert>(a =>
                a.Id == "alert123" &&
                a.ResolvedAt == timestamp &&
                a.Acknowledged == true
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task ResolveOutOfRangeAlertsAsync_WhenValueBackInRange_AndNoActiveAlert_DoesNothing()
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

        _alertCalculationServiceMock
            .Setup(x => x.IsValueWithinOptimalRange(6.8, variable))
            .Returns(true);

        _alertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync("PH"))
            .ReturnsAsync((SensorAlert?)null);

        // Act
        await _sut.ResolveOutOfRangeAlertsAsync(reading, variable, DateTime.UtcNow);

        // Assert
        _alertRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<SensorAlert>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ResolveOutOfRangeAlertsAsync_WhenResolvingAlert_LogsInformation()
    {
        // Arrange
        var reading = new ReadingBuilder()
            .WithValue(6.8)
            .WithVariableCode("PH")
            .Build();

        var variable = new VariableBuilder()
            .WithCode("PH")
            .Build();

        var activeAlert = new SensorAlert
        {
            VariableCode = "PH",
            Value = 5.0
        };
        activeAlert.SetId("alert123");

        _alertCalculationServiceMock
            .Setup(x => x.IsValueWithinOptimalRange(It.IsAny<double>(), It.IsAny<Variable>()))
            .Returns(true);

        _alertRepositoryMock
            .Setup(x => x.GetActiveByVariableCodeAsync("PH"))
            .ReturnsAsync(activeAlert);

        // Act
        await _sut.ResolveOutOfRangeAlertsAsync(reading, variable, DateTime.UtcNow);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("resolved")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}
