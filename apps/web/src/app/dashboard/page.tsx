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
    fetchSystemStatus();
  }, []);

  // Auto-refresh dinámico basado en timestamp + 2 minutos
  // El clima se actualiza automáticamente con cada fetch del BFF

  const lastTimestampRef = useRef<string | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const followUpStartTimeRef = useRef<number | null>(null);

  useEffect(() => {
    if (!lastUpdateTimestamp) return;

    const READING_INTERVAL_MS = 2 * 60 * 1000; // 2 minutos desde la LECTURA
    const BUFFER_MS = 20 * 1000; // +20 segundos de margen
    const FOLLOW_UP_INTERVAL_MS = 20 * 1000; // 20 segundos
    const MAX_FOLLOW_UP_MS = 2 * 60 * 1000; // 2 minutos máximo en modo seguimiento

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

    const scheduleNext = (timestamp: string, isFollowUp: boolean = false) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      const delay = isFollowUp ? FOLLOW_UP_INTERVAL_MS : calculateNextFetchDelay(timestamp);

      timeoutRef.current = setTimeout(async () => {
        const oldTimestamp = lastTimestampRef.current;
        const newTimestamp = await fetchSystemStatus();

        if (!newTimestamp) {
          scheduleNext(oldTimestamp || lastUpdateTimestamp, true);
          return;
        }

        const timestampsChanged = oldTimestamp !== newTimestamp;

        if (timestampsChanged) {
          followUpStartTimeRef.current = null;
          lastTimestampRef.current = newTimestamp;
          scheduleNext(newTimestamp, false);
        } else {
          if (!followUpStartTimeRef.current) {
            followUpStartTimeRef.current = Date.now();
          }

          const elapsed = Date.now() - followUpStartTimeRef.current;

          if (elapsed < MAX_FOLLOW_UP_MS) {
            scheduleNext(newTimestamp, true);
          } else {
            followUpStartTimeRef.current = null;
            scheduleNext(newTimestamp, false);
          }
        }
      }, delay);
    };

    lastTimestampRef.current = lastUpdateTimestamp;
    scheduleNext(lastUpdateTimestamp, false);

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [lastUpdateTimestamp, fetchSystemStatus]);




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

  if (error) {
    return (
      <PageLayout
        title="Información General Del Cultivo"
        subtitle="Monitoreo de las variables y estado actual del sistema hidropónico"
        maxWidth="xl"
      >
        <div className="mb-6 flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
          <AlertTriangleIcon size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-medium text-red-800">Error al cargar datos</p>
            <p className="text-xs text-red-700 mt-1">{error}</p>
            <p className="text-xs text-red-600 mt-2">El sistema reintentará automáticamente en breve...</p>
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
      <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-green-700 text-2xl">🔄</span>
          <span className="text-sm font-medium text-green-800 font-inter">
            Última actualización: {lastUpdateTimestamp ? formatColombiaDateTime(lastUpdateTimestamp) : 'Cargando...'}
          </span>
        </div>
        <p className="text-xs text-green-700 ml-8">
          Las lecturas se actualizan automáticamente cada 2 minutos desde la última lectura del backend
        </p>
      </div>

      {/* Variables del sistema */}
      <section className="mb-8" aria-labelledby="system-variables-heading">
        <h2 id="system-variables-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Variables del Sistema
        </h2>
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
      </section>

      {/* Estado actual del controlador */}
      <section className="mb-8" aria-labelledby="controller-status-heading">
        {systemStatus && lastUpdateTimestamp && (
          <ControllerStatus
            timeSinceUpdate={Math.floor((new Date().getTime() - new Date(lastUpdateTimestamp).getTime()) / 1000)}
            jobStatus={systemStatus.jobStatus}
            stats={systemStatus.stats}
            internalRoutines={systemStatus.internalRoutines}
          />
        )}
      </section>
    </PageLayout>
  );
}