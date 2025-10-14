'use client';

import React, { useMemo } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importar Plotly dinámicamente para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface HumidityChartProps {
  data: SensorData[];
  timeRange: string;
}

const HumidityChart: React.FC<HumidityChartProps> = ({ data, timeRange }) => {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      return {
        data: [],
        layout: {}
      };
    }

    const timestamps = data.map(d => new Date(d.timestamp));
    const humidity = data.map(d => d.humidity);

    // Rangos óptimos para humedad
    const optimalMin = 60;
    const optimalMax = 70;
    const warningMin = 50;
    const warningMax = 80;

    return {
      data: [
        // Área de rango crítico superior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(100),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Crítico Alto'
        },
        // Área de rango de advertencia superior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMax),
          fill: 'tonexty',
          fillcolor: 'rgba(245, 158, 11, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Advertencia Alto'
        },
        // Área óptima
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMax),
          fill: 'tonexty',
          fillcolor: 'rgba(59, 130, 246, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Óptimo'
        },
        // Área de rango de advertencia inferior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          fill: 'tonexty',
          fillcolor: 'rgba(245, 158, 11, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Advertencia Bajo'
        },
        // Área crítica inferior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMin),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Crítico Bajo'
        },
        // Línea de humedad actual
        {
          x: timestamps,
          y: humidity,
          type: 'scatter',
          mode: 'lines+markers',
          name: 'Humedad',
          line: {
            color: '#3b82f6',
            width: 3,
            shape: 'spline'
          },
          marker: {
            color: '#3b82f6',
            size: 6,
            line: {
              color: '#ffffff',
              width: 2
            }
          },
          hovertemplate: '<b>%{y:.1f}%</b><br>%{x}<br><extra></extra>'
        },
        // Líneas de referencia
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          type: 'scatter',
          mode: 'lines',
          name: 'Mín. Óptimo',
          line: {
            color: '#3b82f6',
            width: 1,
            dash: 'dash'
          },
          hoverinfo: 'skip'
        },
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMax),
          type: 'scatter',
          mode: 'lines',
          name: 'Máx. Óptimo',
          line: {
            color: '#3b82f6',
            width: 1,
            dash: 'dash'
          },
          hoverinfo: 'skip'
        }
      ],
      layout: {
        title: {
          text: '',
          font: { family: 'Inter, sans-serif' }
        },
        xaxis: {
          title: {
            text: 'Tiempo',
            font: { family: 'Inter, sans-serif', size: 12 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 10 },
          type: 'date'
        },
        yaxis: {
          title: {
            text: 'Humedad (%)',
            font: { family: 'Inter, sans-serif', size: 12 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 10 },
          range: [30, 100]
        },
        plot_bgcolor: 'rgba(0,0,0,0)',
        paper_bgcolor: 'rgba(0,0,0,0)',
        font: { family: 'Inter, sans-serif' },
        margin: { l: 60, r: 20, t: 20, b: 60 },
        showlegend: false,
        hovermode: 'x unified'
      }
    };
  }, [data]);

  const config = {
    displayModeBar: false,
    displaylogo: false,
    responsive: true
  };

  if (!data || data.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 bg-gray-50 rounded-lg border border-gray-200">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin mx-auto mb-2"></div>
          <p className="text-sm text-gray-600 font-inter">Cargando datos de humedad...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-64" role="img" aria-label="Gráfico de humedad en el tiempo">
      <Plot
        data={chartData.data as any}
        layout={chartData.layout as any}
        config={config as any}
        className="chart-container"
        useResizeHandler={true}
      />
    </div>
  );
};

export default HumidityChart;