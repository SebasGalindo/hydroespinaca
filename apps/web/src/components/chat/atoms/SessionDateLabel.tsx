import { useMemo } from 'react';

interface SessionDateLabelProps {
    /** ISO 8601 date string of the session's last update */
    date: string;
}

/**
 * Atom — temporal grouping label for the session sidebar.
 *
 * Converts an ISO date into a human-readable relative label:
 * "Hoy", "Ayer", "Esta semana", "Este mes", or the year.
 */
export function SessionDateLabel({ date }: SessionDateLabelProps) {
    const label = useMemo(() => getRelativeLabel(date), [date]);
    return (
        <p className="px-3 py-1 text-[10px] font-bold uppercase tracking-widest text-green-700/80 select-none">
            {label}
        </p>
    );
}

// ──────────────────────────────────────
//  Helpers
// ──────────────────────────────────────

function getRelativeLabel(isoDate: string): string {
    const now = new Date();
    const d = new Date(isoDate);

    const diffMs = now.getTime() - d.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Hoy';
    if (diffDays === 1) return 'Ayer';
    if (diffDays <= 6) return 'Esta semana';
    if (diffDays <= 29) return 'Este mes';
    return d.getFullYear().toString();
}
