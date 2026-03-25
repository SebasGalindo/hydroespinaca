using ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;

namespace ChatbotService.Application.Tests.Features.Rag;

public class SyncKnowledgeChunkCommandValidatorTests
{
    private readonly SyncKnowledgeChunkCommandValidator _validator = new();

    [Theory]
    [InlineData("fuzzy_rule", "rule-1", "upsert")]
    [InlineData("fuzzy_system", "sys-1", "delete")]
    [InlineData("fuzzy_variable", "var-1", "upsert")]
    [InlineData("fuzzy_term", "term-1", "delete")]
    [InlineData("system_manual", "manual-1", "upsert")]
    public void Validate_ValidCommand_ShouldPass(string sourceType, string sourceId, string action)
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand(sourceType, sourceId, action);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptySourceType_ShouldFail(string? sourceType)
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand(sourceType!, "id-1", "upsert");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceType");
    }

    [Fact]
    public void Validate_InvalidSourceType_ShouldFail()
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand("invalid_type", "id-1", "upsert");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "SourceType" &&
            e.ErrorMessage.Contains("fuzzy_rule"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptySourceId_ShouldFail(string? sourceId)
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand("fuzzy_rule", sourceId!, "upsert");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyAction_ShouldFail(string? action)
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand("fuzzy_rule", "id-1", action!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Action");
    }

    [Fact]
    public void Validate_InvalidAction_ShouldFail()
    {
        // Arrange
        var command = new SyncKnowledgeChunkCommand("fuzzy_rule", "id-1", "update");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Action" &&
            e.ErrorMessage.Contains("upsert"));
    }
}
