'use client';

import React, { useState } from 'react';
import { ActuatorActivity } from '@/lib/actuator-analytics-mapper';
import ActuatorTimelineChart from '../charts/ActuatorTimelineChart';
import ActuatorDurationChart from '../charts/ActuatorDurationChart';
import ActuatorProportionChart from '../charts/ActuatorProportionChart';

interface ActuatorsLevelProps {
  data: ActuatorActivity[];
  isLoading?: boolean;
  error?: string | null;
}

type ViewMode = 'timeline' | 'duration' | 'proportion' | 'all';

export default function ActuatorsLevel({ data, isLoading, error }: ActuatorsLevelProps) {
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
      {/* Error Alert */}
      {error && (
        <div className="bg-red-50 border-l-4 border-red-500 p-4 rounded">
          <p className="text-sm text-red-800">
            <strong>Error:</strong> {error}
          </p>
        </div>
      )}

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
                    px-4 py-2 text-sm rounded-md transition-all border
                    ${
                      viewMode === mode.value
                        ? 'bg-hidro-green-primary text-white border-hidro-green-primary hover:bg-hidro-green-dark hover:border-hidro-green-dark focus:ring-2 focus:ring-hidro-green-primary'
                        : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50 hover:border-gray-400 focus:ring-2 focus:ring-gray-300'
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
                    px-3 py-1.5 text-sm rounded-md transition-all border
                    ${
                      selectedActuators.includes(actuator.actuatorId)
                        ? 'bg-hidro-green-primary text-white border-hidro-green-primary hover:bg-hidro-green-dark hover:border-hidro-green-dark focus:ring-2 focus:ring-hidro-green-primary'
                        : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50 hover:border-gray-400 focus:ring-2 focus:ring-gray-300'
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
