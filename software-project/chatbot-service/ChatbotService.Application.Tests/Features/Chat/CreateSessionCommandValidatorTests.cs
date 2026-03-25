using ChatbotService.Application.Features.Chat.Commands.CreateSession;

namespace ChatbotService.Application.Tests.Features.Chat;

public class CreateSessionCommandValidatorTests
{
    private readonly CreateSessionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateSessionCommand("user-abc-123");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_EmptyOrNullUserId_ShouldFail(string? userId)
    {
        // Arrange
        var command = new CreateSessionCommand(userId!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
