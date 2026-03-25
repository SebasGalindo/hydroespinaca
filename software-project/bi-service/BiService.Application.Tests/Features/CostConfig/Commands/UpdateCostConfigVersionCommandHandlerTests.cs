using BiService.Application.Features.CostConfig.Commands.UpdateCostConfigVersion;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.CostConfig.Commands;

public class UpdateCostConfigVersionCommandHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _repo = new();
    private readonly UpdateCostConfigVersionCommandHandler _handler;

    public UpdateCostConfigVersionCommandHandlerTests()
    {
        _handler = new UpdateCostConfigVersionCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_VersionExists_ShouldUpdateAndReturnDto()
    {
        var existing = new CostConfigVersion
        {
            Currency = "COP",
            ElectricityCostPerKwh = 500m,
            WaterCostPerLiter = 3m,
            NutrientCostPerLiter = 8m,
            EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true
        };
        existing.SetId("v-1");
        _repo.Setup(r => r.GetByIdAsync("v-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => v);

        var cmd = new UpdateCostConfigVersionCommand("v-1", "USD", 900m, 7m, 15m, new DateTime(2025, 6, 1), null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Id.Should().Be("v-1");
        result.Currency.Should().Be("USD");
        result.ElectricityCostPerKwh.Should().Be(900m);
        result.WaterCostPerLiter.Should().Be(7m);
        result.NutrientCostPerLiter.Should().Be(15m);
        result.EffectiveFrom.Should().Be(new DateTime(2025, 6, 1));
    }

    [Fact]
    public async Task Handle_VersionNotFound_ShouldThrowKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion?)null);

        var cmd = new UpdateCostConfigVersionCommand("missing", "COP", 100m, 1m, 2m, null, null, "user-1");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyCurrency_ShouldDefaultToCOP()
    {
        var existing = new CostConfigVersion { EffectiveFrom = new DateTime(2025, 1, 1) };
        existing.SetId("v-1");
        _repo.Setup(r => r.GetByIdAsync("v-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => v);

        var cmd = new UpdateCostConfigVersionCommand("v-1", "", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_NullEffectiveFrom_ShouldKeepExisting()
    {
        var existing = new CostConfigVersion { EffectiveFrom = new DateTime(2025, 1, 1) };
        existing.SetId("v-1");
        _repo.Setup(r => r.GetByIdAsync("v-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => v);

        var cmd = new UpdateCostConfigVersionCommand("v-1", "COP", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.EffectiveFrom.Should().Be(new DateTime(2025, 1, 1));
    }

    [Fact]
    public async Task Handle_ShouldSetEffectiveTo()
    {
        var existing = new CostConfigVersion { EffectiveFrom = new DateTime(2025, 1, 1) };
        existing.SetId("v-1");
        _repo.Setup(r => r.GetByIdAsync("v-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => v);

        var endDate = new DateTime(2025, 12, 31);
        var cmd = new UpdateCostConfigVersionCommand("v-1", "COP", 100m, 1m, 2m, null, endDate, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.EffectiveTo.Should().Be(endDate);
    }
}
