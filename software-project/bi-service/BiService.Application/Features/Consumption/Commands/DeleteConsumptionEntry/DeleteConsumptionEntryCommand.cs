using MediatR;

namespace BiService.Application.Features.Consumption.Commands.DeleteConsumptionEntry;

/// <summary>
/// Comando para eliminar un registro de consumo manual por su identificador.
/// </summary>
public record DeleteConsumptionEntryCommand(string Id) : IRequest<bool>;
