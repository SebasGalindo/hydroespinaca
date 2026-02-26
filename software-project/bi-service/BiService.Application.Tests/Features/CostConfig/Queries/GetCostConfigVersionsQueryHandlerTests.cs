using BiService.Application.Features.CostConfig.Queries.GetCostConfigVersions;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.CostConfig.Queries;

public class GetCostConfigVersionsQueryHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _repo = new();
    private readonly GetCostConfigVersionsQueryHandler _handler;

    public GetCostConfigVersionsQueryHandlerTests()
    {
        _handler = new GetCostConfigVersionsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedVersions()
    {
        var v1 = new CostConfigVersion
        {
            Currency = "COP", ElectricityCostPerKwh = 800m, WaterCostPerLiter = 5m,
            NutrientCostPerLiter = 12m, EffectiveFrom = new DateTime(2025, 1, 1), IsActive = false
        };
        v1.SetId("v-1");
        var v2 = new CostConfigVersion
        {
            Currency = "COP", ElectricityCostPerKwh = 900m, WaterCostPerLiter = 6m,
            NutrientCostPerLiter = 14m, EffectiveFrom = new DateTime(2025, 6, 1), IsActive = true
        };
        v2.SetId("v-2");

        _repo.Setup(r => r.GetVersionsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { v1, v2 });

        var query = new GetCostConfigVersionsQuery(null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("v-1");
        result[0].IsActive.Should().BeFalse();
        result[1].Id.Should().Be("v-2");
        result[1].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyVersions_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetVersionsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion>());

        var result = await _handler.Handle(new GetCostConfigVersionsQuery(null, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithDateFilters_ShouldPassThemToRepository()
    {
        _repo.Setup(r => r.GetVersionsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion>());

        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);
        await _handler.Handle(new GetCostConfigVersionsQuery(from, to), CancellationToken.None);

        _repo.Verify(r => r.GetVersionsAsync(from, to, It.IsAny<CancellationToken>()), Times.Once);
    }
}
