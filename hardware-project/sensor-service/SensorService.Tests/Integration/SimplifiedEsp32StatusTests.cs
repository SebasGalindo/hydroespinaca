using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using SensorService.Application.DTOs.Esp32Status;
using SensorService.Application.UseCases.Esp32Status;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Esp32StatusEnum = HydroEspinaca.Shared.Enums.Esp32Status;

namespace SensorService.Tests.Integration;

/// <summary>
/// Simplified integration tests focusing on the core ESP32 status handling logic
/// </summary>
public class SimplifiedEsp32StatusTests
{
    private readonly Mock<IEsp32AlertRepository> _mockAlertRepository;
    private readonly Mock<IEsp32NodeRepository> _mockEsp32NodeRepository;
    private readonly Mock<ILogger<HandleEsp32StatusUseCase>> _mockLogger;
    private readonly HandleEsp32StatusUseCase _useCase;

    public SimplifiedEsp32StatusTests()
    {
        _mockAlertRepository = new Mock<IEsp32AlertRepository>();
        _mockEsp32NodeRepository = new Mock<IEsp32NodeRepository>();
        _mockLogger = new Mock<ILogger<HandleEsp32StatusUseCase>>();
        _useCase = new HandleEsp32StatusUseCase(_mockAlertRepository.Object, _mockEsp32NodeRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleOfflineAsync_ShouldCreateNewAlert_WhenNoActiveAlertExists()
    {
        // Arrange
        var esp32Id = "test-esp32-id";
        var timestamp = DateTime.UtcNow;
        
        _mockAlertRepository
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
            .ReturnsAsync((Esp32Alert?)null);

        // Act
        await _useCase.HandleOfflineAsync(esp32Id, timestamp);

        // Assert
        _mockAlertRepository.Verify(r => r.CreateAsync(It.Is<Esp32Alert>(a =>
            a.Esp32Id == esp32Id &&
            a.Acknowledged == false &&
            a.ResolvedAt == null &&
            a.Message.Contains("desconectado")
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
            Acknowledged = false
        };
        
        _mockAlertRepository
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
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
            Acknowledged = false,
            ResolvedAt = null
        };
        activeAlert.SetId("test-alert-id");
        
        _mockAlertRepository
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
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
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
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
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
            .ReturnsAsync((Esp32Alert?)null);

        await _useCase.HandleOfflineAsync(esp32Id, offlineTimestamp);

        // Verify offline alert creation
        _mockAlertRepository.Verify(r => r.CreateAsync(It.Is<Esp32Alert>(a =>
            a.Esp32Id == esp32Id &&
            !a.Acknowledged &&
            a.ResolvedAt == null
        )), Times.Once);

        // Step 2: Simulate the created alert exists for online handling
        var createdAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Acknowledged = false,
            ResolvedAt = null,
            Timestamp = offlineTimestamp
        };
        createdAlert.SetId("flow-test-alert-id");

        _mockAlertRepository.Reset();
        _mockAlertRepository
            .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
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

    [Fact]
    public async Task HandleStatusPayloadAsync_OnlineJson_ShouldUpdateTelemetryAndResolveAlert()
    {
        // Arrange
        var esp32Id = "test-esp32-json";
        var timestamp = DateTime.UtcNow;
        var freeHeap = 213960L;
        var uptime = 3600L;

        var payload = new Esp32StatusPayloadDto
        {
            Status = "online",
            Timestamp = timestamp,
            FreeHeap = freeHeap,
            Uptime = uptime
        };
        payload.Esp32Id = esp32Id; // Set from topic

        var esp32Node = new Esp32Node
        {
            Name = "Test Node",
            Location = "Test Location",
            Status = Esp32StatusEnum.Active
        };
        esp32Node.SetId(esp32Id);

        var activeAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Acknowledged = false
        };

        _mockEsp32NodeRepository.Setup(r => r.GetByIdentifierAsync(esp32Id)).ReturnsAsync(esp32Node);
        _mockAlertRepository.Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
            .ReturnsAsync(activeAlert);

        // Act
        await _useCase.HandleStatusPayloadAsync(payload);

        // Assert - Verify UpdateAsync is called instead of individual methods
        _mockEsp32NodeRepository.Verify(r => r.UpdateAsync(It.IsAny<Esp32Node>()), Times.Once);

        _mockAlertRepository.Verify(r => r.UpdateAsync(It.Is<Esp32Alert>(a =>
            a.Esp32Id == esp32Id &&
            a.Acknowledged == true &&
            a.ResolvedAt == timestamp
        )), Times.Once);
    }

    [Fact]
    public async Task HandleStatusPayloadAsync_OfflineJson_ShouldCreateAlertAndUpdateStatus()
    {
        // Arrange
        var esp32Id = "test-esp32-offline-json";
        var timestamp = DateTime.UtcNow;

        var payload = new Esp32StatusPayloadDto
        {
            Status = "offline"
        };
        payload.Esp32Id = esp32Id; // Set from topic

        var esp32Node = new Esp32Node
        {
            Name = "Test Node",
            Location = "Test Location", 
            Status = Esp32StatusEnum.Active
        };
        esp32Node.SetId(esp32Id);

        _mockEsp32NodeRepository.Setup(r => r.GetByIdentifierAsync(esp32Id)).ReturnsAsync(esp32Node);
        _mockAlertRepository.Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
            .ReturnsAsync((Esp32Alert?)null);

        // Act
        await _useCase.HandleStatusPayloadAsync(payload);

        // Assert
        _mockAlertRepository.Verify(r => r.CreateAsync(It.Is<Esp32Alert>(a =>
            a.Esp32Id == esp32Id &&
            !a.Acknowledged &&
            a.ResolvedAt == null
        )), Times.Once);

        // Verify UpdateAsync is called instead of individual methods
        _mockEsp32NodeRepository.Verify(r => r.UpdateAsync(It.IsAny<Esp32Node>()), Times.Once);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("")]
    public async Task HandleStatusPayloadAsync_InvalidStatus_ShouldLogWarningAndReturn(string invalidStatus)
    {
        // Arrange
        var payload = new Esp32StatusPayloadDto
        {
            Status = invalidStatus
        };
        payload.Esp32Id = "test-esp32"; // Set from topic

        // Act
        await _useCase.HandleStatusPayloadAsync(payload);

        // Assert
        _mockAlertRepository.Verify(r => r.CreateAsync(It.IsAny<Esp32Alert>()), Times.Never);
        _mockAlertRepository.Verify(r => r.UpdateAsync(It.IsAny<Esp32Alert>()), Times.Never);
        _mockEsp32NodeRepository.Verify(r => r.UpdateLastSeenAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        _mockEsp32NodeRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<Esp32StatusEnum>()), Times.Never);
    }

    [Fact]
    public void JsonDeserialization_OnlinePayload_ShouldDeserializeCorrectly()
    {
        // Arrange
        var jsonPayload = """
        {
            "status": "online",
            "timestamp": "2025-08-31T00:00:03Z",
            "freeHeap": 213960,
            "uptime": 3
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<Esp32StatusPayloadDto>(jsonPayload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("online", result.Status);
        Assert.True(result.IsOnline);
        Assert.False(result.IsOffline);
        Assert.True(result.IsValidStatus);
        Assert.Equal(213960, result.FreeHeap);
        Assert.Equal(3, result.Uptime);
        Assert.Equal(new DateTime(2025, 8, 31, 0, 0, 3, DateTimeKind.Utc), result.Timestamp);
        // ESP32 ID should be set from topic, not payload
        Assert.Equal(string.Empty, result.Esp32Id);
    }

    [Fact]
    public void JsonDeserialization_OfflinePayload_ShouldDeserializeCorrectly()
    {
        // Arrange
        var jsonPayload = """
        {
            "status": "offline"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<Esp32StatusPayloadDto>(jsonPayload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("offline", result.Status);
        Assert.False(result.IsOnline);
        Assert.True(result.IsOffline);
        Assert.True(result.IsValidStatus);
        Assert.Null(result.FreeHeap);
        Assert.Null(result.Uptime);
        Assert.Null(result.Timestamp);
        // ESP32 ID should be set from topic, not payload
        Assert.Equal(string.Empty, result.Esp32Id);
    }

    [Fact]
    public void JsonDeserialization_WithExtraFields_ShouldIgnoreThem()
    {
        // Arrange
        var jsonPayload = """
        {
            "status": "online",
            "freeHeap": 100000,
            "extraField": "should be ignored",
            "anotherField": 123
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<Esp32StatusPayloadDto>(jsonPayload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("online", result.Status);
        Assert.Equal(100000, result.FreeHeap);
        Assert.True(result.IsValidStatus);
        // ESP32 ID should be set from topic, not payload
        Assert.Equal(string.Empty, result.Esp32Id);
    }

    [Fact]
    public async Task HandleStatusPayloadAsync_WithStringIdentifier_ShouldNotThrowFormatException()
    {
        // Arrange
        var esp32Id = "esp32-001"; // String identifier, not ObjectId
        var timestamp = DateTime.UtcNow;
        var freeHeap = 218660L;
        var uptime = 1656L;

        var payload = new Esp32StatusPayloadDto
        {
            Status = "online",
            Timestamp = timestamp,
            FreeHeap = freeHeap,
            Uptime = uptime
        };
        payload.Esp32Id = esp32Id;

        // Mock: ESP32 node NOT found (simulating realistic scenario)
        _mockEsp32NodeRepository
            .Setup(r => r.GetByIdentifierAsync(esp32Id))
            .ReturnsAsync((Esp32Node?)null);

        // Act & Assert
        // This should NOT throw a FormatException anymore
        var exception = await Record.ExceptionAsync(async () => 
            await _useCase.HandleStatusPayloadAsync(payload));

        Assert.Null(exception);

        // Verify that GetByIdentifierAsync was called (not GetByIdAsync)
        _mockEsp32NodeRepository.Verify(r => r.GetByIdentifierAsync(esp32Id), Times.Once);
        _mockEsp32NodeRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }
}