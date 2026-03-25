using MediatR;

namespace BiService.Application.Features.Production.Commands.DeleteProductionRecord;

/// <summary>
/// Comando para eliminar un registro de producción por su identificador.
/// </summary>
public record DeleteProductionRecordCommand(string Id) : IRequest<bool>;
