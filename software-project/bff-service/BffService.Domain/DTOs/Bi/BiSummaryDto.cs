namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// DTO for the BI consumption and cost summary forwarded from the BI service.
/// </summary>
public class BiSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Currency { get; set; } = "COP";
    public decimal TotalElectricityKwh { get; set; }
    public decimal TotalWaterLiters { get; set; }
    public decimal TotalNutrientLiters { get; set; }
    public decimal CostElectricity { get; set; }
    public decimal CostWater { get; set; }
    public decimal CostNutrients { get; set; }
    public decimal CostTotal { get; set; }
}
