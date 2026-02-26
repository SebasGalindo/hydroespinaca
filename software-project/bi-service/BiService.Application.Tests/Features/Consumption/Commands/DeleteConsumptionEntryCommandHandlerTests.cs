using BiService.Application.Features.Consumption.Commands.DeleteConsumptionEntry;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Consumption.Commands;

public class DeleteConsumptionEntryCommandHandlerTests
{
    private readonly Mock<IManualConsumptionEntryRepository> _repo = new();
    private readonly DeleteConsumptionEntryCommandHandler _handler;

    public DeleteConsumptionEntryCommandHandlerTests()
    {
        _handler = new DeleteConsumptionEntryCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_EntryExists_ShouldDeleteAndReturnTrue()
    {
        var entry = new ManualConsumptionEntry();
        entry.SetId("e-1");
        _repo.Setup(r => r.GetByIdAsync("e-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repo.Setup(r => r.DeleteAsync("e-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new DeleteConsumptionEntryCommand("e-1"), CancellationToken.None);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync("e-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EntryNotFound_ShouldThrowKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new DeleteConsumptionEntryCommand("missing"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCallGetByIdFirst()
    {
        var entry = new ManualConsumptionEntry();
        entry.SetId("e-1");
        _repo.Setup(r => r.GetByIdAsync("e-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repo.Setup(r => r.DeleteAsync("e-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(new DeleteConsumptionEntryCommand("e-1"), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync("e-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
