using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Application.Services;
using ActuatorService.Application.Validators;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ActuatorService.Tests;

public class ControlOutputServiceTests
{
    private readonly Mock<IControlOutputRepository> _mockControlOutputRepo;
    private readonly Mock<IActuatorRepository> _mockActuatorRepo;
    private readonly Mock<ILogger<ControlOutputServiceApplication>> _mockLogger;
    private readonly IControlOutputService _service;

    public ControlOutputServiceTests()
    {
        _mockControlOutputRepo = new Mock<IControlOutputRepository>();
        _mockActuatorRepo = new Mock<IActuatorRepository>();
        _mockLogger = new Mock<ILogger<ControlOutputServiceApplication>>();

        var createValidator = new CreateControlOutputValidator();
        var updateValidator = new UpdateControlOutputValidator();

        _service = new ControlOutputServiceApplication(
            _mockControlOutputRepo.Object,
            _mockActuatorRepo.Object,
            createValidator,
            updateValidator,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllControlOutputs()
    {
        // Arrange
        var controlOutputs = new List<ControlOutput>
        {
            CreateTestControlOutput("1", "Temperature Control", "act1"),
            CreateTestControlOutput("2", "Humidity Control", "act2")
        };
        _mockControlOutputRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(controlOutputs);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Temperature Control");
        result[1].Name.Should().Be("Humidity Control");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnControlOutput()
    {
        // Arrange
        var id = "507f1f77bcf86cd799439011";
        var controlOutput = CreateTestControlOutput(id, "Temperature Control", "act1");
        _mockControlOutputRepo.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(controlOutput);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Name.Should().Be("Temperature Control");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldThrowControlOutputNotFoundException()
    {
        // Arrange
        var id = "507f1f77bcf86cd799439011";
        _mockControlOutputRepo.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((ControlOutput?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ControlOutputNotFoundException>(() => _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task AddAsync_WithValidDto_ShouldCreateControlOutput()
    {
        // Arrange
        var actuatorId = "507f1f77bcf86cd799439011";
        var actuator = CreateTestActuator(actuatorId);
        _mockActuatorRepo.Setup(x => x.GetByIdAsync(actuatorId)).ReturnsAsync(actuator);

        _mockControlOutputRepo.Setup(x => x.AddAsync(It.IsAny<ControlOutput>()))
            .Callback<ControlOutput>(co => co.SetId("507f1f77bcf86cd799439012"))
            .Returns(Task.CompletedTask);

        var dto = new CreateControlOutputDto
        {
            Name = "Temperature Control",
            Description = "Controls temperature output",
            Unit = "°C",
            ActuatorId = actuatorId,
            MinValue = 0,
            MaxValue = 100
        };

        // Act
        var result = await _service.AddAsync(dto);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mockControlOutputRepo.Verify(x => x.AddAsync(It.IsAny<ControlOutput>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithNonExistentActuator_ShouldThrowActuatorNotFoundException()
    {
        // Arrange
        var actuatorId = "507f1f77bcf86cd799439011";
        _mockActuatorRepo.Setup(x => x.GetByIdAsync(actuatorId)).ReturnsAsync((Actuator?)null);

        var dto = new CreateControlOutputDto
        {
            Name = "Temperature Control",
            ActuatorId = actuatorId,
            MinValue = 0,
            MaxValue = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<ActuatorNotFoundException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_WithInvalidDto_ShouldThrowValidationException()
    {
        // Arrange - MinValue > MaxValue
        var dto = new CreateControlOutputDto
        {
            Name = "Temperature Control",
            ActuatorId = "507f1f77bcf86cd799439011",
            MinValue = 100,
            MaxValue = 0  // Invalid: MaxValue < MinValue
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldUpdateControlOutput()
    {
        // Arrange
        var id = "507f1f77bcf86cd799439011";
        var actuatorId = "507f1f77bcf86cd799439012";
        var existingControlOutput = CreateTestControlOutput(id, "Old Name", actuatorId);
        var actuator = CreateTestActuator(actuatorId);

        _mockControlOutputRepo.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(existingControlOutput);
        _mockActuatorRepo.Setup(x => x.GetByIdAsync(actuatorId)).ReturnsAsync(actuator);

        var updateDto = new UpdateControlOutputDto
        {
            Name = "Updated Name",
            Description = "Updated description",
            Unit = "updated",
            ActuatorId = actuatorId,
            MinValue = 10,
            MaxValue = 90
        };

        // Act
        await _service.UpdateAsync(id, updateDto);

        // Assert
        _mockControlOutputRepo.Verify(x => x.UpdateAsync(It.Is<ControlOutput>(co =>
            co.Name == "Updated Name" &&
            co.MinValue == 10 &&
            co.MaxValue == 90
        )), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteControlOutput()
    {
        // Arrange
        var id = "507f1f77bcf86cd799439011";
        var controlOutput = CreateTestControlOutput(id, "Temperature Control", "act1");
        _mockControlOutputRepo.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(controlOutput);

        // Act
        await _service.DeleteAsync(id);

        // Assert
        _mockControlOutputRepo.Verify(x => x.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByActuatorIdAsync_ShouldReturnControlOutputsForActuator()
    {
        // Arrange
        var actuatorId = "507f1f77bcf86cd799439011";
        var controlOutputs = new List<ControlOutput>
        {
            CreateTestControlOutput("1", "Output 1", actuatorId),
            CreateTestControlOutput("2", "Output 2", actuatorId)
        };
        _mockControlOutputRepo.Setup(x => x.GetByActuatorIdAsync(actuatorId)).ReturnsAsync(controlOutputs);

        // Act
        var result = await _service.GetByActuatorIdAsync(actuatorId);

        // Assert
        result.Should().HaveCount(2);
        result.All(co => co.ActuatorId == actuatorId).Should().BeTrue();
    }

    // Helper methods
    private static ControlOutput CreateTestControlOutput(string id, string name, string actuatorId)
    {
        var controlOutput = new ControlOutput
        {
            Name = name,
            Description = "Test description",
            Unit = "test-unit",
            ActuatorId = actuatorId,
            MinValue = 0,
            MaxValue = 100,
            LastModified = DateTime.UtcNow
        };
        controlOutput.SetId(id);
        return controlOutput;
    }

    private static Actuator CreateTestActuator(string id)
    {
        var actuator = new Actuator
        {
            Esp32Id = "esp32-test",
            Code = "TEST-ACT",
            Type = HydroEspinaca.Shared.Enums.ActuatorType.Led,
            Mode = HydroEspinaca.Shared.Enums.ActuatorMode.DIGITAL,
            PhysicalId = "PHY-001",
            Pin = "GPIO1",
            Location = "Test Location",
            Status = HydroEspinaca.Shared.Enums.ActuatorStatus.Active
        };
        actuator.SetId(id);
        return actuator;
    }
}
