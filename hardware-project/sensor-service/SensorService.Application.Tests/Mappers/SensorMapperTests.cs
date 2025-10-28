using HydroEspinaca.Shared.DTOs.Sensors;
using HydroEspinaca.Shared.Enums;
using SensorService.Application.Mappers;
using SensorService.Domain.Entities;
using SensorService.Application.Tests.Builders;

namespace SensorService.Application.Tests.Mappers;

public class SensorMapperTests
{
    [Fact]
    public void ToDto_WithValidSensor_MapsAllProperties()
    {
        // Arrange
        var createdAt = new DateTime(2025, 10, 25, 10, 30, 0, DateTimeKind.Utc);
        var sensor = new SensorBuilder()
            .WithId("67890abcdef1234567890abc")
            .WithCode("DHT22-01")
            .WithPhysicalId("DHT22-A1")
            .WithLocation("Greenhouse Zone A")
            .WithEsp32Id("esp32-01")
            .WithSamplingFrequency(60)
            .WithVariables(new List<string> { "T_AMB", "HUM" })
            .WithStatus(SensorStatus.Active)
            .WithAllowMissing(false)
            .Build();
        sensor.CreatedAt = createdAt;

        // Act
        var dto = SensorMapper.ToDto(sensor);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be("67890abcdef1234567890abc");
        dto.Code.Should().Be("DHT22-01");
        dto.PhysicalId.Should().Be("DHT22-A1");
        dto.Location.Should().Be("Greenhouse Zone A");
        dto.Esp32Id.Should().Be("esp32-01");
        dto.SamplingFrequency.Should().Be(60);
        dto.Variables.Should().BeEquivalentTo(new List<string> { "T_AMB", "HUM" });
        dto.Status.Should().Be("Active");
        dto.AllowMissing.Should().BeFalse();
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ToDto_WithInactiveSensor_MapsStatusCorrectly()
    {
        // Arrange
        var sensor = new SensorBuilder()
            .WithStatus(SensorStatus.Inactive)
            .Build();

        // Act
        var dto = SensorMapper.ToDto(sensor);

        // Assert
        dto.Status.Should().Be("Inactive");
    }

    [Fact]
    public void ToEntity_WithValidCreateDto_CreatesNewSensor()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-02",
            PhysicalId = "DHT22-B2",
            Location = "Greenhouse Zone B",
            Esp32Id = "esp32-02",
            SamplingFrequency = 120,
            Variables = new List<string> { "T_WATER", "EC" },
            AllowMissing = true
        };

        // Act
        var sensor = SensorMapper.ToEntity(dto);

        // Assert
        sensor.Should().NotBeNull();
        sensor.Code.Should().Be("DHT22-02");
        sensor.PhysicalId.Should().Be("DHT22-B2");
        sensor.Location.Should().Be("Greenhouse Zone B");
        sensor.Esp32Id.Should().Be("esp32-02");
        sensor.SamplingFrequency.Should().Be(120);
        sensor.Variables.Should().BeEquivalentTo(new List<string> { "T_WATER", "EC" });
        sensor.AllowMissing.Should().BeTrue();
        sensor.Status.Should().Be(SensorStatus.Active);
        sensor.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MapUpdate_WithValidUpdateDto_UpdatesExistingSensor()
    {
        // Arrange
        var existingSensor = new SensorBuilder()
            .WithCode("DHT22-01") // Code should not be updated
            .WithPhysicalId("DHT22-A1-OLD")
            .WithLocation("Old Location")
            .WithEsp32Id("esp32-old")
            .WithSamplingFrequency(30)
            .WithVariables(new List<string> { "T_AMB" })
            .WithStatus(SensorStatus.Active)
            .WithAllowMissing(false)
            .Build();

        var updateDto = new SensorUpdateDto
        {
            PhysicalId = "DHT22-A1-NEW",
            Location = "New Location",
            Esp32Id = "esp32-new",
            SamplingFrequency = 90,
            Variables = new List<string> { "T_AMB", "HUM", "PH" },
            AllowMissing = true,
            Status = "Inactive"
        };

        // Act
        SensorMapper.MapUpdate(updateDto, existingSensor);

        // Assert
        existingSensor.Code.Should().Be("DHT22-01"); // Code unchanged
        existingSensor.PhysicalId.Should().Be("DHT22-A1-NEW");
        existingSensor.Location.Should().Be("New Location");
        existingSensor.Esp32Id.Should().Be("esp32-new");
        existingSensor.SamplingFrequency.Should().Be(90);
        existingSensor.Variables.Should().BeEquivalentTo(new List<string> { "T_AMB", "HUM", "PH" });
        existingSensor.AllowMissing.Should().BeTrue();
        existingSensor.Status.Should().Be(SensorStatus.Inactive);
    }

    [Fact]
    public void MapUpdate_WithInvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var existingSensor = new SensorBuilder().Build();
        var updateDto = new SensorUpdateDto
        {
            PhysicalId = "DHT22-A1",
            Location = "Location",
            Esp32Id = "esp32-01",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" },
            AllowMissing = false,
            Status = "InvalidStatus"
        };

        // Act
        var act = () => SensorMapper.MapUpdate(updateDto, existingSensor);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid sensor status*");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("active")]
    [InlineData("ACTIVE")]
    public void MapUpdate_WithValidStatusCaseInsensitive_UpdatesCorrectly(string status)
    {
        // Arrange
        var existingSensor = new SensorBuilder().Build();
        var updateDto = new SensorUpdateDto
        {
            PhysicalId = "DHT22-A1",
            Location = "Location",
            Esp32Id = "esp32-01",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" },
            AllowMissing = false,
            Status = status
        };

        // Act
        SensorMapper.MapUpdate(updateDto, existingSensor);

        // Assert
        existingSensor.Status.Should().Be(SensorStatus.Active);
    }

    [Fact]
    public void ToDto_WithEmptyVariables_ReturnsEmptyList()
    {
        // Arrange
        var sensor = new SensorBuilder()
            .WithVariables(new List<string>())
            .Build();

        // Act
        var dto = SensorMapper.ToDto(sensor);

        // Assert
        dto.Variables.Should().BeEmpty();
    }
}
