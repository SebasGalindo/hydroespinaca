using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Production.Commands.DeleteProductionRecord;

/// <summary>
/// Handler que elimina un registro de producción. Lanza excepción si el registro no existe.
/// </summary>
public class DeleteProductionRecordCommandHandler
    : IRequestHandler<DeleteProductionRecordCommand, bool>
{
    private readonly IProductionRecordRepository _repository;

    public DeleteProductionRecordCommandHandler(IProductionRecordRepository repository)
        => _repository = repository;

    public async Task<bool> Handle(
        DeleteProductionRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"No se encontró el registro de producción con ID '{request.Id}'");

        return await _repository.DeleteAsync(request.Id, cancellationToken);
    }
}
