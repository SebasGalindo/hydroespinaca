'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importación dinámica de Plotly para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface CustomChartProps {
  data: SensorData[];
  timeRange: '1h' | '6h' | '24h' | '7d';
}

type VariableType = 'temperature' | 'humidity' | 'ph' | 'light' | 'conductivity';
type ChartType = 'line' | 'bar' | 'area' | 'histogram';

const CustomChart: React.FC<CustomChartProps> = ({ data, timeRange }) => {
  const [selectedVariable, setSelectedVariable] = useState<VariableType>('temperature');
  const [chartType, setChartType] = useState<ChartType>('line');
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  // Función para obtener el texto del rango de tiempo
  const getTimeRangeText = (range: string) => {
    switch (range) {
      case '1h':
        return 'Última hora';
      case '6h':
        return 'Últimas 6 horas';
      case '24h':
        return 'Últimas 24 horas';
      case '7d':
        return 'Últimos 7 días';
      default:
        return 'Últimos 60 minutos';
    }
  };

  const variableOptions = [
    { value: 'temperature', label: 'Temperatura', unit: '°C', color: '#ef4444' },
    { value: 'humidity', label: 'Humedad', unit: '%', color: '#3b82f6' },
    { value: 'ph', label: 'pH', unit: '', color: '#8b5cf6' },
    { value: 'light', label: 'Luz', unit: 'lux', color: '#f59e0b' },
    { value: 'conductivity', label: 'Conductividad', unit: 'mS/cm', color: '#10b981' }
  ];

  const chartTypeOptions = [
    { value: 'line', label: 'Línea' },
    { value: 'bar', label: 'Barras' },
    { value: 'area', label: 'Área' },
    { value: 'histogram', label: 'Histograma' }
  ];

  const selectedOption = variableOptions.find(opt => opt.value === selectedVariable);
  const values = data.map(d => d[selectedVariable]).filter((val): val is number => typeof val === 'number');
  const timestamps = data.map(d => d.timestamp);

  // Calcular estadísticas
  const currentValue = values[values.length - 1] || 0;
  const previousValue = values[values.length - 2] || 0;
  const change = currentValue - previousValue;
  const changePercent = previousValue !== 0 ? (change / previousValue) * 100 : 0;
  const average = values.reduce((a, b) => a + b, 0) / values.length || 0;
  const min = Math.min(...values);
  const max = Math.max(...values);

  // Preparar datos según el tipo de gráfico
  const getPlotData = () => {
    switch (chartType) {
      case 'line':
        return [{
          x: timestamps,
          y: values,
          type: 'scatter' as const,
          mode: 'lines' as const,
          line: { color: selectedOption?.color, width: 2 },
          name: selectedOption?.label
        }];
      
      case 'bar':
        return [{
          x: timestamps,
          y: values,
          type: 'bar' as const,
          marker: { color: selectedOption?.color },
          name: selectedOption?.label
        }];
      
      case 'area':
        return [{
          x: timestamps,
          y: values,
          type: 'scatter' as const,
          mode: 'lines' as const,
          fill: 'tonexty' as const,
          fillcolor: `${selectedOption?.color}20`,
          line: { color: selectedOption?.color, width: 2 },
          name: selectedOption?.label
        }];
      
      case 'histogram':
        return [{
          x: values,
          type: 'histogram' as const,
          marker: { color: selectedOption?.color },
          name: selectedOption?.label,
          nbinsx: 20
        }];
      
      default:
        return [];
    }
  };

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
        text: chartType === 'histogram' ? selectedOption?.label : 'Tiempo',
        font: { size: 12, color: '#6b7280' }
      }
    },
    yaxis: {
      showgrid: true,
      gridcolor: '#f3f4f6',
      showline: true,
      linecolor: '#e5e7eb',
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' },
      title: {
        text: chartType === 'histogram' ? 'Frecuencia' : `${selectedOption?.label} (${selectedOption?.unit})`,
        font: { size: 12, color: '#6b7280' }
      }
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
      {/* Selectores */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="variable-custom" className="hidro-label">
            Variable
          </label>
          <select
            id="variable-custom"
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
        
        <div>
          <label htmlFor="chart-type-custom" className="hidro-label">
            Tipo de Gráfico
          </label>
          <select
            id="chart-type-custom"
            value={chartType}
            onChange={(e) => setChartType(e.target.value as ChartType)}
            className="hidro-select"
          >
            {chartTypeOptions.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Estadísticas */}
      <div className="bg-gray-50 rounded-lg p-4">
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div className="flex-shrink-0">
            <h4 className="text-lg font-semibold hidro-text-primary">
              {selectedOption?.label}
            </h4>
            <p className="text-sm hidro-text-secondary">Gráfico personalizado - {getTimeRangeText(timeRange)}</p>
          </div>
          <div className="flex-1">
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 text-sm">
              <div className="flex flex-col sm:flex-row sm:items-center gap-1">
                <span className="hidro-text-secondary text-xs sm:text-sm">Actual:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{currentValue.toFixed(2)} {selectedOption?.unit}</span>
              </div>
              <div className="flex flex-col sm:flex-row sm:items-center gap-1">
                <span className="hidro-text-secondary text-xs sm:text-sm">Promedio:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{average.toFixed(2)} {selectedOption?.unit}</span>
              </div>
              <div className="flex flex-col sm:flex-row sm:items-center gap-1">
                <span className="hidro-text-secondary text-xs sm:text-sm">Mín:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{min.toFixed(2)} {selectedOption?.unit}</span>
              </div>
              <div className="flex flex-col sm:flex-row sm:items-center gap-1">
                <span className="hidro-text-secondary text-xs sm:text-sm">Máx:</span>
                <span className="font-semibold hidro-text-primary text-sm sm:text-base">{max.toFixed(2)} {selectedOption?.unit}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Gráfico */}
      <div className="h-64">
        <Plot
          data={getPlotData() as any}
          layout={layout as any}
          config={config as any}
          className="chart-container"
        />
      </div>
    </div>
  );
};

export default CustomChart;