'use client';

import React from 'react';
import CorrelationScatterChart from '../charts/CorrelationScatterChart';
import CorrelationHeatmapChart from '../charts/CorrelationHeatmapChart';

interface CorrelationsLevelProps {
  isLoading?: boolean;
}

const environmentalVariables = ['temperature', 'humidity', 'ph', 'conductivity', 'light'];
const actuators = ['pump-1', 'heater-1', 'fan-1', 'light-1', 'cooler-1'];

export default function CorrelationsLevel({ isLoading }: CorrelationsLevelProps) {
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
        <p className="text-sm text-blue-800">
          <strong>Objetivo:</strong> Explorar relaciones entre variables ambientales y la actividad de actuadores.
          Los datos actuales son <strong>mocks temporales</strong> y serán reemplazados por cálculos reales desde fuzzy-service o data-service vía BFF.
        </p>
      </div>

      <CorrelationScatterChart
        environmentalVariables={environmentalVariables}
        actuators={actuators}
      />

      <CorrelationHeatmapChart />

      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-3">
          Información sobre Correlaciones
        </h3>
        <div className="space-y-2 text-sm text-gray-700">
          <p>
            <strong>Coeficiente de Pearson:</strong> Mide la correlación lineal entre dos variables.
            Valores cercanos a +1 indican correlación positiva fuerte, valores cercanos a -1 indican
            correlación negativa fuerte, y valores cercanos a 0 indican ausencia de correlación lineal.
          </p>
          <p>
            <strong>Uso práctico:</strong> Identificar qué actuadores responden más a cambios en variables
            ambientales específicas puede ayudar a optimizar las rutinas de control y reducir consumo energético.
          </p>
          <p className="text-xs text-gray-500 mt-3">
            Nota: Las correlaciones mostradas son ejemplos generados. En producción, estos valores se
            calcularán a partir de datos históricos reales del sistema.
          </p>
        </div>
      </div>
    </div>
  );
}
