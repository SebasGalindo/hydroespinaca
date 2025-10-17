'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { generateActuatorEnvironmentCorrelation, variableDisplayNames, actuatorDisplayNames } from '@/lib/analytics-mocks';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface CorrelationScatterChartProps {
  environmentalVariables: string[];
  actuators: string[];
}

export default function CorrelationScatterChart({
  environmentalVariables,
  actuators,
}: CorrelationScatterChartProps) {
  const [selectedEnvVar, setSelectedEnvVar] = useState<string>(environmentalVariables[0] || 'temperature');
  const [selectedActuator, setSelectedActuator] = useState<string>(actuators[0] || 'heater-1');
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

  const correlationData = generateActuatorEnvironmentCorrelation(selectedEnvVar, selectedActuator);

  const trace = {
    x: correlationData.data.x,
    y: correlationData.data.y,
    type: 'scatter' as const,
    mode: 'markers' as const,
    marker: {
      size: 8,
      color: '#16a34a',
      opacity: 0.6,
    },
    hovertemplate: `${variableDisplayNames[selectedEnvVar] || selectedEnvVar}: %{x:.2f}<br>Duración: %{y:.1f} min<extra></extra>`,
  };

  // Calculate trend line
  const n = correlationData.data.x.length;
  const sumX = correlationData.data.x.reduce((a, b) => a + b, 0);
  const sumY = correlationData.data.y.reduce((a, b) => a + b, 0);
  const sumXY = correlationData.data.x.reduce((sum, x, i) => sum + x * (correlationData.data.y[i] || 0), 0);
  const sumX2 = correlationData.data.x.reduce((sum, x) => sum + x * x, 0);

  const slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
  const intercept = (sumY - slope * sumX) / n;

  const minX = Math.min(...correlationData.data.x);
  const maxX = Math.max(...correlationData.data.x);

  const trendTrace = {
    x: [minX, maxX],
    y: [slope * minX + intercept, slope * maxX + intercept],
    type: 'scatter' as const,
    mode: 'lines' as const,
    line: {
      color: '#ef4444',
      width: 2,
      dash: 'dash' as const,
    },
    name: 'Tendencia',
    hoverinfo: 'skip' as const,
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Correlación: Variable Ambiental vs Actuador
      </h3>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Variable Ambiental
          </label>
          <select
            value={selectedEnvVar}
            onChange={(e) => setSelectedEnvVar(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent bg-white"
          >
            {environmentalVariables.map((variable) => (
              <option key={variable} value={variable}>
                {variableDisplayNames[variable]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Actuador
          </label>
          <select
            value={selectedActuator}
            onChange={(e) => setSelectedActuator(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-hidro-green-primary focus:border-transparent bg-white"
          >
            {actuators.map((actuator) => (
              <option key={actuator} value={actuator}>
                {actuatorDisplayNames[actuator]}
              </option>
            ))}
          </select>
        </div>
      </div>

      <Plot
        data={[trace, trendTrace]}
        layout={{
          autosize: true,
          height: 400,
          margin: { l: 60, r: 30, t: 30, b: 60 },
          xaxis: {
            title: { text: variableDisplayNames[selectedEnvVar] || selectedEnvVar },
            gridcolor: '#f3f4f6',
          },
          yaxis: {
            title: { text: 'Duración Actuador (min)' },
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

      <div className="mt-4 p-4 bg-gray-50 rounded-md">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-gray-700">Coeficiente de Correlación (Pearson)</p>
            <p className="text-xs text-gray-500 mt-1">
              Mide la fuerza y dirección de la relación lineal
            </p>
          </div>
          <div className="text-right">
            <p className={`text-2xl font-bold ${
              Math.abs(correlationData.correlation) > 0.7
                ? 'text-hidro-success'
                : Math.abs(correlationData.correlation) > 0.4
                ? 'text-hidro-warning'
                : 'text-gray-500'
            }`}>
              {correlationData.correlation.toFixed(3)}
            </p>
            <p className="text-xs text-gray-500 mt-1">
              {Math.abs(correlationData.correlation) > 0.7
                ? 'Correlación fuerte'
                : Math.abs(correlationData.correlation) > 0.4
                ? 'Correlación moderada'
                : 'Correlación débil'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
