using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Consumption.Commands.DeleteConsumptionEntry;

/// <summary>
/// Handler que procesa la eliminación de un registro de consumo manual. Lanza excepción si el registro no existe.
/// </summary>
public class DeleteConsumptionEntryCommandHandler
    : IRequestHandler<DeleteConsumptionEntryCommand, bool>
{
    private readonly IManualConsumptionEntryRepository _repository;

    public DeleteConsumptionEntryCommandHandler(IManualConsumptionEntryRepository repository)
        => _repository = repository;

    public async Task<bool> Handle(
        DeleteConsumptionEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"No se encontró la entrada de consumo con ID '{request.Id}'");

        return await _repository.DeleteAsync(request.Id, cancellationToken);
    }
}
