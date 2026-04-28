namespace BffService.Domain.DTOs.Fuzzy;

// ──────────────────────────────────────────────
//  Evaluation Read Models
// ──────────────────────────────────────────────

/// <summary>
/// Crisp input value provided to the fuzzy engine during an evaluation.
/// </summary>
public class FuzzyEvaluationInputDto
{
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
}

/// <summary>
/// Output value emitted by a single rule activation during an evaluation.
/// </summary>
public class FuzzyEvaluationOutputValueDto
{
    public string ReferenceCode { get; set; } = string.Empty;
    public string? Power { get; set; }
    public double? DutyCycle { get; set; }
    public double Duration { get; set; }
}

/// <summary>
/// One activated rule from a persisted fuzzy evaluation.
/// </summary>
public class FuzzyEvaluationRuleActivationDto
{
    public string RuleId { get; set; } = string.Empty;
    public double FiringStrength { get; set; }
    public List<FuzzyEvaluationOutputValueDto> OutputValues { get; set; } = new();
}

/// <summary>
/// A persisted fuzzy evaluation record (RF-F05/F07).
/// </summary>
public class FuzzyEvaluationDto
{
    public string? Id { get; set; }
    public string SystemId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public List<FuzzyEvaluationInputDto> Inputs { get; set; } = new();
    public List<FuzzyEvaluationRuleActivationDto> ActivatedRules { get; set; } = new();
}

/// <summary>
/// Paginated response from the fuzzy-service evaluations list / recent endpoints.
/// </summary>
public class FuzzyEvaluationsListResponseDto
{
    public List<FuzzyEvaluationDto> Evaluations { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

/// <summary>
/// Date range descriptor returned by the stats endpoint.
/// </summary>
public class FuzzyEvaluationDateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
}

/// <summary>
/// Summary statistics over a window of fuzzy evaluations (RF-F07).
/// </summary>
public class FuzzyEvaluationStatsDto
{
    public int TotalEvaluations { get; set; }
    public FuzzyEvaluationDateRangeDto DateRange { get; set; } = new();
    public Dictionary<string, int> SystemsStats { get; set; } = new();
    public Dictionary<string, int> DailyStats { get; set; } = new();
    public double AvgEvaluationsPerDay { get; set; }
}
