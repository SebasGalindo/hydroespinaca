import type { ConsumptionType } from '@hydroespinaca/shared';
import { chartColors } from '@hydroespinaca/shared';

/**
 * Format a number as currency with the given currency code.
 */
export function formatCurrency(amount: number, currency: string = 'COP'): string {
  if (currency === 'USD') {
    return `$${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }
  return `$${amount.toLocaleString('es-CO', { minimumFractionDigits: 0, maximumFractionDigits: 0 })} ${currency}`;
}

/**
 * Format a number with a unit suffix.
 */
export function formatAmount(amount: number, unit: string): string {
  return `${amount.toLocaleString('es-CO', { maximumFractionDigits: 2 })} ${unit}`;
}

/**
 * Format an ISO 8601 date string as a short date (DD/MM/YYYY).
 */
export function formatDate(isoDate: string): string {
  const d = new Date(isoDate);
  return d.toLocaleDateString('es-CO', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

/**
 * Format an ISO 8601 date string as a short date (DD MMM YYYY).
 */
export function formatDateShort(isoDate: string): string {
  const d = new Date(isoDate);
  return d.toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' });
}

/**
 * Get the number of days between two ISO date strings.
 */
export function getDaysBetween(from: string, to: string): number {
  const a = new Date(from);
  const b = new Date(to);
  return Math.ceil(Math.abs(b.getTime() - a.getTime()) / (1000 * 60 * 60 * 24));
}

/**
 * Get today's date as ISO string (YYYY-MM-DD).
 */
export function getTodayISO(): string {
  return new Date().toISOString().split('T')[0]!;
}

/**
 * Convert a Date to a noon-UTC ISO string to avoid timezone day-shift.
 * E.g. a Date for "2025-11-01" becomes "2025-11-01T12:00:00Z".
 */
export function toNoonUTC(date: Date): string {
  const iso = date.toISOString().split('T')[0]!;
  return `${iso}T12:00:00Z`;
}

/**
 * Get a date N days ago as ISO string (YYYY-MM-DD).
 */
export function getDaysAgoISO(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().split('T')[0]!;
}

/**
 * Icon name for a consumption type.
 */
export function getConsumptionIcon(type: ConsumptionType): string {
  const map: Record<ConsumptionType, string> = {
    1: 'bolt',
    2: 'droplet',
    3: 'plant',
  };
  return map[type];
}

/**
 * Color for a consumption type.
 */
export function getConsumptionColor(type: ConsumptionType): string {
  const map: Record<ConsumptionType, string> = {
    1: chartColors.amber, // electricity
    2: chartColors.blue, // water
    3: chartColors.green, // nutrients
  };
  return map[type];
}

/**
 * Short label for a consumption type.
 */
export function getConsumptionShortLabel(type: ConsumptionType): string {
  const map: Record<ConsumptionType, string> = {
    1: 'Electricidad',
    2: 'Agua',
    3: 'Nutrientes',
  };
  return map[type];
}

/**
 * Currency select options.
 */
export const CURRENCY_OPTIONS = [
  { label: 'COP - Peso Colombiano', value: 'COP' },
  { label: 'USD - Dólar', value: 'USD' },
];
