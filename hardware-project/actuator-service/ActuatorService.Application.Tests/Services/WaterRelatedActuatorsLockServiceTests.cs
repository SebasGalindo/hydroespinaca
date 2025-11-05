using ActuatorService.Application.Services;

namespace ActuatorService.Application.Tests.Services;

/// <summary>
/// Tests para WaterRelatedActuatorsLockService
/// Valida el comportamiento del bloqueo de actuadores críticos relacionados con agua
/// </summary>
public class WaterRelatedActuatorsLockServiceTests
{
    private readonly WaterRelatedActuatorsLockService _service;

    public WaterRelatedActuatorsLockServiceTests()
    {
        _service = new WaterRelatedActuatorsLockService();
    }

    [Fact]
    public void Should_ActivateLock_When_BothActuatorsAreOff()
    {
        // Arrange
        var commands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };

        // Act
        _service.EvaluateCommands(commands);

        // Assert
        Assert.True(_service.IsLocked);
        Assert.NotNull(_service.LockedSince);
    }

    [Fact]
    public void Should_NotActivateLock_When_OnlyOneActuatorIsOff()
    {
        // Arrange
        var commands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "ON")
        };

        // Act
        _service.EvaluateCommands(commands);

        // Assert
        Assert.False(_service.IsLocked);
        Assert.Null(_service.LockedSince);
    }

    [Fact]
    public void Should_DeactivateLock_When_SubsequentCommandsDoNotIncludeCriticalActuators()
    {
        // Arrange - Primero activamos el bloqueo
        var criticalCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(criticalCommands);
        Assert.True(_service.IsLocked); // Verificar que se activó

        // Act - Enviamos comandos subsecuentes que NO incluyen actuadores críticos
        var subsequentCommands = new[]
        {
            ("humidificador-ultrasonico", "OFF"),
            ("termoventilador", "OFF")
        };
        _service.EvaluateCommands(subsequentCommands);

        // Assert - El bloqueo debe haberse liberado
        Assert.False(_service.IsLocked);
        Assert.Null(_service.LockedSince);
    }

    [Fact]
    public void Should_MaintainLock_When_SubsequentCommandsStillIncludeBothActuatorsOff()
    {
        // Arrange - Primero activamos el bloqueo
        var criticalCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(criticalCommands);
        Assert.True(_service.IsLocked);

        // Act - Enviamos comandos subsecuentes que SIGUEN incluyendo ambos actuadores OFF
        var subsequentCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF"),
            ("humidificador-ultrasonico", "ON")
        };
        _service.EvaluateCommands(subsequentCommands);

        // Assert - El bloqueo debe mantenerse
        Assert.True(_service.IsLocked);
        Assert.NotNull(_service.LockedSince);
    }

    [Fact]
    public void Should_DeactivateLock_When_OneActuatorTurnsOn()
    {
        // Arrange - Primero activamos el bloqueo
        var criticalCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(criticalCommands);
        Assert.True(_service.IsLocked);

        // Act - Uno de los actuadores se enciende
        var onCommands = new[]
        {
            ("bomba-agua", "ON"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(onCommands);

        // Assert - El bloqueo debe liberarse
        Assert.False(_service.IsLocked);
        Assert.Null(_service.LockedSince);
    }

    [Fact]
    public void Should_NotActivateLock_When_OnlyOneActuatorIsInBatch()
    {
        // Arrange
        var commands = new[]
        {
            ("bomba-agua", "OFF")
        };

        // Act
        _service.EvaluateCommands(commands);

        // Assert - No se debe activar el bloqueo con un solo actuador
        Assert.False(_service.IsLocked);
    }

    [Fact]
    public void Should_MaintainLock_When_SubsequentBatchContainsOnlyOneActuatorOff()
    {
        // Arrange - Primero activamos el bloqueo con ambos OFF
        var criticalCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(criticalCommands);
        Assert.True(_service.IsLocked);

        // Act - Subsecuente batch solo incluye uno de los actuadores con otros comandos
        // Nota: fuzzy-service SIEMPRE enviará ambos actuadores juntos cuando la regla se active
        // Si solo llega uno, no se evaluará la combinación crítica nuevamente
        var subsequentCommands = new[]
        {
            ("bomba-agua", "OFF"),
            ("humidificador-ultrasonico", "ON")
        };
        _service.EvaluateCommands(subsequentCommands);

        // Assert - El bloqueo permanece porque no hay suficiente información para cambiar el estado
        // Para liberarlo, fuzzy-service debe enviar comandos que NO incluyan ninguno de los actuadores críticos
        Assert.True(_service.IsLocked);
        Assert.NotNull(_service.LockedSince);
    }

    [Fact]
    public void ClearLock_Should_ForciblyRemoveLock()
    {
        // Arrange - Activar bloqueo
        var commands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(commands);
        Assert.True(_service.IsLocked);

        // Act
        _service.ClearLock();

        // Assert
        Assert.False(_service.IsLocked);
        Assert.Null(_service.LockedSince);
    }

    [Fact]
    public void GetStatus_Should_ReturnCorrectInformation()
    {
        // Arrange - Activar bloqueo
        var commands = new[]
        {
            ("bomba-agua", "OFF"),
            ("piedra-difusora", "OFF")
        };
        _service.EvaluateCommands(commands);

        // Act
        var status = _service.GetStatus();

        // Assert
        Assert.True(status.IsLocked);
        Assert.NotNull(status.LockedSince);
        Assert.NotNull(status.LockedDuration);
        Assert.NotNull(status.Reason);
        Assert.Contains("bomba-agua", status.LastKnownStates.Keys);
        Assert.Contains("piedra-difusora", status.LastKnownStates.Keys);
    }
}
