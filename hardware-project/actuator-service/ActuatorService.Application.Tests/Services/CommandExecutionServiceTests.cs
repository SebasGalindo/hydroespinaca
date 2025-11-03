using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Tests.Services;

/// <summary>
/// Tests unitarios para CommandExecutionService
/// Cobertura: scheduling de comandos, gestión de cola, resolución de conflictos de pines
/// </summary>
public class CommandExecutionServiceTests
{
    private readonly Mock<IPinLockRegistry> _mockPinLockRegistry;
    private readonly Mock<IActuatorStateMachine> _mockStateMachine;
    private readonly Mock<IRoutineCommandPublisher> _mockMqttPublisher;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<CommandExecutionService>> _mockLogger;
    private readonly CommandExecutionService _service;

    public CommandExecutionServiceTests()
    {
        _mockPinLockRegistry = new Mock<IPinLockRegistry>();
        _mockStateMachine = new Mock<IActuatorStateMachine>();
        _mockMqttPublisher = new Mock<IRoutineCommandPublisher>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<CommandExecutionService>>();

        _service = new CommandExecutionService(
            _mockPinLockRegistry.Object,
            _mockStateMachine.Object,
            _mockMqttPublisher.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WhenPinAvailable_ShouldActivateImmediately()
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60)
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Contain("BombaRiego");

        _mockPinLockRegistry.Verify(r => r.TryLock(
            It.Is<List<string>>(pins => pins.Contains("GPIO_15")),
            It.IsAny<string>()), Times.Once);

        _mockStateMachine.Verify(s => s.UpdateState(
            "actuator-001",
            PowerState.ON,
            60,
            null,
            It.IsAny<string>()), Times.Once);

        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(
            It.Is<JobScheduleDto>(dto => dto.Esp32Id == esp32Id && dto.Queue.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WhenPinLocked_ShouldAddToPending()
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60)
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Assert
        result.Should().HaveCount(1);

        // Should not publish to MQTT (command is pending)
        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(It.IsAny<JobScheduleDto>()), Times.Never);

        // Should not update state machine (command is pending)
        _mockStateMachine.Verify(s => s.UpdateState(
            It.IsAny<string>(),
            It.IsAny<PowerState>(),
            It.IsAny<double?>(),
            It.IsAny<double?>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleCommandsAsync_MultipleCommands_AllAvailable_ShouldActivateAll()
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60),
            CreateResolvedCommand("BombaAire", "actuator-002", "GPIO_20", "ON", 30),
            CreateResolvedCommand("Ventiladores", "actuator-003", "GPIO_25", "ON", 120)
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Assert
        result.Should().HaveCount(3);

        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(
            It.Is<JobScheduleDto>(dto => dto.Queue.Count == 3)),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WhenPinHasRunningAndPending_ShouldRejectCommand()
    {
        // Arrange
        const string pin = "GPIO_15";
        const string esp32Id = "esp32-001";
        
        var command1 = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", pin, "ON", 60)
        };
        
        var command2 = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego2", "actuator-002", pin, "ON", 30)
        };
        
        var command3 = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego3", "actuator-003", pin, "ON", 45)
        };

        // SetupSequence: First call returns true (becomes running), second returns false (becomes pending), third fails check
        _mockPinLockRegistry.SetupSequence(r => r.TryLock(It.Is<List<string>>(pins => pins.Contains(pin)), It.IsAny<string>()))
            .Returns(true)   // First command: lock succeeds
            .Returns(false); // Second command: lock fails, goes to pending
        
        // Act
        var firstResult = await _service.ScheduleCommandsAsync(command1, esp32Id);
        var secondResult = await _service.ScheduleCommandsAsync(command2, esp32Id);
        var thirdResult = await _service.ScheduleCommandsAsync(command3, esp32Id);

