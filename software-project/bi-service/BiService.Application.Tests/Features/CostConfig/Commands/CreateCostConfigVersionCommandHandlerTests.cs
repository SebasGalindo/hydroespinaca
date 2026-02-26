using BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.CostConfig.Commands;

public class CreateCostConfigVersionCommandHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _repo = new();
    private readonly CreateCostConfigVersionCommandHandler _handler;

    public CreateCostConfigVersionCommandHandlerTests()
    {
        _handler = new CreateCostConfigVersionCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateAndReturnDto()
    {
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var cmd = new CreateCostConfigVersionCommand("COP", 800m, 5m, 12m, new DateTime(2025, 1, 1), null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be("v-1");
        result.Currency.Should().Be("COP");
        result.ElectricityCostPerKwh.Should().Be(800m);
        result.WaterCostPerLiter.Should().Be(5m);
        result.NutrientCostPerLiter.Should().Be(12m);
    }

    [Fact]
    public async Task Handle_EmptyCurrency_ShouldDefaultToCOP()
    {
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var cmd = new CreateCostConfigVersionCommand("", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_WhitespaceCurrency_ShouldDefaultToCOP()
    {
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var cmd = new CreateCostConfigVersionCommand("  ", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_CurrencyShouldBeUppercase()
    {
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var cmd = new CreateCostConfigVersionCommand("usd", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_NoEffectiveFrom_ShouldDefaultToNow()
    {
        var before = DateTime.UtcNow;
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var cmd = new CreateCostConfigVersionCommand("COP", 100m, 1m, 2m, null, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.EffectiveFrom.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Handle_WithEffectiveFrom_ShouldUseProvided()
    {
        _repo.Setup(r => r.CreateVersionAsync(It.IsAny<CostConfigVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion v, CancellationToken _) => { v.SetId("v-1"); return v; });

        var date = new DateTime(2025, 6, 1);
        var cmd = new CreateCostConfigVersionCommand("COP", 100m, 1m, 2m, date, null, "user-1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.EffectiveFrom.Should().Be(date);
    }
}
