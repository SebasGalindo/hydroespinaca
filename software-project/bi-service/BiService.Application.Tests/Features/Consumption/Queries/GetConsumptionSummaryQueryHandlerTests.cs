using BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;
using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Consumption.Queries;

public class GetConsumptionSummaryQueryHandlerTests
{
    private readonly Mock<IManualConsumptionEntryRepository> _entriesRepo = new();
    private readonly Mock<ICostConfigVersionRepository> _costConfigRepo = new();
    private readonly GetConsumptionSummaryQueryHandler _handler;

    public GetConsumptionSummaryQueryHandlerTests()
    {
        _handler = new GetConsumptionSummaryQueryHandler(_entriesRepo.Object, _costConfigRepo.Object);
    }

    [Fact]
    public async Task Handle_ShouldAggregateByType()
    {
        var entries = new List<ManualConsumptionEntry>
        {
            MakeEntry(ConsumptionType.ElectricityKwh, 100m, 80000m),
            MakeEntry(ConsumptionType.ElectricityKwh, 50m, 40000m),
            MakeEntry(ConsumptionType.WaterLiters, 200m, 1000m),
            MakeEntry(ConsumptionType.NutrientLiters, 30m, 360m)
        };
        _entriesRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CostConfigVersion { Currency = "COP" });

        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalElectricityKwh.Should().Be(150m);
        result.TotalWaterLiters.Should().Be(200m);
        result.TotalNutrientLiters.Should().Be(30m);
        result.CostElectricity.Should().Be(120000m);
        result.CostWater.Should().Be(1000m);
        result.CostNutrients.Should().Be(360m);
        result.CostTotal.Should().Be(121360m);
    }

    [Fact]
    public async Task Handle_EmptyEntries_ShouldReturnZeros()
    {
        _entriesRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CostConfigVersion { Currency = "COP" });

        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalElectricityKwh.Should().Be(0m);
        result.TotalWaterLiters.Should().Be(0m);
        result.TotalNutrientLiters.Should().Be(0m);
        result.CostTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_NoActiveConfig_ShouldUseCOPDefault()
    {
        _entriesRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion?)null);

        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_ShouldSetFromAndTo()
    {
        _entriesRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion?)null);

        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 6, 30);
        var query = new GetConsumptionSummaryQuery(from, to);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.From.Should().Be(from);
        result.To.Should().Be(to);
    }

    private static ManualConsumptionEntry MakeEntry(ConsumptionType type, decimal amount, decimal cost)
    {
        return new ManualConsumptionEntry
        {
            Type = type,
            Amount = amount,
            CostAmount = cost,
            DateFrom = new DateTime(2025, 2, 1),
            DateTo = new DateTime(2025, 2, 28)
        };
    }
}
