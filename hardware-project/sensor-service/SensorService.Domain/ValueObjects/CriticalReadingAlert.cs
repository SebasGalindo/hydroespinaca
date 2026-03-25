namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Representa una lectura crítica de una variable de regulación manual (pH, EC, nivel de agua).
/// Indica si el valor actual excede el umbral óptimo definido para el cultivo.
/// </summary>
public record CriticalReadingAlert
{
    /// <summary>
    /// Código de la variable de regulación manual (ej: ph, tds, water_level).
    /// </summary>
    public string Code { get; init; } = default!;

    /// <summary>
    /// Nombre descriptivo de la variable (ej: "pH", "Conductividad Eléctrica").
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Valor actual de la lectura del sensor.
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Descripción del umbral óptimo configurado para la variable (ej: "6.0 - 7.0 pH").
    /// </summary>
    public string Threshold { get; init; } = default!;

    /// <summary>
    /// Indica si el valor actual está fuera del rango óptimo y constituye una alerta.
    /// </summary>
    public bool IsAlert { get; init; }

    /// <summary>
    /// Alias del código de variable para compatibilidad con versiones anteriores.
    /// </summary>
    public string VariableCode => Code;
}

/// <summary>
/// Representa una lectura contextual (no alerta) de una variable automática para propósitos informativos.
/// </summary>
public record ContextualReading
{
    /// <summary>
    /// Nombre descriptivo de la variable automática (ej: "Temperatura", "Humedad").
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Valor actual de la lectura del sensor automático.
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Unidad de medida de la variable (ej: °C, %).
    /// </summary>
    public string Unit { get; init; } = default!;
}

/// <summary>
/// Datos consolidados de una alerta crítica que incluye lecturas de variables manuales
/// y lecturas contextuales de variables automáticas para proporcionar información completa.
/// </summary>
public record CriticalAlertData
{
    /// <summary>
    /// Identificador del nodo ESP32 que generó las lecturas.
    /// </summary>
    public string Esp32Id { get; init; } = default!;

    /// <summary>
    /// Marca temporal de la evaluación de las lecturas críticas.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Lecturas de variables de regulación manual evaluadas contra los rangos óptimos.
    /// </summary>
    public IReadOnlyList<CriticalReadingAlert> ManualReadings { get; init; } = Array.Empty<CriticalReadingAlert>();

    /// <summary>
    /// Lecturas contextuales de sensores automáticos (temperatura, humedad, etc.).
    /// No son alertas, solo proporcionan contexto informativo.
    /// </summary>
    public IReadOnlyList<ContextualReading> ContextualReadings { get; init; } = Array.Empty<ContextualReading>();

    /// <summary>
    /// Indica si existe al menos una lectura manual que constituya una alerta.
    /// </summary>
    public bool HasAnyAlert => ManualReadings.Any(r => r.IsAlert);

    /// <summary>
    /// Lecturas manuales que están fuera del rango óptimo y constituyen alertas.
    /// </summary>
    public IEnumerable<CriticalReadingAlert> AlertReadings => ManualReadings.Where(r => r.IsAlert);
}