using BiService.Application.DTOs.CostConfig;
using MediatR;

namespace BiService.Application.Features.CostConfig.Queries.GetCostConfigVersions;

/// <summary>
/// Query para listar versiones de configuración de costos con filtro opcional por rango de fechas.
/// </summary>
public record GetCostConfigVersionsQuery(
    DateTime? From,
    DateTime? To
) : IRequest<IReadOnlyList<CostConfigVersionDto>>;
