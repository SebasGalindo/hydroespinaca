using BiService.Application.DTOs.Production;
using MediatR;

namespace BiService.Application.Features.Production.Commands.CreateProductionRecord;

/// <summary>
/// Comando para registrar una nueva producción de cultivo.
/// </summary>
public record CreateProductionRecordCommand(
    string CropName,
    DateTime StartDate,
    DateTime HarvestDate,
    decimal KilosProduced,
    decimal PricePerKilo,
    string Currency,
    string? Note,
    string UserId
) : IRequest<ProductionRecordDto>;
