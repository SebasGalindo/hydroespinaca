namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Request body for cloning a fuzzy system with an optional custom name.
/// </summary>
public class CloneFuzzySystemRequest
{
    /// <summary>
    /// Optional custom name for the cloned system.
    /// If null, defaults to "Copia de {original_name}".
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// A single input for fuzzy simulation with its reference code and value.
/// </summary>
public class SimulateInputDto
{
    public string ReferenceCode { get; set; } = string.Empty;
    public double Value { get; set; }
}

/// <summary>
/// Request body for simulating a fuzzy system evaluation.
/// </summary>
public class SimulateFuzzySystemRequest
{
    public List<SimulateInputDto> Inputs { get; set; } = new();
}

/// <summary>
/// Individual rule activation result from simulation.
/// </summary>
public class SimulateRuleActivationDto
{
    public string? RuleId { get; set; }
    public string? RuleName { get; set; }
    public double FiringStrength { get; set; }
    public Dictionary<string, double>? OutputValues { get; set; }
}

/// <summary>
/// Individual output result from simulation.
/// </summary>
public class SimulateOutputDto
{
    public string? VariableName { get; set; }
    public string? ReferenceCode { get; set; }
    public double CrispValue { get; set; }
    public string? ActuatorType { get; set; }
}

/// <summary>
/// Response from fuzzy system simulation.
/// </summary>
public class SimulateFuzzySystemResponse
{
    public string? SystemId { get; set; }
    public string? SystemName { get; set; }
    public List<SimulateInputDto>? Inputs { get; set; }
    public List<SimulateRuleActivationDto>? ActivatedRules { get; set; }
    public List<SimulateOutputDto>? FinalOutputs { get; set; }
    public DateTime? SimulatedAt { get; set; }
}
