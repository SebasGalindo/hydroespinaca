using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Tests.Entities;

public class ChatMessageTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var message = new ChatMessage { Role = "user", Content = "¿Qué sensores activos hay?" };

        // Assert
        message.Role.Should().Be("user");
        message.Content.Should().Be("¿Qué sensores activos hay?");
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.TokensUsed.Should().BeNull();
    }

    [Fact]
    public void ModelMessage_ShouldAcceptTokensUsed()
    {
        // Act
        var message = new ChatMessage
        {
            Role = "model",
            Content = "Actualmente hay 3 sensores activos...",
            TokensUsed = 42
        };

        // Assert
        message.Role.Should().Be("model");
        message.TokensUsed.Should().Be(42);
    }

    [Fact]
    public void SystemMessage_ShouldHaveNullTokens()
    {
        // Act
        var message = new ChatMessage { Role = "system", Content = "Instrucciones del sistema." };

        // Assert
        message.Role.Should().Be("system");
        message.TokensUsed.Should().BeNull();
    }
}
