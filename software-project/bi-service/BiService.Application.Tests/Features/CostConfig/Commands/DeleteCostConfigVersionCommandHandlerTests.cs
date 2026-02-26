using BiService.Application.Features.CostConfig.Commands.DeleteCostConfigVersion;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Tests.Features.CostConfig.Commands;

public class DeleteCostConfigVersionCommandHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _repo = new();
    private readonly DeleteCostConfigVersionCommandHandler _handler;

    public DeleteCostConfigVersionCommandHandlerTests()
    {
        _handler = new DeleteCostConfigVersionCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_VersionExists_ShouldDeleteAndReturnUnit()
    {
        _repo.Setup(r => r.DeleteVersionAsync("v-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new DeleteCostConfigVersionCommand("v-1"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        _repo.Verify(r => r.DeleteVersionAsync("v-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VersionNotFound_ShouldThrowKeyNotFound()
    {
        _repo.Setup(r => r.DeleteVersionAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new DeleteCostConfigVersionCommand("missing"), CancellationToken.None));
    }
}
