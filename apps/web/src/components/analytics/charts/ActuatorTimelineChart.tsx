'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { ActuatorActivity } from '@/lib/actuator-analytics-mapper';
import { getColombiaDate } from '@/lib/dateUtils';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ActuatorTimelineChartProps {
  data: ActuatorActivity[];
}

const actuatorColors: { [key: string]: string } = {
  'pump-1': '#3b82f6',
  'heater-1': '#ef4444',
  'fan-1': '#10b981',
  'light-1': '#f59e0b',
  'cooler-1': '#06b6d4',
};

export default function ActuatorTimelineChart({ data }: ActuatorTimelineChartProps) {
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

  // Create traces for each actuator
  // Convert UTC timestamps to Colombia local time for consistency
  const traces = data.map((actuator, index) => {
    const bars = actuator.activations.map((activation) => ({
      x: [
        getColombiaDate(activation.startTime).getTime(),
        getColombiaDate(activation.endTime).getTime(),
      ],
      y: [index, index],
    }));

    return bars.map((bar, barIndex) => ({
      x: bar.x,
      y: bar.y,
      type: 'scatter' as const,
      mode: 'lines' as const,
      line: {
        color: actuatorColors[actuator.actuatorId] || '#6b7280',
        width: 20,
      },
      showlegend: barIndex === 0,
      name: actuator.actuatorName,
      hovertemplate: `<b>${actuator.actuatorName}</b><br>Duración: ${actuator.activations[barIndex]?.duration} min<extra></extra>`,
    }));
  }).flat();

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Timeline de Activaciones
      </h3>

      <Plot
        data={traces}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 150, r: 30, t: 30, b: 60 },
          xaxis: {
            title: { text: 'Fecha y Hora' },
            type: 'date',
            gridcolor: '#f3f4f6',
          },
          yaxis: {
            title: { text: '' },
            tickmode: 'array',
            tickvals: data.map((_, i) => i),
            ticktext: data.map((a) => a.actuatorName),
            gridcolor: '#f3f4f6',
          },
          hovermode: 'closest',
          showlegend: false,
        }}
        config={{
          displayModeBar: false,
          responsive: true,
        }}
        style={{ width: '100%' }}
      />

      <div className="mt-4 p-3 bg-gray-50 rounded-md">
        <p className="text-sm text-gray-600">
          Cada barra horizontal representa una activación del actuador. La longitud indica la duración de la activación.
        </p>
      </div>
    </div>
  );
}
