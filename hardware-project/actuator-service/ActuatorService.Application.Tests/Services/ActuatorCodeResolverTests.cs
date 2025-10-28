using ActuatorService.Application.Services;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Tests.Services;

/// <summary>
/// Tests unitarios para ActuatorCodeResolver
/// Cobertura: resolución de códigos de actuadores a entidades físicas
/// </summary>
public class ActuatorCodeResolverTests
{
    private readonly Mock<IActuatorRepository> _mockActuatorRepository;
    private readonly ActuatorCodeResolver _resolver;

    public ActuatorCodeResolverTests()
    {
        _mockActuatorRepository = new Mock<IActuatorRepository>();
        _resolver = new ActuatorCodeResolver(_mockActuatorRepository.Object);
    }

    [Fact]
    public async Task ResolveAsync_WhenActuatorExists_ShouldReturnActuator()
    {
        // Arrange
        const string actuatorCode = "BombaRiego";
        var expectedActuator = new Actuator
        {
            Code = actuatorCode,
            Esp32Id = "esp32-001",
            Pin = "GPIO_15",
            Type = ActuatorType.Pump,
            Mode = ActuatorMode.DIGITAL,
            PhysicalId = "PUMP-001",
            Location = "Reservoir",
            Status = ActuatorStatus.Active
        };
        expectedActuator.SetId("actuator-001");

        var allActuators = new List<Actuator> { expectedActuator };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(actuatorCode);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("actuator-001");
        result.Code.Should().Be(actuatorCode);
        result.Esp32Id.Should().Be("esp32-001");
        result.Pin.Should().Be("GPIO_15");
    }

