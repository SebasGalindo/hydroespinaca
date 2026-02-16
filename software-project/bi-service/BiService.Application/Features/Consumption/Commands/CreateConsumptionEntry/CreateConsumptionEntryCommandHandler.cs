using BiService.Application.DTOs.Consumption;
using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;

/// <summary>
/// Handler que procesa la creación de una entrada de consumo manual.
/// Resuelve el costo unitario vigente desde la configuración de costos activa y calcula el costo total.
/// </summary>
public class CreateConsumptionEntryCommandHandler
    : IRequestHandler<CreateConsumptionEntryCommand, ManualConsumptionEntryDto>
{
    private readonly IManualConsumptionEntryRepository _entriesRepository;
    private readonly ICostConfigVersionRepository _costConfigRepository;

    public CreateConsumptionEntryCommandHandler(
        IManualConsumptionEntryRepository entriesRepository,
        ICostConfigVersionRepository costConfigRepository)
    {
        _entriesRepository = entriesRepository;
        _costConfigRepository = costConfigRepository;
    }

    public async Task<ManualConsumptionEntryDto> Handle(
        CreateConsumptionEntryCommand request,
        CancellationToken cancellationToken)
    {
        var activeConfig = await _costConfigRepository.GetCurrentAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe una configuración de costos activa. Cree una antes de registrar consumos.");

        var unitCost = request.Type switch
        {
            ConsumptionType.ElectricityKwh => activeConfig.ElectricityCostPerKwh,
            ConsumptionType.WaterLiters => activeConfig.WaterCostPerLiter,
            ConsumptionType.NutrientLiters => activeConfig.NutrientCostPerLiter,
            _ => throw new ArgumentOutOfRangeException(nameof(request.Type), "Tipo de consumo no soportado")
        };

        var entry = new ManualConsumptionEntry
        {
            Date = request.Date,
            Type = request.Type,
            Amount = request.Amount,
            UnitCostSnapshot = unitCost,
            CurrencySnapshot = activeConfig.Currency,
            CostConfigVersionId = activeConfig.Id,
            CostAmount = decimal.Round(request.Amount * unitCost, 4),
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.UserId
        };

        var created = await _entriesRepository.CreateAsync(entry, cancellationToken);

        return new ManualConsumptionEntryDto
        {
            Id = created.Id,
            Date = created.Date,
            Type = created.Type,
            Amount = created.Amount,
            UnitCostSnapshot = created.UnitCostSnapshot,
            CurrencySnapshot = created.CurrencySnapshot,
            CostConfigVersionId = created.CostConfigVersionId,
            CostAmount = created.CostAmount,
            Note = created.Note,
            CreatedAt = created.CreatedAt
        };
    }
}
