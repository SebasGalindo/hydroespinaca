using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using FluentAssertions;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ActuatorService.Tests;

public class JobScheduleServiceTests
{
    private readonly Mock<IActuatorRepository> _mockActuatorRepository;
    private readonly Mock<IJobScheduleStateManager> _mockStateManager;
    private readonly Mock<ILogger<JobScheduleService>> _mockLogger;
    private readonly JobScheduleService _jobScheduleService;

    // Test actuators with different pins
    private readonly List<Actuator> _testActuators;

    public JobScheduleServiceTests()
    {
        _testActuators = new()
        {
            CreateTestActuator("act1", "1", ActuatorMode.DIGITAL),
            CreateTestActuator("act2", "2", ActuatorMode.PWM),
            CreateTestActuator("act3", "3", ActuatorMode.DIGITAL),
            CreateTestActuator("act4", "4", ActuatorMode.PWM)
        };

        _mockActuatorRepository = new Mock<IActuatorRepository>();
        _mockStateManager = new Mock<IJobScheduleStateManager>();
        _mockLogger = new Mock<ILogger<JobScheduleService>>();
        
        _jobScheduleService = new JobScheduleService(
            _mockActuatorRepository.Object,
            _mockStateManager.Object,
            _mockLogger.Object
        );

        // Setup mock to return test actuators
        foreach (var actuator in _testActuators)
        {
            _mockActuatorRepository.Setup(x => x.GetByIdAsync(actuator.Id))
                .ReturnsAsync(actuator);
        }

        _mockActuatorRepository.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync((List<string> ids) => _testActuators.Where(a => ids.Contains(a.Id)).ToList());

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

    private static Actuator CreateTestActuator(string id, string pin, ActuatorMode mode)
    {
        var actuator = new Actuator
        {
            Pin = pin,
            Mode = mode,
            Esp32Id = "esp32-001",
            Code = $"Test{id}",
            PhysicalId = id,
            Location = "Test Location"
        };
        actuator.SetId(id);
        return actuator;
    }

    [Fact]
    public async Task Should_Distribute_Jobs_Across_Multiple_Channels_When_No_Actuator_Conflicts()
    {
        // Arrange: Simulate channel 0 already has 1 job, channel 1 is empty
        var existingSchedule = new JobScheduleDto 
        { 
            Esp32Id = "esp32-001", 
            JobSchedule = new()
            {
                new() 
                { 
                    Channel = 0, 
                    Queue = new() 
                    { 
                        new() { CommandId = "existing_cmd", Steps = new() { new() { Pin = "5" } } } 
                    } 
                },
                new() { Channel = 1, Queue = new() } // Empty channel
            }
        };
        
        _mockStateManager.Setup(x => x.GetCurrentJobSchedule("esp32-001"))
            .Returns(existingSchedule);
            
        _mockStateManager.Setup(x => x.GetActiveEsp32Ids())
            .Returns(new List<string> { "esp32-001" }); // Return active ESP32
        
        // 3 routines with different actuators (different pins to avoid conflicts)
        var routines = new List<RoutineCommandDto>
        {
            new()
            {
                RoutineId = "Ventiladores",
                Steps = new() { new() { Actuator = "act1", Power = "ON", Duration = 120 } }
            },
            new()
            {
                RoutineId = "Calefactor", 
                Steps = new() { new() { Actuator = "act2", DutyCycle = 80, Duration = 60 } }
            },
            new()
            {
                RoutineId = "Luz",
                Steps = new() { new() { Actuator = "act3", Power = "ON", Duration = 30 } }
            }
        };

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        var dynamicChannelLoads = new Dictionary<int, int> { { 0, 1 }, { 1, 0 } }; // Channel 0 starts with 1 job
        
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
                
                // Simulate that adding a routine increases the channel load
                dynamicChannelLoads[channelId]++;
                
                // Update the mock to return updated load
                var updatedSchedule = new JobScheduleDto 
                { 
                    Esp32Id = "esp32-001", 
                    JobSchedule = new()
                    {
                        new() 
                        { 
                            Channel = 0, 
                            Queue = Enumerable.Range(0, dynamicChannelLoads[0])
                                .Select(i => new JobRoutineDto { CommandId = $"cmd_{i}", Steps = new() }).ToList()
                        },
                        new() 
                        { 
                            Channel = 1, 
                            Queue = Enumerable.Range(0, dynamicChannelLoads[1])
                                .Select(i => new JobRoutineDto { CommandId = $"cmd_ch1_{i}", Steps = new() }).ToList()
                        }
                    }
                };
                
                _mockStateManager.Setup(x => x.GetCurrentJobSchedule("esp32-001"))
                    .Returns(updatedSchedule);
            });

