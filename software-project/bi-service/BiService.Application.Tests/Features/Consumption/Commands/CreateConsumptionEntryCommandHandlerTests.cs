using BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;
using BiService.Application.DTOs.Consumption;
using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Consumption.Commands;

public class CreateConsumptionEntryCommandHandlerTests
{
    private readonly Mock<IManualConsumptionEntryRepository> _entriesRepo = new();
    private readonly Mock<ICostConfigVersionRepository> _costConfigRepo = new();
    private readonly CreateConsumptionEntryCommandHandler _handler;

    public CreateConsumptionEntryCommandHandlerTests()
    {
        _handler = new CreateConsumptionEntryCommandHandler(_entriesRepo.Object, _costConfigRepo.Object);
    }

    private static CostConfigVersion MakeConfig(string id = "cfg-1") => new()
    {
        Currency = "COP",
        ElectricityCostPerKwh = 800m,
        WaterCostPerLiter = 5m,
        NutrientCostPerLiter = 12m,
        EffectiveFrom = new DateTime(2025, 1, 1),
        IsActive = true
    };

    [Fact]
    public async Task Handle_ElectricityType_ShouldCalculateCostAndCreate()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("entry-1"); return e; });

        var cmd = new CreateConsumptionEntryCommand(
            new DateTime(2025, 3, 1), null, ConsumptionType.ElectricityKwh, 100m, "test", "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be("entry-1");
        result.Type.Should().Be(ConsumptionType.ElectricityKwh);
        result.Amount.Should().Be(100m);
        result.UnitCostSnapshot.Should().Be(800m);
        result.CostAmount.Should().Be(80000m);
        result.CostConfigVersionId.Should().Be("cfg-1");
    }

    [Fact]
    public async Task Handle_WaterType_ShouldUseWaterCost()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("entry-2"); return e; });

        var cmd = new CreateConsumptionEntryCommand(
            new DateTime(2025, 3, 1), null, ConsumptionType.WaterLiters, 200m, null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.UnitCostSnapshot.Should().Be(5m);
        result.CostAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_NutrientType_ShouldUseNutrientCost()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("entry-3"); return e; });

        var cmd = new CreateConsumptionEntryCommand(
            new DateTime(2025, 3, 1), null, ConsumptionType.NutrientLiters, 50m, null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.UnitCostSnapshot.Should().Be(12m);
        result.CostAmount.Should().Be(600m);
    }

    [Fact]
    public async Task Handle_NoConfigForRange_ShouldThrowInvalidOperation()
    {
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion>());

        var cmd = new CreateConsumptionEntryCommand(
            new DateTime(2025, 3, 1), null, ConsumptionType.ElectricityKwh, 100m, null, "user-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDateTo_ShouldSetDateTo()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("e-1"); return e; });

        var from = new DateTime(2025, 3, 1);
        var to = new DateTime(2025, 3, 15);
        var cmd = new CreateConsumptionEntryCommand(from, to, ConsumptionType.WaterLiters, 100m, null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.DateFrom.Should().Be(from);
        result.DateTo.Should().Be(to);
    }

    [Fact]
    public async Task Handle_WithoutDateTo_ShouldUseDateFromAsDateTo()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("e-1"); return e; });

        var date = new DateTime(2025, 3, 1);
        var cmd = new CreateConsumptionEntryCommand(date, null, ConsumptionType.WaterLiters, 100m, null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.DateFrom.Should().Be(date);
        result.DateTo.Should().Be(date);
    }

    [Fact]
    public async Task Handle_ShouldSetCurrencyFromConfig()
    {
        var config = MakeConfig();
        config.SetId("cfg-1");
        config.Currency = "USD";
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });
        _entriesRepo.Setup(r => r.CreateAsync(It.IsAny<ManualConsumptionEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualConsumptionEntry e, CancellationToken _) => { e.SetId("e-1"); return e; });

        var cmd = new CreateConsumptionEntryCommand(
            new DateTime(2025, 3, 1), null, ConsumptionType.ElectricityKwh, 10m, null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.CurrencySnapshot.Should().Be("USD");
    }
}
