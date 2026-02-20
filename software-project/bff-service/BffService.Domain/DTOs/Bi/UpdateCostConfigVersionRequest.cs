namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Request DTO for updating a cost config version via the BFF.
/// </summary>
public class UpdateCostConfigVersionRequest
{
    public string Currency { get; set; } = "COP";
    public decimal ElectricityCostPerKwh { get; set; }
    public decimal WaterCostPerLiter { get; set; }
    public decimal NutrientCostPerLiter { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
