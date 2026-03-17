'use client';

import React from 'react';
import { CalendarIcon } from '@/components/ui/icons/Icons';

interface DateRangeFilterProps {
  from: string;
  to: string;
  onFromChange: (value: string) => void;
  onToChange: (value: string) => void;
  onApply: () => void;
  isLoading?: boolean;
  className?: string;
  /** Optional extra content to render after the buttons (e.g. type filter) */
  children?: React.ReactNode;
}

const DateRangeFilter = React.memo(function DateRangeFilter({
  from,
  to,
  onFromChange,
  onToChange,
  onApply,
  isLoading = false,
  className = '',
  children,
}: DateRangeFilterProps) {
  const today = new Date().toISOString().split('T')[0];

  const handleQuickSelect = (days: number) => {
    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - days);
    onFromChange(start.toISOString().split('T')[0] ?? '');
    onToChange(end.toISOString().split('T')[0] ?? '');
  };

  return (
    <div className={`hidro-card p-4 ${className}`}>
      <div className="flex flex-col gap-3">
        {/* Date inputs row */}
        <div className="flex flex-col sm:flex-row gap-3 items-end">
          <div className="flex-1 w-full">
            <label className="block text-xs font-medium text-gray-600 mb-1 font-inter">
              Desde
            </label>
            <div className="relative">
              <input
                type="date"
                value={from}
                max={to || today}
                onChange={(e) => onFromChange(e.target.value)}
                className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
              />
              <CalendarIcon size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            </div>
          </div>
          <div className="flex-1 w-full">
            <label className="block text-xs font-medium text-gray-600 mb-1 font-inter">
              Hasta
            </label>
            <div className="relative">
              <input
                type="date"
                value={to}
                min={from}
                max={today}
                onChange={(e) => onToChange(e.target.value)}
                className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
              />
              <CalendarIcon size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            </div>
          </div>
          <button
            onClick={onApply}
            disabled={isLoading || !from || !to}
            className="hidro-button-primary text-sm px-5 py-2 font-inter whitespace-nowrap disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <span className="flex items-center gap-2">
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white" />
                Cargando...
              </span>
            ) : (
              'Aplicar'
            )}
          </button>
        </div>

        {/* Quick select buttons */}
        <div className="flex flex-wrap gap-2 items-center">
          <span className="text-xs text-gray-500 font-inter">Rápido:</span>
          {[
            { label: '7 días', days: 7 },
            { label: '30 días', days: 30 },
            { label: '90 días', days: 90 },
            { label: '6 meses', days: 180 },
          ].map(({ label, days }) => (
            <button
              key={days}
              onClick={() => handleQuickSelect(days)}
              className="px-3 py-1 text-xs font-medium text-gray-600 bg-gray-100 hover:bg-green-100 hover:text-green-700 rounded-full transition-colors font-inter"
            >
              {label}
            </button>
          ))}
        </div>

        {/* Optional extra filters */}
        {children}
      </div>
    </div>
  );
});

export default DateRangeFilter;
