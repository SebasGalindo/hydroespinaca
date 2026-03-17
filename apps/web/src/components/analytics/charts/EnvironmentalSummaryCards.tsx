'use client';

import React from 'react';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';

interface EnvironmentalSummaryCardsProps {
  variables: EnvironmentalVariableAggregate[];
}

// Función para obtener metadatos basados en el nombre de la variable
const getVariableMetadata = (variableName: string): { unit: string; color: string } => {
  const name = variableName.toLowerCase();

  if (name.includes('ph')) return { unit: '', color: 'bg-purple-500' };
  if (name.includes('conductividad') || name.includes('ec')) return { unit: 'mS/cm', color: 'bg-amber-500' };
  if (name.includes('temperatura')) return { unit: '°C', color: 'bg-red-500' };
  if (name.includes('humedad')) return { unit: '%', color: 'bg-blue-500' };
  if (name.includes('luz') || name.includes('luminosidad')) return { unit: 'lux', color: 'bg-yellow-500' };
  if (name.includes('nivel') && name.includes('agua')) return { unit: 'cm', color: 'bg-blue-600' };
  if (name.includes('tds')) return { unit: 'ppm', color: 'bg-green-500' };

  // Default
  return { unit: '', color: 'bg-gray-500' };
};

// Formatear número (el backend ya redondea a 2 decimales)
const formatValue = (value: number): string => {
  return value.toLocaleString('es-ES', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
};

const EnvironmentalSummaryCards = React.memo(function EnvironmentalSummaryCards({ variables }: EnvironmentalSummaryCardsProps) {
  if (variables.length === 0) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
        <p className="text-sm text-yellow-800">
          No hay datos disponibles para el rango de fechas seleccionado.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold text-gray-900">Resumen de Variables Ambientales</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {variables.map((variable) => {
          const metadata = getVariableMetadata(variable.variableName);

          return (
            <div
              key={variable.variableCode || variable.variableName}
              className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow"
            >
              {/* Header con color de variable */}
              <div className="flex items-center gap-3 mb-3">
                <div className={`w-3 h-3 rounded-full ${metadata.color}`}></div>
                <h4 className="text-sm font-medium text-gray-700">{variable.variableName}</h4>
              </div>

              {/* Valor promedio (prominente) */}
              <div className="mb-4">
                <div className="text-3xl font-bold text-gray-900">
                  {formatValue(variable.summary.avg)}
                  {metadata.unit && <span className="text-lg text-gray-500 ml-1">{metadata.unit}</span>}
                </div>
                <p className="text-xs text-gray-500 mt-1">Promedio</p>
              </div>

              {/* Mínimo y Máximo */}
              <div className="flex justify-between items-center mb-3 text-sm">
                <div>
                  <p className="text-gray-500 text-xs">Mínimo</p>
                  <p className="font-semibold text-gray-700">
                    {formatValue(variable.summary.min)}
                    {metadata.unit && <span className="text-xs ml-0.5">{metadata.unit}</span>}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-gray-500 text-xs">Máximo</p>
                  <p className="font-semibold text-gray-700">
                    {formatValue(variable.summary.max)}
                    {metadata.unit && <span className="text-xs ml-0.5">{metadata.unit}</span>}
                  </p>
                </div>
              </div>

              {/* Total de lecturas (badge) */}
              <div className="pt-3 border-t border-gray-100">
                <div className="flex items-center justify-between">
                  <span className="text-xs text-gray-500">Total de lecturas</span>
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                    {variable.summary.count.toLocaleString()}
                  </span>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
});

export default EnvironmentalSummaryCards;
