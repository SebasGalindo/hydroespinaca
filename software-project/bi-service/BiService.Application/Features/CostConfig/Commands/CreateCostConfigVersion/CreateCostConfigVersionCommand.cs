using BiService.Application.DTOs.CostConfig;
using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;

/// <summary>
/// Comando para crear una nueva versión de configuración de costos unitarios.
/// </summary>
public record CreateCostConfigVersionCommand(
    string Currency,
    decimal ElectricityCostPerKwh,
    decimal WaterCostPerLiter,
    decimal NutrientCostPerLiter,
    DateTime? EffectiveFrom,
    string UserId
) : IRequest<CostConfigVersionDto>;