    [Fact]
    public async Task ResolveAsync_WhenActuatorNotExists_ShouldThrowActuatorNotFoundException()
    {
        // Arrange
        const string actuatorCode = "NonExistentActuator";
        var allActuators = new List<Actuator>
        {
            CreateActuator("BombaRiego", "GPIO_15"),
            CreateActuator("BombaAire", "GPIO_20")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var act = async () => await _resolver.ResolveAsync(actuatorCode);

        // Assert
        await act.Should().ThrowAsync<ActuatorNotFoundException>()
            .WithMessage($"*{actuatorCode}*");
    }

    [Fact]
    public async Task ResolveAsync_MultipleActuatorsInRepository_ShouldReturnCorrectOne()
    {
        // Arrange
        const string targetCode = "Ventiladores";
        var allActuators = new List<Actuator>
        {
            CreateActuator("BombaRiego", "GPIO_15"),
            CreateActuator("BombaAire", "GPIO_20"),
            CreateActuator("Ventiladores", "GPIO_25"),
            CreateActuator("Luz de Espectro Completo", "GPIO_30")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(targetCode);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(targetCode);
        result.Pin.Should().Be("GPIO_25");
    }

    [Fact]
    public async Task ResolveAsync_EmptyRepository_ShouldThrowActuatorNotFoundException()
    {
        // Arrange
        const string actuatorCode = "BombaRiego";
        var allActuators = new List<Actuator>();
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var act = async () => await _resolver.ResolveAsync(actuatorCode);

        // Assert
        await act.Should().ThrowAsync<ActuatorNotFoundException>();
    }

    [Theory]
    [InlineData("BombaRiego")]
    [InlineData("BombaAire")]
    [InlineData("Ventiladores")]
    [InlineData("Luz de Espectro Completo")]
    [InlineData("Calefactor de Agua")]
    public async Task ResolveAsync_CommonActuatorCodes_ShouldResolveSuccessfully(string actuatorCode)
    {
        // Arrange
        var allActuators = new List<Actuator>
        {
            CreateActuator("BombaRiego", "GPIO_15"),
            CreateActuator("BombaAire", "GPIO_20"),
            CreateActuator("Ventiladores", "GPIO_25"),
            CreateActuator("Luz de Espectro Completo", "GPIO_30"),
            CreateActuator("Calefactor de Agua", "GPIO_35")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(actuatorCode);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(actuatorCode);
    }

    [Fact]
    public async Task ResolveAsync_CaseSensitiveCode_ShouldMatchExactly()
    {
        // Arrange
        const string correctCode = "BombaRiego";
        const string incorrectCode = "bombariego"; // lowercase
        var allActuators = new List<Actuator>
        {
            CreateActuator(correctCode, "GPIO_15")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act - correct case
        var resultCorrect = await _resolver.ResolveAsync(correctCode);
        
        // Act - incorrect case
        var actIncorrect = async () => await _resolver.ResolveAsync(incorrectCode);

        // Assert
        resultCorrect.Should().NotBeNull();
        resultCorrect.Code.Should().Be(correctCode);
        
        await actIncorrect.Should().ThrowAsync<ActuatorNotFoundException>();
    }

    [Fact]
    public async Task ResolveAsync_DuplicateCodesInRepository_ShouldReturnFirstMatch()
    {
        // Arrange
        const string duplicateCode = "BombaRiego";
        var allActuators = new List<Actuator>
        {
            CreateActuator(duplicateCode, "GPIO_15", "actuator-001"),
            CreateActuator(duplicateCode, "GPIO_20", "actuator-002") // Duplicate
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(duplicateCode);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(duplicateCode);
        result.Id.Should().Be("actuator-001"); // First match
    }

    [Fact]
    public async Task ResolveAsync_RepositoryCallsOnce_ShouldBeOptimized()
    {
        // Arrange
        const string actuatorCode = "BombaRiego";
        var allActuators = new List<Actuator>
        {
            CreateActuator(actuatorCode, "GPIO_15")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        await _resolver.ResolveAsync(actuatorCode);

        // Assert
        _mockActuatorRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_MultipleCallsSequentially_ShouldCallRepositoryEachTime()
    {
        // Arrange
        var allActuators = new List<Actuator>
        {
            CreateActuator("BombaRiego", "GPIO_15"),
            CreateActuator("BombaAire", "GPIO_20")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        await _resolver.ResolveAsync("BombaRiego");
        await _resolver.ResolveAsync("BombaAire");
        await _resolver.ResolveAsync("BombaRiego");

        // Assert
        _mockActuatorRepository.Verify(r => r.GetAllAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task ResolveAsync_WithSpecialCharactersInCode_ShouldResolveCorrectly()
    {
        // Arrange
        const string actuatorCode = "Luz de Espectro Completo"; // Contains spaces
        var allActuators = new List<Actuator>
        {
            CreateActuator(actuatorCode, "GPIO_30")
        };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(actuatorCode);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(actuatorCode);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsActuatorWithAllProperties_ShouldBeComplete()
    {
        // Arrange
        const string actuatorCode = "TestActuator";
        var expectedActuator = new Actuator
        {
            Code = actuatorCode,
            Esp32Id = "esp32-test",
            Pin = "GPIO_99",
            Type = ActuatorType.Led,
            Mode = ActuatorMode.PWM,
            PhysicalId = "TEST-001",
            Location = "Test Location",
            Status = ActuatorStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        expectedActuator.SetId("test-actuator-id");

        var allActuators = new List<Actuator> { expectedActuator };
        _mockActuatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allActuators);

        // Act
        var result = await _resolver.ResolveAsync(actuatorCode);

        // Assert
        result.Should().BeEquivalentTo(expectedActuator, options => options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1)))
            .WhenTypeIs<DateTime>());
    }

    // Helper method to create test actuators
    private static Actuator CreateActuator(string code, string pin, string? id = null)
    {
        var actuator = new Actuator
        {
            Code = code,
            Esp32Id = "esp32-001",
            Pin = pin,
            Type = ActuatorType.Pump,
            Mode = ActuatorMode.DIGITAL,
            PhysicalId = $"ACTUATOR-{code}",
            Location = "Test Location",
            Status = ActuatorStatus.Active
        };
        actuator.SetId(id ?? $"actuator-{Guid.NewGuid()}");
        return actuator;
    }
}
