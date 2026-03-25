using BiService.Application.DTOs.Consumption;
using BiService.Domain.Enums;
using MediatR;

namespace BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;

/// <summary>
/// Comando para registrar un consumo manual de recurso (electricidad, agua o nutrientes).
/// </summary>
public record CreateConsumptionEntryCommand(
    DateTime DateFrom,
    DateTime? DateTo,
    ConsumptionType Type,
    decimal Amount,
    string? Note,
    string UserId
) : IRequest<ManualConsumptionEntryDto>;
