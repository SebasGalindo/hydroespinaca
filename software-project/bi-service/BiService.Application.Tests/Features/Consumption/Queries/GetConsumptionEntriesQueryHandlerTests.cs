using BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;
using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Consumption.Queries;

public class GetConsumptionEntriesQueryHandlerTests
{
    private readonly Mock<IManualConsumptionEntryRepository> _repo = new();
    private readonly GetConsumptionEntriesQueryHandler _handler;

    public GetConsumptionEntriesQueryHandlerTests()
    {
        _handler = new GetConsumptionEntriesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedEntries()
    {
        var entries = new List<ManualConsumptionEntry>
        {
            CreateEntry("e-1", ConsumptionType.ElectricityKwh, 100m, 80000m),
            CreateEntry("e-2", ConsumptionType.WaterLiters, 200m, 1000m)
        };
        _repo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ConsumptionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("e-1");
        result[0].Type.Should().Be(ConsumptionType.ElectricityKwh);
        result[1].Id.Should().Be("e-2");
        result[1].Type.Should().Be(ConsumptionType.WaterLiters);
    }

    [Fact]
    public async Task Handle_EmptyResults_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ConsumptionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTypeFilter_ShouldPassTypeToRepository()
    {
        _repo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), ConsumptionType.WaterLiters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), ConsumptionType.WaterLiters);
        await _handler.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetByRangeAsync(
            new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), ConsumptionType.WaterLiters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var entry = CreateEntry("e-1", ConsumptionType.NutrientLiters, 50m, 600m);
        _repo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ConsumptionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry> { entry });

        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), null);
        var result = await _handler.Handle(query, CancellationToken.None);

        var dto = result[0];
        dto.Amount.Should().Be(50m);
        dto.CostAmount.Should().Be(600m);
        dto.UnitCostSnapshot.Should().Be(12m);
        dto.CurrencySnapshot.Should().Be("COP");
        dto.Note.Should().Be("test note");
    }

    private static ManualConsumptionEntry CreateEntry(string id, ConsumptionType type, decimal amount, decimal cost)
    {
        var entry = new ManualConsumptionEntry
        {
            DateFrom = new DateTime(2025, 2, 1),
            DateTo = new DateTime(2025, 2, 28),
            Type = type,
            Amount = amount,
            UnitCostSnapshot = 12m,
            CurrencySnapshot = "COP",
            CostConfigVersionId = "cfg-1",
            CostAmount = cost,
            Note = "test note",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = "user-1"
        };
        entry.SetId(id);
        return entry;
    }
}