        // Act
        var result = await _jobScheduleService.CreateJobScheduleAsync(routines);

        // Assert
        addedRoutines.Should().HaveCount(3, "should have processed 3 routines");
        
        // Verify channel distribution - should use both channel 0 and 1
        var channelsUsed = addedRoutines.Select(r => r.channelId).Distinct().ToList();
        channelsUsed.Should().HaveCountGreaterThan(1, "should distribute across multiple channels when no conflicts");
        
        // Since channel 0 has load=1 and channel 1 has load=0, 
        // new routines should prefer channel 1 for load balancing
        var channel1Jobs = addedRoutines.Where(r => r.channelId == 1).ToList();
        channel1Jobs.Should().NotBeEmpty("some jobs should go to channel 1 for load balancing");
        
        // Verify priorities are correctly assigned (all single-step routines)
        addedRoutines.Should().OnlyContain(r => r.priority == 1, "all should be single-step priority");
    }

    [Fact]
    public async Task Should_Group_Same_Actuator_Jobs_In_Same_Channel()
    {
        // Arrange: 2 routines using the same actuator
        var routines = new List<RoutineCommandDto>
        {
            new()
            {
                RoutineId = "EncenderLuz",
                Steps = new() { new() { Actuator = "act1", Power = "ON", Duration = 60 } }
            },
            new()
            {
                RoutineId = "ApagarLuz",
                Steps = new() { new() { Actuator = "act1", Power = "OFF", Duration = 0 } }
            }
        };

        // Simulate existing routine in channel 0 with act1
        var existingChannel0 = new JobChannelDto 
        { 
            Channel = 0, 
            Queue = new() 
            { 
                new() 
                { 
                    CommandId = "existing_cmd", 
                    Steps = new() { new() { Pin = "1" } } 
                } 
            } 
        };
        
        _mockStateManager.Setup(x => x.GetCurrentJobSchedule("esp32-001"))
            .Returns(new JobScheduleDto 
            { 
                Esp32Id = "esp32-001", 
                JobSchedule = new() { existingChannel0, new() { Channel = 1, Queue = new() } }
            });

        // Setup internal state to return actuator IDs
        var mockInternalState = new JobScheduleState("esp32-001");
        mockInternalState.Channels[0] = new ChannelState 
        { 
            ChannelId = 0, 
            Queue = new() 
            { 
                new() 
                { 
                    CommandId = "existing_cmd", 
                    Steps = new() { new() { Pin = "1", ActuatorId = "act1" } } 
                } 
            } 
        };
        
        _mockStateManager.Setup(x => x.GetInternalScheduleState("esp32-001"))
            .Returns(mockInternalState);

        _mockStateManager.Setup(x => x.GetActiveEsp32Ids())
            .Returns(new List<string> { "esp32-001" });

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
            });

        // Act
        var result = await _jobScheduleService.CreateJobScheduleAsync(routines);

        // Assert
        addedRoutines.Should().HaveCount(2, "should have processed 2 routines");
        addedRoutines.Should().OnlyContain(r => r.channelId == 0, "both routines should be assigned to channel 0 (same actuator)");
        
        // Verify priorities - control command should have higher priority
        var controlRoutine = addedRoutines.FirstOrDefault(r => r.routine.BaseId == "ApagarLuz");
        var normalRoutine = addedRoutines.FirstOrDefault(r => r.routine.BaseId == "EncenderLuz");
        
        controlRoutine.Should().NotBeNull();
        controlRoutine.priority.Should().Be(2, "OFF command should be control priority");
        
        normalRoutine.Should().NotBeNull(); 
        normalRoutine.priority.Should().Be(1, "single step should be priority 1");
    }

    [Fact]
    public async Task Should_Prioritize_Control_Commands_Correctly()
    {
        // Arrange: Mix of control and normal commands
        var routines = new List<RoutineCommandDto>
        {
            new()
            {
                RoutineId = "NormalTask",
                Steps = new() 
                { 
                    new() { Actuator = "act1", Power = "ON", Duration = 60 },
                    new() { Actuator = "act2", DutyCycle = 50, Duration = 30 }
                }
            },
            new()
            {
                RoutineId = "EmergencyShutdown",
                Steps = new() { new() { Actuator = "act3", Power = "OFF", Duration = 0 } }
            },
            new()
            {
                RoutineId = "SingleStep",
                Steps = new() { new() { Actuator = "act4", DutyCycle = 75, Duration = 45 } }
            }
        };

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
            });

        // Act
        var result = await _jobScheduleService.CreateJobScheduleAsync(routines);

        // Assert
        addedRoutines.Should().HaveCount(3, "should have processed 3 routines");
        
        var priorities = addedRoutines.ToDictionary(r => r.routine.BaseId, r => r.priority);
        priorities["EmergencyShutdown"].Should().Be(2, "control command should have highest priority");
        priorities["SingleStep"].Should().Be(1, "single step should have medium priority");
        priorities["NormalTask"].Should().Be(0, "multi-step should have lowest priority");
    }

    [Fact]
    public async Task Should_Order_By_Duration_Within_Same_Priority()
    {
        // This test verifies that the JobScheduleStateManager correctly orders by duration
        // We can't test the internal ordering logic directly from JobScheduleService,
        // but we can verify that the correct total durations are calculated
        
        var routines = new List<RoutineCommandDto>
        {
            new()
            {
                RoutineId = "LongTask",
                Steps = new() { new() { Actuator = "act1", Power = "ON", Duration = 120 } }
            },
            new()
            {
                RoutineId = "ShortTask", 
                Steps = new() { new() { Actuator = "act2", Power = "ON", Duration = 30 } }
            },
            new()
            {
                RoutineId = "MediumTask",
                Steps = new() { new() { Actuator = "act3", Power = "ON", Duration = 60 } }
            }
        };

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
            });

        // Act
        await _jobScheduleService.CreateJobScheduleAsync(routines);

        // Assert - verify durations are correctly calculated in JobRoutineState
        addedRoutines.Should().HaveCount(3);
        
        var durations = addedRoutines.ToDictionary(
            r => r.routine.BaseId, 
            r => r.routine.Steps.Sum(s => s.Duration)
        );
        
        durations["ShortTask"].Should().Be(30);
        durations["MediumTask"].Should().Be(60);
        durations["LongTask"].Should().Be(120);
    }

    [Theory]
    [InlineData("OFF", 0, 2)] // Control priority
    [InlineData("ON", 60, 1)] // Single step priority  
    public async Task Should_Classify_Priority_Correctly(string power, int duration, int expectedPriority)
    {
        var routine = new List<RoutineCommandDto>
        {
            new()
            {
                RoutineId = "TestRoutine",
                Steps = new() { new() { Actuator = "act1", Power = power, Duration = duration } }
            }
        };

        var addedRoutines = new List<(string esp32Id, JobRoutineState routine, int channelId, int priority)>();
        _mockStateManager.Setup(x => x.AddRoutineToSchedule(It.IsAny<string>(), It.IsAny<JobRoutineState>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, JobRoutineState, int, int>((esp32Id, routine, channelId, priority) =>
            {
                addedRoutines.Add((esp32Id, routine, channelId, priority));
            });

        // Act
        await _jobScheduleService.CreateJobScheduleAsync(routine);

        // Assert
        addedRoutines.Should().ContainSingle();
        addedRoutines[0].priority.Should().Be(expectedPriority);
    }
}