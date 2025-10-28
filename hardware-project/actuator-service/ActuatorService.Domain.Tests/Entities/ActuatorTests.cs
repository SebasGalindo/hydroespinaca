using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Domain.Tests.Entities;

/// <summary>
/// Tests unitarios para la entidad Actuator
/// Cobertura: propiedades, validación de estado, y comportamiento de la entidad
/// </summary>
public class ActuatorTests
{
    [Fact]
    public void Actuator_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var actuator = new Actuator();

        // Assert
        actuator.Id.Should().BeNull();
        actuator.Mode.Should().Be(ActuatorMode.DIGITAL);
        actuator.Status.Should().Be(ActuatorStatus.Active);
        actuator.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetId_ShouldSetIdProperty()
    {
        // Arrange
        var actuator = new Actuator();
        const string expectedId = "actuator-001";

        // Act
        actuator.SetId(expectedId);

        // Assert
        actuator.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Actuator_WithDigitalMode_ShouldBeValid()
    {
        // Arrange & Act
        var actuator = new Actuator
        {
            Code = "BombaRiego",
            Esp32Id = "esp32-001",
            Pin = "GPIO_15",
            Type = ActuatorType.Pump,
            Mode = ActuatorMode.DIGITAL,
            PhysicalId = "PUMP-001",
            Location = "Reservoir",
            Status = ActuatorStatus.Active
        };

        // Assert
        actuator.Code.Should().Be("BombaRiego");
        actuator.Mode.Should().Be(ActuatorMode.DIGITAL);
        actuator.Status.Should().Be(ActuatorStatus.Active);
    }

    [Fact]
    public void Actuator_WithPwmMode_ShouldBeValid()
    {
        // Arrange & Act
        var actuator = new Actuator
        {
            Code = "Ventiladores",
            Esp32Id = "esp32-001",
            Pin = "GPIO_25",
            Type = ActuatorType.Fan,
            Mode = ActuatorMode.PWM,
            PhysicalId = "FAN-001",
            Location = "Grow Room",
            Status = ActuatorStatus.Active
        };

        // Assert
        actuator.Mode.Should().Be(ActuatorMode.PWM);
        actuator.Type.Should().Be(ActuatorType.Fan);
    }

    [Theory]
    [InlineData(ActuatorStatus.Active)]
    [InlineData(ActuatorStatus.Inactive)]
    [InlineData(ActuatorStatus.Maintenance)]
    public void Actuator_WithDifferentStatuses_ShouldBeSet(ActuatorStatus status)
    {
        // Arrange & Act
        var actuator = new Actuator
        {
            Status = status
        };

        // Assert
        actuator.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(ActuatorType.Pump)]
    [InlineData(ActuatorType.Led)]
    [InlineData(ActuatorType.Fan)]
    [InlineData(ActuatorType.Heater)]
    [InlineData(ActuatorType.AirPump)]
    [InlineData(ActuatorType.Humidifier)]
    public void Actuator_WithDifferentTypes_ShouldBeSet(ActuatorType type)
    {
        // Arrange & Act
        var actuator = new Actuator
        {
            Type = type
        };

        // Assert
        actuator.Type.Should().Be(type);
    }

    [Fact]
    public void Actuator_AllPropertiesSet_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var actuator = new Actuator
        {
            Code = "TestActuator",
            Esp32Id = "esp32-test",
            Pin = "GPIO_99",
            Type = ActuatorType.Led,
            Mode = ActuatorMode.PWM,
            PhysicalId = "LED-TEST",
            Location = "Test Lab",
            Status = ActuatorStatus.Maintenance,
            CreatedAt = createdAt
        };
        actuator.SetId("test-id");

        // Assert
        actuator.Id.Should().Be("test-id");
        actuator.Code.Should().Be("TestActuator");
        actuator.Esp32Id.Should().Be("esp32-test");
        actuator.Pin.Should().Be("GPIO_99");
        actuator.Type.Should().Be(ActuatorType.Led);
        actuator.Mode.Should().Be(ActuatorMode.PWM);
        actuator.PhysicalId.Should().Be("LED-TEST");
        actuator.Location.Should().Be("Test Lab");
        actuator.Status.Should().Be(ActuatorStatus.Maintenance);
        actuator.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Actuator_PropertiesAreSettable_AfterConstruction()
    {
        // Arrange
        var actuator = new Actuator();

        // Act
        actuator.Code = "ModifiedCode";
        actuator.Esp32Id = "esp32-modified";
        actuator.Pin = "GPIO_50";
        actuator.Location = "New Location";
        actuator.Status = ActuatorStatus.Inactive;

        // Assert
        actuator.Code.Should().Be("ModifiedCode");
        actuator.Esp32Id.Should().Be("esp32-modified");
        actuator.Pin.Should().Be("GPIO_50");
        actuator.Location.Should().Be("New Location");
        actuator.Status.Should().Be(ActuatorStatus.Inactive);
    }
}
