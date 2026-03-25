using BiService.Application.DTOs.Summary;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;

/// <summary>
/// Handler que calcula el resumen consolidado de consumo y costos agrupando por tipo de recurso.
/// </summary>
public class GetConsumptionSummaryQueryHandler
    : IRequestHandler<GetConsumptionSummaryQuery, BiSummaryDto>
{
    private readonly IManualConsumptionEntryRepository _entriesRepository;
    private readonly ICostConfigVersionRepository _costConfigRepository;

    public GetConsumptionSummaryQueryHandler(
        IManualConsumptionEntryRepository entriesRepository,
        ICostConfigVersionRepository costConfigRepository)
    {
        _entriesRepository = entriesRepository;
        _costConfigRepository = costConfigRepository;
    }

    public async Task<BiSummaryDto> Handle(
        GetConsumptionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await _entriesRepository.GetByRangeAsync(
            request.From, request.To, null, cancellationToken);

        var activeConfig = await _costConfigRepository.GetCurrentAsync(cancellationToken);

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

        return new BiSummaryDto
        {
            From = request.From,
            To = request.To,
            Currency = activeConfig?.Currency ?? "COP",
            TotalElectricityKwh = totalElectricityKwh,
            TotalWaterLiters = totalWaterLiters,
            TotalNutrientLiters = totalNutrientLiters,
            CostElectricity = costElectricity,
            CostWater = costWater,
            CostNutrients = costNutrients,
            CostTotal = costElectricity + costWater + costNutrients
        };
    }
}
