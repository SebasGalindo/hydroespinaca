using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.CostConfig.Commands.DeleteCostConfigVersion;

/// <summary>
/// Handler que procesa la eliminación de una versión de configuración de costos.
/// </summary>
public class DeleteCostConfigVersionCommandHandler
    : IRequestHandler<DeleteCostConfigVersionCommand, Unit>
{
    private readonly ICostConfigVersionRepository _repository;

    public DeleteCostConfigVersionCommandHandler(ICostConfigVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        DeleteCostConfigVersionCommand request,
        CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteVersionAsync(request.Id, cancellationToken);

        if (!deleted)
            throw new KeyNotFoundException($"No se encontró la versión de configuración con id '{request.Id}'");

        return Unit.Value;
    }
}
