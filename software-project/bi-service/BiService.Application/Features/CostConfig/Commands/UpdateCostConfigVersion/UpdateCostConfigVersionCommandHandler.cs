using BiService.Application.DTOs.CostConfig;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.UpdateCostConfigVersion;

/// <summary>
/// Handler que procesa la actualización de una versión de configuración de costos.
/// </summary>
public class UpdateCostConfigVersionCommandHandler
    : IRequestHandler<UpdateCostConfigVersionCommand, CostConfigVersionDto>
{
    private readonly ICostConfigVersionRepository _repository;

    public UpdateCostConfigVersionCommandHandler(ICostConfigVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CostConfigVersionDto> Handle(
        UpdateCostConfigVersionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la versión de configuración con id '{request.Id}'");

        existing.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "COP"
            : request.Currency.Trim().ToUpperInvariant();
        existing.ElectricityCostPerKwh = request.ElectricityCostPerKwh;
        existing.WaterCostPerLiter = request.WaterCostPerLiter;
        existing.NutrientCostPerLiter = request.NutrientCostPerLiter;
        existing.EffectiveFrom = request.EffectiveFrom ?? existing.EffectiveFrom;
        existing.EffectiveTo = request.EffectiveTo;

        var updated = await _repository.UpdateVersionAsync(existing, cancellationToken);

        return new CostConfigVersionDto
        {
            Id = updated.Id,
            Currency = updated.Currency,
            ElectricityCostPerKwh = updated.ElectricityCostPerKwh,
            WaterCostPerLiter = updated.WaterCostPerLiter,
            NutrientCostPerLiter = updated.NutrientCostPerLiter,
            EffectiveFrom = updated.EffectiveFrom,
            EffectiveTo = updated.EffectiveTo,
            IsActive = updated.IsActive,
            CreatedAt = updated.CreatedAt
        };
    }
}
