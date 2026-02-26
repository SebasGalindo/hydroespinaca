using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Tests.Entities;

public class KnowledgeChunkTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var chunk = new KnowledgeChunk
        {
            SourceType = "fuzzy_rule",
            SourceId = "rule-001",
            Content = "SI temperatura ES alta ENTONCES ventilador = máxima",
            Embedding = new float[] { 0.1f, 0.2f, 0.3f }
        };

        // Assert
        chunk.Id.Should().BeEmpty();
        chunk.SourceType.Should().Be("fuzzy_rule");
        chunk.SourceId.Should().Be("rule-001");
        chunk.Content.Should().Contain("temperatura");
        chunk.Embedding.Should().HaveCount(3);
        chunk.Metadata.Should().BeNull();
        chunk.Version.Should().Be(1);
        chunk.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        chunk.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void SetId_ShouldAssignObjectId()
    {
        // Arrange
        var chunk = new KnowledgeChunk
        {
            SourceType = "fuzzy_system",
            SourceId = "sys-001",
            Content = "Sistema de control de temperatura",
            Embedding = Array.Empty<float>()
        };

        // Act
        chunk.SetId("65a1b2c3d4e5f6a7b8c9d0e1");

        // Assert
        chunk.Id.Should().Be("65a1b2c3d4e5f6a7b8c9d0e1");
    }

    [Fact]
    public void VersionIncrement_ShouldWorkCorrectly()
    {
        // Arrange
        var chunk = new KnowledgeChunk
        {
            SourceType = "system_manual",
            SourceId = "manual-1",
            Content = "Guía del usuario v1",
            Embedding = new float[] { 0.5f },
            Version = 1
        };

        // Act
        chunk.Version += 1;
        chunk.UpdatedAt = DateTime.UtcNow;

        // Assert
        chunk.Version.Should().Be(2);
        chunk.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Metadata_ShouldAcceptArbitraryObject()
    {
        // Arrange & Act
        var chunk = new KnowledgeChunk
        {
            SourceType = "fuzzy_rule",
            SourceId = "rule-002",
            Content = "Regla con metadata",
            Embedding = new float[] { 0.1f },
            Metadata = new { SystemName = "Invernadero-A", Priority = 3 }
        };

        // Assert
        chunk.Metadata.Should().NotBeNull();
    }
}
