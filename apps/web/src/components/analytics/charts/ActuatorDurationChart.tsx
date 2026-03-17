'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { ActuatorActivity } from '@/lib/actuator-analytics-mapper';
import { formatNumericValue } from '@hydroespinaca/shared';
import { getActuatorColor, formatTooltipValue } from '@/lib/actuator-colors';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface ActuatorDurationChartProps {
  data: ActuatorActivity[];
}

const ActuatorDurationChart = React.memo(function ActuatorDurationChart({ data }: ActuatorDurationChartProps) {
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
    y: data.map((a) => a.totalDuration / 60), // Convert to hours
    type: 'bar' as const,
    marker: {
      color: data.map((a) => getActuatorColor(a.actuatorId)),
    },
    text: data.map((a) => `${formatTooltipValue(a.totalDuration / 60)}h`),
    textposition: 'auto' as const,
    hovertemplate: '<b>%{x}</b><br>Duración: %{y:.2f} horas<br>Activaciones: %{customdata}<extra></extra>',
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
          height: 500,
          margin: { l: 80, r: 50, t: 50, b: 120 },
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
        style={{ width: '100%', minHeight: '500px' }}
      />

      <div className="mt-4 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {data.map((actuator) => (
          <div key={actuator.actuatorId} className="p-3 bg-gray-50 rounded-md">
            <div className="flex items-center gap-2 mb-1">
              <div
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: getActuatorColor(actuator.actuatorId) }}
              ></div>
              <p className="text-xs font-medium text-gray-700 truncate">{actuator.actuatorName}</p>
            </div>
            <p className="text-sm text-gray-600">{actuator.activationCount} activaciones</p>
          </div>
        ))}
      </div>
    </div>
  );
});

export default ActuatorDurationChart;
