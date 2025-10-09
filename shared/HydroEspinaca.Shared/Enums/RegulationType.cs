namespace HydroEspinaca.Shared.Enums;

/// <summary>
/// Tipo de regulación de la variable crítica
/// </summary>
public enum RegulationType
{
    /// <summary>
    /// Variable crítica que requiere intervención humana (pH, EC, nivel de agua)
    /// </summary>
    Manual = 0,
    
    /// <summary>
    /// Variable crítica regulada automáticamente por el sistema (temperatura, oxigenación, iluminación)
    /// </summary>
    Automatic = 1
}