using BiService.Application.Features.Production.Commands.DeleteProductionRecord;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Production.Commands;

public class DeleteProductionRecordCommandHandlerTests
{
    private readonly Mock<IProductionRecordRepository> _repo = new();
    private readonly DeleteProductionRecordCommandHandler _handler;

    public DeleteProductionRecordCommandHandlerTests()
    {
        _handler = new DeleteProductionRecordCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_RecordExists_ShouldDeleteAndReturnTrue()
    {
        var record = new ProductionRecord { CropName = "Espinaca" };
        record.SetId("p-1");
        _repo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _repo.Setup(r => r.DeleteAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new DeleteProductionRecordCommand("p-1"), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RecordNotFound_ShouldThrowKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new DeleteProductionRecordCommand("missing"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteAfterGet()
    {
        var record = new ProductionRecord();
        record.SetId("p-1");
        _repo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _repo.Setup(r => r.DeleteAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(new DeleteProductionRecordCommand("p-1"), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.DeleteAsync("p-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
