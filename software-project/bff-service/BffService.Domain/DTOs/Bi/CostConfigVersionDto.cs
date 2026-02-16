namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// DTO for cost configuration version data forwarded from the BI service.
/// </summary>
public class CostConfigVersionDto
{
    public string Id { get; set; } = string.Empty;
    public string Currency { get; set; } = "COP";
    public decimal ElectricityCostPerKwh { get; set; }
    public decimal WaterCostPerLiter { get; set; }
    public decimal NutrientCostPerLiter { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
