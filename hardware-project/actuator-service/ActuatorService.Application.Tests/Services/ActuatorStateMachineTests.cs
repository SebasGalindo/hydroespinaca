using ActuatorService.Application.Services;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Tests.Services;

/// <summary>
/// Tests unitarios para ActuatorStateMachine
/// Cobertura: gestión de estados de actuadores, inicialización, actualización y reset
/// </summary>
public class ActuatorStateMachineTests
{
    private readonly ActuatorStateMachine _stateMachine;
    private readonly Mock<ILogger<ActuatorStateMachine>> _mockLogger;

    public ActuatorStateMachineTests()
    {
        _mockLogger = new Mock<ILogger<ActuatorStateMachine>>();
        _stateMachine = new ActuatorStateMachine(_mockLogger.Object);
    }

    [Fact]
    public void InitializeActuator_ShouldCreateNewState()
    {
        // Arrange
        const string actuatorId = "led-001";
        const string esp32Id = "esp32-001";
        const string pin = "GPIO_5";
        const ActuatorMode mode = ActuatorMode.DIGITAL;

        // Act
        _stateMachine.InitializeActuator(actuatorId, esp32Id, pin, mode);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.ActuatorId.Should().Be(actuatorId);
        state.Esp32Id.Should().Be(esp32Id);
        state.Pin.Should().Be(pin);
        state.Mode.Should().Be(mode);
        state.State.Should().Be(PowerState.OFF);
    }

    [Fact]
    public void InitializeActuator_ShouldUpdateExistingState()
    {
        // Arrange
        const string actuatorId = "led-001";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_5", ActuatorMode.DIGITAL);

        // Act - reinitialize with different values
        _stateMachine.InitializeActuator(actuatorId, "esp32-002", "GPIO_10", ActuatorMode.PWM);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.Esp32Id.Should().Be("esp32-002");
        state.Pin.Should().Be("GPIO_10");
        state.Mode.Should().Be(ActuatorMode.PWM);
    }

    [Fact]
    public void UpdateState_ShouldCreateStateIfNotInitialized()
    {
        // Arrange
        const string actuatorId = "pump-001";
        const PowerState newState = PowerState.ON;
        const double duration = 120.0;

        // Act
        _stateMachine.UpdateState(actuatorId, newState, duration);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.State.Should().Be(PowerState.ON);
        state.RemainingDuration.Should().Be(duration);
    }

    [Fact]
    public void UpdateState_ShouldUpdateExistingState()
    {
        // Arrange
        const string actuatorId = "pump-001";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_15", ActuatorMode.DIGITAL);

        // Act
        _stateMachine.UpdateState(actuatorId, PowerState.ON, duration: 60.0, commandId: "cmd-123");

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.State.Should().Be(PowerState.ON);
        state.RemainingDuration.Should().Be(60.0);
        state.LastCommandId.Should().Be("cmd-123");
        state.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateState_WithDutyCycle_ShouldUpdatePwmState()
    {
        // Arrange
        const string actuatorId = "fan-001";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_20", ActuatorMode.PWM);

        // Act
        _stateMachine.UpdateState(actuatorId, PowerState.ON, duration: 300.0, dutyCycle: 75.5);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.State.Should().Be(PowerState.ON);
        state.DutyCycle.Should().Be(75.5);
        state.RemainingDuration.Should().Be(300.0);
    }

    [Fact]
    public void GetState_WhenActuatorNotExists_ShouldReturnNull()
    {
        // Arrange
        const string nonExistentId = "non-existent-actuator";

        // Act
        var state = _stateMachine.GetState(nonExistentId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetAllStates_ShouldReturnAllInitializedStates()
    {
        // Arrange
        _stateMachine.InitializeActuator("led-001", "esp32-001", "GPIO_5", ActuatorMode.DIGITAL);
        _stateMachine.InitializeActuator("pump-001", "esp32-001", "GPIO_15", ActuatorMode.DIGITAL);
        _stateMachine.InitializeActuator("fan-001", "esp32-002", "GPIO_20", ActuatorMode.PWM);

        // Act
        var allStates = _stateMachine.GetAllStates();

        // Assert
        allStates.Should().HaveCount(3);
        allStates.Should().ContainKey("led-001");
        allStates.Should().ContainKey("pump-001");
        allStates.Should().ContainKey("fan-001");
    }

    [Fact]
    public void ResetAll_ShouldClearAllStates()
    {
        // Arrange
        _stateMachine.InitializeActuator("led-001", "esp32-001", "GPIO_5", ActuatorMode.DIGITAL);
        _stateMachine.InitializeActuator("pump-001", "esp32-001", "GPIO_15", ActuatorMode.DIGITAL);
        
        // Act
        _stateMachine.ResetAll();

        // Assert
        var allStates = _stateMachine.GetAllStates();
        allStates.Should().BeEmpty();
    }

    [Fact]
    public void UpdateState_MultipleUpdates_ShouldMaintainLatestState()
    {
        // Arrange
        const string actuatorId = "led-001";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_5", ActuatorMode.DIGITAL);

        // Act
        _stateMachine.UpdateState(actuatorId, PowerState.ON, 60.0, commandId: "cmd-1");
        _stateMachine.UpdateState(actuatorId, PowerState.ON, 120.0, commandId: "cmd-2");
        _stateMachine.UpdateState(actuatorId, PowerState.OFF, 0.0, commandId: "cmd-3");

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.State.Should().Be(PowerState.OFF);
        state.RemainingDuration.Should().Be(0.0);
        state.LastCommandId.Should().Be("cmd-3");
    }

    [Theory]
    [InlineData(PowerState.ON)]
    [InlineData(PowerState.OFF)]
    public void UpdateState_DifferentPowerStates_ShouldBeHandledCorrectly(PowerState powerState)
    {
        // Arrange
        const string actuatorId = "test-actuator";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_10", ActuatorMode.DIGITAL);

        // Act
        _stateMachine.UpdateState(actuatorId, powerState, 30.0);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.State.Should().Be(powerState);
    }

    [Fact]
    public async Task InitializeActuator_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        const string actuatorId = "concurrent-test";
        const int threadCount = 10;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var esp32Id = $"esp32-{i:D3}";
            var pin = $"GPIO_{i}";
            tasks.Add(Task.Run(() => 
                _stateMachine.InitializeActuator(actuatorId, esp32Id, pin, ActuatorMode.DIGITAL)
            ));
        }

        await Task.WhenAll(tasks);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        state!.ActuatorId.Should().Be(actuatorId);
        // El estado final debería ser uno de los valores establecidos
        state.Esp32Id.Should().MatchRegex(@"^esp32-\d{3}$");
    }

    [Fact]
    public async Task UpdateState_ConcurrentUpdates_ShouldBeThreadSafe()
    {
        // Arrange
        const string actuatorId = "concurrent-update-test";
        _stateMachine.InitializeActuator(actuatorId, "esp32-001", "GPIO_5", ActuatorMode.DIGITAL);
        const int updateCount = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < updateCount; i++)
        {
            var commandId = $"cmd-{i:D4}";
            var powerState = i % 2 == 0 ? PowerState.ON : PowerState.OFF;
            tasks.Add(Task.Run(() => 
                _stateMachine.UpdateState(actuatorId, powerState, 10.0, commandId: commandId)
            ));
        }

        await Task.WhenAll(tasks);

        // Assert
        var state = _stateMachine.GetState(actuatorId);
        state.Should().NotBeNull();
        // Debería tener uno de los commandIds asignados
        state!.LastCommandId.Should().MatchRegex(@"^cmd-\d{4}$");
    }
}
