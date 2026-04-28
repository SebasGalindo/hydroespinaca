using BiService.Application.DTOs.OperationalCost;
using BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;
using BiService.Application.Features.Profitability.Queries.CalculateProfitability;
using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Tests.Features.Profitability.Queries;

public class CalculateProfitabilityQueryHandlerTests
{
    private readonly Mock<IProductionRecordRepository> _productionRepo = new();
    private readonly Mock<IManualConsumptionEntryRepository> _consumptionRepo = new();
    private readonly Mock<ISender> _sender = new();
    private readonly CalculateProfitabilityQueryHandler _handler;

    public CalculateProfitabilityQueryHandlerTests()
    {
        _handler = new CalculateProfitabilityQueryHandler(
            _productionRepo.Object, _consumptionRepo.Object, _sender.Object);
    }

    [Fact]
    public async Task Handle_ShouldCalculateNetBenefitCorrectly()
    {
        var production = MakeProduction("p-1", 10m, 15000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 5000m, TotalEstimatedKwh = 10m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var query = new CalculateProfitabilityQuery("p-1", new List<ActuatorDurationInput>());
        var result = await _handler.Handle(query, CancellationToken.None);

        // Revenue = 10 * 15000 = 150000
        // Expenses = 5000 (operational) + 0 (manual) = 5000
        // Net = 150000 - 5000 = 145000
        result.Revenue.TotalRevenue.Should().Be(150000m);
        result.Expenses.TotalExpenses.Should().Be(5000m);
        result.NetBenefit.Should().Be(145000m);
    }

    [Fact]
    public async Task Handle_ShouldCalculateProfitMarginPercent()
    {
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 50000m, TotalEstimatedKwh = 10m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        // Revenue = 100000, Expenses = 50000, Net = 50000, Margin = 50%
        result.ProfitMarginPercent.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_WithManualConsumption_ShouldIncludeInExpenses()
    {
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 10000m, TotalEstimatedKwh = 5m, Actuators = new() });

        var entries = new List<ManualConsumptionEntry>
        {
            MakeConsumption(ConsumptionType.ElectricityKwh, 100m, 80000m),
            MakeConsumption(ConsumptionType.WaterLiters, 200m, 1000m),
            MakeConsumption(ConsumptionType.NutrientLiters, 50m, 600m)
        };
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.Expenses.ManualConsumptionCost.TotalElectricityKwh.Should().Be(100m);
        result.Expenses.ManualConsumptionCost.TotalWaterLiters.Should().Be(200m);
        result.Expenses.ManualConsumptionCost.TotalNutrientLiters.Should().Be(50m);
        result.Expenses.ManualConsumptionCost.CostElectricity.Should().Be(80000m);
        result.Expenses.ManualConsumptionCost.CostWater.Should().Be(1000m);
        result.Expenses.ManualConsumptionCost.CostNutrients.Should().Be(600m);
        result.Expenses.ManualConsumptionCost.TotalManualCost.Should().Be(81600m);
        // Total expenses: 10000 + 81600 = 91600
        result.Expenses.TotalExpenses.Should().Be(91600m);
    }

    [Fact]
    public async Task Handle_ProductionNotFound_ShouldThrowKeyNotFound()
    {
        _productionRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new CalculateProfitabilityQuery("missing", new()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldMapProductionInfo()
    {
        var production = MakeProduction("p-1", 25m, 12000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 0m, TotalEstimatedKwh = 0m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.Production.Id.Should().Be("p-1");
        result.Production.CropName.Should().Be("Espinaca");
        result.Production.KilosProduced.Should().Be(25m);
        result.Production.PricePerKilo.Should().Be(12000m);
        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_ZeroRevenue_ShouldReturnZeroProfitMargin()
    {
        var production = MakeProduction("p-1", 0m, 0m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 1000m, TotalEstimatedKwh = 1m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.ProfitMarginPercent.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithInitialInvestment_ShouldCalculateRoiPercent()
    {
        // Revenue = 10 * 10000 = 100000, Expenses = 20000, Net = 80000
        // ROI = 80000 / (50000 + 20000) = 80000 / 70000 ≈ 114.29%
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 20000m, TotalEstimatedKwh = 5m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var query = new CalculateProfitabilityQuery("p-1", new(), InitialInvestmentCost: 50000m);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.RoiPercent.Should().NotBeNull();
        result.RoiPercent!.Value.Should().BeApproximately(114.29m, 0.01m);
    }

    [Fact]
    public async Task Handle_WithoutInitialInvestment_ShouldReturnNullRoi()
    {
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 5000m, TotalEstimatedKwh = 1m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.RoiPercent.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCalculateCostPerKiloProduced()
    {
        // Expenses = 30000, Kilos = 10 → CostPerKg = 3000
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 30000m, TotalEstimatedKwh = 5m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.CostPerKiloProduced.Should().Be(3000m);
    }

    [Fact]
    public async Task Handle_WithWaterConsumption_ShouldCalculateWaterFootprint()
    {
        // Water = 200 L, Kilos = 10 → Footprint = 20 L/kg
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 5000m, TotalEstimatedKwh = 1m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>
            {
                MakeConsumption(ConsumptionType.WaterLiters, 200m, 500m)
            });

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.WaterFootprintLitersPerKg.Should().NotBeNull();
        result.WaterFootprintLitersPerKg!.Value.Should().Be(20m);
    }

    [Fact]
    public async Task Handle_WithoutWaterConsumption_ShouldReturnNullWaterFootprint()
    {
        var production = MakeProduction("p-1", 10m, 10000m);
        _productionRepo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(production);
        _sender.Setup(s => s.Send(It.IsAny<CalculateOperationalCostQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalCostResponse { TotalOperationalCost = 5000m, TotalEstimatedKwh = 1m, Actuators = new() });
        _consumptionRepo.Setup(r => r.GetByRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualConsumptionEntry>());

        var result = await _handler.Handle(new CalculateProfitabilityQuery("p-1", new()), CancellationToken.None);

        result.WaterFootprintLitersPerKg.Should().BeNull();
    }

    private static ProductionRecord MakeProduction(string id, decimal kilos, decimal pricePerKilo)
    {
        var record = new ProductionRecord
        {
            CropName = "Espinaca",
            StartDate = new DateTime(2025, 1, 1),
            HarvestDate = new DateTime(2025, 2, 1),
            KilosProduced = kilos,
            PricePerKilo = pricePerKilo,
            Currency = "COP",
            CreatedByUserId = "user-1"
        };
        record.SetId(id);
        return record;
    }

    private static ManualConsumptionEntry MakeConsumption(ConsumptionType type, decimal amount, decimal cost)
    {
        return new ManualConsumptionEntry
        {
            Type = type,
            Amount = amount,
            CostAmount = cost,
            DateFrom = new DateTime(2025, 1, 15),
            DateTo = new DateTime(2025, 1, 20)
        };
    }
}
