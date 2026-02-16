namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Represents a membership function for a fuzzy term.
/// </summary>
public class MembershipFunctionDto
{
    /// <summary>
    /// Type of membership function (trimf, trapmf, gaussmf, etc.)
    /// </summary>
    public string FunctionType { get; set; } = string.Empty;

    /// <summary>
    /// Parameters defining the membership function shape
    /// </summary>
    public List<double> Parameters { get; set; } = new();

    /// <summary>
    /// Minimum value of the universe of discourse
    /// </summary>
    public double UniverseMin { get; set; }

    /// <summary>
    /// Maximum value of the universe of discourse
    /// </summary>
    public double UniverseMax { get; set; }
}
