using HydroEspinaca.Shared.DTOs.Sensors;
using SensorService.Application.Validators.Sensor;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Tests.Validators;

public class SensorCreateValidatorTests
{
    private readonly Mock<IVariableRepository> _variableRepositoryMock;
    private readonly Mock<IEsp32NodeRepository> _esp32RepositoryMock;
    private readonly SensorCreateValidator _sut;

    public SensorCreateValidatorTests()
    {
        _variableRepositoryMock = new Mock<IVariableRepository>();
        _esp32RepositoryMock = new Mock<IEsp32NodeRepository>();
        _sut = new SensorCreateValidator(_variableRepositoryMock.Object, _esp32RepositoryMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_WithValidData_Passes()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse Zone A",
            Esp32Id = "67890abcdef1234567890abc", // Valid 24-char ObjectId
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB", "HUM" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCode_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task ValidateAsync_WithCodeTooLong_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = new string('A', 51), // 51 characters
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyPhysicalId_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalId");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyLocation_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Location");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyEsp32Id_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "",
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Esp32Id");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidObjectId_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "invalid-object-id", // Invalid ObjectId
            SamplingFrequency = 60,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Esp32Id");
    }

    [Fact]
    public async Task ValidateAsync_WithZeroSamplingFrequency_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 0,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SamplingFrequency");
    }

    [Fact]
    public async Task ValidateAsync_WithNegativeSamplingFrequency_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = -10,
            Variables = new List<string> { "T_AMB" }
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SamplingFrequency");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyVariablesList_Fails()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "DHT22-01",
            PhysicalId = "DHT22-A1",
            Location = "Greenhouse",
            Esp32Id = "67890abcdef1234567890abc",
            SamplingFrequency = 60,
            Variables = new List<string>() // Empty list
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Variables");
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var dto = new SensorCreateDto
        {
            Code = "", // Empty
            PhysicalId = "", // Empty
            Location = "", // Empty
            Esp32Id = "", // Empty
            SamplingFrequency = 0, // Invalid
            Variables = new List<string>() // Empty
        };

        // Act
        var result = await _sut.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5);
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalId");
        result.Errors.Should().Contain(e => e.PropertyName == "Location");
        result.Errors.Should().Contain(e => e.PropertyName == "Esp32Id");
        result.Errors.Should().Contain(e => e.PropertyName == "SamplingFrequency");
        result.Errors.Should().Contain(e => e.PropertyName == "Variables");
    }
}
