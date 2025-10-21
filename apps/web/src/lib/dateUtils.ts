/**
 * Date utilities for internal frontend operations
 *
 * IMPORTANT: These utilities are ONLY for internal date handling in the frontend
 * (e.g., date pickers, local timestamps, UI components).
 *
 * DO NOT use these functions to convert timestamps received from the backend.
 * The backend already sends timestamps in Colombia timezone, so no conversion is needed.
 */

// Colombia timezone identifier
export const COLOMBIA_TIMEZONE = 'America/Bogota';

/**
 * Gets the current date/time in Colombia timezone
 * Use this for generating local timestamps for UI components like date pickers
 * @returns Date object representing current time in Colombia
 */
export function getNowInColombia(): Date {
  // Create a date in Colombia timezone using Intl API
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: COLOMBIA_TIMEZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });

  const parts = formatter.formatToParts(new Date());
  const values: { [key: string]: string } = {};
  parts.forEach((part) => {
    if (part.type !== 'literal') {
      values[part.type] = part.value;
    }
  });

  return new Date(
    Number(values.year),
    Number(values.month) - 1,
    Number(values.day),
    Number(values.hour),
    Number(values.minute),
    Number(values.second)
  );
}

/**
 * Formats a date to YYYY-MM-DD format for date input fields
 * @param date - Date object to format
 * @returns Date string in YYYY-MM-DD format
 */
export function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
