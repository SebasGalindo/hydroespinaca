namespace BffService.Domain.DTOs.Fuzzy;

// ──────────────────────────────────────────────
//  System Requests
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating a new fuzzy system.
/// Only the name is required; all other fields use sensible defaults.
/// </summary>
public class CreateFuzzySystemRequest
{
    /// <summary>
    /// Name of the fuzzy system (1-100 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Defuzzification method. Defaults to "centroid".
    /// Options: centroid, bisector, mom, som, lom, weighted_average.
    /// </summary>
    public string? DefuzzificationMethod { get; set; }

    /// <summary>
    /// Operators configuration for the fuzzy system.
    /// </summary>
    public OperatorsConfigDto? Operators { get; set; }
}

/// <summary>
/// Request body for updating an existing fuzzy system.
/// All fields are optional — only provided fields are updated.
/// </summary>
public class UpdateFuzzySystemRequest
{
    /// <summary>
    /// Updated name for the system (1-100 characters).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated defuzzification method.
    /// </summary>
    public string? DefuzzificationMethod { get; set; }

    /// <summary>
    /// Updated operators configuration.
    /// </summary>
    public OperatorsConfigDto? Operators { get; set; }

    /// <summary>
    /// Updated list of input variable IDs.
    /// </summary>
    public List<string>? InputVariableIds { get; set; }

    /// <summary>
    /// Updated list of output variable IDs.
    /// </summary>
    public List<string>? OutputVariableIds { get; set; }

    /// <summary>
    /// Updated list of rule IDs.
    /// </summary>
    public List<string>? RuleIds { get; set; }
}

/// <summary>
/// Request body for changing the status of a fuzzy system.
/// </summary>
public class UpdateFuzzySystemStatusRequest
{
    /// <summary>
    /// New status for the system: DRAFT, ACTIVE, INACTIVE, or TESTING.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

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

// ──────────────────────────────────────────────
//  Variable Requests
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating a new fuzzy variable.
/// </summary>
public class CreateFuzzyVariableRequest
{
    /// <summary>
    /// Name of the variable (1-100 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of variable: "input" or "output".
    /// </summary>
    public string VariableType { get; set; } = "input";

    /// <summary>
    /// ID of the system this variable belongs to.
    /// </summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the variable.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Actuator type for output variables: "PWM" or "DIGITAL".
    /// Required for output variables, forbidden for input variables.
    /// </summary>
    public string? ActuatorType { get; set; }

    /// <summary>
    /// Defuzzification threshold for DIGITAL actuators (0-100). Defaults to 50.
    /// </summary>
    public double? DefuzzificationThreshold { get; set; }

    /// <summary>
    /// Minimum value of the variable universe.
    /// </summary>
    public double? UniverseMin { get; set; }

    /// <summary>
    /// Maximum value of the variable universe. Must be greater than UniverseMin.
    /// </summary>
    public double? UniverseMax { get; set; }

    /// <summary>
    /// Reference code mapping to a sensor (input) or actuator (output).
    /// </summary>
    public string? ReferenceCode { get; set; }
}

/// <summary>
/// Request body for updating an existing fuzzy variable.
/// All fields are optional — only provided fields are updated.
/// </summary>
public class UpdateFuzzyVariableRequest
{
    /// <summary>
    /// Updated name for the variable.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated actuator type for output variables.
    /// </summary>
    public string? ActuatorType { get; set; }

    /// <summary>
    /// Updated defuzzification threshold.
    /// </summary>
    public double? DefuzzificationThreshold { get; set; }

    /// <summary>
    /// Updated universe minimum value.
    /// </summary>
    public double? UniverseMin { get; set; }

    /// <summary>
    /// Updated universe maximum value.
    /// </summary>
    public double? UniverseMax { get; set; }

    /// <summary>
    /// Updated reference code.
    /// </summary>
    public string? ReferenceCode { get; set; }
}

// ──────────────────────────────────────────────
//  Term Requests
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating a new fuzzy term.
/// </summary>
public class CreateFuzzyTermRequest
{
    /// <summary>
    /// ID of the variable this term belongs to.
    /// </summary>
    public string VariableId { get; set; } = string.Empty;

    /// <summary>
    /// Linguistic label for the term (1-30 characters). E.g. "baja", "media", "alta".
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Membership function definition for this term.
    /// </summary>
    public MembershipFunctionDto MembershipFunction { get; set; } = new();
}

/// <summary>
/// Request body for updating an existing fuzzy term.
/// All fields are optional — only provided fields are updated.
/// </summary>
public class UpdateFuzzyTermRequest
{
    /// <summary>
    /// Updated linguistic label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Updated membership function definition.
    /// </summary>
    public MembershipFunctionDto? MembershipFunction { get; set; }
}

// ──────────────────────────────────────────────
//  Rule Requests
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating a new fuzzy rule.
/// </summary>
public class CreateFuzzyRuleRequest
{
    /// <summary>
    /// Name of the rule (1-100 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the system this rule belongs to.
    /// </summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// List of conditions (IF part). Minimum 1 condition required.
    /// </summary>
    public List<ConditionDto> Conditions { get; set; } = new();

    /// <summary>
    /// List of logical connectors between conditions (AND/OR).
    /// Count must be conditions.count - 1.
    /// </summary>
    public List<string> Connectors { get; set; } = new();

    /// <summary>
    /// List of Mamdani consequents (THEN part). Minimum 1 required.
    /// </summary>
    public List<RuleConsequentDto> Consequents { get; set; } = new();
}

/// <summary>
/// Request body for updating an existing fuzzy rule.
/// All fields are optional — only provided fields are updated.
/// </summary>
public class UpdateFuzzyRuleRequest
{
    /// <summary>
    /// Updated rule name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated conditions list.
    /// </summary>
    public List<ConditionDto>? Conditions { get; set; }

    /// <summary>
    /// Updated connectors list.
    /// </summary>
    public List<string>? Connectors { get; set; }

    /// <summary>
    /// Updated consequents list.
    /// </summary>
    public List<RuleConsequentDto>? Consequents { get; set; }
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
/// Individual output value produced by a rule activation during simulation.
/// </summary>
public class SimulateOutputValueDto
{
    public string? ReferenceCode { get; set; }
    public string? Power { get; set; }
    public double? DutyCycle { get; set; }
    public double Duration { get; set; }
}

/// <summary>
/// Individual rule activation result from simulation.
/// </summary>
public class SimulateRuleActivationDto
{
    public string? RuleId { get; set; }
    public string? RuleName { get; set; }
    public double FiringStrength { get; set; }
    public List<SimulateOutputValueDto>? OutputValues { get; set; }
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
