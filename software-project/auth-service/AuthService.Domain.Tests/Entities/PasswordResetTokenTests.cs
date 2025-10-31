using AuthService.Domain.Entities;

namespace AuthService.Domain.Tests.Entities;

public class PasswordResetTokenTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreatePasswordResetTokenInstance()
    {
        // Arrange
        var userId = "user123";
        var code = "ABC123";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // Act
        var token = new PasswordResetToken(userId, code, expiresAt);

        // Assert
        token.Should().NotBeNull();
        token.UserId.Should().Be(userId);
        token.Code.Should().Be(code);
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsUsed.Should().BeFalse();
        token.Id.Should().NotBeNullOrEmpty();
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyUserId_ShouldThrowArgumentException(string userId)
    {
        // Arrange
        var code = "ABC123";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // Act
        Action act = () => new PasswordResetToken(userId, code, expiresAt);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyCode_ShouldThrowArgumentException(string code)
    {
        // Arrange
        var userId = "user123";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // Act
        Action act = () => new PasswordResetToken(userId, code, expiresAt);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("code");
    }

    [Fact]
    public void Constructor_ExpirationDateInPast_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "user123";
        var code = "ABC123";
        var expiresAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        Action act = () => new PasswordResetToken(userId, code, expiresAt);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("expiresAt");
    }

    [Fact]
    public void SetId_ValidId_ShouldSetTokenId()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));
        var tokenId = "token123";

        // Act
        token.SetId(tokenId);

        // Assert
        token.Id.Should().Be(tokenId);
    }

    [Fact]
    public void MarkAsUsed_ValidToken_ShouldMarkTokenAsUsed()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        token.MarkAsUsed();

        // Assert
        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_AlreadyUsedToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));
        token.MarkAsUsed();

        // Act
        Action act = () => token.MarkAsUsed();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Token is already used");
    }

    [Fact]
    public void MarkAsUsed_ExpiredToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMilliseconds(1));
        Thread.Sleep(100); // Ensure token expires

        // Act
        Action act = () => token.MarkAsUsed();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot use an expired token");
    }

    [Fact]
    public void IsExpired_NotExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        var isExpired = token.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ExpiredToken_ShouldReturnTrue()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMilliseconds(1));
        Thread.Sleep(100); // Ensure token expires

        // Act
        var isExpired = token.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        var isValid = token.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_UsedToken_ShouldReturnFalse()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));
        token.MarkAsUsed();

        // Act
        var isValid = token.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMilliseconds(1));
        Thread.Sleep(100); // Ensure token expires

        // Act
        var isValid = token.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_MatchingCode_ShouldReturnTrue()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        var isValid = token.ValidateCode("ABC123");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_NonMatchingCode_ShouldReturnFalse()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        var isValid = token.ValidateCode("XYZ789");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_CaseSensitive_ShouldReturnFalse()
    {
        // Arrange
        var token = new PasswordResetToken("user123", "ABC123", DateTime.UtcNow.AddMinutes(15));

        // Act
        var isValid = token.ValidateCode("abc123");

        // Assert
        isValid.Should().BeFalse();
    }
}
