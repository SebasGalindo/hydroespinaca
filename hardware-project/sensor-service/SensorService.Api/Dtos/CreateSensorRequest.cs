using System.ComponentModel.DataAnnotations;

namespace SensorService.Api.Dtos;

public class CreateSensorRequest
{
    [Required]
    public string Code { get; set; } = default!;

    [Required]
    public string Type { get; set; } = default!;

    [Required]
    public string Unit { get; set; } = default!;

    [Required]
    public string PhysicalId { get; set; } = default!;

    public string? Location { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SamplingFrequency must be greater than 0.")]
    public int SamplingFrequency { get; set; }

    [Required]
    public string Status { get; set; } = "active";
}
