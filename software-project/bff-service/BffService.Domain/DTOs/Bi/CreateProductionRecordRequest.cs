namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Request DTO for creating a production record via the BFF.
/// </summary>
public class CreateProductionRecordRequest
{
    public string CropName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal KilosProduced { get; set; }
    public decimal PricePerKilo { get; set; }
    public string Currency { get; set; } = "COP";
    public string? Note { get; set; }
}
