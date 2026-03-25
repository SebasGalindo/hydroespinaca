'use client';

import React from 'react';
import VariableCard from '@/components/dashboard/VariableCard';
import ControllerStatus from '@/components/dashboard/ControllerStatus';
import WeatherCard from '@/components/dashboard/WeatherCard';
import WeatherAlertWidget from '@/components/dashboard/WeatherAlertWidget';
import PageLayout from '@/components/layout/PageLayout';
import { AlertTriangleIcon } from '@/components/ui/icons/Icons';
import {
  formatNumericValue,
  calculateVariableStatus,
  calculateTrend,
  getAlertConfig,
} from '@hydroespinaca/shared';
import { useSystemDashboard, formatColombiaDateTime, getIconType } from '@/hooks/useSystemDashboard';

export default function DashboardPage() {
  const {
    systemStatus,
    lastUpdateTimestamp,
    isLoading,
    error,
    isPollingPaused,
    hasReadingsData,
    weather,
    weatherLoading,
    weatherError,
    handleRetryPolling,
    isArtificialLightActive,
    getPreviousValue,
  } = useSystemDashboard();

  if (isLoading && !systemStatus) {
    return (
      <PageLayout
        title="Información General Del Cultivo"
        subtitle="Monitoreo de las variables y estado actual del sistema hidropónico"
        maxWidth="xl"
      >
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-green-600 mx-auto mb-4"></div>
            <p className="text-gray-600">Cargando datos del sistema...</p>
          </div>
        </div>
      </PageLayout>
    );
  }

  if (error && !systemStatus) {
    return (
      <PageLayout
        title="Información General Del Cultivo"
        subtitle="Monitoreo de las variables y estado actual del sistema hidropónico"
        maxWidth="xl"
      >
        <div className="mb-6 flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
          <AlertTriangleIcon size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium text-red-800">Error al cargar datos</p>
            <p className="text-xs text-red-700 mt-1">{error}</p>
            {isPollingPaused ? (
              <button
                onClick={handleRetryPolling}
                className="mt-3 px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 transition-colors"
              >
                🔄 Reintentar ahora
              </button>
            ) : (
              <p className="text-xs text-red-600 mt-2">El sistema reintentará automáticamente en breve...</p>
            )}
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout
      title="Información General Del Cultivo"
      subtitle="Monitoreo de las variables y estado actual del sistema hidropónico"
      maxWidth="xl"
    >
      {/* Banner de advertencia si el polling está pausado y tenemos datos (no se muestra si no hay readings) */}
      {isPollingPaused && error && hasReadingsData && (
        <div className="mb-6 flex items-start gap-3 p-4 bg-orange-50 border border-orange-200 rounded-lg">
          <AlertTriangleIcon size={20} className="text-orange-600 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium text-orange-800">Actualizaciones automáticas pausadas</p>
            <p className="text-xs text-orange-700 mt-1">{error}</p>
            <button
              onClick={handleRetryPolling}
              className="mt-3 px-4 py-2 bg-orange-600 text-white text-sm font-medium rounded-md hover:bg-orange-700 transition-colors"
            >
              🔄 Reintentar ahora
            </button>
          </div>
        </div>
      )}

      {/* Información Meteorológica */}
      <section className="mb-8" aria-labelledby="weather-heading">
        <h2 id="weather-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Condiciones Climáticas
        </h2>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <WeatherCard
              weather={weather}
              isLoading={weatherLoading}
              error={weatherError}
            />
          </div>
          <div className="lg:col-span-1">
            <WeatherAlertWidget />
          </div>
        </div>
      </section>

      {/* Indicador global de última actualización */}
      <div className="mb-6 p-3 bg-green-50 border border-green-200 rounded-lg">
        <div className="flex items-center gap-2">
          <span className="text-green-700 text-xl flex-shrink-0">🔄</span>
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-green-800 font-inter">
              Última actualización: {lastUpdateTimestamp ? formatColombiaDateTime(lastUpdateTimestamp) : 'Cargando...'}
            </p>
            <p className="text-xs text-green-700 mt-0.5">
              Actualiza cada 2 minutos
            </p>
          </div>
        </div>
      </div>

      {/* Variables del sistema */}
      <section className="mb-8" aria-labelledby="system-variables-heading">
        <h2 id="system-variables-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Variables del Sistema
        </h2>

        {/* Mensaje cuando no hay datos de lecturas */}
        {!hasReadingsData ? (
          <div className="bg-red-50 border-2 border-red-200 rounded-lg p-8 text-center">
            <div className="flex flex-col items-center gap-4">
              <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center">
                <AlertTriangleIcon size={32} className="text-red-600" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-red-800 mb-2">
                  Sistema Desconectado
                </h3>
                <p className="text-sm text-red-700 mb-1">
                  No se han recibido lecturas de los sensores.
                </p>
                <p className="text-xs text-red-600">
                  Esto puede deberse a que el ESP32 está desconectado o no está enviando datos.
                </p>
              </div>
              {isPollingPaused && (
                <button
                  onClick={handleRetryPolling}
                  className="mt-2 px-6 py-3 bg-red-600 text-white text-sm font-semibold rounded-md hover:bg-red-700 transition-colors shadow-md"
                >
                  🔄 Reintentar Conexión
                </button>
              )}
              {!isPollingPaused && (
                <div className="flex items-center gap-2 text-xs text-red-600">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-red-600"></div>
                  <span>Intentando reconectar...</span>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 items-stretch">
            {systemStatus?.readings.readings.map((reading) => {
              const alertConfig = getAlertConfig(reading.name);
              const status = calculateVariableStatus(reading, alertConfig);
              const previousValue = getPreviousValue(reading.name);
              const trend = calculateTrend(reading.value, previousValue);
              const lightActive = isArtificialLightActive;

              return (
                <VariableCard
                  key={reading.name}
                  title={reading.name}
                  value={`${formatNumericValue(reading.value)} ${reading.unit}`}
                  optimal={`Óptima: ${formatNumericValue(reading.optimalMin)} – ${formatNumericValue(reading.optimalMax)} ${reading.unit}`}
                  iconType={getIconType(reading.name)}
                  status={status}
                  trend={trend}
                  artificialLightActive={lightActive}
                />
              );
            })}
          </div>
        )}
      </section>

      {/* Estado actual del controlador */}
      <section className="mb-8" aria-labelledby="controller-status-heading">
        {systemStatus && (
          <ControllerStatus
            timeSinceUpdate={lastUpdateTimestamp ? Math.floor((new Date().getTime() - new Date(lastUpdateTimestamp).getTime()) / 1000) : 0}
            jobStatus={systemStatus.jobStatus}
            stats={systemStatus.stats}
            internalRoutines={systemStatus.internalRoutines}
          />
        )}
      </section>
    </PageLayout>
  );
}