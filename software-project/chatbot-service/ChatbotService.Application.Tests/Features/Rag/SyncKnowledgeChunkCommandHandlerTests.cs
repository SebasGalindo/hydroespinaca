using System.Text.Json;
using ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Domain.Models.Fuzzy;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;

namespace ChatbotService.Application.Tests.Features.Rag;

public class SyncKnowledgeChunkCommandHandlerTests
{
    private readonly Mock<IKnowledgeChunkRepository> _chunkRepoMock = new();
    private readonly Mock<IEmbeddingProvider> _embeddingMock = new();
    private readonly Mock<IFuzzyServiceClient> _fuzzyClientMock = new();
    private readonly Mock<IFuzzyEntityHydratorService> _hydratorMock = new();
    private readonly ContentSerializerService _serializer = new();
    private readonly ILogger<SyncKnowledgeChunkCommandHandler> _logger = NullLogger<SyncKnowledgeChunkCommandHandler>.Instance;
    private readonly SyncKnowledgeChunkCommandHandler _sut;

    public SyncKnowledgeChunkCommandHandlerTests()
    {
        _sut = new SyncKnowledgeChunkCommandHandler(
            _chunkRepoMock.Object,
            _embeddingMock.Object,
            _fuzzyClientMock.Object,
            _hydratorMock.Object,
            _serializer,
            _logger);
    }

    [Fact]
    public async Task Handle_DeleteAction_ShouldDeleteAndReturnTrue()
    {
        _chunkRepoMock
            .Setup(x => x.DeleteBySourceAsync("sys-1", "fuzzy_system", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.Handle(new SyncKnowledgeChunkCommand("fuzzy_system", "sys-1", "delete"), CancellationToken.None);

        result.Should().BeTrue();
        _chunkRepoMock.Verify(x => x.DeleteBySourceAsync("sys-1", "fuzzy_system", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpsertSystem_ShouldHydrateAndIndex()
    {
        var hydSys = new HydratedFuzzySystem("sys-1", "Sys 1", "Desc", "active", "centroid", new List<string>(), new List<string>(), new List<string>());
        _hydratorMock
            .Setup(x => x.GetHydratedSystemAsync("sys-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hydSys);

        _embeddingMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), EmbeddingTaskType.RetrievalDocument, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        var result = await _sut.Handle(new SyncKnowledgeChunkCommand("fuzzy_system", "sys-1", "upsert"), CancellationToken.None);

        result.Should().BeTrue();
        _chunkRepoMock.Verify(x => x.UpsertAsync(It.IsAny<KnowledgeChunk>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TermUpsert_ShouldRedirectToVariable()
    {
        var termJson = JsonDocument.Parse("""{"id":"term-1", "variable_id":"var-1"}""").RootElement;
        _fuzzyClientMock
            .Setup(x => x.GetEntityByIdAsync("fuzzy_term", "term-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(termJson);

        var hydVar = new HydratedFuzzyVariable("var-1", "Var 1", "Sys 1", "input", "desc", 0, 10, "VAR1", new List<HydratedFuzzyTerm>());
        _hydratorMock
            .Setup(x => x.GetHydratedVariableAsync("var-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hydVar);

        _embeddingMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), EmbeddingTaskType.RetrievalDocument, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.5f });

        var result = await _sut.Handle(new SyncKnowledgeChunkCommand("fuzzy_term", "term-1", "upsert"), CancellationToken.None);

        result.Should().BeTrue();
        _chunkRepoMock.Verify(x => x.UpsertAsync(It.Is<KnowledgeChunk>(c => c.SourceId == "var-1" && c.SourceType == "fuzzy_variable"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
