using BiService.Application.DTOs.Production;
using MediatR;

namespace BiService.Application.Features.Production.Queries.GetProductionRecordById;

/// <summary>
/// Query para obtener un registro de producción por su identificador.
/// </summary>
public record GetProductionRecordByIdQuery(string Id) : IRequest<ProductionRecordDto?>;
