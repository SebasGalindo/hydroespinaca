'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importación dinámica de Plotly para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ScatterChartProps {
  data: SensorData[];
  timeRange: '1h' | '6h' | '24h' | '7d';
}

type VariableType = 'temperature' | 'humidity' | 'ph' | 'light' | 'conductivity';

const ScatterChart: React.FC<ScatterChartProps> = ({ data, timeRange }) => {
  // Función para obtener el texto del rango de tiempo
  const getTimeRangeText = (range: string): string => {
    switch (range) {
      case '1h': return 'Última hora';
      case '6h': return 'Últimas 6 horas';
      case '24h': return 'Últimas 24 horas';
      case '7d': return 'Últimos 7 días';
      default: return 'Últimos 60 minutos';
    }
  };
  const [variableX, setVariableX] = useState<VariableType>('temperature');
  const [variableY, setVariableY] = useState<VariableType>('humidity');
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const variableOptions = [
    { value: 'temperature', label: 'Temperatura', unit: '°C' },
    { value: 'humidity', label: 'Humedad', unit: '%' },
    { value: 'ph', label: 'pH', unit: '' },
    { value: 'light', label: 'Luz', unit: 'lux' },
    { value: 'conductivity', label: 'Conductividad', unit: 'mS/cm' }
  ];

  const selectedXOption = variableOptions.find(opt => opt.value === variableX);
  const selectedYOption = variableOptions.find(opt => opt.value === variableY);

  // Preparar datos para el gráfico de dispersión
  const xValues = data.map(d => d[variableX]).filter((val): val is number => typeof val === 'number');
  const yValues = data.map(d => d[variableY]).filter((val): val is number => typeof val === 'number');

  // Calcular estadísticas
  const xMean = xValues.reduce((a, b) => a + b, 0) / xValues.length || 0;
  const yMean = yValues.reduce((a, b) => a + b, 0) / yValues.length || 0;
  const xMin = Math.min(...xValues);
  const xMax = Math.max(...xValues);
  const yMin = Math.min(...yValues);
  const yMax = Math.max(...yValues);

  const plotData = [
    {
      x: xValues,
      y: yValues,
      type: 'scatter' as const,
      mode: 'markers' as const,
      marker: {
        color: '#16a34a',
        size: 8,
        opacity: 0.7,
        line: {
          color: '#15803d',
          width: 1
        }
      },
      name: `${selectedXOption?.label} vs ${selectedYOption?.label}`
    }
  ];

  const layout = {
    autosize: true,
    margin: { l: 60, r: 20, t: 20, b: 60 },
    plot_bgcolor: 'transparent',
    paper_bgcolor: 'transparent',
    xaxis: {
      showgrid: true,
      gridcolor: '#f3f4f6',
      showline: true,
      linecolor: '#e5e7eb',
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' },
      title: {
        text: `${selectedXOption?.label} (${selectedXOption?.unit})`,
        font: { size: 12, color: '#6b7280' }
      },
      range: [xMin * 0.95, xMax * 1.05]
    },
    yaxis: {
      showgrid: true,
      gridcolor: '#f3f4f6',
      showline: true,
      linecolor: '#e5e7eb',
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' },
      title: {
        text: `${selectedYOption?.label} (${selectedYOption?.unit})`,
        font: { size: 12, color: '#6b7280' }
      },
      range: [yMin * 0.95, yMax * 1.05]
    },
    showlegend: false,
    hovermode: 'closest' as const
  };

  const config = {
    displayModeBar: false,
    responsive: true
  };

  if (!isClient) {
    return (
      <div className="h-64 flex items-center justify-center bg-gray-50 rounded-lg">
        <div className="text-gray-500">Cargando gráfico...</div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Selectores de Variables */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="variable-x-scatter" className="hidro-label">
            Variable del Eje X
          </label>
          <select
            id="variable-x-scatter"
            value={variableX}
            onChange={(e) => setVariableX(e.target.value as VariableType)}
            className="hidro-select"
          >
            {variableOptions.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        
        <div>
          <label htmlFor="variable-y-scatter" className="hidro-label">
            Variable del Eje Y
          </label>
          <select
            id="variable-y-scatter"
            value={variableY}
            onChange={(e) => setVariableY(e.target.value as VariableType)}
            className="hidro-select"
          >
            {variableOptions.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Estadísticas */}
      <div className="bg-gray-50 rounded-lg p-3 sm:p-4">
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-3">
          <div className="flex-shrink-0">
            <h4 className="text-base sm:text-lg font-semibold hidro-text-primary">
              {selectedXOption?.label} VS {selectedYOption?.label}
            </h4>
            <p className="text-xs sm:text-sm hidro-text-secondary">{getTimeRangeText(timeRange)}</p>
          </div>
          <div className="flex-1">
            <div className="grid grid-cols-2 gap-3 text-xs sm:text-sm">
              <div className="text-center lg:text-right">
                <span className="hidro-text-secondary block">Promedio X:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{xMean.toFixed(2)}</span>
              </div>
              <div className="text-center lg:text-right">
                <span className="hidro-text-secondary block">Promedio Y:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{yMean.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Gráfico */}
      <div className="h-64">
        <Plot
          data={plotData as any}
          layout={layout as any}
          config={config as any}
          className="chart-container"
        />
      </div>
    </div>
  );
};

export default ScatterChart;