        // Assert
        firstResult.Should().HaveCount(1, "first command should be scheduled");
        secondResult.Should().HaveCount(1, "second command should be added to pending");
        thirdResult.Should().BeEmpty("third command should be rejected (1 running + 1 pending already)");
    }

    [Fact]
    public async Task OnCommandCompletedAsync_ShouldReleasePin_AndUpdateState()
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60)
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        var scheduledIds = await _service.ScheduleCommandsAsync(commands, esp32Id);
        var commandId = scheduledIds[0];

        // Act
        await _service.OnCommandCompletedAsync(commandId);

        // Assert
        _mockPinLockRegistry.Verify(r => r.Release(
            It.Is<List<string>>(pins => pins.Contains("GPIO_15"))),
            Times.Once);

        _mockStateMachine.Verify(s => s.UpdateState(
            "actuator-001",
            PowerState.OFF,
            0,
            null,
            commandId), Times.Once);
    }

    [Fact]
    public async Task OnCommandCompletedAsync_WhenPendingCommandsExist_ShouldActivateNext()
    {
        // Arrange
        const string pin = "GPIO_15";
        const string esp32Id = "esp32-001";

        // First command - should activate immediately
        var firstCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", pin, "ON", 60)
        };

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        var firstCommandIds = await _service.ScheduleCommandsAsync(firstCommand, esp32Id);

        // Second command - DIFFERENT ACTUATOR, same pin - should go to pending (pin locked)
        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(false);

        var secondCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaAire", "actuator-002", pin, "ON", 30)
        };

        await _service.ScheduleCommandsAsync(secondCommand, esp32Id);

        // Reset MQTT publisher mock to track new calls
        _mockMqttPublisher.Reset();

        // Act - Complete first command
        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        await _service.OnCommandCompletedAsync(firstCommandIds[0]);

        // Assert - Second command should be activated
        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(
            It.Is<JobScheduleDto>(dto => dto.Queue.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnActiveAndPendingCommands()
    {
        // Arrange
        const string esp32Id = "esp32-001";
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60),
            CreateResolvedCommand("BombaAire", "actuator-002", "GPIO_20", "ON", 30)
        };

        _mockPinLockRegistry.SetupSequence(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true)  // First command activates
            .Returns(false); // Second command goes to pending

        await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Act
        var status = await _service.GetStatusAsync(esp32Id);

        // Assert
        status.Should().NotBeNull();
        status.Esp32Id.Should().Be(esp32Id);
        status.Queue.Should().HaveCount(2);
        status.Queue.Should().Contain(cmd => cmd.Status == "running");
        status.Queue.Should().Contain(cmd => cmd.Status == "scheduled");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        const string esp32Id = "esp32-001";
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60),
            CreateResolvedCommand("BombaAire", "actuator-002", "GPIO_20", "ON", 30),
            CreateResolvedCommand("Ventiladores", "actuator-003", "GPIO_25", "ON", 120)
        };

        _mockPinLockRegistry.SetupSequence(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true)  // First activates
            .Returns(false) // Second pending
            .Returns(false); // Third pending

        _mockPinLockRegistry.Setup(r => r.GetAllLocks())
            .Returns(new Dictionary<string, string> { { "GPIO_15", "cmd-1" } });

        await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.ActiveCount.Should().Be(1);
        stats.PendingCount.Should().Be(2);
        stats.TotalLockedPins.Should().Be(1);
        stats.Esp32Ids.Should().Contain(esp32Id);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllCommands_AndReleasePins()
    {
        // Arrange
        const string esp32Id = "esp32-001";
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", "ON", 60),
            CreateResolvedCommand("BombaAire", "actuator-002", "GPIO_20", "ON", 30)
        };

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        // Mock IRoutineCommandRepository
        var mockRoutineCommandRepository = new Mock<IRoutineCommandRepository>();
        mockRoutineCommandRepository
            .Setup(r => r.DeleteRunningCommandsAsync(It.IsAny<string>()))
            .ReturnsAsync(0);

        // Mock IServiceScope and IServiceProvider
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IRoutineCommandRepository)))
            .Returns(mockRoutineCommandRepository.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope
            .Setup(s => s.ServiceProvider)
            .Returns(mockServiceProvider.Object);

        _mockScopeFactory
            .Setup(sf => sf.CreateScope())
            .Returns(mockScope.Object);

        await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Act
        await _service.ClearAsync(esp32Id);

        // Assert
        _mockPinLockRegistry.Verify(r => r.Release(It.IsAny<List<string>>()), Times.AtLeast(2));
        mockRoutineCommandRepository.Verify(r => r.DeleteRunningCommandsAsync(esp32Id), Times.Once);

        var status = await _service.GetStatusAsync(esp32Id);
        status.Queue.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WithPwmCommand_ShouldIncludeDutyCycle()
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            new ResolvedCommandDto
            {
                ActuatorCode = "Ventiladores",
                ActuatorId = "actuator-003",
                Esp32Id = "esp32-001",
                Pin = "GPIO_25",
                Mode = ActuatorMode.PWM,
                Power = null,
                DutyCycle = 75.0,
                Duration = 120
            }
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Assert
        _mockStateMachine.Verify(s => s.UpdateState(
            "actuator-003",
            PowerState.ON,
            120,
            75.0,
            It.IsAny<string>()), Times.Once);

        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(
            It.Is<JobScheduleDto>(dto => 
                dto.Queue.Any(j => j.Steps.Any(step => step.DutyCycle == 75.0)))),
            Times.Once);
    }

    [Fact]
    public async Task OnCommandCompletedAsync_WithNonExistentCommand_ShouldThrowException()
    {
        // Arrange
        const string nonExistentCommandId = "non-existent-command-id";

        // Act
        var act = async () => await _service.OnCommandCompletedAsync(nonExistentCommandId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Command {nonExistentCommandId} not found in active commands");
    }

    [Theory]
    [InlineData("ON")]
    [InlineData("OFF")]
    public async Task ScheduleCommandsAsync_DifferentPowerStates_ShouldHandleCorrectly(string power)
    {
        // Arrange
        var commands = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand("BombaRiego", "actuator-001", "GPIO_15", power, 60)
        };
        const string esp32Id = "esp32-001";

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _service.ScheduleCommandsAsync(commands, esp32Id);

        // Assert
        // CommandExecutionService always sets state to ON when scheduling,
        // regardless of power value. OFF state is set when command completes.
        _mockStateMachine.Verify(s => s.UpdateState(
            "actuator-001",
            PowerState.ON,
            60,
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WhenExtendingActiveCommand_ShouldRePublishToMqtt()
    {
        // Arrange
        const string esp32Id = "esp32-001";
        const string actuatorCode = "luz-amplio-espectro";
        const string actuatorId = "actuator-001";
        const string pin = "GPIO_18";

        // First command - should activate immediately
        var firstCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand(actuatorCode, actuatorId, pin, "ON", 60)
        };

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(true);

        var firstCommandIds = await _service.ScheduleCommandsAsync(firstCommand, esp32Id);

        // Reset mocks to track new calls
        _mockMqttPublisher.Reset();
        _mockStateMachine.Reset();

        // Act - Second command for same actuator with extended duration
        var extendedCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand(actuatorCode, actuatorId, pin, "ON", 195)
        };

        var extendedCommandIds = await _service.ScheduleCommandsAsync(extendedCommand, esp32Id);

        // Assert
        // Should return the same command ID (consolidated)
        extendedCommandIds.Should().HaveCount(1);
        extendedCommandIds[0].Should().Be(firstCommandIds[0], "should reuse existing command ID");

        // Should re-publish to MQTT with new duration
        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(
            It.Is<JobScheduleDto>(dto =>
                dto.Esp32Id == esp32Id &&
                dto.Queue.Count == 1 &&
                dto.Queue[0].CommandId == firstCommandIds[0] &&
                dto.Queue[0].Steps[0].Duration == 195)),
            Times.Once,
            "should re-publish extended command to MQTT");

        // Should update state machine with new duration
        _mockStateMachine.Verify(s => s.UpdateState(
            actuatorId,
            PowerState.ON,
            195,
            null,
            firstCommandIds[0]),
            Times.Once,
            "should update state machine with new duration");
    }

    [Fact]
    public async Task ScheduleCommandsAsync_WhenExtendingPendingCommand_ShouldNotRePublishToMqtt()
    {
        // Arrange
        const string esp32Id = "esp32-001";
        const string actuatorCode = "BombaRiego";
        const string actuatorId = "actuator-001";
        const string pin = "GPIO_15";

        // First command - should go to pending (pin locked)
        var firstCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand(actuatorCode, actuatorId, pin, "ON", 60)
        };

        _mockPinLockRegistry.Setup(r => r.TryLock(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(false);

        var firstCommandIds = await _service.ScheduleCommandsAsync(firstCommand, esp32Id);

        // Reset mocks
        _mockMqttPublisher.Reset();

        // Act - Second command for same actuator
        var extendedCommand = new List<ResolvedCommandDto>
        {
            CreateResolvedCommand(actuatorCode, actuatorId, pin, "ON", 120)
        };

        var extendedCommandIds = await _service.ScheduleCommandsAsync(extendedCommand, esp32Id);

        // Assert
        extendedCommandIds.Should().HaveCount(1);
        extendedCommandIds[0].Should().Be(firstCommandIds[0], "should reuse existing pending command ID");

        // Should NOT re-publish to MQTT (pending commands are not sent until activated)
        _mockMqttPublisher.Verify(p => p.PublishJobScheduleAsync(It.IsAny<JobScheduleDto>()),
            Times.Never,
            "should not publish pending commands");
    }

    // Helper method
    private static ResolvedCommandDto CreateResolvedCommand(
        string actuatorCode,
        string actuatorId,
        string pin,
        string power,
        double duration)
    {
        return new ResolvedCommandDto
        {
            ActuatorCode = actuatorCode,
            ActuatorId = actuatorId,
            Esp32Id = "esp32-001",
            Pin = pin,
            Mode = ActuatorMode.DIGITAL,
            Power = power,
            DutyCycle = null,
            Duration = duration
        };
    }
}
