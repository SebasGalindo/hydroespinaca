'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface EnvironmentalBoxplotChartProps {
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

export default function EnvironmentalBoxplotChart({ variables }: EnvironmentalBoxplotChartProps) {
  // Initialize with first available variable
  const [selectedVariable, setSelectedVariable] = useState<string>(
    variables.length > 0 && variables[0] ? variables[0].variableCode : ''
  );
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Update selected variable when variables prop changes
  useEffect(() => {
    if (variables.length > 0 && variables[0] && !variables.find((v) => v.variableCode === selectedVariable)) {
      setSelectedVariable(variables[0].variableCode);
    }
  }, [variables, selectedVariable]);

  if (!isMounted || variables.length === 0) {
    return (
      <div className="h-96 flex items-center justify-center bg-gray-50 rounded-lg">
        <p className="text-gray-500">Cargando gráfico...</p>
      </div>
    );
  }

  // Find the selected variable data
  const selectedVariableData = variables.find((v) => v.variableCode === selectedVariable);

  if (!selectedVariableData || selectedVariableData.variability.length === 0) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
        <p className="text-sm text-yellow-800">
          No hay datos de variabilidad disponibles para esta variable.
        </p>
      </div>
    );
  }

  // Create boxplot traces from variability data
  const traces = selectedVariableData.variability.map((point: { timestamp: string; q1: number; median: number; q3: number; min: number; max: number }) => {
    // Format date for display
    const date = new Date(point.timestamp).toLocaleDateString('es-ES', {
      month: 'short',
      day: 'numeric',
    });

    return {
      type: 'box' as const,
      name: date,
      q1: [point.q1],
      median: [point.median],
      q3: [point.q3],
      lowerfence: [point.min],
      upperfence: [point.max],
      marker: {
        color: variableColors[selectedVariable] || '#6b7280',
      },
      boxmean: false,
    };
  });

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex flex-wrap items-center justify-between mb-4 gap-2">
        <h3 className="text-lg font-semibold text-gray-900">
          Variabilidad Diaria
        </h3>
        <select
          value={selectedVariable}
          onChange={(e) => setSelectedVariable(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent bg-white"
        >
          {variables.map((variable) => (
            <option key={variable.variableCode} value={variable.variableCode}>
              {variableDisplayNames[variable.variableCode] || variable.variableCode}
            </option>
          ))}
        </select>
      </div>

      <Plot
        data={traces}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 60, r: 30, t: 30, b: 80 },
          xaxis: {
            title: { text: 'Fecha' },
            gridcolor: '#f3f4f6',
          },
          yaxis: {
            title: { text: variableDisplayNames[selectedVariable] || selectedVariable },
            gridcolor: '#f3f4f6',
          },
          showlegend: false,
        }}
        config={{
          displayModeBar: false,
          responsive: true,
        }}
        style={{ width: '100%' }}
      />

      <div className="mt-4 p-3 bg-gray-50 rounded-md">
        <p className="text-sm text-gray-600">
          Este gráfico muestra la dispersión de lecturas por día. La caja representa el rango intercuartílico (IQR),
          la línea central es la mediana, y los puntos externos son valores atípicos.
        </p>
      </div>
    </div>
  );
}
