using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.DeleteCostConfigVersion;

/// <summary>
/// Comando para eliminar una versión de configuración de costos.
/// </summary>
public record DeleteCostConfigVersionCommand(string Id) : IRequest<Unit>;
