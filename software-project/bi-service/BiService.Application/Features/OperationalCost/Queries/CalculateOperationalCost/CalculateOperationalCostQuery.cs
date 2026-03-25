using BiService.Application.DTOs.OperationalCost;
using MediatR;

namespace BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;

/// <summary>
/// Query para calcular el costo operacional estimado por consumo eléctrico de actuadores en un periodo.
/// </summary>
public record CalculateOperationalCostQuery(
    DateTime From,
    DateTime To,
    List<ActuatorDurationInput> ActuatorDurations
) : IRequest<OperationalCostResponse>;
