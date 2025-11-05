using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Domain.Models;
using FluentValidation;
using FluentValidation.Results;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Tests.UseCases;

/// <summary>
/// Tests de integración para ExecuteCommandsUseCase
/// Cobertura: validación, resolución de actuadores, scheduling y casos de error
/// </summary>
public class ExecuteCommandsUseCaseTests
{
    private readonly Mock<IValidator<ExecuteCommandsDto>> _mockValidator;
    private readonly Mock<IActuatorCodeResolver> _mockActuatorCodeResolver;
    private readonly Mock<ICommandExecutionService> _mockCommandExecutionService;
    private readonly Mock<IRoutineCommandRepository> _mockRoutineCommandRepository;
    private readonly Mock<IActuatorStateMachine> _mockStateMachine;
    private readonly Mock<IWaterRelatedActuatorsLockService> _mockWaterLockService;
    private readonly Mock<ILogger<ExecuteCommandsUseCase>> _mockLogger;
    private readonly ExecuteCommandsUseCase _useCase;

    public ExecuteCommandsUseCaseTests()
    {
        _mockValidator = new Mock<IValidator<ExecuteCommandsDto>>();
        _mockActuatorCodeResolver = new Mock<IActuatorCodeResolver>();
        _mockCommandExecutionService = new Mock<ICommandExecutionService>();
        _mockRoutineCommandRepository = new Mock<IRoutineCommandRepository>();
        _mockStateMachine = new Mock<IActuatorStateMachine>();
        _mockWaterLockService = new Mock<IWaterRelatedActuatorsLockService>();
        _mockLogger = new Mock<ILogger<ExecuteCommandsUseCase>>();

        _useCase = new ExecuteCommandsUseCase(
            _mockValidator.Object,
            _mockActuatorCodeResolver.Object,
            _mockCommandExecutionService.Object,
            _mockRoutineCommandRepository.Object,
            _mockStateMachine.Object,
            _mockWaterLockService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_ValidDigitalCommand_ShouldSucceed()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = "ON", Duration = 60 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns((ActuatorState?)null);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("BombaRiego"))
            .ReturnsAsync((RoutineCommand?)null);
        _mockRoutineCommandRepository.Setup(r => r.AddAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        _mockRoutineCommandRepository.Verify(r => r.AddAsync(It.Is<RoutineCommand>(cmd =>
            cmd.ActuatorCode == "BombaRiego" &&
            cmd.StatusGeneral == RoutineCommandStatus.RUNNING
        )), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPwmCommand_ShouldSucceed()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "Ventiladores", DutyCycle = 75.0, Duration = 120 }
            }
        };

