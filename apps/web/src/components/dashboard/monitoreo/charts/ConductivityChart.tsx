'use client';

import React, { useMemo } from 'react';
import dynamic from 'next/dynamic';
import { SensorData } from '../DashboardMonitoreo';

// Importar Plotly dinámicamente para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ConductivityChartProps {
  data: SensorData[];
  timeRange: string;
}

const ConductivityChart: React.FC<ConductivityChartProps> = ({ data, timeRange }) => {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      return {
        data: [],
        layout: {}
      };
    }

    const timestamps = data.map(d => new Date(d.timestamp));
    const conductivityValues = data.map(d => d.conductivity);

    // Rangos óptimos para conductividad eléctrica en hidroponía (mS/cm)
    const optimalMin = 1.0;
    const optimalMax = 1.5;
    const warningMin = 0.8;
    const warningMax = 1.8;

    return {
      data: [
        // Área de exceso de nutrientes
        {
          x: timestamps,
          y: Array(timestamps.length).fill(2.5),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Exceso de Nutrientes'
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
          name: 'Alta Concentración'
        },
        // Área óptima
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMax),
          fill: 'tonexty',
          fillcolor: 'rgba(16, 185, 129, 0.1)',
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
          name: 'Baja Concentración'
        },
        // Área de deficiencia de nutrientes
        {
          x: timestamps,
          y: Array(timestamps.length).fill(warningMin),
          fill: 'tonexty',
          fillcolor: 'rgba(239, 68, 68, 0.1)',
          line: { color: 'transparent' },
          showlegend: false,
          hoverinfo: 'skip',
          name: 'Deficiencia de Nutrientes'
        },
        // Línea de conductividad actual
        {
          x: timestamps,
          y: conductivityValues,
          type: 'scatter',
          mode: 'lines+markers',
          name: 'Conductividad Eléctrica',
          line: {
            color: '#10b981',
            width: 3,
            shape: 'spline'
          },
          marker: {
            color: '#10b981',
            size: 6,
            line: {
              color: '#ffffff',
              width: 2
            }
          },
          hovertemplate: '<b>%{y:.3f} mS/cm</b><br>%{x}<br><extra></extra>'
        },
        // Líneas de referencia
        {
          x: timestamps,
          y: Array(timestamps.length).fill(optimalMin),
          type: 'scatter',
          mode: 'lines',
          name: 'Mín. Óptimo',
          line: {
            color: '#10b981',
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
            color: '#10b981',
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
            font: { family: 'Inter, sans-serif', size: 14 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 12 },
          type: 'date'
        },
        yaxis: {
          title: {
            text: 'Conductividad Eléctrica (mS/cm)',
            font: { family: 'Inter, sans-serif', size: 14 }
          },
          showgrid: true,
          gridcolor: '#f3f4f6',
          tickfont: { family: 'Inter, sans-serif', size: 12 },
          range: [0.5, 2.5],
          tickvals: [0.5, 0.8, 1.0, 1.2, 1.5, 1.8, 2.0, 2.5],
          ticktext: ['0.5', '0.8', '1.0', '1.2', '1.5', '1.8', '2.0', '2.5']
        },
        plot_bgcolor: 'rgba(0,0,0,0)',
        paper_bgcolor: 'rgba(0,0,0,0)',
        font: { family: 'Inter, sans-serif' },
        margin: { l: 100, r: 40, t: 40, b: 80 },
        showlegend: false,
        hovermode: 'x unified',
        annotations: [
          {
            x: 0.02,
            y: 0.95,
            xref: 'paper',
            yref: 'paper',
            text: 'Deficiencia',
            showarrow: false,
            font: { size: 12, color: '#ef4444', family: 'Inter, sans-serif', weight: 'bold' },
            xanchor: 'left',
            yanchor: 'top'
          },
          {
            x: 0.5,
            y: 0.95,
            xref: 'paper',
            yref: 'paper',
            text: 'Concentración Óptima de Nutrientes',
            showarrow: false,
            font: { size: 12, color: '#10b981', family: 'Inter, sans-serif', weight: 'bold' },
            xanchor: 'center',
            yanchor: 'top'
          },
          {
            x: 0.98,
            y: 0.95,
            xref: 'paper',
            yref: 'paper',
            text: 'Exceso',
            showarrow: false,
            font: { size: 12, color: '#ef4444', family: 'Inter, sans-serif', weight: 'bold' },
            xanchor: 'right',
            yanchor: 'top'
          },
          {
            x: 0.5,
            y: 0.05,
            xref: 'paper',
            yref: 'paper',
            text: 'La conductividad eléctrica indica la concentración de sales y nutrientes disueltos en la solución',
            showarrow: false,
            font: { size: 10, color: '#6b7280', family: 'Inter, sans-serif' },
            xanchor: 'center',
            yanchor: 'bottom'
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
      <div className="flex items-center justify-center h-80 bg-gray-50 rounded-lg border border-gray-200">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-gray-300 border-t-emerald-600 rounded-full animate-spin mx-auto mb-2"></div>
          <p className="text-sm text-gray-600 font-inter">Cargando datos de conductividad...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-80" role="img" aria-label="Gráfico de conductividad eléctrica en el tiempo">
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

export default ConductivityChart;