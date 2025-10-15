'use client';

import React, { useState, useEffect, useCallback } from 'react';
import VariableCard from '@/components/dashboard/VariableCard';
import ControllerStatus from '@/components/dashboard/ControllerStatus';
import PageLayout from '@/components/layout/PageLayout';
import { AlertTriangleIcon } from '@/components/ui/icons/Icons';
import { systemStatusService } from '@hydroespinaca/shared';
import type { SystemStatusResponse, ReadingItem } from '@hydroespinaca/shared';
import { useRouter } from 'next/navigation';
import { IconType } from '@hydroespinaca/shared/types/common';

export default function DashboardPage() {
  const router = useRouter();
  const [systemStatus, setSystemStatus] = useState<SystemStatusResponse | null>(null);
  const [lastUpdateTimestamp, setLastUpdateTimestamp] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSystemStatus = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await systemStatusService.getSystemStatus();
      setSystemStatus(data);
      setLastUpdateTimestamp(data.timestamp);
    } catch (err: any) {
      console.error('Error fetching system status:', err);
      setError(err.message || 'Error al cargar los datos del sistema');

      // Si es error 401, redirigir a login
      if (err.message?.includes('Unauthorized')) {
        router.push('/login');
      }
    } finally {
      setIsLoading(false);
    }
  }, [router]);

  // Auto-refresh dinámico basado en timestamp + 2 minutos
  useEffect(() => {
    // Cargar datos iniciales
    fetchSystemStatus();
  }, [fetchSystemStatus]);

  useEffect(() => {
    if (!lastUpdateTimestamp) return;

    // Calcular cuándo debe ocurrir la siguiente actualización (timestamp + 2 minutos)
    const lastUpdate = new Date(lastUpdateTimestamp);
    const nextUpdate = new Date(lastUpdate.getTime() + 2 * 60 * 1000); // +2 minutos
    const now = new Date();
    const msUntilNextUpdate = nextUpdate.getTime() - now.getTime();

    // Si el tiempo ya pasó, actualizar inmediatamente
    if (msUntilNextUpdate <= 0) {
      fetchSystemStatus();
      return;
    }

    // Programar la siguiente actualización
    const timeoutId = setTimeout(() => {
      fetchSystemStatus();
    }, msUntilNextUpdate);

    return () => clearTimeout(timeoutId);
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

  const formatNumericValue = (value: number): string => {
    // Formatear a 2 decimales y eliminar ceros innecesarios
    const formatted = parseFloat(value.toFixed(2));
    return formatted.toString();
  };

  const getReadingByName = (name: string): ReadingItem | undefined => {
    return systemStatus?.readings.readings.find(
      r => r.name.toLowerCase().includes(name.toLowerCase())
    );
  };

  const getStatusForReading = (reading?: ReadingItem): 'optimal' | 'warning' | 'critical' => {
    if (
      !reading ||
      reading.value == null ||
      reading.optimalMin == null ||
      reading.optimalMax == null
    ) {
      return 'critical';
    }

    const { value, optimalMin, optimalMax } = reading;
    const range = optimalMax - optimalMin;
    const tolerance = range * 0.1;

    if (value >= optimalMin && value <= optimalMax) return 'optimal';
    if (value >= optimalMin - tolerance && value <= optimalMax + tolerance) return 'warning';
    return 'critical';
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
          {systemStatus?.readings.readings.map((reading) => (
            <VariableCard
              key={reading.name}
              title={reading.name}
              value={`${formatNumericValue(reading.value)} ${reading.unit}`}
              optimal={`Óptima: ${formatNumericValue(reading.optimalMin)} – ${formatNumericValue(reading.optimalMax)} ${reading.unit}`}
              iconType={getIconType(reading.name)}
              status={getStatusForReading(reading)}
            />
          ))}
        </div>
      </section>

      {/* Estado actual del controlador */}
      {systemStatus && lastUpdateTimestamp && (
        <ControllerStatus
          timeSinceUpdate={Math.floor((new Date().getTime() - new Date(lastUpdateTimestamp).getTime()) / 1000)}
          jobStatus={systemStatus.jobStatus}
          stats={systemStatus.stats}
          internalRoutines={systemStatus.internalRoutines}
        />
      )}
    </PageLayout>
  );
}