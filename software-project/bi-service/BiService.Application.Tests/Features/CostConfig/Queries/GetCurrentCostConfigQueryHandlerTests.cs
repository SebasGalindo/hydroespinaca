using BiService.Application.Features.CostConfig.Queries.GetCurrentCostConfig;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.CostConfig.Queries;

public class GetCurrentCostConfigQueryHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _repo = new();
    private readonly GetCurrentCostConfigQueryHandler _handler;

    public GetCurrentCostConfigQueryHandlerTests()
    {
        _handler = new GetCurrentCostConfigQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ActiveConfigExists_ShouldReturnDto()
    {
        var config = new CostConfigVersion
        {
            Currency = "COP", ElectricityCostPerKwh = 800m, WaterCostPerLiter = 5m,
            NutrientCostPerLiter = 12m, EffectiveFrom = new DateTime(2025, 1, 1), IsActive = true
        };
        config.SetId("v-1");
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _handler.Handle(new GetCurrentCostConfigQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("v-1");
        result.Currency.Should().Be("COP");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoActiveConfig_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion?)null);

        var result = await _handler.Handle(new GetCurrentCostConfigQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var config = new CostConfigVersion
        {
            Currency = "USD", ElectricityCostPerKwh = 150m, WaterCostPerLiter = 2m,
            NutrientCostPerLiter = 8m, EffectiveFrom = new DateTime(2025, 3, 1),
            EffectiveTo = new DateTime(2025, 12, 31), IsActive = true,
            CreatedAt = new DateTime(2025, 2, 28)
        };
        config.SetId("v-2");
        _repo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _handler.Handle(new GetCurrentCostConfigQuery(), CancellationToken.None);

        result!.ElectricityCostPerKwh.Should().Be(150m);
        result.WaterCostPerLiter.Should().Be(2m);
        result.NutrientCostPerLiter.Should().Be(8m);
        result.EffectiveFrom.Should().Be(new DateTime(2025, 3, 1));
        result.EffectiveTo.Should().Be(new DateTime(2025, 12, 31));
        result.CreatedAt.Should().Be(new DateTime(2025, 2, 28));
    }
}
