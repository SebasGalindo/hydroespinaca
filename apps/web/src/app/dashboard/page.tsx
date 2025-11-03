'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import VariableCard from '@/components/dashboard/VariableCard';
import ControllerStatus from '@/components/dashboard/ControllerStatus';
import WeatherCard from '@/components/dashboard/WeatherCard';
import PageLayout from '@/components/layout/PageLayout';
import { AlertTriangleIcon } from '@/components/ui/icons/Icons';
import { systemStatusService, formatNumericValue } from '@hydroespinaca/shared';
import type { SystemStatusResponse, ReadingItem, WeatherSummary } from '@hydroespinaca/shared';
import { useRouter } from 'next/navigation';
import { IconType } from '@hydroespinaca/shared/types/common';
import { useAuthStore } from '@hydroespinaca/shared';
import {
  calculateVariableStatus,
  calculateTrend,
  requiresArtificialLight,
  getAlertConfig
} from '@/utils/variableAlerts';

export default function DashboardPage() {
  const router = useRouter();
  const routerRef = useRef(router);
  const { logout } = useAuthStore();
  const [systemStatus, setSystemStatus] = useState<SystemStatusResponse | null>(null);
  const [previousReadings, setPreviousReadings] = useState<ReadingItem[]>([]);
  const [lastUpdateTimestamp, setLastUpdateTimestamp] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isPollingPaused, setIsPollingPaused] = useState(false);
  const [hasReadingsData, setHasReadingsData] = useState(true);
  const [shouldStartPolling, setShouldStartPolling] = useState(false);

  // Estado para el clima
  const [weather, setWeather] = useState<WeatherSummary | null>(null);
  const [weatherLoading, setWeatherLoading] = useState(false);
  const [weatherError, setWeatherError] = useState<string | null>(null);

  // Mantener routerRef actualizado
  useEffect(() => {
    routerRef.current = router;
  }, [router]);

  const fetchSystemStatus = useCallback(async (): Promise<string | null> => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await systemStatusService.getSystemStatus();

      // Verificar si el backend devolvió readings vacías
      const hasValidReadings = data.readings.readings &&
                               data.readings.readings.length > 0 &&
                               data.readings.timestamp;

      if (!hasValidReadings) {
        console.warn('⚠️ Backend devolvió readings vacías o sin timestamp');
        setHasReadingsData(false);

        // Pero aún así actualizar los otros datos que sí vienen (jobStatus, stats, internalRoutines, weather)
        setSystemStatus(data);

        // Actualizar clima desde la respuesta consolidada del BFF
        if (data.weather) {
          setWeather(data.weather);
          setWeatherError(null);
        }

        // Retornar null para indicar que no hay lecturas nuevas
        return null;
      }

      // Si llegamos aquí, tenemos datos válidos de readings
      setHasReadingsData(true);

      // Almacenar lecturas anteriores para calcular tendencias
      if (systemStatus?.readings.readings) {
        setPreviousReadings(systemStatus.readings.readings);
      }

      setSystemStatus(data);
      setLastUpdateTimestamp(data.readings.timestamp);

      // Actualizar clima desde la respuesta consolidada del BFF
      if (data.weather) {
        setWeather(data.weather);
        setWeatherError(null);
      }

      return data.readings.timestamp;
    } catch (err: any) {
      console.error('Error fetching system status:', err);
      setError(err.message || 'Error al cargar los datos del sistema');

      // Si es error 401, cerrar sesión y redirigir a login
      if (err.response?.status === 401 || err.message?.includes('Unauthorized')) {
        await logout();
        routerRef.current.push('/login');
      }
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [systemStatus, logout]);

  // Cargar datos iniciales (incluye clima desde el BFF)
  useEffect(() => {
    fetchSystemStatus().then(() => {
      // Activar el polling después del fetch inicial
      setShouldStartPolling(true);
    });
  }, []);

  // Función para reiniciar el polling
  const handleRetryPolling = useCallback(() => {
    console.log('🔄 Reiniciando polling...');
    // Resetear contadores
    consecutiveEmptyResponsesRef.current = 0;
    consecutiveUnchangedRef.current = 0;
    followUpStartTimeRef.current = null;
    setIsPollingPaused(false);
    setError(null);
    setHasReadingsData(true); // Resetear estado de readings

    // Forzar refetch inmediato y reactivar polling
    fetchSystemStatus().then(() => {
      setShouldStartPolling(true);
    });
  }, [fetchSystemStatus]);

  // Auto-refresh dinámico basado en timestamp + 2 minutos
  // El clima se actualiza automáticamente con cada fetch del BFF

  const lastTimestampRef = useRef<string | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const followUpStartTimeRef = useRef<number | null>(null);
  const consecutiveEmptyResponsesRef = useRef<number>(0);
  const consecutiveUnchangedRef = useRef<number>(0);

  useEffect(() => {
    // No iniciar hasta que se complete el fetch inicial
    if (!shouldStartPolling) return;

    const READING_INTERVAL_MS = 2 * 60 * 1000; // 2 minutos desde la LECTURA
    const BUFFER_MS = 20 * 1000; // +20 segundos de margen
    const FOLLOW_UP_INTERVAL_MS = 30 * 1000; // 30 segundos (base) - más espaciado
    const MAX_FOLLOW_UP_MS = 90 * 1000; // 90 segundos máximo en modo seguimiento (más restrictivo)
    const MAX_CONSECUTIVE_EMPTY = 3; // Máximo de respuestas vacías consecutivas (más restrictivo)
    const MAX_CONSECUTIVE_UNCHANGED = 3; // Máximo de timestamps sin cambios (más restrictivo)
    const MAX_BACKOFF_MS = 2 * 60 * 1000; // 2 minutos máximo de backoff

    const calculateNextFetchDelay = (readingTimestamp: string): number => {
      const readingTime = new Date(readingTimestamp).getTime();
      const nextReadingTime = readingTime + READING_INTERVAL_MS;
      const now = Date.now();
      const timeUntilNextReading = nextReadingTime - now;

      if (timeUntilNextReading <= 0) {
        return 0;
      }

      return timeUntilNextReading + BUFFER_MS;
    };

    // Backoff exponencial con límite máximo
    const calculateBackoffDelay = (retryCount: number): number => {
      const baseDelay = FOLLOW_UP_INTERVAL_MS;
      const exponentialDelay = baseDelay * Math.pow(2, Math.min(retryCount, 5));
      return Math.min(exponentialDelay, MAX_BACKOFF_MS);
    };

    const scheduleNext = (timestamp: string | null, isFollowUp: boolean = false) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      // Verificar si se alcanzó el límite de respuestas vacías
      if (consecutiveEmptyResponsesRef.current >= MAX_CONSECUTIVE_EMPTY) {
        console.warn(`⚠️ Se alcanzó el límite de ${MAX_CONSECUTIVE_EMPTY} respuestas vacías consecutivas. Deteniendo polling.`);
        setError('Sistema desconectado: No se detectan lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      // Verificar si se alcanzó el límite de timestamps sin cambios
      if (consecutiveUnchangedRef.current >= MAX_CONSECUTIVE_UNCHANGED) {
        console.warn(`⚠️ Se alcanzó el límite de ${MAX_CONSECUTIVE_UNCHANGED} intentos sin datos nuevos. Deteniendo polling.`);
        setError('No se detectan nuevas lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      // Calcular delay con backoff exponencial si estamos en modo follow-up
      let delay: number;
      if (isFollowUp) {
        delay = calculateBackoffDelay(consecutiveUnchangedRef.current);
        console.log(`📡 Reintento ${consecutiveUnchangedRef.current + 1}/${MAX_CONSECUTIVE_UNCHANGED} - Próximo intento en ${Math.round(delay / 1000)}s`);
      } else {
        // Si hay timestamp, calcular próximo fetch basado en él
        // Si no hay timestamp, usar intervalo de seguimiento
        delay = timestamp ? calculateNextFetchDelay(timestamp) : FOLLOW_UP_INTERVAL_MS;
      }

      timeoutRef.current = setTimeout(async () => {
        const oldTimestamp = lastTimestampRef.current;
        const newTimestamp = await fetchSystemStatus();

        // Caso 1: Error en la petición (newTimestamp es null - no hay readings)
        if (!newTimestamp) {
          consecutiveEmptyResponsesRef.current++;
          consecutiveUnchangedRef.current++;
          console.warn(`⚠️ Respuesta sin readings ${consecutiveEmptyResponsesRef.current}/${MAX_CONSECUTIVE_EMPTY}`);

          if (consecutiveEmptyResponsesRef.current < MAX_CONSECUTIVE_EMPTY) {
            // Continuar intentando con backoff
            scheduleNext(oldTimestamp || lastUpdateTimestamp, true);
          }
          return;
        }

        // Caso 2: Se recibieron datos - resetear contador de respuestas vacías
        consecutiveEmptyResponsesRef.current = 0;

        const timestampsChanged = oldTimestamp !== newTimestamp;

        // Caso 3: Timestamp cambió - hay nuevos datos
        if (timestampsChanged) {
          consecutiveUnchangedRef.current = 0;
          followUpStartTimeRef.current = null;
          lastTimestampRef.current = newTimestamp;
          console.log('✅ Nuevos datos recibidos, timestamp actualizado');
          scheduleNext(newTimestamp, false);
        }
        // Caso 4: Timestamp no cambió - no hay nuevos datos
        else {
          consecutiveUnchangedRef.current++;

          if (!followUpStartTimeRef.current) {
            followUpStartTimeRef.current = Date.now();
          }

          const elapsed = Date.now() - followUpStartTimeRef.current;

          // Modo seguimiento por 2 minutos
          if (elapsed < MAX_FOLLOW_UP_MS && consecutiveUnchangedRef.current < MAX_CONSECUTIVE_UNCHANGED) {
            scheduleNext(newTimestamp, true);
          }
          // Después de 2 minutos, volver al ciclo normal
          else if (consecutiveUnchangedRef.current < MAX_CONSECUTIVE_UNCHANGED) {
            followUpStartTimeRef.current = null;
            scheduleNext(newTimestamp, false);
          }
        }
      }, delay);
    };

    // Iniciar polling con el timestamp actual o null si no hay
    const currentTimestamp = lastUpdateTimestamp || lastTimestampRef.current;
    lastTimestampRef.current = currentTimestamp;
    scheduleNext(currentTimestamp, false);

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [shouldStartPolling, lastUpdateTimestamp, fetchSystemStatus]);




  const formatColombiaDateTime = (isoString: string): string => {
    // Convertir ISO string a fecha GMT-5 (Colombia)
    const date = new Date(isoString);

    // Opciones de formato para Colombia
    const options: Intl.DateTimeFormatOptions = {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      timeZone: 'America/Bogota'
    };

    const formatter = new Intl.DateTimeFormat('es-CO', options);
    return formatter.format(date);
  };

  /**
   * Verifica si la luz artificial (amplio espectro) está activa
   */
  const isArtificialLightActive = (): boolean => {
    if (!systemStatus?.jobStatus.queue) return false;

    // Buscar comandos relacionados con luz en la cola
    return systemStatus.jobStatus.queue.some(job =>
      job.commandId.toLowerCase().includes('luz') ||
      job.commandId.toLowerCase().includes('light') ||
      job.commandId.toLowerCase().includes('amplio-espectro')
    );
  };

  /**
   * Obtiene el valor anterior de una variable para calcular tendencia
   */
  const getPreviousValue = (variableName: string): number | null => {
    const previous = previousReadings.find(
      r => r.name.toLowerCase() === variableName.toLowerCase()
    );
    return previous?.value || null;
  };



  // Mapeo de tipos de iconos según el nombre de la variable
  const getIconType = (name: string): IconType => {
    const lowerName = name.toLowerCase();

    if (lowerName.includes('temperatura') && lowerName.includes('agua')) return 'water';
    if (lowerName.includes('temperatura')) return 'temperature';
    if (lowerName.includes('humedad')) return 'humidity';
    if (lowerName.includes('luz') || lowerName.includes('luminosidad')) return 'sun';
    if (lowerName.includes('conductividad') || lowerName.includes('ec')) return 'electric';
    if (lowerName.includes('nivel')) return 'ruler';
    if (lowerName.includes('ph')) return 'ph';

    return 'temperature'; // default seguro
  };

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
        <WeatherCard
          weather={weather}
          isLoading={weatherLoading}
          error={weatherError}
        />
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
              const needsLightCheck = requiresArtificialLight(reading.name);
              const lightActive = isArtificialLightActive();
              const showLightAlert = needsLightCheck && reading.value < reading.optimalMin;

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
                  showArtificialLightAlert={showLightAlert}
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