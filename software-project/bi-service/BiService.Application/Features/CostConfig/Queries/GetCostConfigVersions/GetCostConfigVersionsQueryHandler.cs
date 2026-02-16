using BiService.Application.DTOs.CostConfig;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.CostConfig.Queries.GetCostConfigVersions;

/// <summary>
/// Handler que obtiene y mapea las versiones de configuración de costos.
/// </summary>
public class GetCostConfigVersionsQueryHandler
    : IRequestHandler<GetCostConfigVersionsQuery, IReadOnlyList<CostConfigVersionDto>>
{
    private readonly ICostConfigVersionRepository _repository;

    public GetCostConfigVersionsQueryHandler(ICostConfigVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CostConfigVersionDto>> Handle(
        GetCostConfigVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var versions = await _repository.GetVersionsAsync(request.From, request.To, cancellationToken);

        return versions.Select(v => new CostConfigVersionDto
        {
            Id = v.Id,
            Currency = v.Currency,
            ElectricityCostPerKwh = v.ElectricityCostPerKwh,
            WaterCostPerLiter = v.WaterCostPerLiter,
            NutrientCostPerLiter = v.NutrientCostPerLiter,
            EffectiveFrom = v.EffectiveFrom,
            EffectiveTo = v.EffectiveTo,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        }).ToList();
    }
}
