using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Tests.Entities;

public class ChatSessionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var session = new ChatSession { UserId = "user-1" };

        // Assert
        session.Id.Should().BeEmpty();
        session.UserId.Should().Be("user-1");
        session.Title.Should().Be("Nueva conversación");
        session.Messages.Should().BeEmpty();
        session.IsArchived.Should().BeFalse();
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void SetId_ShouldAssignId()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };

        // Act
        session.SetId("session-abc-123");

        // Assert
        session.Id.Should().Be("session-abc-123");
    }

    [Fact]
    public void AddMessage_ShouldAppendMessageAndUpdateTimestamp()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.UpdatedAt.Should().BeNull();

        var message = new ChatMessage { Role = "user", Content = "Hola, ¿cómo estás?" };

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Should().BeSameAs(message);
        session.UpdatedAt.Should().NotBeNull();
        session.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddMessage_MultipleMessages_ShouldAppendInOrder()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        var msg1 = new ChatMessage { Role = "user", Content = "Pregunta 1" };
        var msg2 = new ChatMessage { Role = "model", Content = "Respuesta 1" };
        var msg3 = new ChatMessage { Role = "user", Content = "Pregunta 2" };

        // Act
        session.AddMessage(msg1);
        var firstUpdate = session.UpdatedAt;

        // Small delay to ensure timestamps differ
        session.AddMessage(msg2);
        session.AddMessage(msg3);

        // Assert
        session.Messages.Should().HaveCount(3);
        session.Messages[0].Content.Should().Be("Pregunta 1");
        session.Messages[1].Content.Should().Be("Respuesta 1");
        session.Messages[2].Content.Should().Be("Pregunta 2");
        session.UpdatedAt.Should().BeOnOrAfter(firstUpdate!.Value);
    }

    [Fact]
    public void AddMessage_ShouldAllowDifferentRoles()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };

        // Act
        session.AddMessage(new ChatMessage { Role = "user", Content = "Hola" });
        session.AddMessage(new ChatMessage { Role = "model", Content = "¡Hola! Soy tu asistente." });
        session.AddMessage(new ChatMessage { Role = "system", Content = "Contexto del sistema." });

        // Assert
        session.Messages.Select(m => m.Role).Should().ContainInOrder("user", "model", "system");
    }
}
