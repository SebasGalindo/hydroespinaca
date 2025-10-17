'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface EnvironmentalTimelineChartProps {
  variables: EnvironmentalVariableAggregate[];
}

// Mapeo de códigos de variables a nombres legibles
const variableDisplayNames: Record<string, string> = {
  PH: 'pH',
  EC: 'Conductividad (mS/cm)',
  TEMP: 'Temperatura (°C)',
  HUMIDITY: 'Humedad (%)',
  LIGHT: 'Luz (lux)',
  WATER_TEMP: 'Temp. Agua (°C)',
  TDS: 'TDS (ppm)',
};

const variableColors: Record<string, string> = {
  PH: '#8b5cf6',
  EC: '#f59e0b',
  TEMP: '#ef4444',
  HUMIDITY: '#3b82f6',
  LIGHT: '#eab308',
  WATER_TEMP: '#06b6d4',
  TDS: '#10b981',
};

export default function EnvironmentalTimelineChart({ variables }: EnvironmentalTimelineChartProps) {
  // Initialize with all available variables
  const [selectedVariables, setSelectedVariables] = useState<string[]>(
    variables.map((v) => v.variableCode)
  );

  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Update selected variables when variables prop changes
  useEffect(() => {
    setSelectedVariables(variables.map((v) => v.variableCode));
  }, [variables]);

  const handleVariableToggle = (variableCode: string) => {
    setSelectedVariables((prev) =>
      prev.includes(variableCode)
        ? prev.filter((v) => v !== variableCode)
        : [...prev, variableCode]
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
  const traces = variables
    .filter((variable) => selectedVariables.includes(variable.variableCode))
    .map((variable) => ({
      x: variable.trend.map((point: { timestamp: string }) => point.timestamp),
      y: variable.trend.map((point: { avg: number }) => point.avg),
      type: 'scatter' as const,
      mode: 'lines+markers' as const,
      name: variableDisplayNames[variable.variableCode] || variable.variableCode,
      line: {
        color: variableColors[variable.variableCode] || '#6b7280',
        width: 2,
      },
      marker: {
        size: 4,
      },
    }));

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex flex-wrap items-center justify-between mb-4 gap-2">
        <h3 className="text-lg font-semibold text-gray-900">
          Tendencias de Variables Ambientales
        </h3>
        <div className="flex flex-wrap gap-2">
          {variables.map((variable) => (
            <button
              key={variable.variableCode}
              onClick={() => handleVariableToggle(variable.variableCode)}
              className={`
                px-3 py-1.5 text-sm rounded-md transition-all
                ${
                  selectedVariables.includes(variable.variableCode)
                    ? 'bg-hidro-green-primary text-white'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }
              `}
            >
              {variableDisplayNames[variable.variableCode] || variable.variableCode}
            </button>
          ))}
        </div>
      </div>

      <Plot
        data={traces}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 60, r: 30, t: 30, b: 60 },
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
            y: -0.2,
            x: 0.5,
            xanchor: 'center',
          },
        }}
        config={{
          displayModeBar: false,
          responsive: true,
        }}
        style={{ width: '100%' }}
      />
    </div>
  );
}
