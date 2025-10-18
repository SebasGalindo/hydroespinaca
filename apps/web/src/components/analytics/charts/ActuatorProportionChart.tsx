'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { ActuatorActivity } from '@/lib/analytics-mocks';
import { formatNumericValue } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ActuatorProportionChartProps {
  data: ActuatorActivity[];
}

const actuatorColors: { [key: string]: string } = {
  'pump-1': '#3b82f6',
  'heater-1': '#ef4444',
  'fan-1': '#10b981',
  'light-1': '#f59e0b',
  'cooler-1': '#06b6d4',
};

export default function ActuatorProportionChart({ data }: ActuatorProportionChartProps) {
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

  const totalDuration = data.reduce((sum, a) => sum + a.totalDuration, 0);

  const trace = {
    labels: data.map((a) => a.actuatorName),
    values: data.map((a) => a.totalDuration),
    type: 'pie' as const,
    marker: {
      colors: data.map((a) => actuatorColors[a.actuatorId] || '#6b7280'),
    },
    customdata: data.map((a) => a.totalDuration),
    textinfo: 'label+percent' as const,
    hovertemplate:
      '<b>%{label}</b><br>' +
      'Duración: %{customdata:.2f} min<br>' +
      'Proporción: %{percent:.2%}<extra></extra>',
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Proporción de Tiempo Activo
      </h3>

      <Plot
        data={[trace]}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 30, r: 30, t: 30, b: 30 },
          showlegend: true,
          legend: {
            orientation: 'v',
            x: 1.05,
            y: 0.5,
          },
        }}
        config={{
          displayModeBar: false,
          responsive: true,
        }}
        style={{ width: '100%' }}
      />

      <div className="mt-4 p-3 bg-gray-50 rounded-md">
        <p className="text-sm text-gray-600">
          <strong>Total de tiempo activo:</strong>{' '}
          {formatNumericValue(totalDuration / 60)} horas (
          {formatNumericValue(totalDuration)} minutos)
        </p>
      </div>
    </div>
  );
}
