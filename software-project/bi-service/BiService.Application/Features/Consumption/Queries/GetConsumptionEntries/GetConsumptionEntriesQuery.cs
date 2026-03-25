using BiService.Application.DTOs.Consumption;
using BiService.Domain.Enums;
using MediatR;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;

/// <summary>
/// Query para obtener registros de consumo manual dentro de un rango de fechas con filtro opcional por tipo.
/// </summary>
public record GetConsumptionEntriesQuery(
    DateTime From,
    DateTime To,
    ConsumptionType? Type
) : IRequest<IReadOnlyList<ManualConsumptionEntryDto>>;
