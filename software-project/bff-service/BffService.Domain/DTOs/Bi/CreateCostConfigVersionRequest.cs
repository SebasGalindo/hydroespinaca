namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Request DTO for creating a new cost configuration version via the BFF.
/// </summary>
public class CreateCostConfigVersionRequest
{
    public string Currency { get; set; } = "COP";
    public decimal ElectricityCostPerKwh { get; set; }
    public decimal WaterCostPerLiter { get; set; }
    public decimal NutrientCostPerLiter { get; set; }
    public DateTime? EffectiveFrom { get; set; }
}
