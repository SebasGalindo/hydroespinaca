using BiService.Application.DTOs.CostConfig;
using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.UpdateCostConfigVersion;

/// <summary>
/// Comando para actualizar una versión existente de configuración de costos.
/// </summary>
public record UpdateCostConfigVersionCommand(
    string Id,
    string Currency,
    decimal ElectricityCostPerKwh,
    decimal WaterCostPerLiter,
    decimal NutrientCostPerLiter,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string UserId
) : IRequest<CostConfigVersionDto>;
