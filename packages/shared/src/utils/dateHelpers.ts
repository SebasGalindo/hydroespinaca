/**
 * Shared date formatting utilities.
 * Pure functions with no platform dependency — usable by both web and mobile.
 */

const DEFAULT_LOCALE = 'es-CO';

/**
 * Returns a relative date label: "Hoy", "Ayer", or a formatted short date.
 * Useful for chat date dividers and grouped lists.
 *
 * @example
 * formatRelativeDate('2026-03-03T10:00:00Z') // "Hoy" (if today is 2026-03-03)
 * formatRelativeDate('2026-03-02T10:00:00Z') // "Ayer"
 * formatRelativeDate('2026-02-15T10:00:00Z') // "15 feb"
 * formatRelativeDate('2025-01-10T10:00:00Z') // "10 ene 2025"
 */
export function formatRelativeDate(iso: string, locale: string = DEFAULT_LOCALE): string {
  const now = new Date();
  const d = new Date(iso);
  const diffMs = now.getTime() - d.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) return 'Hoy';
  if (diffDays === 1) return 'Ayer';

  return d.toLocaleDateString(locale, {
    day: 'numeric',
    month: 'short',
    year: diffDays > 300 ? 'numeric' : undefined,
  });
}

/**
 * Returns a compact relative time string: "Ahora", "5 min", "3 h", "2 d", "1 sem", "3 mes".
 * Useful for timestamps on list items (sessions, notifications, etc.).
 *
 * @example
 * formatRelativeTime('2026-03-03T12:55:00Z') // "5 min"  (if now is 12:00)
 * formatRelativeTime('2026-03-03T09:00:00Z') // "3 h"
 * formatRelativeTime('2026-02-28T10:00:00Z') // "3 d"
 */
export function formatRelativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'Ahora';
  if (mins < 60) return `${mins} min`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs} h`;
  const days = Math.floor(hrs / 24);
  if (days < 7) return `${days} d`;
  if (days < 30) return `${Math.floor(days / 7)} sem`;
  return `${Math.floor(days / 30)} mes`;
}

/**
 * Formats an ISO date as a short DD/MM HH:mm timestamp.
 * Useful for notification history items and log entries.
 *
 * @example
 * formatDateTimestamp('2026-03-03T14:30:00Z') // "03/03 14:30"
 */
export function formatDateTimestamp(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  const day = d.getDate().toString().padStart(2, '0');
  const month = (d.getMonth() + 1).toString().padStart(2, '0');
  const hours = d.getHours().toString().padStart(2, '0');
  const mins = d.getMinutes().toString().padStart(2, '0');
  return `${day}/${month} ${hours}:${mins}`;
}
