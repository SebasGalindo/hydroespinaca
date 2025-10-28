using BffService.Application.Services;
using BffService.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BffService.Application.Tests.Services;

public class CsrfValidationServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<CsrfValidationService>> _mockLogger;
    private readonly CsrfValidationService _service;

    public CsrfValidationServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<CsrfValidationService>>();
        _service = new CsrfValidationService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void IsStateChangingOperation_WithPostMethod_ReturnsTrue()
    {
        // Act
        var result = _service.IsStateChangingOperation("POST");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStateChangingOperation_WithPutMethod_ReturnsTrue()
    {
        // Act
        var result = _service.IsStateChangingOperation("PUT");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStateChangingOperation_WithDeleteMethod_ReturnsTrue()
    {
        // Act
        var result = _service.IsStateChangingOperation("DELETE");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStateChangingOperation_WithPatchMethod_ReturnsTrue()
    {
        // Act
        var result = _service.IsStateChangingOperation("PATCH");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStateChangingOperation_WithGetMethod_ReturnsFalse()
    {
        // Act
        var result = _service.IsStateChangingOperation("GET");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsStateChangingOperation_WithHeadMethod_ReturnsFalse()
    {
        // Act
        var result = _service.IsStateChangingOperation("HEAD");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsStateChangingOperation_WithLowercaseMethod_ReturnsTrue()
    {
        // Act
        var result = _service.IsStateChangingOperation("post");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCsrfToken_WithNoHeaderToken_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCsrfToken_WithEmptyHeaderToken_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-CSRF-Token"] = "";
        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCsrfToken_WithMatchingTokensInCookie_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "test-csrf-token-12345";

        context.Request.Headers["X-CSRF-Token"] = token;
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { "CsrfToken", token }
        });

        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCsrfToken_WithMismatchedTokens_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        context.Request.Headers["X-CSRF-Token"] = "header-token";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { "CsrfToken", "cookie-token" }
        });

        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCsrfToken_WithUrlEncodedHeaderToken_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "test+token+with+encoded+chars";
        var encodedToken = "test%2Btoken%2Bwith%2Bencoded%2Bchars";

        context.Request.Headers["X-CSRF-Token"] = encodedToken;
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { "CsrfToken", token }
        });

        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCsrfToken_WithNoCookieForMobileClient_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "mobile-csrf-token";

        context.Request.Headers["X-CSRF-Token"] = token;
        // No cookie set - simulates mobile client

        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCsrfToken_WithCustomHeaderName_UsesCustomHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "custom-header-token";

        context.Request.Headers["X-Custom-CSRF"] = token;
        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-Custom-CSRF");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCsrfToken_WithDifferentLengthTokens_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        context.Request.Headers["X-CSRF-Token"] = "short";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { "CsrfToken", "much-longer-token" }
        });

        _mockConfiguration.Setup(c => c[It.IsAny<string>()]).Returns("X-CSRF-Token");

        // Act
        var result = _service.ValidateCsrfToken(context);

        // Assert
        result.Should().BeFalse();
    }
}

// Helper class to mock request cookies
public class MockRequestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public MockRequestCookieCollection(Dictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
    public int Count => _cookies.Count;
    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
    {
        if (_cookies.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = null;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
}
