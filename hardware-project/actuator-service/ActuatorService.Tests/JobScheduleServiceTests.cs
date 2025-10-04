using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Domain.Entities;
using FluentAssertions;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ActuatorService.Tests;

/// <summary>
/// NOTE: These tests have been disabled after the outputVariable refactoring.
/// JobScheduleService now works with ResolvedRoutineDto instead of RoutineCommandDto,
/// and no longer depends on IActuatorRepository.
///
/// The new flow is:
/// 1. RoutineCommandDto with OutputVariable (control_outputs _id)
/// 2. OutputVariableResolver maps to physical actuator
/// 3. ResolvedRoutineDto with physical data (esp32Id, pin, mode)
/// 4. JobScheduleService schedules ResolvedRoutineDto
///
/// These tests need to be rewritten to:
/// - Create ResolvedRoutineDto objects directly
/// - Test the new CreateJobScheduleAsync(List<ResolvedRoutineDto>) method
/// - Remove IActuatorRepository mocks
///
/// For now, integration tests at the ExecuteMultiRoutineCommandUseCase level
/// provide coverage for the end-to-end flow.
/// </summary>
public class JobScheduleServiceTests
{
    private readonly Mock<IJobScheduleStateManager> _mockStateManager;
    private readonly Mock<ILogger<JobScheduleService>> _mockLogger;
    private readonly JobScheduleService _jobScheduleService;

    public JobScheduleServiceTests()
    {
        _mockStateManager = new Mock<IJobScheduleStateManager>();
        _mockLogger = new Mock<ILogger<JobScheduleService>>();

        _jobScheduleService = new JobScheduleService(
            _mockStateManager.Object,
            _mockLogger.Object
        );

        // Setup state manager to return empty channels initially
        _mockStateManager.Setup(x => x.GetActiveEsp32Ids())
            .Returns(new List<string>());

        _mockStateManager.Setup(x => x.GetCurrentJobSchedule(It.IsAny<string>()))
            .Returns(new JobScheduleDto
            {
                Esp32Id = "esp32-001",
                JobSchedule = new()
                {
                    new() { Channel = 0, Queue = new() },
                    new() { Channel = 1, Queue = new() }
                }
            });

        _mockStateManager.Setup(x => x.GetInternalScheduleState(It.IsAny<string>()))
            .Returns((JobScheduleState?)null);
    }

    [Fact]
    public async Task Should_Create_Job_Schedule_For_Resolved_Routines()
    {
        // Arrange: Create resolved routines with physical actuator data
        var resolvedRoutines = new List<ResolvedRoutineDto>
        {
            new()
            {
                RoutineId = "Ventiladores",
                Esp32Id = "esp32-001",
                ResolvedSteps = new()
                {
                    new()
                    {
                        OutputVariableId = "output1",
                        OutputVariableName = "Ventilador Principal",
                        ActuatorId = "act1",
                        Esp32Id = "esp32-001",
                        Pin = "1",
                        Mode = ActuatorMode.DIGITAL,
                        Power = ActuatorConstants.PowerStates.On,
                        Duration = 120
                    }
                }
            }
        };

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
            });

        // Act
        var result = await _jobScheduleService.CreateJobScheduleAsync(resolvedRoutines);

        // Assert
        addedRoutines.Should().ContainSingle("should have processed 1 routine");
        addedRoutines[0].esp32Id.Should().Be("esp32-001");
        addedRoutines[0].routine.BaseId.Should().Be("Ventiladores");
        result.Esp32Id.Should().Be("esp32-001");
    }

    [Fact]
    public async Task Should_Reject_Empty_Routine_List()
    {
        // Arrange
        var emptyList = new List<ResolvedRoutineDto>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _jobScheduleService.CreateJobScheduleAsync(emptyList)
        );
    }
}
