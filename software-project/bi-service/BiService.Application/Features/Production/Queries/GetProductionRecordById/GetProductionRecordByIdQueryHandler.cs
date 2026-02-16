using BiService.Application.DTOs.Production;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Production.Queries.GetProductionRecordById;

/// <summary>
/// Handler que obtiene un registro de producción por ID y lo mapea a DTO.
/// </summary>
public class GetProductionRecordByIdQueryHandler
    : IRequestHandler<GetProductionRecordByIdQuery, ProductionRecordDto?>
{
    private readonly IProductionRecordRepository _repository;

    public GetProductionRecordByIdQueryHandler(IProductionRecordRepository repository)
        => _repository = repository;

    public async Task<ProductionRecordDto?> Handle(
        GetProductionRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null) return null;

        return new ProductionRecordDto
        {
            Id = record.Id,
            CropName = record.CropName,
            StartDate = record.StartDate,
            HarvestDate = record.HarvestDate,
            KilosProduced = record.KilosProduced,
            PricePerKilo = record.PricePerKilo,
            Currency = record.Currency,
            Note = record.Note,
            CreatedAt = record.CreatedAt
        };
    }
}
