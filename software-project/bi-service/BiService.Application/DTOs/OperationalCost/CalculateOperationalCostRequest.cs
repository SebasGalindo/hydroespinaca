namespace BiService.Application.DTOs.OperationalCost;

/// <summary>
/// Solicitud para calcular el costo operacional estimado por consumo eléctrico de actuadores.
/// </summary>
public class CalculateOperationalCostRequest
{
    /// <summary>Fecha de inicio del periodo a calcular.</summary>
    public DateTime From { get; set; }

    /// <summary>Fecha de fin del periodo a calcular.</summary>
    public DateTime To { get; set; }

    /// <summary>Lista de duraciones y potencias de los actuadores involucrados.</summary>
    public List<ActuatorDurationInput> ActuatorDurations { get; set; } = new();
}
