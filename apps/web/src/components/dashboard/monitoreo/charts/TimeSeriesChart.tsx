'use client';

import React, { useState } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importación dinámica de Plotly para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { 
  ssr: false,
  loading: () => (
    <div className="h-64 flex items-center justify-center bg-gray-50 rounded-lg">
      <p>Cargando gráfico...</p>
    </div>
  )
});

interface TimeSeriesChartProps {
  data: SensorData[];
  timeRange: '1h' | '6h' | '24h' | '7d';
}

type VariableType = 'temperature' | 'humidity' | 'ph' | 'light' | 'conductivity';

const TimeSeriesChart: React.FC<TimeSeriesChartProps> = ({ data, timeRange }) => {
  const [selectedVariable, setSelectedVariable] = useState<VariableType>('temperature');

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

  const variableOptions = [
    { value: 'temperature', label: 'Temperatura', unit: '°C' },
    { value: 'humidity', label: 'Humedad', unit: '%' },
    { value: 'ph', label: 'pH', unit: '' },
    { value: 'light', label: 'Luz', unit: 'lux' },
    { value: 'conductivity', label: 'Conductividad', unit: 'mS/cm' }
  ];

  const selectedOption = variableOptions.find(opt => opt.value === selectedVariable);

  // Preparar datos para el gráfico - filtrar datos válidos
  const validData = data.filter(d => typeof d[selectedVariable] === 'number');
  const timestamps = validData.map(d => new Date(d.timestamp));
  const values = validData.map(d => d[selectedVariable] as number);

  // Calcular estadísticas
  const currentValue = values.length > 0 ? (values[values.length - 1] ?? 0) : 0;
  const previousValue = values.length > 1 ? (values[values.length - 2] ?? currentValue) : currentValue;
  const change = currentValue - previousValue;
  const changePercent = previousValue !== 0 ? ((change / previousValue) * 100) : 0;

  const plotData = [
    {
      x: timestamps,
      y: values,
      type: 'scatter' as const,
      mode: 'lines+markers' as const,
      line: {
        color: '#16a34a',
        width: 2
      },
      marker: {
        color: '#16a34a',
        size: 4
      },
      name: selectedOption?.label || 'Variable'
    }
  ];

  const layout = {
    autosize: true,
    margin: { l: 50, r: 20, t: 20, b: 50 },
    plot_bgcolor: 'transparent',
    paper_bgcolor: 'transparent',
    xaxis: {
      showgrid: true,
      gridcolor: '#f3f4f6',
      showline: false,
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' }
    },
    yaxis: {
      showgrid: true,
      gridcolor: '#f3f4f6',
      showline: false,
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' },
      title: {
        text: selectedOption?.unit || '',
        font: { size: 12, color: '#6b7280' }
      }
    },
    showlegend: false,
    hovermode: 'x unified' as const
  };

  const config = {
    displayModeBar: false,
    responsive: true
  };

  return (
    <div className="space-y-4">
      {/* Selector de Variable */}
      <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center">
        <div className="flex-1">
          <label htmlFor="variable-select" className="hidro-label">
            Variable
          </label>
          <select
            id="variable-select"
            value={selectedVariable}
            onChange={(e) => setSelectedVariable(e.target.value as VariableType)}
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
              {selectedOption?.label}
            </h4>
            <p className="text-xs sm:text-sm hidro-text-secondary">{getTimeRangeText(timeRange)}</p>
          </div>
          <div className="flex-1 flex justify-center lg:justify-end">
            <div className="flex items-center gap-2">
              <span className="text-xl sm:text-2xl font-bold text-green-600">
                {changePercent > 0 ? '+' : ''}{changePercent.toFixed(1)}%
              </span>
              <svg 
                className={`w-4 h-4 sm:w-5 sm:h-5 ${changePercent >= 0 ? 'text-green-600' : 'text-red-600'}`}
                fill="currentColor" 
                viewBox="0 0 20 20"
              >
                {changePercent >= 0 ? (
                  <path fillRule="evenodd" d="M5.293 7.707a1 1 0 010-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 01-1.414 1.414L11 5.414V17a1 1 0 11-2 0V5.414L6.707 7.707a1 1 0 01-1.414 0z" clipRule="evenodd" />
                ) : (
                  <path fillRule="evenodd" d="M14.707 12.293a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 111.414-1.414L9 14.586V3a1 1 0 012 0v11.586l2.293-2.293a1 1 0 011.414 0z" clipRule="evenodd" />
                )}
              </svg>
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

export default TimeSeriesChart;