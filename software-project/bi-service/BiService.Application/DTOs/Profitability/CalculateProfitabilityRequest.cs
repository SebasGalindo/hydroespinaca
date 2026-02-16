using BiService.Application.DTOs.OperationalCost;

namespace BiService.Application.DTOs.Profitability;

/// <summary>
/// Solicitud para calcular la rentabilidad de un registro de producción.
/// </summary>
public class CalculateProfitabilityRequest
{
    /// <summary>Identificador del registro de producción a evaluar.</summary>
    public string ProductionRecordId { get; set; } = string.Empty;

    /// <summary>Lista de duraciones y potencias de los actuadores utilizados durante el ciclo.</summary>
    public List<ActuatorDurationInput> ActuatorDurations { get; set; } = new();
}
