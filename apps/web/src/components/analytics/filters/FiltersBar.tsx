'use client';

import React, { useState, useEffect } from 'react';
import Button from '@/components/ui/Button';
import { DownloadIcon, FilterIcon } from '@/components/ui/icons/Icons';
import {
  validateFilters,
  adjustDateRangeForView,
  getSuggestedRanges,
  getDaysDifference,
  filtersHaveChanged,
  type ViewMode,
  type FilterValidationResult,
} from '@/lib/analytics-filters';

export type { ViewMode };

export interface DateRange {
  from: string;
  to: string;
}

export interface FilterState {
  dateRange: DateRange;
  viewMode: ViewMode;
}

interface FiltersBarProps {
  onApplyFilters: (filters: FilterState) => void;
  onExport: () => void;
  isLoading?: boolean;
  currentFilters?: FilterState; // Filtros actualmente aplicados
}

export default function FiltersBar({
  onApplyFilters,
  onExport,
  isLoading,
  currentFilters,
}: FiltersBarProps) {
  const [dateRange, setDateRange] = useState<DateRange>({
    from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0] || '',
    to: new Date().toISOString().split('T')[0] || '',
  });

  const [viewMode, setViewMode] = useState<ViewMode>('daily');
  const [validationError, setValidationError] = useState<FilterValidationResult | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Validar filtros cada vez que cambien
  useEffect(() => {
    const validation = validateFilters(dateRange.from, dateRange.to, viewMode);
    setValidationError(validation.valid ? null : validation);

    // Verificar si hay cambios respecto a los filtros actuales
    if (currentFilters) {
      const changed = filtersHaveChanged(
        { startDate: dateRange.from, endDate: dateRange.to, view: viewMode },
        { startDate: currentFilters.dateRange.from, endDate: currentFilters.dateRange.to, view: currentFilters.viewMode }
      );
      setHasChanges(changed);
    } else {
      setHasChanges(true);
    }
  }, [dateRange, viewMode, currentFilters]);

  const handleApplyFilters = () => {
    const validation = validateFilters(dateRange.from, dateRange.to, viewMode);

    if (!validation.valid) {
      setValidationError(validation);
      return;
    }

    onApplyFilters({ dateRange, viewMode });
    setHasChanges(false);
  };

  const handleQuickSelect = (days: number) => {
    const to = new Date().toISOString().split('T')[0] || '';
    const from = new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString().split('T')[0] || '';
    setDateRange({ from, to });
  };

  const handleViewModeChange = (newView: ViewMode) => {
    setViewMode(newView);

    // Ajustar automáticamente el rango si excede el máximo para la nueva vista
    const adjusted = adjustDateRangeForView(dateRange.from, dateRange.to, newView);

    if (adjusted.startDate !== dateRange.from || adjusted.endDate !== dateRange.to) {
      setDateRange({ from: adjusted.startDate, to: adjusted.endDate });
      // Mostrar mensaje informativo temporal
      setValidationError({
        valid: false,
        message: `El rango se ajustó automáticamente al máximo permitido para vista ${newView === 'daily' ? 'Diaria' : newView === 'weekly' ? 'Semanal' : 'Mensual'}.`,
      });

      // Limpiar mensaje después de 3 segundos
      setTimeout(() => {
        setValidationError(null);
      }, 3000);
    }
  };

  const isApplyDisabled =
    isLoading ||
    !hasChanges ||
    (validationError !== null && !validationError.valid);

  const currentDays = getDaysDifference(dateRange.from, dateRange.to);
  const suggestedRanges = getSuggestedRanges(viewMode);

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-6">
      <div className="flex flex-col gap-4">
        {/* Fila principal de filtros */}
        <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-end">
          {/* Date Range Picker */}
          <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 gap-4 w-full lg:w-auto">
            <div>
              <label htmlFor="date-from" className="block text-sm font-medium text-gray-700 mb-1">
                Fecha Inicio
              </label>
              <input
                id="date-from"
                type="date"
                value={dateRange.from}
                onChange={(e) => setDateRange({ ...dateRange, from: e.target.value })}
                max={dateRange.to}
                disabled={isLoading}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed"
              />
            </div>
            <div>
              <label htmlFor="date-to" className="block text-sm font-medium text-gray-700 mb-1">
                Fecha Fin
              </label>
              <input
                id="date-to"
                type="date"
                value={dateRange.to}
                onChange={(e) => setDateRange({ ...dateRange, to: e.target.value })}
                min={dateRange.from}
                max={new Date().toISOString().split('T')[0]}
                disabled={isLoading}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed"
              />
            </div>
          </div>

          {/* Quick Select Buttons */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Accesos rápidos
            </label>
            <div className="flex gap-2 flex-wrap">
              {suggestedRanges.map((range) => (
                <button
                  key={range.days}
                  onClick={() => handleQuickSelect(range.days)}
                  disabled={isLoading}
                  className="px-3 py-1.5 text-sm bg-gray-100 hover:bg-gray-200 rounded-md transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {range.label}
                </button>
              ))}
            </div>
          </div>

          {/* View Mode Dropdown */}
          <div className="w-full sm:w-auto">
            <label htmlFor="view-mode" className="block text-sm font-medium text-gray-700 mb-1">
              Vista
            </label>
            <select
              id="view-mode"
              value={viewMode}
              onChange={(e) => handleViewModeChange(e.target.value as ViewMode)}
              disabled={isLoading}
              className="w-full sm:w-48 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent bg-white disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <option value="daily">Diaria</option>
              <option value="weekly">Semanal</option>
              <option value="monthly">Mensual</option>
            </select>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-2 w-full sm:w-auto">
            <Button
              onClick={handleApplyFilters}
              variant="primary"
              isLoading={isLoading || false}
              disabled={isApplyDisabled}
              className="flex-1 sm:flex-initial"
              title={
                !hasChanges
                  ? 'No hay cambios para aplicar'
                  : validationError
                  ? validationError.message
                  : 'Aplicar filtros'
              }
            >
              <FilterIcon className="w-4 h-4 mr-2" />
              Aplicar Filtros
            </Button>
            <Button
              onClick={onExport}
              variant="secondary"
              disabled={isLoading}
              className="flex-1 sm:flex-initial"
            >
              <DownloadIcon className="w-4 h-4 mr-2" />
              Exportar
            </Button>
          </div>
        </div>

        {/* Información y mensajes */}
        <div className="flex flex-col sm:flex-row gap-2 items-start sm:items-center justify-between text-sm">
          {/* Información del rango actual */}
          <div className="text-gray-600">
            <span className="font-medium">Rango actual:</span> {currentDays} días
            {viewMode && (
              <span className="ml-2 text-gray-500">
                (Máx. para {viewMode === 'daily' ? 'Diaria' : viewMode === 'weekly' ? 'Semanal' : 'Mensual'}:{' '}
                {viewMode === 'daily' ? '7' : viewMode === 'weekly' ? '31' : '180'} días)
              </span>
            )}
          </div>

          {/* Indicador de cambios pendientes */}
          {hasChanges && !validationError && (
            <div className="flex items-center gap-1 text-blue-600">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                  clipRule="evenodd"
                />
              </svg>
              <span className="text-xs">Cambios pendientes</span>
            </div>
          )}
        </div>

        {/* Mensaje de error o validación */}
        {validationError && !validationError.valid && (
          <div className="flex items-start gap-2 p-3 bg-yellow-50 border border-yellow-200 rounded-md text-yellow-800">
            <svg
              className="w-5 h-5 flex-shrink-0 mt-0.5"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
            <div className="flex-1">
              <p className="text-sm font-medium">{validationError.message}</p>
              {validationError.maxDays && (
                <p className="text-xs mt-1">
                  Sugerencia: Usa uno de los accesos rápidos o ajusta el rango manualmente.
                </p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
