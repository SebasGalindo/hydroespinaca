using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using SensorService.Application.UseCases.Esp32Status;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Services;
using Xunit;

namespace SensorService.Tests.Integration;

public class Esp32StatusMqttIntegrationTests
{
    private readonly Mock<IEsp32AlertRepository> _mockAlertRepository;
    private readonly Mock<ILogger<HandleEsp32StatusUseCase>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly HandleEsp32StatusUseCase _useCase;

    public Esp32StatusMqttIntegrationTests()
    {
        _mockAlertRepository = new Mock<IEsp32AlertRepository>();
        _mockLogger = new Mock<ILogger<HandleEsp32StatusUseCase>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IHandleEsp32StatusUseCase))).Returns(_useCase);

        var mockEsp32NodeRepository = new Mock<IEsp32NodeRepository>();
        _useCase = new HandleEsp32StatusUseCase(_mockAlertRepository.Object, mockEsp32NodeRepository.Object, _mockLogger.Object);
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

    [Theory]
    [InlineData("online")]
    [InlineData("offline")]
    public async Task IntegrationTest_SimulateEsp32StatusFlow(string status)
    {
        // Arrange
        var esp32Id = "integration-test-esp32";
        var timestamp = DateTime.UtcNow;

        if (status == "offline")
        {
            // Simulate no existing alert for offline scenario
            _mockAlertRepository
                .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
                .ReturnsAsync((Esp32Alert?)null);
        }
        else
        {
            // Simulate existing alert for online scenario
            var existingAlert = new Esp32Alert
            {
                Esp32Id = esp32Id,
                Acknowledged = false,
                ResolvedAt = null
            };
            existingAlert.SetId("integration-alert-id");

            _mockAlertRepository
                .Setup(r => r.GetActiveByEsp32IdAsync(esp32Id))
                .ReturnsAsync(existingAlert);
        }

        // Act
        if (status == "offline")
        {
            await _useCase.HandleOfflineAsync(esp32Id, timestamp);
        }
        else
        {
            await _useCase.HandleOnlineAsync(esp32Id, timestamp);
        }

        // Assert
        if (status == "offline")
        {
            _mockAlertRepository.Verify(r => r.CreateAsync(It.IsAny<Esp32Alert>()), Times.Once);
        }
        else
        {
            _mockAlertRepository.Verify(r => r.UpdateAsync(It.IsAny<Esp32Alert>()), Times.Once);
        }
    }

    [Fact]
    public async Task CompleteFlowIntegrationTest_OfflineThenOnline()
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
}