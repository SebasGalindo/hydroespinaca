using BffService.Application.Services;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HydroEspinaca.Shared.DTOs.Analytics;
using System.Text.Json;

namespace BffService.Application.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IProxyService> _mockProxyService;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<AnalyticsService>> _mockLogger;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        _mockProxyService = new Mock<IProxyService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<AnalyticsService>>();
        _service = new AnalyticsService(_mockProxyService.Object, _memoryCache, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var service = new AnalyticsService(_mockProxyService.Object, _memoryCache, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEnvironmentalAggregatesAsync_WithValidRequest_ShouldReturnAggregates()
    {
        // Arrange
        var request = new EnvironmentalAnalyticsRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            View = "daily"
        };

        var responseData = new EnvironmentalAggregatesResponse
        {
            Variables = new List<EnvironmentalAggregateResponse>
            {
                new EnvironmentalAggregateResponse
                {
                    VariableCode = "TEMP",
                    VariableName = "Temperature",
                    Summary = new AggregateSummary(),
                    Trend = new List<AggregateTrendPoint>(),
                    Variability = new List<AggregateVariabilityPoint>()
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.IsAny<ProxyRequest>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), jsonResponse));

        // Act
        var result = await _service.GetEnvironmentalAggregatesAsync(request, "token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().NotBeEmpty();
        result.Variables.First().VariableCode.Should().Be("TEMP");
    }

    [Fact]
    public async Task GetEnvironmentalAggregatesAsync_WithInvalidView_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new EnvironmentalAnalyticsRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            View = "invalid_view"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetEnvironmentalAggregatesAsync(request, "token", CancellationToken.None));
    }

    [Fact]
    public async Task GetEnvironmentalAggregatesAsync_WithStartDateAfterEndDate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new EnvironmentalAnalyticsRequest
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-7),
            View = "daily"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetEnvironmentalAggregatesAsync(request, "token", CancellationToken.None));
    }

    [Fact]
    public async Task GetActuatorAnalyticsAsync_WithValidRequest_ShouldReturnAnalytics()
    {
        // Arrange
        var request = new ActuatorAnalyticsRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            View = "daily"
        };

        var responseData = new ActuatorAnalyticsResponse();

        var jsonResponse = JsonSerializer.Serialize(responseData);

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.IsAny<ProxyRequest>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), jsonResponse));

        // Act
        var result = await _service.GetActuatorAnalyticsAsync(request, "token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActuatorAnalyticsAsync_WithInvalidView_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new ActuatorAnalyticsRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            View = "invalid_view"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetActuatorAnalyticsAsync(request, "token", CancellationToken.None));
    }

    [Fact]
    public async Task GetActuatorAnalyticsAsync_WithStartDateAfterEndDate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new ActuatorAnalyticsRequest
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-7),
            View = "daily"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetActuatorAnalyticsAsync(request, "token", CancellationToken.None));
    }

    [Fact]
    public async Task GetEnvironmentalAggregatesAsync_WhenCached_ShouldReturnCachedResult()
    {
        // Arrange
        var request = new EnvironmentalAnalyticsRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            View = "daily"
        };

        var responseData = new EnvironmentalAggregatesResponse
        {
            Variables = new List<EnvironmentalAggregateResponse>
            {
                new() { VariableCode = "TEMP", VariableName = "Temperature" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);

        _mockProxyService
            .Setup(x => x.ForwardRequestAsync(
                It.IsAny<ProxyRequest>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyResponse(200, new Dictionary<string, string>(), jsonResponse));

        // Act - First call should hit the service
        var result1 = await _service.GetEnvironmentalAggregatesAsync(request, "token", CancellationToken.None);

        // Act - Second call should use cache
        var result2 = await _service.GetEnvironmentalAggregatesAsync(request, "token", CancellationToken.None);

        // Assert
        result2.Should().NotBeNull();
        result2.Variables.Should().HaveCount(1);

        // Verify the proxy service was only called once (first call), not twice
        _mockProxyService.Verify(x => x.ForwardRequestAsync(
            It.IsAny<ProxyRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
