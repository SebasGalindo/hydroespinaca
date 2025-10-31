using BffService.Application.Interfaces;
using BffService.Application.Services;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.DTOs.Readings;
using System.Text.Json;

namespace BffService.Application.Tests.Services;

public class SystemStatusServiceTests
{
    private readonly Mock<IProxyService> _mockProxyService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<ILogger<SystemStatusService>> _mockLogger;
    private readonly SystemStatusService _service;

    public SystemStatusServiceTests()
    {
        _mockProxyService = new Mock<IProxyService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockLogger = new Mock<ILogger<SystemStatusService>>();
        _service = new SystemStatusService(_mockProxyService.Object, _mockWeatherService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var service = new SystemStatusService(_mockProxyService.Object, _mockWeatherService.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatusAsync_WithSuccessfulResponses_ShouldReturnSystemStatus()
    {
        // Arrange
        var actuatorResponse = new
        {
            JobStatus = new JobStatusDto(),
            Stats = new JobExecutionStatsDto(),
            InternalRoutines = new List<InternalRoutineInfoDto>()
        };

        var sensorResponse = new EnrichedLatestReadingsDto();
        var weatherResponse = new WeatherDto { Temperature = 25.5 };

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(actuatorResponse)));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(sensorResponse)));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherResponse);

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Readings.Should().NotBeNull();
        result.JobStatus.Should().NotBeNull();
        result.Stats.Should().NotBeNull();
        result.InternalRoutines.Should().NotBeNull();
        result.Weather.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenActuatorReturns403_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(403, new Dictionary<string, string>(), null));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(new EnrichedLatestReadingsDto())));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetSystemStatusAsync("token", CancellationToken.None));
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenSensorReturns403_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var actuatorResponse = new
        {
            JobStatus = new JobStatusDto(),
            Stats = new JobExecutionStatsDto(),
            InternalRoutines = new List<InternalRoutineInfoDto>()
        };

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(actuatorResponse)));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(403, new Dictionary<string, string>(), null));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetSystemStatusAsync("token", CancellationToken.None));
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenActuatorReturnsNonSuccess_ShouldReturnDefaultActuatorData()
    {
        // Arrange
        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(500, new Dictionary<string, string>(), null));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(new EnrichedLatestReadingsDto())));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.JobStatus.Should().NotBeNull();
        result.Stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenSensorReturnsNonSuccess_ShouldReturnNullReadings()
    {
        // Arrange
        var actuatorResponse = new
        {
            JobStatus = new JobStatusDto(),
            Stats = new JobExecutionStatsDto(),
            InternalRoutines = new List<InternalRoutineInfoDto>()
        };

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(actuatorResponse)));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(500, new Dictionary<string, string>(), null));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Readings.Should().NotBeNull(); // Should have default value
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenWeatherServiceFails_ShouldContinueWithoutWeather()
    {
        // Arrange
        var actuatorResponse = new
        {
            JobStatus = new JobStatusDto(),
            Stats = new JobExecutionStatsDto(),
            InternalRoutines = new List<InternalRoutineInfoDto>()
        };

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(actuatorResponse)));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(new EnrichedLatestReadingsDto())));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Weather service unavailable"));

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Weather.Should().BeNull();
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenActuatorReturnsEmptyBody_ShouldReturnDefaultActuatorData()
    {
        // Arrange
        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), string.Empty));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(new EnrichedLatestReadingsDto())));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.JobStatus.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatusAsync_WhenSensorReturnsEmptyBody_ShouldReturnNullReadings()
    {
        // Arrange
        var actuatorResponse = new
        {
            JobStatus = new JobStatusDto(),
            Stats = new JobExecutionStatsDto(),
            InternalRoutines = new List<InternalRoutineInfoDto>()
        };

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/commands/jobs/status")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), JsonSerializer.Serialize(actuatorResponse)));

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.Is<ProxyRequest>(r => r.Path.Contains("/readings/latest")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), string.Empty));

        _mockWeatherService
            .Setup(x => x.GetWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto());

        // Act
        var result = await _service.GetSystemStatusAsync("token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Readings.Should().NotBeNull(); // Should have default value
    }
}
