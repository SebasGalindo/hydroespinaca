'use client';

import React, { useMemo } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importar Plotly dinámicamente para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface PhChartProps {
  data: SensorData[];
  timeRange: string;
}

const PhChart: React.FC<PhChartProps> = ({ data, timeRange }) => {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      return {
        data: [],
        layout: {}
      };
    }

    const timestamps = data.map(d => new Date(d.timestamp));
    const phValues = data.map(d => d.ph);

    // Rangos óptimos para pH en hidroponía
    const optimalMin = 6.0;
    const optimalMax = 7.0;
    const warningMin = 5.5;
    const warningMax = 7.5;

    return {
      data: [
        // Área de rango crítico superior (muy alcalino)
        {
          x: timestamps,
          y: Array(timestamps.length).fill(8.5),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Muy Alcalino'
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
          name: 'Ligeramente Alcalino'
        },
        // Área óptima
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMax),
          fill: 'tonexty',
          fillcolor: 'rgba(168, 85, 247, 0.1)',
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
          name: 'Ligeramente Ácido'
        },
        // Área crítica inferior (muy ácido)
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMin),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Muy Ácido'
        },
        // Línea de pH actual
        {
          x: timestamps,
          y: phValues,
          type: 'scatter',
          mode: 'lines+markers',
          name: 'pH',
          line: {
            color: '#8b5cf6',
            width: 3,
            shape: 'spline'
          },
          marker: {
            color: '#8b5cf6',
            size: 6,
            line: {
              color: '#ffffff',
              width: 2
            }
          },
          hovertemplate: '<b>pH %{y:.2f}</b><br>%{x}<br><extra></extra>'
        },
        // Líneas de referencia
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          type: 'scatter',
          mode: 'lines',
          name: 'pH Mín. Óptimo',
          line: {
            color: '#8b5cf6',
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
          name: 'pH Máx. Óptimo',
          line: {
            color: '#8b5cf6',
            width: 1,
            dash: 'dash'
          },
          hoverinfo: 'skip'
        },
        // Línea neutra (pH 7)
        {
          x: timestamps,
          y: Array(timestamps.length).fill(7.0),
          type: 'scatter',
          mode: 'lines',
          name: 'pH Neutro',
          line: {
            color: '#6b7280',
            width: 1,
            dash: 'dot'
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
            text: 'pH',
            font: { family: 'Inter, sans-serif', size: 12 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 10 },
          range: [4.5, 8.5],
          tickvals: [5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0],
          ticktext: ['5.0', '5.5', '6.0', '6.5', '7.0', '7.5', '8.0']
        },
        plot_bgcolor: 'rgba(0,0,0,0)',
        paper_bgcolor: 'rgba(0,0,0,0)',
        font: { family: 'Inter, sans-serif' },
        margin: { l: 60, r: 20, t: 20, b: 60 },
        showlegend: false,
        hovermode: 'x unified',
        annotations: [
          {
            x: 0.02,
            y: 0.98,
            xref: 'paper',
            yref: 'paper',
            text: 'Ácido',
            showarrow: false,
            font: { size: 10, color: '#ef4444', family: 'Inter, sans-serif' },
            xanchor: 'left',
            yanchor: 'top'
          },
          {
            x: 0.98,
            y: 0.98,
            xref: 'paper',
            yref: 'paper',
            text: 'Alcalino',
            showarrow: false,
            font: { size: 10, color: '#ef4444', family: 'Inter, sans-serif' },
            xanchor: 'right',
            yanchor: 'top'
          }
        ]
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
          <div className="w-8 h-8 border-2 border-gray-300 border-t-purple-600 rounded-full animate-spin mx-auto mb-2"></div>
          <p className="text-sm text-gray-600 font-inter">Cargando datos de pH...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-64" role="img" aria-label="Gráfico de pH en el tiempo">
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

export default PhChart;