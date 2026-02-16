using BiService.Application.DTOs.Production;
using MediatR;

namespace BiService.Application.Features.Production.Queries.GetProductionRecords;

/// <summary>
/// Query para obtener todos los registros de producción.
/// </summary>
public record GetProductionRecordsQuery() : IRequest<IReadOnlyList<ProductionRecordDto>>;
