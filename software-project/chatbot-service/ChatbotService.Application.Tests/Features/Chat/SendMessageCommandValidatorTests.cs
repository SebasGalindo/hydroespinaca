using ChatbotService.Application.Features.Chat.Commands.SendMessage;

namespace ChatbotService.Application.Tests.Features.Chat;

public class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura tiene el invernadero?", null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptySessionId_ShouldFail(string? sessionId)
    {
        // Arrange
        var command = new SendMessageCommand(sessionId!, "user-1", "Hola", null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyUserId_ShouldFail(string? userId)
    {
        // Arrange
        var command = new SendMessageCommand("session-1", userId!, "Hola", null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyMessage_ShouldFail(string? message)
    {
        // Arrange
        var command = new SendMessageCommand("session-1", "user-1", message!, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Message");
    }

    [Fact]
    public void Validate_MessageTooLong_ShouldFail()
    {
        // Arrange
        var longMessage = new string('X', 4001);
        var command = new SendMessageCommand("session-1", "user-1", longMessage, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Message" &&
            e.ErrorMessage.Contains("4000"));
    }

    [Fact]
    public void Validate_MessageExactlyAtLimit_ShouldPass()
    {
        // Arrange
        var exactMessage = new string('A', 4000);
        var command = new SendMessageCommand("session-1", "user-1", exactMessage, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllFieldsEmpty_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new SendMessageCommand("", "", "", null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
