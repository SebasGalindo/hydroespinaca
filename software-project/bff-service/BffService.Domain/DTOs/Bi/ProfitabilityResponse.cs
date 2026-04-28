namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Mirrors BiService.Application.DTOs.Profitability.ProfitabilityResponse.
/// </summary>
public class ProfitabilityResponse
{
    public ProductionInfo Production { get; set; } = new();
    public ExpensesInfo Expenses { get; set; } = new();
    public RevenueInfo Revenue { get; set; } = new();
    public decimal NetBenefit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public decimal? RoiPercent { get; set; }
    public decimal CostPerKiloProduced { get; set; }
    public decimal? WaterFootprintLitersPerKg { get; set; }
    public string Currency { get; set; } = "COP";
}

public class ProductionInfo
{
    public string Id { get; set; } = string.Empty;
    public string CropName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal KilosProduced { get; set; }
    public decimal PricePerKilo { get; set; }
    public string Currency { get; set; } = "COP";
}

public class ExpensesInfo
{
    public OperationalCostDetail OperationalCost { get; set; } = new();
    public ManualConsumptionCostDetail ManualConsumptionCost { get; set; } = new();
    public decimal TotalExpenses { get; set; }
}

public class OperationalCostDetail
{
    public List<ActuatorOperationalCostItem> Actuators { get; set; } = new();
    public decimal TotalEstimatedKwh { get; set; }
    public decimal TotalOperationalCost { get; set; }
}

public class ManualConsumptionCostDetail
{
    public decimal TotalElectricityKwh { get; set; }
    public decimal TotalWaterLiters { get; set; }
    public decimal TotalNutrientLiters { get; set; }
    public decimal CostElectricity { get; set; }
    public decimal CostWater { get; set; }
    public decimal CostNutrients { get; set; }
    public decimal TotalManualCost { get; set; }
    public List<ManualConsumptionEntryItem> Entries { get; set; } = new();
}

public class ManualConsumptionEntryItem
{
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public decimal CostAmount { get; set; }
    public string? Note { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class RevenueInfo
{
    public decimal KilosProduced { get; set; }
    public decimal PricePerKilo { get; set; }
    public decimal TotalRevenue { get; set; }
}
