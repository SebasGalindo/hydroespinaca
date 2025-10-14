'use client';

import React, { useMemo } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importar Plotly dinámicamente para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface LightChartProps {
  data: SensorData[];
  timeRange: string;
}

const LightChart: React.FC<LightChartProps> = ({ data, timeRange }) => {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      return {
        data: [],
        layout: {}
      };
    }

    const timestamps = data.map(d => new Date(d.timestamp));
    const lightValues = data.map(d => d.light);

    // Rangos óptimos para luz en hidroponía (lux)
    const optimalMin = 600;
    const optimalMax = 1000;
    const warningMin = 400;
    const warningMax = 1200;

    return {
      data: [
        // Área de exceso de luz
        {
          x: timestamps,
          y: Array(timestamps.length).fill(1500),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Exceso de Luz'
        },
        // Área de advertencia superior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMax),
          fill: 'tonexty',
          fillcolor: 'rgba(245, 158, 11, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Luz Alta'
        },
        // Área óptima
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMax),
          fill: 'tonexty',
          fillcolor: 'rgba(251, 191, 36, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Óptimo'
        },
        // Área de advertencia inferior
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          fill: 'tonexty',
          fillcolor: 'rgba(245, 158, 11, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Luz Baja'
        },
        // Área de luz insuficiente
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMin),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Luz Insuficiente'
        },
        // Línea de luz actual
        {
          x: timestamps,
          y: lightValues,
          type: 'scatter',
          mode: 'lines+markers',
          name: 'Intensidad Lumínica',
          line: {
            color: '#f59e0b',
            width: 3,
            shape: 'spline'
          },
          marker: {
            color: '#f59e0b',
            size: 6,
            line: {
              color: '#ffffff',
              width: 2
            }
          },
          hovertemplate: '<b>%{y:.0f} lux</b><br>%{x}<br><extra></extra>'
        },
        // Líneas de referencia
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          type: 'scatter',
          mode: 'lines',
          name: 'Mín. Óptimo',
          line: {
            color: '#f59e0b',
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
            color: '#f59e0b',
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
            text: 'Intensidad Lumínica (lux)',
            font: { family: 'Inter, sans-serif', size: 12 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 10 },
          range: [0, 1500]
        },
        plot_bgcolor: 'rgba(0,0,0,0)',
        paper_bgcolor: 'rgba(0,0,0,0)',
        font: { family: 'Inter, sans-serif' },
        margin: { l: 80, r: 20, t: 20, b: 60 },
        showlegend: false,
        hovermode: 'x unified',
        annotations: [
          {
            x: 0.02,
            y: 0.98,
            xref: 'paper',
            yref: 'paper',
            text: 'Insuficiente',
            showarrow: false,
            font: { size: 10, color: '#ef4444', family: 'Inter, sans-serif' },
            xanchor: 'left',
            yanchor: 'top'
          },
          {
            x: 0.5,
            y: 0.98,
            xref: 'paper',
            yref: 'paper',
            text: 'Óptimo',
            showarrow: false,
            font: { size: 10, color: '#f59e0b', family: 'Inter, sans-serif' },
            xanchor: 'center',
            yanchor: 'top'
          },
          {
            x: 0.98,
            y: 0.98,
            xref: 'paper',
            yref: 'paper',
            text: 'Excesivo',
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
          <div className="w-8 h-8 border-2 border-gray-300 border-t-yellow-600 rounded-full animate-spin mx-auto mb-2"></div>
          <p className="text-sm text-gray-600 font-inter">Cargando datos de luz...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-64" role="img" aria-label="Gráfico de intensidad lumínica en el tiempo">
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

export default LightChart;