        var actuator = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns((ActuatorState?)null);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("Ventiladores"))
            .ReturnsAsync((RoutineCommand?)null);
        _mockRoutineCommandRepository.Setup(r => r.AddAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.Is<List<ResolvedCommandDto>>(cmds =>
                cmds.Any(c => c.DutyCycle == 75.0 && c.Mode == ActuatorMode.PWM)),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidValidation_ShouldThrowValidationException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>()
        };

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Commands", "Commands list cannot be empty")
        };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ExecuteCommandsDto>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task ExecuteAsync_ActuatorNotFound_ShouldThrowActuatorNotFoundException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "NonExistent", Power = "ON", Duration = 60 }
            }
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("NonExistent"))
            .ThrowsAsync(new ActuatorNotFoundException("Actuator not found"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<ActuatorNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_DigitalActuatorWithDutyCycle_ShouldThrowException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", DutyCycle = 50.0, Duration = 60 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<RoutineScheduleConflictException>()
            .WithMessage("*DIGITAL*cannot use 'dutyCycle'*");
    }

    [Fact]
    public async Task ExecuteAsync_DigitalActuatorWithoutPower_ShouldThrowException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Duration = 60 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<RoutineScheduleConflictException>()
            .WithMessage("*requires 'power' parameter*");
    }

    [Fact]
    public async Task ExecuteAsync_PwmActuatorWithPower_ShouldThrowException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "Ventiladores", Power = "ON", Duration = 60 }
            }
        };

        var actuator = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores"))
            .ReturnsAsync(actuator);

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<RoutineScheduleConflictException>()
            .WithMessage("*PWM*cannot use 'power'*");
    }

    [Fact]
    public async Task ExecuteAsync_PwmActuatorWithoutDutyCycle_ShouldThrowException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "Ventiladores", Duration = 60 }
            }
        };

        var actuator = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores"))
            .ReturnsAsync(actuator);

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<RoutineScheduleConflictException>()
            .WithMessage("*requires 'dutyCycle' parameter*");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleCommandsDifferentEsp32_ShouldThrowException()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = "ON", Duration = 60 },
                new() { ActuatorCode = "BombaAire", Power = "ON", Duration = 30 }
            }
        };

        var actuator1 = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15", "esp32-001");
        var actuator2 = CreateDigitalActuator("BombaAire", "actuator-002", "GPIO_20", "esp32-002");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator1);
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaAire"))
            .ReturnsAsync(actuator2);

        // Act
        var act = async () => await _useCase.ExecuteAsync(executeCommands);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*same ESP32 device*");
    }

    [Fact]
    public async Task ExecuteAsync_ExistingRunningCommand_ShouldUpdateInsteadOfCreate()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = "ON", Duration = 120 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");
        var existingCommand = new RoutineCommand
        {
            CommandId = "existing-cmd-001",
            ActuatorCode = "BombaRiego",
            Esp32Id = "esp32-001",
            StatusGeneral = RoutineCommandStatus.RUNNING,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns((ActuatorState?)null);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("BombaRiego"))
            .ReturnsAsync(existingCommand);
        _mockRoutineCommandRepository.Setup(r => r.UpdateAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("existing-cmd-001");
        
        _mockRoutineCommandRepository.Verify(r => r.UpdateAsync(It.Is<RoutineCommand>(cmd =>
            cmd.CommandId == "existing-cmd-001" &&
            cmd.ExtendedAt.HasValue
        )), Times.Once);
        
        _mockRoutineCommandRepository.Verify(r => r.AddAsync(It.IsAny<RoutineCommand>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PowerOffCommand_ShouldNotSaveToDatabase()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = ActuatorConstants.PowerStates.Off, Duration = 0 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns((ActuatorState?)null);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("BombaRiego"))
            .ReturnsAsync((RoutineCommand?)null);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Contain("off");
        
        // Should not save to database for OFF commands
        _mockRoutineCommandRepository.Verify(r => r.AddAsync(It.IsAny<RoutineCommand>()), Times.Never);
        _mockRoutineCommandRepository.Verify(r => r.UpdateAsync(It.IsAny<RoutineCommand>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleCommands_SameEsp32_ShouldSucceed()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = "ON", Duration = 60 },
                new() { ActuatorCode = "BombaAire", Power = "ON", Duration = 30 },
                new() { ActuatorCode = "Ventiladores", DutyCycle = 75.0, Duration = 120 }
            }
        };

        var actuator1 = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15", "esp32-001");
        var actuator2 = CreateDigitalActuator("BombaAire", "actuator-002", "GPIO_20", "esp32-001");
        var actuator3 = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25", "esp32-001");

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego")).ReturnsAsync(actuator1);
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaAire")).ReturnsAsync(actuator2);
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores")).ReturnsAsync(actuator3);

        _mockStateMachine.Setup(s => s.GetState(It.IsAny<string>()))
            .Returns((ActuatorState?)null);

        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((RoutineCommand?)null);
        _mockRoutineCommandRepository.Setup(r => r.AddAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), "esp32-001"))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(3);
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.Is<List<ResolvedCommandDto>>(cmds => cmds.Count == 3),
            "esp32-001"
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RedundantOffCommand_ShouldSkipCommand()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = ActuatorConstants.PowerStates.Off, Duration = 0 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");
        var currentState = new ActuatorState
        {
            ActuatorId = actuator.Id,
            State = PowerState.OFF,
            LastUpdated = DateTime.UtcNow
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns(currentState);

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().BeEmpty();
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.IsAny<List<ResolvedCommandDto>>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RedundantOnCommandDigital_ShouldExtendCommand()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "BombaRiego", Power = ActuatorConstants.PowerStates.On, Duration = 60 }
            }
        };

        var actuator = CreateDigitalActuator("BombaRiego", "actuator-001", "GPIO_15");
        var currentState = new ActuatorState
        {
            ActuatorId = actuator.Id,
            State = PowerState.ON,
            LastUpdated = DateTime.UtcNow,
            RemainingDuration = 120
        };

        var existingCommand = new RoutineCommand
        {
            CommandId = "existing-cmd-001",
            ActuatorCode = "BombaRiego",
            Esp32Id = "esp32-001",
            StatusGeneral = RoutineCommandStatus.RUNNING,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("BombaRiego"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns(currentState);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("BombaRiego"))
            .ReturnsAsync(existingCommand);
        _mockRoutineCommandRepository.Setup(r => r.UpdateAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("existing-cmd-001");

        // Should extend the existing command
        _mockRoutineCommandRepository.Verify(r => r.UpdateAsync(It.Is<RoutineCommand>(cmd =>
            cmd.CommandId == "existing-cmd-001" &&
            cmd.ExtendedAt.HasValue
        )), Times.Once);

        // Should publish to MQTT to restart/extend duration
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.IsAny<List<ResolvedCommandDto>>(),
            "esp32-001"
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RedundantPwmCommand_ShouldExtendCommand()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "Ventiladores", DutyCycle = 75.0, Duration = 120 }
            }
        };

        var actuator = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25");
        var currentState = new ActuatorState
        {
            ActuatorId = actuator.Id,
            State = PowerState.ON,
            DutyCycle = 75.0,
            LastUpdated = DateTime.UtcNow
        };

        var existingCommand = new RoutineCommand
        {
            CommandId = "existing-cmd-002",
            ActuatorCode = "Ventiladores",
            Esp32Id = "esp32-001",
            StatusGeneral = RoutineCommandStatus.RUNNING,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3)
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns(currentState);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("Ventiladores"))
            .ReturnsAsync(existingCommand);
        _mockRoutineCommandRepository.Setup(r => r.UpdateAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("existing-cmd-002");

        // Should extend the existing command
        _mockRoutineCommandRepository.Verify(r => r.UpdateAsync(It.Is<RoutineCommand>(cmd =>
            cmd.CommandId == "existing-cmd-002" &&
            cmd.ExtendedAt.HasValue
        )), Times.Once);

        // Should publish to MQTT to restart/extend duration
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.IsAny<List<ResolvedCommandDto>>(),
            "esp32-001"
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentPwmDutyCycle_ShouldExecuteCommand()
    {
        // Arrange
        var executeCommands = new ExecuteCommandsDto
        {
            Commands = new List<ActuatorControlDto>
            {
                new() { ActuatorCode = "Ventiladores", DutyCycle = 85.0, Duration = 120 }
            }
        };

        var actuator = CreatePwmActuator("Ventiladores", "actuator-003", "GPIO_25");
        var currentState = new ActuatorState
        {
            ActuatorId = actuator.Id,
            State = PowerState.ON,
            DutyCycle = 75.0,
            LastUpdated = DateTime.UtcNow
        };

        SetupValidValidation();
        _mockActuatorCodeResolver.Setup(r => r.ResolveAsync("Ventiladores"))
            .ReturnsAsync(actuator);
        _mockStateMachine.Setup(s => s.GetState(actuator.Id))
            .Returns(currentState);
        _mockRoutineCommandRepository.Setup(r => r.GetRunningByActuatorCodeAsync("Ventiladores"))
            .ReturnsAsync((RoutineCommand?)null);
        _mockRoutineCommandRepository.Setup(r => r.AddAsync(It.IsAny<RoutineCommand>()))
            .Returns(Task.CompletedTask);
        _mockCommandExecutionService.Setup(s => s.ScheduleCommandsAsync(It.IsAny<List<ResolvedCommandDto>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _useCase.ExecuteAsync(executeCommands);

        // Assert
        result.Should().HaveCount(1);
        _mockCommandExecutionService.Verify(s => s.ScheduleCommandsAsync(
            It.Is<List<ResolvedCommandDto>>(cmds => cmds.Any(c => c.DutyCycle == 85.0)),
            It.IsAny<string>()
        ), Times.Once);
    }

    // Helper methods
    private void SetupValidValidation()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ExecuteCommandsDto>(), default))
            .ReturnsAsync(new ValidationResult());
    }

    private static Actuator CreateDigitalActuator(string code, string id, string pin, string esp32Id = "esp32-001")
    {
        var actuator = new Actuator
        {
            Code = code,
            Esp32Id = esp32Id,
            Pin = pin,
            Type = ActuatorType.Pump,
            Mode = ActuatorMode.DIGITAL,
            PhysicalId = $"ACTUATOR-{code}",
            Location = "Test Location",
            Status = ActuatorStatus.Active
        };
        actuator.SetId(id);
        return actuator;
    }

    private static Actuator CreatePwmActuator(string code, string id, string pin, string esp32Id = "esp32-001")
    {
        var actuator = new Actuator
        {
            Code = code,
            Esp32Id = esp32Id,
            Pin = pin,
            Type = ActuatorType.Fan,
            Mode = ActuatorMode.PWM,
            PhysicalId = $"ACTUATOR-{code}",
            Location = "Test Location",
            Status = ActuatorStatus.Active
        };
        actuator.SetId(id);
        return actuator;
    }
}
