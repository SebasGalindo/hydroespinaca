'use client';

import React, { useState } from 'react';
import { ActuatorActivity } from '@/lib/analytics-mocks';
import ActuatorTimelineChart from '../charts/ActuatorTimelineChart';
import ActuatorDurationChart from '../charts/ActuatorDurationChart';
import ActuatorProportionChart from '../charts/ActuatorProportionChart';

interface ActuatorsLevelProps {
  data: ActuatorActivity[];
  isLoading?: boolean;
}

type ViewMode = 'timeline' | 'duration' | 'proportion' | 'all';

export default function ActuatorsLevel({ data, isLoading }: ActuatorsLevelProps) {
  const [viewMode, setViewMode] = useState<ViewMode>('all');
  const [selectedActuators, setSelectedActuators] = useState<string[]>(
    data.map((a) => a.actuatorId)
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
        <div className="h-96 bg-gray-100 animate-pulse rounded-lg"></div>
      </div>
    );
  }

  const filteredData = data.filter((a) => selectedActuators.includes(a.actuatorId));

  const handleActuatorToggle = (actuatorId: string) => {
    setSelectedActuators((prev) =>
      prev.includes(actuatorId)
        ? prev.filter((id) => id !== actuatorId)
        : [...prev, actuatorId]
    );
  };

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
        <p className="text-sm text-blue-800">
          <strong>Objetivo:</strong> Visualizar la frecuencia, duración y patrones de uso de los actuadores.
          Los datos actuales son <strong>mocks temporales</strong> y serán reemplazados por datos reales del actuator-service vía BFF.
        </p>
      </div>

      {/* Controls */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
        <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
          {/* View Mode Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Modo de Vista</label>
            <div className="flex flex-wrap gap-2">
              {[
                { value: 'all', label: 'Todos' },
                { value: 'timeline', label: 'Timeline' },
                { value: 'duration', label: 'Duración' },
                { value: 'proportion', label: 'Proporción' },
              ].map((mode) => (
                <button
                  key={mode.value}
                  onClick={() => setViewMode(mode.value as ViewMode)}
                  className={`
                    px-4 py-2 text-sm rounded-md transition-all
                    ${
                      viewMode === mode.value
                        ? 'bg-hidro-green-primary text-white'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    }
                  `}
                >
                  {mode.label}
                </button>
              ))}
            </div>
          </div>

          {/* Actuator Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Actuadores</label>
            <div className="flex flex-wrap gap-2">
              {data.map((actuator) => (
                <button
                  key={actuator.actuatorId}
                  onClick={() => handleActuatorToggle(actuator.actuatorId)}
                  className={`
                    px-3 py-1.5 text-sm rounded-md transition-all
                    ${
                      selectedActuators.includes(actuator.actuatorId)
                        ? 'bg-hidro-green-primary text-white'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    }
                  `}
                >
                  {actuator.actuatorName}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Charts */}
      {filteredData.length === 0 ? (
        <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded">
          <p className="text-sm text-yellow-800">
            No hay actuadores seleccionados. Por favor, selecciona al menos uno para ver los gráficos.
          </p>
        </div>
      ) : (
        <>
          {(viewMode === 'all' || viewMode === 'timeline') && (
            <ActuatorTimelineChart data={filteredData} />
          )}
          {(viewMode === 'all' || viewMode === 'duration') && (
            <ActuatorDurationChart data={filteredData} />
          )}
          {(viewMode === 'all' || viewMode === 'proportion') && (
            <ActuatorProportionChart data={filteredData} />
          )}
        </>
      )}
    </div>
  );
}
