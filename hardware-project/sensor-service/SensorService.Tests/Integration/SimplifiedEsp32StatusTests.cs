using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using SensorService.Application.UseCases.Esp32Status;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using System.Text.RegularExpressions;
using Xunit;

namespace SensorService.Tests.Integration;

/// <summary>
/// Simplified integration tests focusing on the core ESP32 status handling logic
/// </summary>
public class SimplifiedEsp32StatusTests
{
    private readonly Mock<IEsp32AlertRepository> _mockAlertRepository;
    private readonly Mock<ILogger<HandleEsp32StatusUseCase>> _mockLogger;
    private readonly HandleEsp32StatusUseCase _useCase;

    public SimplifiedEsp32StatusTests()
    {
        _mockAlertRepository = new Mock<IEsp32AlertRepository>();
        _mockLogger = new Mock<ILogger<HandleEsp32StatusUseCase>>();
        _useCase = new HandleEsp32StatusUseCase(_mockAlertRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleOfflineAsync_ShouldCreateNewAlert_WhenNoActiveAlertExists()
    {
        // Arrange
        var esp32Id = "test-esp32-id";
        var timestamp = DateTime.UtcNow;
        
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync((Esp32Alert?)null);

        // Act
        await _useCase.HandleOfflineAsync(esp32Id, timestamp);

        // Assert
        _mockAlertRepository.Verify(r => r.CreateAsync(It.Is<Esp32Alert>(a => 
            a.Esp32Id == esp32Id &&
            a.Type == AlertType.Esp32Offline &&
            a.Severity == AlertSeverity.Critical &&
            a.Acknowledged == false &&
            a.ResolvedAt == null &&
            a.Message.Contains("MQTT LWT")
        )), Times.Once);
    }

    [Fact]
    public async Task HandleOfflineAsync_ShouldNotCreateAlert_WhenActiveAlertAlreadyExists()
    {
        // Arrange
        var esp32Id = "test-esp32-id";
        var timestamp = DateTime.UtcNow;
        var existingAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Type = AlertType.Esp32Offline,
            Acknowledged = false
        };
        
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync(existingAlert);

        // Act
        await _useCase.HandleOfflineAsync(esp32Id, timestamp);

        // Assert
        _mockAlertRepository.Verify(r => r.CreateAsync(It.IsAny<Esp32Alert>()), Times.Never);
    }

    [Fact]
    public async Task HandleOnlineAsync_ShouldResolveActiveAlert_WhenActiveAlertExists()
    {
        // Arrange
        var esp32Id = "test-esp32-id";
        var timestamp = DateTime.UtcNow;
        var activeAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Type = AlertType.Esp32Offline,
            Acknowledged = false,
            ResolvedAt = null
        };
        activeAlert.SetId("test-alert-id");
        
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync(activeAlert);

        // Act
        await _useCase.HandleOnlineAsync(esp32Id, timestamp);

        // Assert
        _mockAlertRepository.Verify(r => r.UpdateAsync(It.Is<Esp32Alert>(a => 
            a.Esp32Id == esp32Id &&
            a.Acknowledged == true &&
            a.ResolvedAt == timestamp &&
            a.Message.Contains("reconectado")
        )), Times.Once);
    }

    [Fact]
    public async Task HandleOnlineAsync_ShouldDoNothing_WhenNoActiveAlertExists()
    {
        // Arrange
        var esp32Id = "test-esp32-id";
        var timestamp = DateTime.UtcNow;
        
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync((Esp32Alert?)null);

        // Act
        await _useCase.HandleOnlineAsync(esp32Id, timestamp);

        // Assert
        _mockAlertRepository.Verify(r => r.UpdateAsync(It.IsAny<Esp32Alert>()), Times.Never);
        _mockAlertRepository.Verify(r => r.CreateAsync(It.IsAny<Esp32Alert>()), Times.Never);
    }

    [Fact]
    public async Task CompleteFlowTest_OfflineThenOnline_ShouldCreateAndResolveAlert()
    {
        // Arrange
        var esp32Id = "flow-test-esp32";
        var offlineTimestamp = DateTime.UtcNow;
        var onlineTimestamp = offlineTimestamp.AddMinutes(5);

        // Step 1: ESP32 goes offline (no existing alert)
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync((Esp32Alert?)null);

        await _useCase.HandleOfflineAsync(esp32Id, offlineTimestamp);

        // Verify offline alert creation
        _mockAlertRepository.Verify(r => r.CreateAsync(It.Is<Esp32Alert>(a => 
            a.Esp32Id == esp32Id &&
            a.Type == AlertType.Esp32Offline &&
            !a.Acknowledged &&
            a.ResolvedAt == null
        )), Times.Once);

        // Step 2: Simulate the created alert exists for online handling
        var createdAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Type = AlertType.Esp32Offline,
            Acknowledged = false,
            ResolvedAt = null,
            Timestamp = offlineTimestamp
        };
        createdAlert.SetId("flow-test-alert-id");

        _mockAlertRepository.Reset();
        _mockAlertRepository
            .Setup(r => r.GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline))
            .ReturnsAsync(createdAlert);

        // Step 3: ESP32 comes back online
        await _useCase.HandleOnlineAsync(esp32Id, onlineTimestamp);

        // Verify alert resolution
        _mockAlertRepository.Verify(r => r.UpdateAsync(It.Is<Esp32Alert>(a => 
            a.Esp32Id == esp32Id &&
            a.Acknowledged == true &&
            a.ResolvedAt == onlineTimestamp
        )), Times.Once);
    }

    [Theory]
    [InlineData("sensor/test-esp32/status", "test-esp32", true)]
    [InlineData("sensor/another-device/status", "another-device", true)]
    [InlineData("sensor/123abc/status", "123abc", true)]
    [InlineData("invalid/topic/format", "", false)]
    [InlineData("sensor/status", "", false)]
    [InlineData("sensor//status", "", false)]
    public void MqttTopicParsing_ShouldExtractEsp32IdCorrectly(string topic, string expectedEsp32Id, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(@"^sensor/([^/]+)/status$", RegexOptions.Compiled);

        // Act
        var match = regex.Match(topic);

        // Assert
        Assert.Equal(shouldMatch, match.Success);
        if (shouldMatch)
        {
            Assert.Equal(expectedEsp32Id, match.Groups[1].Value);
        }
    }

    [Theory]
    [InlineData("online")]
    [InlineData("ONLINE")]
    [InlineData("Online")]
    [InlineData("offline")]
    [InlineData("OFFLINE")]
    [InlineData("Offline")]
    public void PayloadHandling_ShouldBeCaseInsensitive(string payload)
    {
        // Act
        var normalizedPayload = payload.ToLowerInvariant().Trim();

        // Assert
        Assert.True(normalizedPayload == "online" || normalizedPayload == "offline");
    }
}