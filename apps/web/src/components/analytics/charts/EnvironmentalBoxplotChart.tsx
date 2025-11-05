'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import type { ViewMode } from '@/lib/analytics-filters';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

// Format date for display without timezone conversion
// Backend already sends timestamps in Colombia time
function formatDateForDisplay(
  timestamp: string,
  viewMode: 'hourly' | 'daily' | 'weekly' | 'monthly'
): string {
  const date = new Date(timestamp);
  const month = date.toLocaleString('es-CO', { month: 'short' });
  const day = date.getDate();
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  switch (viewMode) {
    case 'hourly':
      return `${month} ${day}, ${hours}:${minutes}`;
    case 'daily':
      return `${month} ${day}`;
    case 'weekly':
      return `${month} ${day}, ${year}`;
    case 'monthly':
      return `${month} ${year}`;
    default:
      return `${year}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }
}

interface EnvironmentalBoxplotChartProps {
  variables: EnvironmentalVariableAggregate[];
  viewMode: ViewMode;
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

export default function EnvironmentalBoxplotChart({ variables, viewMode }: EnvironmentalBoxplotChartProps) {
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

  if (!selectedVariableData || !selectedVariableData.variability || selectedVariableData.variability.length === 0) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
        <p className="text-sm text-yellow-800">
          No hay datos de variabilidad disponibles para esta variable.
        </p>
      </div>
    );
  }

  // Validate and sanitize variability data to prevent rendering errors
  // Remove duplicates based on timestamp and filter out invalid entries
  const validVariability = selectedVariableData.variability.filter((point, index, self) => {
    // Check if point has all required properties
    if (!point || !point.timestamp ||
        typeof point.q1 !== 'number' ||
        typeof point.median !== 'number' ||
        typeof point.q3 !== 'number' ||
        typeof point.min !== 'number' ||
        typeof point.max !== 'number') {
      return false;
    }

    // Remove duplicates by timestamp (keep first occurrence)
    return self.findIndex(p => p.timestamp === point.timestamp) === index;
  });

  if (validVariability.length === 0) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
        <p className="text-sm text-yellow-800">
          Los datos de variabilidad para esta variable no son válidos. Por favor, verifica los datos del backend.
        </p>
      </div>
    );
  }

  // Create a single boxplot trace with all data points
  // Extract formatted date labels for X-axis
  // Timestamps from backend are already in Colombia time, just format for display
  const xLabels = validVariability.map((point: { timestamp: string }) =>
    formatDateForDisplay(point.timestamp, viewMode)
  );

  // Create arrays for boxplot statistics
  const q1Values = validVariability.map((point: { q1: number }) => point.q1);
  const medianValues = validVariability.map((point: { median: number }) => point.median);
  const q3Values = validVariability.map((point: { q3: number }) => point.q3);
  const minValues = validVariability.map((point: { min: number }) => point.min);
  const maxValues = validVariability.map((point: { max: number }) => point.max);

  const traces = [{
    type: 'box' as const,
    x: xLabels,
    q1: q1Values,
    median: medianValues,
    q3: q3Values,
    lowerfence: minValues,
    upperfence: maxValues,
    marker: {
      color: variableColors[selectedVariable] || '#6b7280',
    },
    boxmean: false,
    name: variableDisplayNames[selectedVariable] || selectedVariable,
  }];

  // Get title based on view mode
  const chartTitle = viewMode === 'hourly' ? 'Variabilidad Horaria' : 'Variabilidad Diaria';

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex flex-wrap items-center justify-between mb-4 gap-2">
        <h3 className="text-lg font-semibold text-gray-900">
          {chartTitle}
        </h3>
        <select
          value={selectedVariable}
          onChange={(e) => setSelectedVariable(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent bg-white"
        >
          {variables.map((variable) => (
            <option key={variable.variableCode} value={variable.variableCode}>
              {variableDisplayNames[variable.variableName] || variable.variableName}
            </option>
          ))}
        </select>
      </div>

      <Plot
        data={traces}
        layout={{
          autosize: true,
          height: 500,
          margin: { l: 80, r: 50, t: 50, b: 100 },
          xaxis: {
            title: { text: viewMode === 'hourly' ? 'Hora' : 'Fecha' },
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
        style={{ width: '100%', minHeight: '500px' }}
      />

      <div className="mt-4 p-3 bg-gray-50 rounded-md">
        <p className="text-sm text-gray-600">
          {viewMode === 'hourly'
            ? 'Este gráfico muestra la dispersión de lecturas por hora. La caja representa el rango intercuartílico (IQR), la línea central es la mediana, y los bigotes muestran los valores mínimo y máximo.'
            : 'Este gráfico muestra la dispersión de lecturas por día. La caja representa el rango intercuartílico (IQR), la línea central es la mediana, y los bigotes muestran los valores mínimo y máximo.'}
        </p>
      </div>
    </div>
  );
}
