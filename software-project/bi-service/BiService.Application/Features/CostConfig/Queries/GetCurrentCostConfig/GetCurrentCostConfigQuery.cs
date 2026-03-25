using BiService.Application.DTOs.CostConfig;
using MediatR;

namespace BiService.Application.Features.CostConfig.Queries.GetCurrentCostConfig;

/// <summary>
/// Query para obtener la configuración de costos activa actualmente.
/// </summary>
public record GetCurrentCostConfigQuery() : IRequest<CostConfigVersionDto?>;
