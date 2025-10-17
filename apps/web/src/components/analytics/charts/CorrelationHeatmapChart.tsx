'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { generateCorrelationMatrix, variableDisplayNames } from '@/lib/analytics-mocks';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

export default function CorrelationHeatmapChart() {
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!isMounted) {
    return (
      <div className="h-96 flex items-center justify-center bg-gray-50 rounded-lg">
        <p className="text-gray-500">Cargando gráfico...</p>
      </div>
    );
  }

  const correlations = generateCorrelationMatrix();
  const variables = ['temperature', 'humidity', 'ph', 'conductivity', 'light'];

  // Build correlation matrix
  const matrix: number[][] = [];
  for (let i = 0; i < variables.length; i++) {
    const row: number[] = [];
    for (let j = 0; j < variables.length; j++) {
      if (i === j) {
        row.push(1);
      } else {
        const corr = correlations.find(
          (c) =>
            (c.variable1 === variables[i] && c.variable2 === variables[j]) ||
            (c.variable1 === variables[j] && c.variable2 === variables[i])
        );
        row.push(corr?.correlation || 0);
      }
    }
    matrix.push(row);
  }

  const trace = {
    z: matrix,
    x: variables.map((v) => variableDisplayNames[v] || v),
    y: variables.map((v) => variableDisplayNames[v] || v),
    type: 'heatmap' as const,
    colorscale: 'RdYlGn' as const,
    zmin: -1,
    zmax: 1,
    hovertemplate: '%{y} vs %{x}<br>Correlación: %{z:.3f}<extra></extra>',
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Matriz de Correlación entre Variables Ambientales
      </h3>

      <Plot
        data={[trace]}
        layout={{
          autosize: true,
          height: 500,
          margin: { l: 120, r: 30, t: 30, b: 120 },
          xaxis: {
            tickangle: -45,
          },
          yaxis: {
            autorange: 'reversed' as const,
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
          <strong>Interpretación:</strong> Los valores van de -1 (correlación negativa fuerte) a +1 (correlación positiva fuerte).
          El 0 indica ausencia de correlación lineal. Los colores ayudan a identificar patrones rápidamente.
        </p>
      </div>
    </div>
  );
}
