'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { ActuatorActivity } from '@/lib/analytics-mocks';
import { formatNumericValue } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ActuatorDurationChartProps {
  data: ActuatorActivity[];
}

const actuatorColors: { [key: string]: string } = {
  'pump-1': '#3b82f6',
  'heater-1': '#ef4444',
  'fan-1': '#10b981',
  'light-1': '#f59e0b',
  'cooler-1': '#06b6d4',
};

export default function ActuatorDurationChart({ data }: ActuatorDurationChartProps) {
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!isMounted || data.length === 0) {
    return (
      <div className="h-96 flex items-center justify-center bg-gray-50 rounded-lg">
        <p className="text-gray-500">Cargando gráfico...</p>
      </div>
    );
  }

  const trace = {
    x: data.map((a) => a.actuatorName),
    y: data.map((a) => formatNumericValue(a.totalDuration / 60)), // Convert to hours with formatting
    type: 'bar' as const,
    marker: {
      color: data.map((a) => actuatorColors[a.actuatorId] || '#6b7280'),
    },
    text: data.map((a) => `${formatNumericValue(a.totalDuration / 60)}h`),
    textposition: 'auto' as const,
    hovertemplate: '<b>%{x}</b><br>Duración: %{y} horas<br>Activaciones: %{customdata}<extra></extra>',
    customdata: data.map((a) => a.activationCount),
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Duración Total por Actuador
      </h3>

      <Plot
        data={[trace]}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 60, r: 30, t: 30, b: 100 },
          xaxis: {
            title: { text: '' },
            gridcolor: '#f3f4f6',
            tickangle: -45,
          },
          yaxis: {
            title: { text: 'Horas' },
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

      <div className="mt-4 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {data.map((actuator) => (
          <div key={actuator.actuatorId} className="p-3 bg-gray-50 rounded-md">
            <div className="flex items-center gap-2 mb-1">
              <div
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: actuatorColors[actuator.actuatorId] || '#6b7280' }}
              ></div>
              <p className="text-xs font-medium text-gray-700 truncate">{actuator.actuatorName}</p>
            </div>
            <p className="text-sm text-gray-600">{actuator.activationCount} activaciones</p>
          </div>
        ))}
      </div>
    </div>
  );
}
