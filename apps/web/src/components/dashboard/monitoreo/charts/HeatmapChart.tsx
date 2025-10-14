'use client';

import React, { useState, useEffect, useCallback } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importación dinámica de Plotly para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface HeatmapChartProps {
  data: SensorData[];
  timeRange: '1h' | '6h' | '24h' | '7d';
}

type VariableType = 'temperature' | 'humidity' | 'ph' | 'light' | 'conductivity';

const HeatmapChart: React.FC<HeatmapChartProps> = ({ data, timeRange }) => {
  const [variableX, setVariableX] = useState<VariableType>('temperature');
  const [variableY, setVariableY] = useState<VariableType>('humidity');
  const [isClient, setIsClient] = useState(false);
  const [correlation, setCorrelation] = useState<number>(0);

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

  useEffect(() => {
    setIsClient(true);
  }, []);

  const variableOptions = [
    { value: 'temperature', label: 'Temperatura' },
    { value: 'humidity', label: 'Humedad' },
    { value: 'ph', label: 'pH' },
    { value: 'light', label: 'Luz' },
    { value: 'conductivity', label: 'Conductividad' }
  ];

  // Calcular correlación entre las variables seleccionadas
  const calculateCorrelation = useCallback(() => {
    if (data.length < 2) return 0;
    
    // Filtrar datos válidos para ambas variables
    const validData = data.filter(d => 
      typeof d[variableX] === 'number' && typeof d[variableY] === 'number'
    );
    
    if (validData.length < 2) return 0;
    
    const xValues = validData.map(d => d[variableX] as number);
    const yValues = validData.map(d => d[variableY] as number);
    
    const n = xValues.length;
    const sumX = xValues.reduce((a, b) => a + b, 0);
    const sumY = yValues.reduce((a, b) => a + b, 0);
    const sumXY = xValues.reduce((sum, x, i) => sum + x * (yValues[i] ?? 0), 0);
    const sumX2 = xValues.reduce((sum, x) => sum + x * x, 0);
    const sumY2 = yValues.reduce((sum, y) => sum + y * y, 0);
    
    const numerator = n * sumXY - sumX * sumY;
    const denominator = Math.sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));
    
    return denominator === 0 ? 0 : numerator / denominator;
  }, [data, variableX, variableY]);

  useEffect(() => {
    setCorrelation(calculateCorrelation());
  }, [calculateCorrelation]);

  // Generar datos del mapa de calor
  const generateHeatmapData = () => {
    // Crear una matriz de correlación simulada para demostración
    const variables = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
    const matrix: number[][] = [];
    
    for (let i = 0; i < variables.length; i++) {
      const row: number[] = [];
      for (let j = 0; j < variables.length; j++) {
        if (i === j) {
          row.push(1); // Correlación perfecta consigo mismo
        } else {
          // Generar correlaciones simuladas basadas en las variables seleccionadas
          let corr = Math.random() * 2 - 1; // Entre -1 y 1
          if ((i === 0 && j === 1) || (i === 1 && j === 0)) {
            corr = correlation; // Usar la correlación calculada para las variables seleccionadas
          }
          row.push(corr);
        }
      }
      matrix.push(row);
    }
    
    return { variables, matrix };
  };

  const { variables, matrix } = generateHeatmapData();

  const plotData = [
    {
      z: matrix,
      x: variables,
      y: variables,
      type: 'heatmap' as const,
      colorscale: [
        [0, '#1e40af'],
        [0.25, '#3b82f6'],
        [0.5, '#ffffff'],
        [0.75, '#f59e0b'],
        [1, '#dc2626']
      ],
      showscale: true,
      colorbar: {
        title: 'Correlación',
        titleside: 'right'
      }
    }
  ];

  const layout = {
    autosize: true,
    margin: { l: 50, r: 80, t: 20, b: 50 },
    plot_bgcolor: 'transparent',
    paper_bgcolor: 'transparent',
    xaxis: {
      showgrid: false,
      showline: false,
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' }
    },
    yaxis: {
      showgrid: false,
      showline: false,
      zeroline: false,
      tickfont: { size: 12, color: '#6b7280' }
    }
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
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label htmlFor="variable-x" className="hidro-label">
            Variable X
          </label>
          <select
            id="variable-x"
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
          <label htmlFor="variable-y" className="hidro-label">
            Variable Y
          </label>
          <select
            id="variable-y"
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
        
        <div className="flex items-end">
          <button className="w-full bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 transition-colors">
            Analyze
          </button>
        </div>
      </div>

      {/* Estadísticas de Correlación */}
      <div className="bg-gray-50 rounded-lg p-3 sm:p-4">
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-3">
          <div className="flex-shrink-0">
            <h4 className="text-base sm:text-lg font-semibold text-gray-900">
              {variableOptions.find(v => v.value === variableX)?.label} VS {variableOptions.find(v => v.value === variableY)?.label}
            </h4>
            <p className="text-xs sm:text-sm text-gray-600">{getTimeRangeText(timeRange)}</p>
          </div>
          <div className="flex-1 flex justify-center lg:justify-end">
            <div className="flex items-center gap-2">
              <span className={`text-lg sm:text-2xl font-bold ${
                correlation > 0.5 ? 'text-green-600' : 
                correlation < -0.5 ? 'text-red-600' : 'text-yellow-600'
              }`}>
                {correlation > 0 ? '+' : ''}{correlation.toFixed(2)} Correlación
              </span>
              <svg 
                className={`w-4 h-4 sm:w-5 sm:h-5 ${
                  correlation > 0.5 ? 'text-green-600' : 
                  correlation < -0.5 ? 'text-red-600' : 'text-yellow-600'
                }`}
                fill="currentColor" 
                viewBox="0 0 20 20"
              >
                <path fillRule="evenodd" d="M5.293 7.707a1 1 0 010-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 01-1.414 1.414L11 5.414V17a1 1 0 11-2 0V5.414L6.707 7.707a1 1 0 01-1.414 0z" clipRule="evenodd" />
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

export default HeatmapChart;