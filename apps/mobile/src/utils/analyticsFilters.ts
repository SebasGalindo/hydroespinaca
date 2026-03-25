/**
 * Analytics filter utilities for mobile.
 * Mirrors the web's analytics-filters.ts logic, adapted for React Native.
 */

export type ViewMode = 'hourly' | 'daily' | 'weekly' | 'monthly';

export interface DateRange {
  from: string; // YYYY-MM-DD
  to: string;   // YYYY-MM-DD
}

export interface FilterState {
  dateRange: DateRange;
  viewMode: ViewMode;
}

export const MAX_RANGES: Record<ViewMode, number> = {
  hourly: 1,
  daily: 7,
  weekly: 31,
  monthly: 180,
};

export const VIEW_LABELS: Record<ViewMode, string> = {
  hourly: 'Horaria (1 día)',
  daily: 'Diaria (7 días)',
  weekly: 'Semanal (31 días)',
  monthly: 'Mensual (180 días)',
};

export interface QuickRange {
  label: string;
  days: number;
}

/**
 * Get the number of days between two date strings.
 */
export function getDaysDifference(startDate: string, endDate: string): number {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diff = end.getTime() - start.getTime();
  return Math.ceil(diff / (1000 * 60 * 60 * 24)) + 1; // +1 to include start day
}

/**
 * Get the suggested quick-select ranges for the given view mode.
 */
export function getSuggestedRanges(viewMode: ViewMode): QuickRange[] {
  switch (viewMode) {
    case 'hourly':
      return [{ label: 'Hoy', days: 0 }];
    case 'daily':
      return [
        { label: '3 días', days: 3 },
        { label: '7 días', days: 7 },
      ];
    case 'weekly':
      return [
        { label: '2 sem', days: 14 },
        { label: '1 mes', days: 31 },
      ];
    case 'monthly':
      return [
        { label: '3 meses', days: 90 },
        { label: '6 meses', days: 180 },
      ];
  }
}

/**
 * Auto-adjust the date range to fit the max allowed for the selected view.
 */
export function adjustDateRangeForView(
  from: string,
  to: string,
  view: ViewMode,
): { from: string; to: string } {
  const maxDays = MAX_RANGES[view];
  const days = getDaysDifference(from, to);

  if (days <= maxDays) return { from, to };

  // Shrink from the start date forward
  const toDate = new Date(to);
  const newFromDate = new Date(toDate);
  newFromDate.setDate(toDate.getDate() - maxDays + 1);

  return {
    from: newFromDate.toISOString().split('T')[0]!,
    to,
  };
}

/**
 * Validate filters, returning an error message if invalid.
 */
export function validateFilters(
  from: string,
  to: string,
  view: ViewMode,
): { valid: boolean; message?: string } {
  if (!from || !to) return { valid: false, message: 'Selecciona ambas fechas.' };

  const startDate = new Date(from);
  const endDate = new Date(to);

  if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
    return { valid: false, message: 'Fechas inválidas.' };
  }
  if (startDate > endDate) {
    return { valid: false, message: 'La fecha inicio no puede ser posterior a la fecha fin.' };
  }

  const days = getDaysDifference(from, to);
  const maxDays = MAX_RANGES[view];

  if (days > maxDays) {
    return {
      valid: false,
      message: `El rango máximo para vista ${VIEW_LABELS[view]} es ${maxDays} día${maxDays !== 1 ? 's' : ''}.`,
    };
  }

  return { valid: true };
}

/**
 * Generate the backend payload with Colombia timezone offsets.
 */
export function generateBackendPayload(filters: FilterState): {
  startDate: string;
  endDate: string;
  view: ViewMode;
} {
  return {
    startDate: `${filters.dateRange.from}T00:00:00-05:00`,
    endDate: `${filters.dateRange.to}T23:59:59.999-05:00`,
    view: filters.viewMode,
  };
}

/**
 * Get today's date in Colombia timezone as YYYY-MM-DD.
 */
export function getColombiaToday(): string {
  return new Date().toLocaleString('en-CA', {
    timeZone: 'America/Bogota',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).replace(/\//g, '-');
}

/**
 * Get a date N days ago as YYYY-MM-DD.
 */
export function getDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().split('T')[0]!;
}

/**
 * Get default filters (today, hourly view).
 */
export function getDefaultFilters(): FilterState {
  const today = getColombiaToday();
  return {
    dateRange: { from: today, to: today },
    viewMode: 'hourly',
  };
}
