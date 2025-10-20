// Analytics Filters Validation and Utilities

export const MAX_RANGES = {
  hourly: 1,
  daily: 7,
  weekly: 31,
  monthly: 180,
} as const;

export type ViewMode = 'hourly' | 'daily' | 'weekly' | 'monthly';

export interface FilterValidationResult {
  valid: boolean;
  message?: string;
  maxDays?: number;
}

export interface AnalyticsFilters {
  startDate: string; // ISO format YYYY-MM-DD
  endDate: string;   // ISO format YYYY-MM-DD
  view: ViewMode;
}

/**
 * Calcula la diferencia en días entre dos fechas
 * Para el mismo día retorna 0, permitiendo consultas hourly
 */
export function getDaysDifference(startDate: string, endDate: string): number {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diffTime = end.getTime() - start.getTime();
  // Use Math.ceil but handle same-day case (0 days)
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  return diffDays;
}

/**
 * Valida que el rango de fechas sea válido según la vista seleccionada
 */
export function validateFilters(
  startDate: string,
  endDate: string,
  view: ViewMode
): FilterValidationResult {
  // Validar que las fechas sean válidas
  const start = new Date(startDate);
  const end = new Date(endDate);

  if (isNaN(start.getTime()) || isNaN(end.getTime())) {
    return {
      valid: false,
      message: 'Las fechas seleccionadas no son válidas.',
    };
  }

  // Validar que la fecha de inicio no sea posterior a la de fin
  // Permitir que sean iguales para vista hourly (mismo día)
  if (start > end) {
    return {
      valid: false,
      message: 'La fecha de inicio no puede ser posterior a la fecha de fin.',
    };
  }

  // Validar que el rango no exceda el máximo permitido
  const diffDays = getDaysDifference(startDate, endDate);
  const maxAllowed = MAX_RANGES[view];

  if (diffDays > maxAllowed) {
    return {
      valid: false,
      message: `El rango máximo para vista ${getViewLabel(view)} es de ${maxAllowed} días.`,
      maxDays: maxAllowed,
    };
  }

  // Validar que no sea una fecha futura
  const today = new Date();
  today.setHours(23, 59, 59, 999);

  if (end > today) {
    return {
      valid: false,
      message: 'No se pueden seleccionar fechas futuras.',
    };
  }

  return { valid: true };
}

/**
 * Ajusta el rango de fechas automáticamente si excede el máximo para la vista
 */
export function adjustDateRangeForView(
  startDate: string,
  endDate: string,
  view: ViewMode
): { startDate: string; endDate: string } {
  const diffDays = getDaysDifference(startDate, endDate);
  const maxAllowed = MAX_RANGES[view];

  if (diffDays <= maxAllowed) {
    return { startDate, endDate };
  }

  // Special case for hourly: use same day (from = to = endDate)
  if (view === 'hourly') {
    return {
      startDate: endDate,
      endDate,
    };
  }

  // For other views: adjust keeping end date and recalculating start date
  const end = new Date(endDate);
  const newStart = new Date(end);
  newStart.setDate(newStart.getDate() - maxAllowed);

  return {
    startDate: newStart.toISOString().split('T')[0] || '',
    endDate,
  };
}

/**
 * Obtiene el label legible de una vista
 */
export function getViewLabel(view: ViewMode): string {
  const labels: Record<ViewMode, string> = {
    hourly: 'Horaria',
    daily: 'Diaria',
    weekly: 'Semanal',
    monthly: 'Mensual',
  };
  return labels[view];
}

/**
 * Obtiene rangos sugeridos para cada tipo de vista
 */
export function getSuggestedRanges(view: ViewMode): Array<{ label: string; days: number }> {
  const ranges: Record<ViewMode, Array<{ label: string; days: number }>> = {
    hourly: [
      { label: 'Últimas 12 horas', days: 0.5 },
      { label: 'Hoy (24h)', days: 1 },
    ],
    daily: [
      { label: 'Hoy', days: 1 },
      { label: 'Últimos 3 días', days: 3 },
      { label: 'Última semana', days: 7 },
    ],
    weekly: [
      { label: 'Última semana', days: 7 },
      { label: 'Últimos 15 días', days: 15 },
      { label: 'Último mes', days: 30 },
    ],
    monthly: [
      { label: 'Último mes', days: 30 },
      { label: 'Últimos 3 meses', days: 90 },
      { label: 'Últimos 6 meses', days: 180 },
    ],
  };
  return ranges[view];
}

/**
 * Genera un payload estandarizado para enviar al backend
 * Envía fechas en hora de Colombia (UTC-5) con offset explícito.
 * El backend las recibirá y convertirá automáticamente a UTC para queries.
 *
 * Ejemplo:
 * - Input: startDate = "2025-10-19"
 * - Output: startDate = "2025-10-19T00:00:00-05:00"
 * - Backend recibe y convierte a UTC: "2025-10-19T05:00:00.000Z"
 */
export function generateBackendPayload(filters: AnalyticsFilters) {
  // Construir strings ISO 8601 con offset de Colombia (UTC-5)
  // Esto hace explícita la zona horaria del usuario
  const startDate = `${filters.startDate}T00:00:00-05:00`;
  const endDate = `${filters.endDate}T23:59:59.999-05:00`;

  return {
    startDate,
    endDate,
    view: filters.view,
  };
}

/**
 * Compara dos conjuntos de filtros para detectar cambios
 */
export function filtersHaveChanged(
  filters1: AnalyticsFilters,
  filters2: AnalyticsFilters
): boolean {
  return (
    filters1.startDate !== filters2.startDate ||
    filters1.endDate !== filters2.endDate ||
    filters1.view !== filters2.view
  );
}

/**
 * Genera una clave de cache única para un conjunto de filtros
 */
export function getCacheKey(filters: AnalyticsFilters): string {
  return `${filters.startDate}_${filters.endDate}_${filters.view}`;
}
