'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface EnvironmentalTimelineChartProps {
  variables: EnvironmentalVariableAggregate[];
}

// Función para obtener color basado en el nombre de la variable
const getVariableColor = (variableName: string): string => {
  const name = variableName.toLowerCase();

  if (name.includes('ph')) return '#8b5cf6';
  if (name.includes('conductividad') || name.includes('ec')) return '#f59e0b';
  if (name.includes('ambiente')) return '#10b981';
  if (name.includes('agua')) return '#3b82f6';
  if (name.includes('humedad')) return '#3b82f6';
  if (name.includes('luz') || name.includes('luminosidad')) return '#eab308';
  if (name.includes('nivel') && name.includes('agua')) return '#1e40af';
  if (name.includes('tds')) return '#10b981';

  // Default
  return '#6b7280';
};

const EnvironmentalTimelineChart = React.memo(function EnvironmentalTimelineChart({ variables }: EnvironmentalTimelineChartProps) {
  // Initialize with all available variables
  const [selectedVariables, setSelectedVariables] = useState<string[]>(
    variables.map((v) => v.variableName)
  );

  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Update selected variables when variables prop changes
  useEffect(() => {
    setSelectedVariables(variables.map((v) => v.variableName));
  }, [variables]);

  const handleVariableToggle = (variableName: string) => {
    setSelectedVariables((prev) =>
      prev.includes(variableName)
        ? prev.filter((v) => v !== variableName)
        : [...prev, variableName]
    );
  };

  if (!isMounted || variables.length === 0) {
    return (
      <div className="h-96 flex items-center justify-center bg-gray-50 rounded-lg">
        <p className="text-gray-500">Cargando gráfico...</p>
      </div>
    );
  }

  // Filter variables based on selection and create traces
  // Convert UTC timestamps to Colombia local time
  const traces = variables
    .filter((variable) => selectedVariables.includes(variable.variableName))
    .map((variable) => ({
      x: variable.trend.map((point: { timestamp: string }) => point.timestamp),
      y: variable.trend.map((point: { avg: number }) => point.avg),
      type: 'scatter' as const,
      mode: 'lines+markers' as const,
      name: variable.variableName,
      line: {
        color: getVariableColor(variable.variableName),
        width: 2,
      },
      marker: {
        size: 4,
      },
    }));

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-3">
          Tendencias de Variables Ambientales
        </h3>
        <div className="flex flex-wrap gap-2">
          {variables.map((variable) => (
            <button
              key={variable.variableCode || variable.variableName}
              onClick={() => handleVariableToggle(variable.variableName)}
              className={`
                px-3 py-1.5 text-sm rounded-md transition-all border
                ${
                  selectedVariables.includes(variable.variableName)
                    ? 'bg-hidro-green-primary text-white border-hidro-green-primary hover:bg-hidro-green-dark hover:border-hidro-green-dark focus:ring-2 focus:ring-hidro-green-primary'
                    : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50 hover:border-gray-400 focus:ring-2 focus:ring-gray-300'
                }
              `}
            >
              {variable.variableName}
            </button>
          ))}
        </div>
      </div>

      <Plot
        data={traces}
        layout={{
          autosize: true,
          height: 500,
          margin: { l: 80, r: 50, t: 50, b: 100 },
          xaxis: {
            title: { text: 'Fecha y Hora' },
            gridcolor: '#f3f4f6',
          },
          yaxis: {
            title: { text: 'Valor' },
            gridcolor: '#f3f4f6',
          },
          hovermode: 'x unified',
          showlegend: true,
          legend: {
            orientation: 'h',
            y: -0.25,
            x: 0.5,
            xanchor: 'center',
          },
        }}
        config={{
          displayModeBar: false,
          responsive: true,
        }}
        style={{ width: '100%', minHeight: '500px' }}
      />
    </div>
  );
});

export default EnvironmentalTimelineChart;
