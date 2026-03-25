using BiService.Application.DTOs.CostConfig;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.CostConfig.Queries.GetCurrentCostConfig;

/// <summary>
/// Handler que obtiene y mapea la configuración de costos activa.
/// </summary>
public class GetCurrentCostConfigQueryHandler
    : IRequestHandler<GetCurrentCostConfigQuery, CostConfigVersionDto?>
{
    private readonly ICostConfigVersionRepository _repository;

    public GetCurrentCostConfigQueryHandler(ICostConfigVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CostConfigVersionDto?> Handle(
        GetCurrentCostConfigQuery request,
        CancellationToken cancellationToken)
    {
        var current = await _repository.GetCurrentAsync(cancellationToken);

        if (current is null) return null;

        return new CostConfigVersionDto
        {
            Id = current.Id,
            Currency = current.Currency,
            ElectricityCostPerKwh = current.ElectricityCostPerKwh,
            WaterCostPerLiter = current.WaterCostPerLiter,
            NutrientCostPerLiter = current.NutrientCostPerLiter,
            EffectiveFrom = current.EffectiveFrom,
            EffectiveTo = current.EffectiveTo,
            IsActive = current.IsActive,
            CreatedAt = current.CreatedAt
        };
    }
}
