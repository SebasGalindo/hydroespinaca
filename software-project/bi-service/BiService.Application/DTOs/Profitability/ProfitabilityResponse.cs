using BiService.Application.DTOs.OperationalCost;
using BiService.Domain.Enums;

namespace BiService.Application.DTOs.Profitability;

/// <summary>
/// Respuesta con el análisis completo de rentabilidad de un ciclo de producción.
/// </summary>
public class ProfitabilityResponse
{
    /// <summary>Información del registro de producción evaluado.</summary>
    public ProductionInfo Production { get; set; } = new();

    /// <summary>Desglose de gastos (operacionales y consumos manuales).</summary>
    public ExpensesInfo Expenses { get; set; } = new();

    /// <summary>Información de ingresos por venta de la cosecha.</summary>
    public RevenueInfo Revenue { get; set; } = new();

    /// <summary>Beneficio neto (ingresos − gastos totales).</summary>
    public decimal NetBenefit { get; set; }

    /// <summary>Margen de ganancia expresado como porcentaje (netBenefit / totalRevenue × 100).</summary>
    public decimal ProfitMarginPercent { get; set; }

    /// <summary>
    /// ROI real (%) = netBenefit / (initialInvestmentCost + totalExpenses) × 100.
    /// Solo presente cuando se proporcionó InitialInvestmentCost en la solicitud.
    /// </summary>
    public decimal? RoiPercent { get; set; }

    /// <summary>Costo total de producción por kilogramo (totalExpenses / kilosProduced).</summary>
    public decimal CostPerKiloProduced { get; set; }

    /// <summary>
    /// Huella hídrica aproximada en litros por kilogramo producido
    /// (totalWaterLiters / kilosProduced). Aproximación para sistemas DFT con recirculación.
    /// </summary>
    public decimal? WaterFootprintLitersPerKg { get; set; }

    /// <summary>Código de moneda utilizada en el cálculo.</summary>
    public string Currency { get; set; } = "COP";
}

/// <summary>
/// Información del registro de producción asociado al análisis de rentabilidad.
/// </summary>
public class ProductionInfo
{
    /// <summary>Identificador único del registro de producción.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre del cultivo.</summary>
    public string CropName { get; set; } = string.Empty;

    /// <summary>Fecha de inicio del ciclo de cultivo.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Fecha de cosecha.</summary>
    public DateTime HarvestDate { get; set; }

    /// <summary>Kilogramos producidos.</summary>
    public decimal KilosProduced { get; set; }

    /// <summary>Precio de venta por kilogramo.</summary>
    public decimal PricePerKilo { get; set; }

    /// <summary>Código de moneda.</summary>
    public string Currency { get; set; } = "COP";
}

/// <summary>
/// Desglose de gastos totales del ciclo de producción.
/// </summary>
public class ExpensesInfo
{
    /// <summary>Detalle de costos operacionales por consumo eléctrico de actuadores.</summary>
    public OperationalCostDetail OperationalCost { get; set; } = new();

    /// <summary>Detalle de costos por consumos manuales registrados.</summary>
    public ManualConsumptionCostDetail ManualConsumptionCost { get; set; } = new();

    /// <summary>Suma total de gastos (operacionales + manuales).</summary>
    public decimal TotalExpenses { get; set; }
}

/// <summary>
/// Detalle del costo operacional por consumo eléctrico de actuadores.
/// </summary>
public class OperationalCostDetail
{
    /// <summary>Desglose de costo por cada actuador.</summary>
    public List<ActuatorOperationalCostItem> Actuators { get; set; } = new();

    /// <summary>Total de kilovatios-hora estimados consumidos.</summary>
    public decimal TotalEstimatedKwh { get; set; }

    /// <summary>Costo operacional total estimado.</summary>
    public decimal TotalOperationalCost { get; set; }
}

/// <summary>
/// Detalle de costos derivados de los consumos manuales de recursos.
/// </summary>
public class ManualConsumptionCostDetail
{
    /// <summary>Total de electricidad consumida manualmente en kWh.</summary>
    public decimal TotalElectricityKwh { get; set; }

    /// <summary>Total de agua consumida en litros.</summary>
    public decimal TotalWaterLiters { get; set; }

    /// <summary>Total de solución nutritiva consumida en litros.</summary>
    public decimal TotalNutrientLiters { get; set; }

    /// <summary>Costo total por electricidad manual.</summary>
    public decimal CostElectricity { get; set; }

    /// <summary>Costo total por agua.</summary>
    public decimal CostWater { get; set; }

    /// <summary>Costo total por solución nutritiva.</summary>
    public decimal CostNutrients { get; set; }

    /// <summary>Costo total de todos los consumos manuales.</summary>
    public decimal TotalManualCost { get; set; }

    /// <summary>Entradas individuales de consumo manual.</summary>
    public List<ManualConsumptionEntryItem> Entries { get; set; } = new();
}

/// <summary>
/// Elemento individual de consumo manual incluido en el análisis de rentabilidad.
/// </summary>
public class ManualConsumptionEntryItem
{
    /// <summary>Tipo de recurso consumido.</summary>
    public ConsumptionType Type { get; set; }

    /// <summary>Cantidad consumida.</summary>
    public decimal Amount { get; set; }

    /// <summary>Costo calculado para esta entrada.</summary>
    public decimal CostAmount { get; set; }

    /// <summary>Nota descriptiva opcional.</summary>
    public string? Note { get; set; }

    /// <summary>Fecha de inicio del consumo.</summary>
    public DateTime DateFrom { get; set; }

    /// <summary>Fecha de fin del consumo.</summary>
    public DateTime DateTo { get; set; }
}

/// <summary>
/// Información de ingresos generados por la venta de la cosecha.
/// </summary>
public class RevenueInfo
{
    /// <summary>Kilogramos producidos.</summary>
    public decimal KilosProduced { get; set; }

    /// <summary>Precio de venta por kilogramo.</summary>
    public decimal PricePerKilo { get; set; }

    /// <summary>Ingreso total (kilos × precio por kilo).</summary>
    public decimal TotalRevenue { get; set; }
}
