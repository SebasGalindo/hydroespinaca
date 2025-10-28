using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Tests.Entities;

/// <summary>
/// Tests unitarios para la entidad InternalRoutine
/// Cobertura: creación, pasos, intervalos, y activación
/// </summary>
public class InternalRoutineTests
{
    [Fact]
    public void InternalRoutine_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var routine = new InternalRoutine();

        // Assert
        routine.Id.Should().BeNull();
        routine.Steps.Should().NotBeNull().And.BeEmpty();
        routine.IsActive.Should().BeTrue();
        routine.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        routine.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetId_ShouldSetIdProperty()
    {
        // Arrange
        var routine = new InternalRoutine();
        const string expectedId = "routine-001";

        // Act
        routine.SetId(expectedId);

        // Assert
        routine.Id.Should().Be(expectedId);
    }

    [Fact]
    public void InternalRoutine_WithSteps_ShouldContainSteps()
    {
        // Arrange
        var routine = new InternalRoutine
        {
            Name = "Irrigation Routine",
            Description = "Automated irrigation cycle",
            Esp32Id = "esp32-001",
            Interval = TimeSpan.FromHours(2),
            Steps = new List<InternalRoutineStep>
            {
                new() { OutputVariable = "BombaRiego", Power = "ON", Duration = 60 },
                new() { OutputVariable = "BombaAire", Power = "ON", Duration = 30 }
            }
        };

        // Assert
        routine.Steps.Should().HaveCount(2);
        routine.Steps[0].OutputVariable.Should().Be("BombaRiego");
        routine.Steps[1].OutputVariable.Should().Be("BombaAire");
    }

    [Theory]
    [InlineData(30, "30 minutes")]
    [InlineData(60, "1 hour")]
    [InlineData(120, "2 hours")]
    [InlineData(1440, "1 day")]
    public void InternalRoutine_WithDifferentIntervals_ShouldBeValid(int minutes, string description)
    {
        // Arrange & Act
        var routine = new InternalRoutine
        {
            Interval = TimeSpan.FromMinutes(minutes)
        };

        // Assert
        routine.Interval.TotalMinutes.Should().Be(minutes, because: description);
    }

    [Fact]
    public void InternalRoutine_CanBeDeactivated()
    {
        // Arrange
        var routine = new InternalRoutine
        {
            IsActive = true
        };

        // Act
        routine.IsActive = false;

        // Assert
        routine.IsActive.Should().BeFalse();
    }

    [Fact]
    public void InternalRoutineStep_WithDigitalMode_ShouldHavePower()
    {
        // Arrange & Act
        var step = new InternalRoutineStep
        {
            OutputVariable = "BombaRiego",
            Power = "ON",
            Duration = 60,
            Mode = "DIGITAL"
        };

        // Assert
        step.OutputVariable.Should().Be("BombaRiego");
        step.Power.Should().Be("ON");
        step.Duration.Should().Be(60);
        step.Mode.Should().Be("DIGITAL");
        step.DutyCycle.Should().BeNull();
    }

    [Fact]
    public void InternalRoutineStep_WithPwmMode_ShouldHaveDutyCycle()
    {
        // Arrange & Act
        var step = new InternalRoutineStep
        {
            OutputVariable = "Ventiladores",
            DutyCycle = 75.0,
            Duration = 120,
            Mode = "PWM"
        };

        // Assert
        step.OutputVariable.Should().Be("Ventiladores");
        step.DutyCycle.Should().Be(75.0);
        step.Duration.Should().Be(120);
        step.Mode.Should().Be("PWM");
        step.Power.Should().BeNull();
    }

    [Fact]
    public void InternalRoutine_CompleteConfiguration_ShouldBeValid()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        
        var routine = new InternalRoutine
        {
            Name = "Irrigation and Aeration",
            Description = "Combined irrigation and aeration cycle",
            Esp32Id = "esp32-001",
            Interval = TimeSpan.FromHours(2),
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Steps = new List<InternalRoutineStep>
            {
                new() { OutputVariable = "BombaAire", Power = "ON", Duration = 30, Mode = "DIGITAL" },
                new() { OutputVariable = "BombaRiego", Power = "ON", Duration = 60, Mode = "DIGITAL" },
                new() { OutputVariable = "BombaAire", Power = "ON", Duration = 30, Mode = "DIGITAL" }
            }
        };
        routine.SetId("routine-irrigation-001");

        // Assert
        routine.Id.Should().Be("routine-irrigation-001");
        routine.Name.Should().Be("Irrigation and Aeration");
        routine.Description.Should().Be("Combined irrigation and aeration cycle");
        routine.Esp32Id.Should().Be("esp32-001");
        routine.Interval.Should().Be(TimeSpan.FromHours(2));
        routine.IsActive.Should().BeTrue();
        routine.CreatedAt.Should().Be(createdAt);
        routine.UpdatedAt.Should().Be(updatedAt);
        routine.Steps.Should().HaveCount(3);
    }

    [Fact]
    public void InternalRoutine_EmptySteps_ShouldBeValid()
    {
        // Arrange & Act
        var routine = new InternalRoutine
        {
            Name = "Empty Routine",
            Esp32Id = "esp32-001",
            Interval = TimeSpan.FromMinutes(30),
            Steps = new List<InternalRoutineStep>()
        };

        // Assert
        routine.Steps.Should().BeEmpty();
    }

    [Fact]
    public void InternalRoutine_MultipleStepsSameActuator_ShouldBeAllowed()
    {
        // Arrange & Act
        var routine = new InternalRoutine
        {
            Steps = new List<InternalRoutineStep>
            {
                new() { OutputVariable = "BombaRiego", Power = "ON", Duration = 30 },
                new() { OutputVariable = "BombaRiego", Power = "OFF", Duration = 0 }
            }
        };

        // Assert
        routine.Steps.Should().HaveCount(2);
        routine.Steps.Should().OnlyContain(s => s.OutputVariable == "BombaRiego");
    }
}
