namespace BiService.Domain.Enums;

/// <summary>
/// Tipos de recurso que pueden registrarse como consumo manual en el sistema de BI.
/// </summary>
public enum ConsumptionType
{
    /// <summary>Consumo de electricidad medido en kilovatios-hora (kWh).</summary>
    ElectricityKwh = 1,

    /// <summary>Consumo de agua medido en litros.</summary>
    WaterLiters = 2,

    /// <summary>Consumo de solución nutritiva medido en litros.</summary>
    NutrientLiters = 3
}
