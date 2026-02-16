using BiService.Application.DTOs.Consumption;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;

/// <summary>
/// Handler que obtiene y mapea los registros de consumo manual dentro del rango de fechas especificado.
/// </summary>
public class GetConsumptionEntriesQueryHandler
    : IRequestHandler<GetConsumptionEntriesQuery, IReadOnlyList<ManualConsumptionEntryDto>>
{
    private readonly IManualConsumptionEntryRepository _repository;

    public GetConsumptionEntriesQueryHandler(IManualConsumptionEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ManualConsumptionEntryDto>> Handle(
        GetConsumptionEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByRangeAsync(
            request.From, request.To, request.Type, cancellationToken);

        return entries.Select(e => new ManualConsumptionEntryDto
        {
            Id = e.Id,
            Date = e.Date,
            Type = e.Type,
            Amount = e.Amount,
            UnitCostSnapshot = e.UnitCostSnapshot,
            CurrencySnapshot = e.CurrencySnapshot,
            CostConfigVersionId = e.CostConfigVersionId,
            CostAmount = e.CostAmount,
            Note = e.Note,
            CreatedAt = e.CreatedAt
        }).ToList();
    }
}
