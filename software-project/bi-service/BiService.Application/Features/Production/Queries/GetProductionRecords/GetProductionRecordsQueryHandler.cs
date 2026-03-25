using BiService.Application.DTOs.Production;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Production.Queries.GetProductionRecords;

/// <summary>
/// Handler que obtiene y mapea todos los registros de producción.
/// </summary>
public class GetProductionRecordsQueryHandler
    : IRequestHandler<GetProductionRecordsQuery, IReadOnlyList<ProductionRecordDto>>
{
    private readonly IProductionRecordRepository _repository;

    public GetProductionRecordsQueryHandler(IProductionRecordRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<ProductionRecordDto>> Handle(
        GetProductionRecordsQuery request, CancellationToken cancellationToken)
    {
        var records = await _repository.GetAllAsync(cancellationToken);

        return records.Select(r => new ProductionRecordDto
        {
            Id = r.Id,
            CropName = r.CropName,
            StartDate = r.StartDate,
            HarvestDate = r.HarvestDate,
            KilosProduced = r.KilosProduced,
            PricePerKilo = r.PricePerKilo,
            Currency = r.Currency,
            Note = r.Note,
            CreatedAt = r.CreatedAt
        }).ToList();
    }
}
