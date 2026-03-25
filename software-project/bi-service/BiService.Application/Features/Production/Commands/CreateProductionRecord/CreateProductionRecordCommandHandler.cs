using BiService.Application.DTOs.Production;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Production.Commands.CreateProductionRecord;

/// <summary>
/// Handler que crea un nuevo registro de producción de cultivo y retorna su DTO.
/// </summary>
public class CreateProductionRecordCommandHandler
    : IRequestHandler<CreateProductionRecordCommand, ProductionRecordDto>
{
    private readonly IProductionRecordRepository _repository;

    public CreateProductionRecordCommandHandler(IProductionRecordRepository repository)
        => _repository = repository;

    public async Task<ProductionRecordDto> Handle(
        CreateProductionRecordCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProductionRecord
        {
            CropName = request.CropName.Trim(),
            StartDate = request.StartDate,
            HarvestDate = request.HarvestDate,
            KilosProduced = request.KilosProduced,
            PricePerKilo = request.PricePerKilo,
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "COP" : request.Currency.Trim().ToUpperInvariant(),
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.UserId
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);

        return new ProductionRecordDto
        {
            Id = created.Id,
            CropName = created.CropName,
            StartDate = created.StartDate,
            HarvestDate = created.HarvestDate,
            KilosProduced = created.KilosProduced,
            PricePerKilo = created.PricePerKilo,
            Currency = created.Currency,
            Note = created.Note,
            CreatedAt = created.CreatedAt
        };
    }
}
