'use client';

import React from 'react';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import type { ViewMode } from '@/lib/analytics-filters';
import EnvironmentalSummaryCards from '../charts/EnvironmentalSummaryCards';
import EnvironmentalTimelineChart from '../charts/EnvironmentalTimelineChart';
import EnvironmentalBoxplotChart from '../charts/EnvironmentalBoxplotChart';

interface EnvironmentalLevelProps {
  variables: EnvironmentalVariableAggregate[];
  viewMode: ViewMode;
  isLoading?: boolean;
  error?: string | null;
}

const EnvironmentalLevel = React.memo(function EnvironmentalLevel({
  variables,
  viewMode,
  isLoading,
  error
}: EnvironmentalLevelProps) {
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-48 bg-gray-100 animate-pulse rounded-lg"></div>
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border-l-4 border-red-500 p-4 rounded">
        <h3 className="text-sm font-semibold text-red-800 mb-2">Error al cargar datos</h3>
        <p className="text-sm text-red-700">{error}</p>
        <p className="text-xs text-red-600 mt-2">
          Por favor, intenta ajustar el rango de fechas o contacta al administrador del sistema.
        </p>
      </div>
    );
  }

  if (variables.length === 0) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
        <p className="text-sm text-yellow-800">
          No hay datos disponibles para el rango de fechas seleccionado.
          Por favor, intenta con otro rango de fechas.
        </p>
      </div>
    );
  }

  // Check if any variable has variability data
  const hasVariabilityData = variables.some(
    (variable) => variable.variability && variable.variability.length > 0
  );

  return (
    <div className="space-y-6">
      
      {/* Summary Cards */}
      <EnvironmentalSummaryCards variables={variables} />

      {/* Trend Chart */}
      <EnvironmentalTimelineChart variables={variables} />

      {/* Variability Chart - Available for hourly and daily views */}
      {(viewMode === 'hourly' || viewMode === 'daily') && hasVariabilityData && (
        <EnvironmentalBoxplotChart variables={variables} viewMode={viewMode} />
      )}

      {(viewMode === 'hourly' || viewMode === 'daily') && !hasVariabilityData && (
        <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
          <p className="text-sm text-blue-800">
            <strong>Nota:</strong> El gráfico de variabilidad solo está disponible cuando hay suficientes datos.
          </p>
        </div>
      )}

      {viewMode !== 'hourly' && viewMode !== 'daily' && (
        <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
          <p className="text-sm text-blue-800">
            <strong>Nota:</strong> El gráfico de variabilidad solo está disponible en las vistas horaria y diaria.
            Cambia a vista horaria o diaria para ver la variabilidad de los datos.
          </p>
        </div>
      )}
    </div>
  );
});

export default EnvironmentalLevel;
