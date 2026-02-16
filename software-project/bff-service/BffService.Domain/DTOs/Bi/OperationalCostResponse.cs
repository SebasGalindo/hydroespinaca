namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Mirrors BiService.Application.DTOs.OperationalCost.OperationalCostResponse.
/// </summary>
public class OperationalCostResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Currency { get; set; } = "COP";
    public List<CostConfigPeriodUsed> CostConfigPeriodsUsed { get; set; } = new();
    public List<ActuatorOperationalCostItem> Actuators { get; set; } = new();
    public decimal TotalEstimatedKwh { get; set; }
    public decimal TotalOperationalCost { get; set; }
}

public class CostConfigPeriodUsed
{
    public string VersionId { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal ElectricityCostPerKwh { get; set; }
    public string Currency { get; set; } = "COP";
}

public class ActuatorOperationalCostItem
{
    public string ActuatorCode { get; set; } = string.Empty;
    public decimal PowerConsumptionWatts { get; set; }
    public double TotalDurationSeconds { get; set; }
    public decimal TotalHours { get; set; }
    public decimal EstimatedKwh { get; set; }
    public decimal EstimatedCost { get; set; }
    public int ActivationCount { get; set; }
}
