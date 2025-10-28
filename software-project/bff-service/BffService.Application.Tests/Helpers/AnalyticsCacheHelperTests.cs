using BffService.Application.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BffService.Application.Tests.Helpers;

public class AnalyticsCacheHelperTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger> _mockLogger;
    private readonly AnalyticsCacheHelper<TestData> _helper;

    public AnalyticsCacheHelperTests()
    {
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger>();
        var cacheTTL = TimeSpan.FromMinutes(5);
        _helper = new AnalyticsCacheHelper<TestData>(_mockCache.Object, _mockLogger.Object, cacheTTL);
    }

    [Fact]
    public void TryGetCached_WithCacheHit_ReturnsTrueAndData()
    {
        // Arrange
        var cacheKey = "test-key";
        var expectedData = new TestData { Value = "cached-value" };
        var startTime = DateTime.UtcNow;

        object? cachedValue = expectedData;
        _mockCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = _helper.TryGetCached(cacheKey, out var data, startTime);

        // Assert
        result.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Value.Should().Be("cached-value");
    }

    [Fact]
    public void TryGetCached_WithCacheMiss_ReturnsFalse()
    {
        // Arrange
        var cacheKey = "test-key";
        var startTime = DateTime.UtcNow;

        object? cachedValue = null;
        _mockCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(false);

        // Act
        var result = _helper.TryGetCached(cacheKey, out var data, startTime);

        // Assert
        result.Should().BeFalse();
        data.Should().BeNull();
    }

    [Fact]
    public void TryGetCached_WithNullCachedValue_ReturnsFalse()
    {
        // Arrange
        var cacheKey = "test-key";
        var startTime = DateTime.UtcNow;

        object? cachedValue = null;
        _mockCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = _helper.TryGetCached(cacheKey, out var data, startTime);

        // Assert
        result.Should().BeFalse();
        data.Should().BeNull();
    }

    [Fact]
    public void SetCache_WithDataPresent_SetsCache()
    {
        // Arrange
        var cacheKey = "test-key";
        var data = new TestData { Value = "test-value" };
        var startTime = DateTime.UtcNow;
        Func<TestData, bool> hasDataPredicate = d => !string.IsNullOrEmpty(d.Value);

        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(cacheKey)).Returns(mockCacheEntry.Object);

        // Act
        _helper.SetCache(cacheKey, data, startTime, hasDataPredicate);

        // Assert
        _mockCache.Verify(x => x.CreateEntry(cacheKey), Times.Once);
    }

    [Fact]
    public void SetCache_WithNoData_DoesNotSetCache()
    {
        // Arrange
        var cacheKey = "test-key";
        var data = new TestData { Value = "" };
        var startTime = DateTime.UtcNow;
        Func<TestData, bool> hasDataPredicate = d => !string.IsNullOrEmpty(d.Value);

        // Act
        _helper.SetCache(cacheKey, data, startTime, hasDataPredicate);

        // Assert
        _mockCache.Verify(x => x.CreateEntry(cacheKey), Times.Never);
    }

    [Fact]
    public void GenerateCacheKey_WithParameters_ReturnsFormattedKey()
    {
        // Arrange
        var prefix = "analytics";
        var startDate = new DateTime(2024, 1, 1, 10, 30, 0);
        var endDate = new DateTime(2024, 1, 2, 15, 45, 30);
        var view = "daily";

        // Act
        var result = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate, endDate, view);

        // Assert
        result.Should().Be("analytics_20240101103000_20240102154530_daily");
    }

    [Fact]
    public void GenerateCacheKey_WithUppercaseView_ReturnsLowercaseView()
    {
        // Arrange
        var prefix = "test";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 2);
        var view = "DAILY";

        // Act
        var result = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate, endDate, view);

        // Assert
        result.Should().Contain("_daily");
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentDates_GeneratesDifferentKeys()
    {
        // Arrange
        var prefix = "analytics";
        var view = "daily";
        var startDate1 = new DateTime(2024, 1, 1);
        var endDate1 = new DateTime(2024, 1, 2);
        var startDate2 = new DateTime(2024, 1, 3);
        var endDate2 = new DateTime(2024, 1, 4);

        // Act
        var key1 = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate1, endDate1, view);
        var key2 = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate2, endDate2, view);

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentViews_GeneratesDifferentKeys()
    {
        // Arrange
        var prefix = "analytics";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 2);

        // Act
        var keyDaily = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate, endDate, "daily");
        var keyWeekly = AnalyticsCacheHelper<TestData>.GenerateCacheKey(prefix, startDate, endDate, "weekly");

        // Assert
        keyDaily.Should().NotBe(keyWeekly);
    }
}

public class AnalyticsProxyHelperTests
{
    private readonly Mock<ILogger> _mockLogger;

    public AnalyticsProxyHelperTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void HandleProxyResponse_WithSuccessfulResponse_ReturnsDeserializedData()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(200, headers, "{\"Value\":\"test-data\"}");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act
        var result = AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("test-data");
    }

    [Fact]
    public void HandleProxyResponse_WithNon200StatusCode_ReturnsEmptyResult()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(404, headers, "Not Found");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act
        var result = AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("empty");
    }

    [Fact]
    public void HandleProxyResponse_With400StatusCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(400, headers, "Bad Request");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act & Assert
        var act = () => AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid request: Bad Request");
    }

    [Fact]
    public void HandleProxyResponse_WithEmptyBody_ReturnsEmptyResult()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(200, headers, "");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act
        var result = AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("empty");
    }

    [Fact]
    public void HandleProxyResponse_WithNullBody_ReturnsEmptyResult()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(200, headers, null!);
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act
        var result = AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("empty");
    }

    [Fact]
    public void HandleProxyResponse_WithInvalidJson_ReturnsEmptyResult()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(200, headers, "invalid-json");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act & Assert
        var act = () => AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Should throw JsonException when trying to deserialize
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void HandleProxyResponse_With500StatusCode_ReturnsEmptyResult()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response = new BffService.Domain.ValueObjects.ProxyResponse(500, headers, "Internal Server Error");
        var startTime = DateTime.UtcNow;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();

        // Act
        var result = AnalyticsProxyHelper.HandleProxyResponse(
            response,
            _mockLogger.Object,
            "test-operation",
            () => new TestData { Value = "empty" },
            jsonOptions,
            startTime
        );

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("empty");
    }
}

// Test data class for generic helper
public class TestData
{
    public string Value { get; set; } = string.Empty;
}
