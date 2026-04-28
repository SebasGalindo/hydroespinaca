namespace BffService.Domain.DTOs;

// --- Alert config DTOs ---

public class WeatherAlertConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string FuzzySystemId { get; set; } = string.Empty;
    public string FuzzySystemName { get; set; } = string.Empty;
    public List<AlertThresholdDto> Alerts { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public int MaxForecastDays { get; set; } = 8;
    public bool AllowDuplicateAlerts { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AlertThresholdDto
{
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public double? ThresholdValue { get; set; }
    public string? Comparison { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class UpdateAlertConfigRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<AlertThresholdDto> Alerts { get; set; } = [];
    public int MaxForecastDays { get; set; } = 8;
    public bool AllowDuplicateAlerts { get; set; } = true;
}

public class SeedAlertConfigRequestDto
{
    public string FuzzySystemName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

// --- Alert DTOs ---

public class WeatherAlertDto
{
    public string Id { get; set; } = string.Empty;
    public string FuzzySystemId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime ForecastDatetime { get; set; }
    public double? ForecastValue { get; set; }
    public string? ForecastCondition { get; set; }
    public bool GovernmentAlert { get; set; }
    public List<NotifiedUserDto> NotifiedUsers { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class NotifiedUserDto
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}
