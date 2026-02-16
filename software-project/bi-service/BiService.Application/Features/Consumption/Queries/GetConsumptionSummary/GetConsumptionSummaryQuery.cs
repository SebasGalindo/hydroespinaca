using BiService.Application.DTOs.Summary;
using MediatR;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;

/// <summary>
/// Query para obtener un resumen consolidado de consumo y costos de todos los tipos de recurso en un periodo.
/// </summary>
public record GetConsumptionSummaryQuery(
    DateTime From,
    DateTime To
) : IRequest<BiSummaryDto>;
