'use client';

import React, { useState, useEffect } from 'react';
import { ActuatorActivity } from '@/lib/actuator-analytics-mapper';
import ActuatorTimelineChart from '../charts/ActuatorTimelineChart';
import ActuatorDurationChart from '../charts/ActuatorDurationChart';
import ActuatorProportionChart from '../charts/ActuatorProportionChart';

interface ActuatorsLevelProps {
  data: ActuatorActivity[];
  isLoading?: boolean;
  error?: string | null;
  isSingleDay?: boolean;
}

type ViewMode = 'timeline' | 'duration' | 'proportion' | 'all';

export default function ActuatorsLevel({ data, isLoading, error, isSingleDay = true }: ActuatorsLevelProps) {
  const [viewMode, setViewMode] = useState<ViewMode>('all');
  const [selectedActuators, setSelectedActuators] = useState<string[]>([]);

  // Update selected actuators when data changes - show all actuators by default
  useEffect(() => {
    if (data.length > 0) {
      setSelectedActuators(data.map((a) => a.actuatorId));
    }
  }, [data]);

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
                { value: 'timeline', label: 'Timeline', disabled: !isSingleDay },
                { value: 'duration', label: 'Duración' },
                { value: 'proportion', label: 'Proporción' },
              ].map((mode) => (
                <button
                  key={mode.value}
                  onClick={() => setViewMode(mode.value as ViewMode)}
                  disabled={mode.disabled}
                  className={`
                    px-4 py-2 text-sm rounded-md transition-all border
                    ${
                      mode.disabled
                        ? 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed'
                        : viewMode === mode.value
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
          {/* Timeline unavailable message for multi-day ranges */}
          {!isSingleDay && (viewMode === 'all' || viewMode === 'timeline') && (
            <div className="bg-amber-50 border-l-4 border-amber-400 p-5 rounded-lg">
              <div className="flex items-start">
                <svg
                  className="w-6 h-6 text-amber-600 mr-3 mt-0.5 flex-shrink-0"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <div className="flex-1">
                  <h4 className="text-base font-semibold text-amber-900 mb-2">
                    Timeline no disponible para este rango de fechas
                  </h4>
                  <p className="text-sm text-amber-800 mb-2">
                    El timeline muestra activaciones individuales, optimizado para monitoreo diario.
                    Para rangos mayores a un día, este gráfico no se genera.
                  </p>
                  <p className="text-sm text-amber-700">
                    💡 <strong>Sugerencia:</strong> Selecciona un rango de un solo día para visualizar el timeline detallado.
                    Los gráficos de duración total y proporción están disponibles para cualquier rango.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Timeline info message for single day */}
          {isSingleDay && (viewMode === 'all' || viewMode === 'timeline') && (
            <>
              <div className="bg-blue-50 border-l-4 border-blue-400 p-4 rounded-lg mb-4">
                <div className="flex items-start">
                  <svg
                    className="w-5 h-5 text-blue-600 mr-3 mt-0.5 flex-shrink-0"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <div>
                    <h4 className="text-sm font-semibold text-blue-900">
                      Vista de monitoreo
                    </h4>
                    <p className="text-sm text-blue-800 mt-1">
                      Este timeline muestra datos sin agrupar (raw) independientemente de la granularidad seleccionada,
                      permitiendo visualizar cada activación individual para efectos de monitoreo detallado.
                    </p>
                  </div>
                </div>
              </div>
              <ActuatorTimelineChart data={filteredData} />
            </>
          )}

          {/* Duration and proportion charts (always available) */}
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
