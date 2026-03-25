using System.Text.Json;
using ChatbotService.Application.Features.Rag.Commands.ReindexAll;
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

public class ReindexAllCommandHandlerTests
{
    private readonly Mock<IKnowledgeChunkRepository> _chunkRepoMock = new();
    private readonly Mock<IEmbeddingProvider> _embeddingMock = new();
    private readonly Mock<IFuzzyServiceClient> _fuzzyClientMock = new();
    private readonly Mock<IFuzzyEntityHydratorService> _hydratorMock = new();
    private readonly ContentSerializerService _serializer = new();
    private readonly ILogger<ReindexAllCommandHandler> _logger = NullLogger<ReindexAllCommandHandler>.Instance;
    private readonly ReindexAllCommandHandler _sut;

    public ReindexAllCommandHandlerTests()
    {
        _sut = new ReindexAllCommandHandler(
            _chunkRepoMock.Object,
            _embeddingMock.Object,
            _fuzzyClientMock.Object,
            _hydratorMock.Object,
            _serializer,
            _logger);
    }

    [Fact]
    public async Task Handle_EmptyResults_ShouldReturnZero()
    {
        _fuzzyClientMock
            .Setup(x => x.GetAllEntitiesByTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JsonElement>());

        var result = await _sut.Handle(new ReindexAllCommand(), CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCallAllThreeEntityTypes()
    {
        _fuzzyClientMock
            .Setup(x => x.GetAllEntitiesByTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JsonElement>());

        await _sut.Handle(new ReindexAllCommand(), CancellationToken.None);

        _fuzzyClientMock.Verify(x => x.GetAllEntitiesByTypeAsync("fuzzy_rule", It.IsAny<CancellationToken>()), Times.Once);
        _fuzzyClientMock.Verify(x => x.GetAllEntitiesByTypeAsync("fuzzy_system", It.IsAny<CancellationToken>()), Times.Once);
        _fuzzyClientMock.Verify(x => x.GetAllEntitiesByTypeAsync("fuzzy_variable", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEntities_ShouldGenerateEmbeddingsAndUpsert()
    {
        var sysJson = JsonDocument.Parse("""{"id":"sys-1"}""").RootElement;
        _fuzzyClientMock
            .Setup(x => x.GetAllEntitiesByTypeAsync("fuzzy_system", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JsonElement> { sysJson });
        _fuzzyClientMock
            .Setup(x => x.GetAllEntitiesByTypeAsync(It.Is<string>(s => s != "fuzzy_system"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JsonElement>());

        var hydSys = new HydratedFuzzySystem("sys-1", "Test System", "Desc", "Active", "Centroid", new List<string>(), new List<string>(), new List<string>());
        _hydratorMock
            .Setup(x => x.GetHydratedSystemAsync("sys-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hydSys);

        _embeddingMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), EmbeddingTaskType.RetrievalDocument, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        _chunkRepoMock
            .Setup(x => x.GetBySourceAsync("sys-1", "fuzzy_system", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeChunk?)null);

        var result = await _sut.Handle(new ReindexAllCommand(), CancellationToken.None);

        result.Should().Be(1);
        _embeddingMock.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), EmbeddingTaskType.RetrievalDocument, It.IsAny<CancellationToken>()), Times.Once);
        _chunkRepoMock.Verify(x => x.UpsertAsync(It.Is<KnowledgeChunk>(c => c.SourceId == "sys-1" && c.SourceType == "fuzzy_system" && c.Version == 1), It.IsAny<CancellationToken>()), Times.Once);
    }
}
