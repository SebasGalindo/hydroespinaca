namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// DTO for production record data forwarded from the BI service.
/// </summary>
public class ProductionRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string CropName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime HarvestDate { get; set; }
    public decimal KilosProduced { get; set; }
    public decimal PricePerKilo { get; set; }
    public string Currency { get; set; } = "COP";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
