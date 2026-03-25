using BiService.Application.DTOs.OperationalCost;
using BiService.Application.DTOs.Profitability;
using BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Profitability.Queries.CalculateProfitability;

/// <summary>
/// Handler que calcula la rentabilidad de un ciclo de producción.
/// Combina costos operacionales de actuadores con consumos manuales registrados
/// y los compara contra los ingresos por venta.
/// </summary>
public class CalculateProfitabilityQueryHandler
    : IRequestHandler<CalculateProfitabilityQuery, ProfitabilityResponse>
{
    private readonly IProductionRecordRepository _productionRepository;
    private readonly IManualConsumptionEntryRepository _consumptionRepository;
    private readonly ISender _sender;

    public CalculateProfitabilityQueryHandler(
        IProductionRecordRepository productionRepository,
        IManualConsumptionEntryRepository consumptionRepository,
        ISender sender)
    {
        _productionRepository = productionRepository;
        _consumptionRepository = consumptionRepository;
        _sender = sender;
    }

    public async Task<ProfitabilityResponse> Handle(
        CalculateProfitabilityQuery request, CancellationToken cancellationToken)
    {
        // 1. Get the production record
        var production = await _productionRepository.GetByIdAsync(
            request.ProductionRecordId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"No se encontró el registro de producción con ID '{request.ProductionRecordId}'");

        // 2. Calculate operational cost using the production's date range
        var operationalCostResult = await _sender.Send(
            new CalculateOperationalCostQuery(
                production.StartDate,
                production.HarvestDate,
                request.ActuatorDurations),
            cancellationToken);

        // 3. Get manual consumption entries for the production's date range
        var entries = await _consumptionRepository.GetByRangeAsync(
            production.StartDate, production.HarvestDate, null, cancellationToken);

        var totalElectricityKwh = entries
            .Where(x => x.Type == ConsumptionType.ElectricityKwh).Sum(x => x.Amount);
        var totalWaterLiters = entries
            .Where(x => x.Type == ConsumptionType.WaterLiters).Sum(x => x.Amount);
        var totalNutrientLiters = entries
            .Where(x => x.Type == ConsumptionType.NutrientLiters).Sum(x => x.Amount);
        var costElectricity = entries
            .Where(x => x.Type == ConsumptionType.ElectricityKwh).Sum(x => x.CostAmount);
        var costWater = entries
            .Where(x => x.Type == ConsumptionType.WaterLiters).Sum(x => x.CostAmount);
        var costNutrients = entries
            .Where(x => x.Type == ConsumptionType.NutrientLiters).Sum(x => x.CostAmount);
        var totalManualCost = costElectricity + costWater + costNutrients;

        // 4. Calculate revenue
        var totalRevenue = production.KilosProduced * production.PricePerKilo;

        // 5. Calculate totals
        var totalExpenses = operationalCostResult.TotalOperationalCost + totalManualCost;
        var netBenefit = totalRevenue - totalExpenses;
        var profitMarginPercent = totalRevenue > 0
            ? decimal.Round(netBenefit / totalRevenue * 100m, 2)
            : 0m;

        return new ProfitabilityResponse
        {
            Production = new ProductionInfo
            {
                Id = production.Id,
                CropName = production.CropName,
                StartDate = production.StartDate,
                HarvestDate = production.HarvestDate,
                KilosProduced = production.KilosProduced,
                PricePerKilo = production.PricePerKilo,
                Currency = production.Currency
            },
            Expenses = new ExpensesInfo
            {
                OperationalCost = new OperationalCostDetail
                {
                    Actuators = operationalCostResult.Actuators,
                    TotalEstimatedKwh = operationalCostResult.TotalEstimatedKwh,
                    TotalOperationalCost = operationalCostResult.TotalOperationalCost
                },
                ManualConsumptionCost = new ManualConsumptionCostDetail
                {
                    TotalElectricityKwh = totalElectricityKwh,
                    TotalWaterLiters = totalWaterLiters,
                    TotalNutrientLiters = totalNutrientLiters,
                    CostElectricity = costElectricity,
                    CostWater = costWater,
                    CostNutrients = costNutrients,
                    TotalManualCost = totalManualCost,
                    Entries = entries.Select(e => new ManualConsumptionEntryItem
                    {
                        Type = e.Type,
                        Amount = e.Amount,
                        CostAmount = e.CostAmount,
                        Note = e.Note,
                        DateFrom = e.DateFrom,
                        DateTo = e.DateTo
                    }).ToList()
                },
                TotalExpenses = decimal.Round(totalExpenses, 4)
            },
            Revenue = new RevenueInfo
            {
                KilosProduced = production.KilosProduced,
                PricePerKilo = production.PricePerKilo,
                TotalRevenue = decimal.Round(totalRevenue, 4)
            },
            NetBenefit = decimal.Round(netBenefit, 4),
            ProfitMarginPercent = profitMarginPercent,
            Currency = production.Currency
        };
    }
}
