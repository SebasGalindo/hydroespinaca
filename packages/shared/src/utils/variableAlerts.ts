import { ReadingItem } from '../types/systemStatus';

export type AlertDirection = 'both' | 'below' | 'above' | 'none';
export type VariableStatus = 'optimal' | 'warning' | 'error';
export type TrendDirection = 'up' | 'down' | 'stable';

export interface VariableAlertConfig {
  name: string;
  alertDirection: AlertDirection;
}

/**
 * Configuración de alertas por variable
 * - both: Alerta tanto por encima como por debajo del rango óptimo
 * - below: Solo alerta si está por debajo del mínimo óptimo
 * - above: Solo alerta si está por encima del máximo óptimo
 * - none: No genera alertas (solo informativo)
 */
export const VARIABLE_ALERT_CONFIG: VariableAlertConfig[] = [
  { name: 'luminosidad', alertDirection: 'below' },
  { name: 'luz', alertDirection: 'below' },
  { name: 'lux', alertDirection: 'below' },
  { name: 'temperatura', alertDirection: 'both' },
  { name: 'humedad', alertDirection: 'both' },
  { name: 'ph', alertDirection: 'both' },
  { name: 'conductividad', alertDirection: 'both' },
  { name: 'ec', alertDirection: 'both' },
  { name: 'nivel', alertDirection: 'below' },
];

/**
 * Obtiene la configuración de alertas para una variable específica
 */
export function getAlertConfig(variableName: string): VariableAlertConfig {
  const lowerName = variableName.toLowerCase();
  const config = VARIABLE_ALERT_CONFIG.find(cfg => lowerName.includes(cfg.name));

  return config || { name: variableName, alertDirection: 'both' };
}

/**
 * Calcula el estado de una variable basándose en su valor y configuración de alertas
 */
export function calculateVariableStatus(
  reading: ReadingItem,
  config?: VariableAlertConfig
): VariableStatus {
  if (
    !reading ||
    reading.value == null ||
    reading.optimalMin == null ||
    reading.optimalMax == null
  ) {
    return 'error';
  }

  const { value, optimalMin, optimalMax } = reading;
  const range = optimalMax - optimalMin;
  const tolerance = range * 0.1; // 10% de tolerancia

  const alertConfig = config || getAlertConfig(reading.name);

  // Verificar si está dentro del rango óptimo
  if (value >= optimalMin && value <= optimalMax) {
    return 'optimal';
  }

  // Verificar si está en el rango de tolerancia
  const inToleranceRange =
    value >= optimalMin - tolerance &&
    value <= optimalMax + tolerance;

  // Aplicar lógica según la dirección de alerta
  switch (alertConfig.alertDirection) {
    case 'below':
      // Solo alerta si está por debajo del mínimo
      if (value < optimalMin) {
        return inToleranceRange ? 'warning' : 'error';
      }
      return 'optimal'; // Por encima del máximo no es problema

    case 'above':
      // Solo alerta si está por encima del máximo
      if (value > optimalMax) {
        return inToleranceRange ? 'warning' : 'error';
      }
      return 'optimal'; // Por debajo del mínimo no es problema

    case 'none':
      return 'optimal'; // Nunca alerta

    case 'both':
    default:
      // Alerta en ambas direcciones
      return inToleranceRange ? 'warning' : 'error';
  }
}

/**
 * Calcula la tendencia comparando el valor actual con el anterior
 */
export function calculateTrend(
  currentValue: number,
  previousValue: number | null
): TrendDirection {
  if (previousValue === null) {
    return 'stable';
  }

  const threshold = 0.01; // Umbral del 1% para considerar cambio significativo
  const percentageChange = Math.abs((currentValue - previousValue) / previousValue);

  if (percentageChange < threshold) {
    return 'stable';
  }

  return currentValue > previousValue ? 'up' : 'down';
}
