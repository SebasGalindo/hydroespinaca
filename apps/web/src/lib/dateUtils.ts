import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

// Extend dayjs with timezone plugins
dayjs.extend(utc);
dayjs.extend(timezone);

// Colombia timezone
export const COLOMBIA_TIMEZONE = 'America/Bogota';

/**
 * Converts a UTC timestamp string to Colombia local time
 * @param utcTimestamp - ISO 8601 UTC timestamp string (e.g., "2025-10-17T07:00:00.000Z")
 * @returns Date object in Colombia timezone
 */
export function utcToColombiaTime(utcTimestamp: string): Date {
  return dayjs.utc(utcTimestamp).tz(COLOMBIA_TIMEZONE).toDate();
}

/**
 * Formats a UTC timestamp as a local Colombia time string
 * @param utcTimestamp - ISO 8601 UTC timestamp string
 * @param format - dayjs format string (default: 'YYYY-MM-DD HH:mm')
 * @returns Formatted date string in Colombia timezone
 */
export function formatColombiaTime(
  utcTimestamp: string,
  format: string = 'YYYY-MM-DD HH:mm'
): string {
  return dayjs.utc(utcTimestamp).tz(COLOMBIA_TIMEZONE).format(format);
}

/**
 * Formats a UTC timestamp for chart axis display in Colombia time
 * @param utcTimestamp - ISO 8601 UTC timestamp string
 * @param viewMode - Chart view mode (hourly, daily, weekly, monthly)
 * @returns Formatted date string appropriate for the view mode
 */
export function formatChartDate(
  utcTimestamp: string,
  viewMode: 'hourly' | 'daily' | 'weekly' | 'monthly'
): string {
  const date = dayjs.utc(utcTimestamp).tz(COLOMBIA_TIMEZONE);

  switch (viewMode) {
    case 'hourly':
      return date.format('MMM D, HH:mm');
    case 'daily':
      return date.format('MMM D');
    case 'weekly':
      return date.format('MMM D, YYYY');
    case 'monthly':
      return date.format('MMM YYYY');
    default:
      return date.format('YYYY-MM-DD');
  }
}

/**
 * Gets a Date object representation in Colombia timezone
 * This is useful for Plotly.js which expects Date objects
 * @param utcTimestamp - ISO 8601 UTC timestamp string
 * @returns Date object representing the Colombia local time
 */
export function getColombiaDate(utcTimestamp: string): Date {
  // Plotly.js works with Date objects, but we need to ensure it displays Colombia time
  // We create a Date object that represents the Colombia local time
  const colombiaTime = dayjs.utc(utcTimestamp).tz(COLOMBIA_TIMEZONE);

  // Create a new Date object with the Colombia local time values
  // This prevents Plotly from doing automatic timezone conversion
  return new Date(
    colombiaTime.year(),
    colombiaTime.month(),
    colombiaTime.date(),
    colombiaTime.hour(),
    colombiaTime.minute(),
    colombiaTime.second(),
    colombiaTime.millisecond()
  );
}

/**
 * Formats tooltip dates for charts
 * @param utcTimestamp - ISO 8601 UTC timestamp string
 * @param viewMode - Chart view mode
 * @returns User-friendly formatted date string
 */
export function formatTooltipDate(
  utcTimestamp: string,
  viewMode: 'hourly' | 'daily' | 'weekly' | 'monthly'
): string {
  const date = dayjs.utc(utcTimestamp).tz(COLOMBIA_TIMEZONE);

  switch (viewMode) {
    case 'hourly':
      return date.format('DD/MM/YYYY HH:mm');
    case 'daily':
      return date.format('DD/MM/YYYY');
    case 'weekly':
      return date.format('DD/MM/YYYY');
    case 'monthly':
      return date.format('MM/YYYY');
    default:
      return date.format('DD/MM/YYYY');
  }
}
