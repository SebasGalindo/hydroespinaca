using BiService.Application.DTOs.CostConfig;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;

/// <summary>
/// Handler que crea una nueva versión de configuración de costos. La versión anterior se desactiva automáticamente.
/// </summary>
public class CreateCostConfigVersionCommandHandler
    : IRequestHandler<CreateCostConfigVersionCommand, CostConfigVersionDto>
{
    private readonly ICostConfigVersionRepository _repository;

    public CreateCostConfigVersionCommandHandler(ICostConfigVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CostConfigVersionDto> Handle(
        CreateCostConfigVersionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = new CostConfigVersion
        {
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "COP"
                : request.Currency.Trim().ToUpperInvariant(),
            ElectricityCostPerKwh = request.ElectricityCostPerKwh,
            WaterCostPerLiter = request.WaterCostPerLiter,
            NutrientCostPerLiter = request.NutrientCostPerLiter,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.UserId
        };

        var created = await _repository.CreateVersionAsync(entity, cancellationToken);

        return new CostConfigVersionDto
        {
            Id = created.Id,
            Currency = created.Currency,
            ElectricityCostPerKwh = created.ElectricityCostPerKwh,
            WaterCostPerLiter = created.WaterCostPerLiter,
            NutrientCostPerLiter = created.NutrientCostPerLiter,
            EffectiveFrom = created.EffectiveFrom,
            EffectiveTo = created.EffectiveTo,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt
        };
    }
}
