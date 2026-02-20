import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, StyleSheet, ScrollView, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { Icon } from '../components/atoms/Icon';
import { Spinner } from '../components/atoms/Spinner';
import { WeatherSection } from '../components/dashboard/WeatherSection';
import { VariablesGrid } from '../components/dashboard/VariablesGrid';
import { ControllerSection } from '../components/dashboard/ControllerSection';
import { LastUpdateBanner } from '../components/dashboard/LastUpdateBanner';
import { DisconnectionBanner } from '../components/molecules/DisconnectionBanner';
import {
  semanticColors,
  spacing,
  colors,
  typography,
  systemStatusService,
  type SystemStatusResponse,
  type ReadingItem,
  type WeatherSummary,
} from '@hydroespinaca/shared';

export function DashboardScreen(): React.ReactElement {

  const [systemStatus, setSystemStatus] = useState<SystemStatusResponse | null>(null);
  const [previousReadings, setPreviousReadings] = useState<ReadingItem[]>([]);
  const [lastUpdateTimestamp, setLastUpdateTimestamp] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isPollingPaused, setIsPollingPaused] = useState(false);
  const [hasReadingsData, setHasReadingsData] = useState(true);
  const [shouldStartPolling, setShouldStartPolling] = useState(false);

  const [weather, setWeather] = useState<WeatherSummary | null>(null);
  const [weatherLoading, setWeatherLoading] = useState(false);
  const [weatherError, setWeatherError] = useState<string | null>(null);

  const fetchSystemStatus = useCallback(async (): Promise<string | null> => {
    try {
      setError(null);
      const data = await systemStatusService.getSystemStatus();

      // Verificar si el backend devolvió readings vacías
      const hasValidReadings = data.readings.readings &&
                               data.readings.readings.length > 0 &&
                               data.readings.timestamp;

      if (!hasValidReadings) {
        setHasReadingsData(false);

        // Pero aún así actualizar los otros datos que sí vienen
        setSystemStatus(data);

        // Actualizar clima desde la respuesta consolidada del BFF
        if (data.weather) {
          setWeather(data.weather);
          setWeatherError(null);
        }

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
      // Los errores 401 son manejados automáticamente por authFetch
      // que hace logout y redirige al login
      // Aquí solo manejamos otros tipos de errores
      setError(err.message || 'Error al cargar los datos del sistema');
      return null;
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [systemStatus]);

  // Función para reiniciar el polling
  const handleRetryPolling = useCallback(() => {
    // Resetear contadores
    consecutiveEmptyResponsesRef.current = 0;
    consecutiveUnchangedRef.current = 0;
    followUpStartTimeRef.current = null;
    setIsPollingPaused(false);
    setError(null);
    setHasReadingsData(true);

    // Forzar refetch inmediato y reactivar polling
    fetchSystemStatus().then(() => {
      setShouldStartPolling(true);
    });
  }, [fetchSystemStatus]);

  // Cargar datos iniciales
  useEffect(() => {
    fetchSystemStatus().then(() => {
      // Activar el polling después del fetch inicial
      setShouldStartPolling(true);
    });
  }, []);

  // Auto-refresh dinámico basado en timestamp + 2 minutos
  const lastTimestampRef = useRef<string | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const followUpStartTimeRef = useRef<number | null>(null);
  const consecutiveEmptyResponsesRef = useRef<number>(0);
  const consecutiveUnchangedRef = useRef<number>(0);

  useEffect(() => {
    // No iniciar hasta que se complete el fetch inicial
    if (!shouldStartPolling) return;

    const READING_INTERVAL_MS = 2 * 60 * 1000; // 2 minutos
    const BUFFER_MS = 20 * 1000; // +20 segundos de margen
    const FOLLOW_UP_INTERVAL_MS = 30 * 1000; // 30 segundos (base)
    const MAX_FOLLOW_UP_MS = 90 * 1000; // 90 segundos máximo
    const MAX_CONSECUTIVE_EMPTY = 3; // Máximo de respuestas vacías consecutivas
    const MAX_CONSECUTIVE_UNCHANGED = 3; // Máximo de timestamps sin cambios
    const MAX_BACKOFF_MS = 2 * 60 * 1000; // 2 minutos máximo de backoff

    const calculateNextFetchDelay = (readingTimestamp: string): number => {
      const readingTime = new Date(readingTimestamp).getTime();
      const nextReadingTime = readingTime + READING_INTERVAL_MS;
      const now = Date.now();
      const timeUntilNextReading = nextReadingTime - now;

      if (timeUntilNextReading <= 0) return 0;
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
        setError('Sistema desconectado: No se detectan lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      // Verificar si se alcanzó el límite de timestamps sin cambios
      if (consecutiveUnchangedRef.current >= MAX_CONSECUTIVE_UNCHANGED) {
        setError('No se detectan nuevas lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      // Calcular delay con backoff exponencial si estamos en modo follow-up
      let delay: number;
      if (isFollowUp) {
        delay = calculateBackoffDelay(consecutiveUnchangedRef.current);
      } else {
        delay = timestamp ? calculateNextFetchDelay(timestamp) : FOLLOW_UP_INTERVAL_MS;
      }

      timeoutRef.current = setTimeout(async () => {
        const oldTimestamp = lastTimestampRef.current;
        const newTimestamp = await fetchSystemStatus();

        // Si no hay nuevo timestamp, incrementar contador de respuestas vacías
        if (!newTimestamp) {
          consecutiveEmptyResponsesRef.current++;
          scheduleNext(oldTimestamp || null, true);
          return;
        }

        // Resetear contador de respuestas vacías
        consecutiveEmptyResponsesRef.current = 0;

        const timestampsChanged = oldTimestamp !== newTimestamp;

        if (timestampsChanged) {
          // Timestamp cambió, resetear contadores
          consecutiveUnchangedRef.current = 0;
          followUpStartTimeRef.current = null;
          lastTimestampRef.current = newTimestamp;
          scheduleNext(newTimestamp, false);
        } else {
          // Timestamp no cambió, incrementar contador
          consecutiveUnchangedRef.current++;

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
  }, [shouldStartPolling, lastUpdateTimestamp, fetchSystemStatus]);

  const isArtificialLightActive = (): boolean => {
    if (!systemStatus?.jobStatus.queue) return false;

    return systemStatus.jobStatus.queue.some(job =>
      job.commandId.toLowerCase().includes('luz') ||
      job.commandId.toLowerCase().includes('light') ||
      job.commandId.toLowerCase().includes('amplio-espectro')
    );
  };

  const handleRefresh = useCallback(() => {
    setIsRefreshing(true);
    // Si el polling está pausado, reiniciarlo
    if (isPollingPaused) {
      handleRetryPolling();
    } else {
      fetchSystemStatus();
    }
  }, [isPollingPaused, handleRetryPolling, fetchSystemStatus]);

  if (isLoading && !systemStatus) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Spinner size="lg" color={semanticColors.primary} />
          <Text variant="body" color={semanticColors.textSecondary} style={styles.loadingText}>
            Cargando datos del sistema...
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  if (error && !systemStatus) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.errorContainer}>
          <Icon name="alert-triangle" size={48} color={semanticColors.errorText} />
          <Text variant="h3" color={semanticColors.errorText} style={styles.errorTitle}>
            Error al cargar datos
          </Text>
          <Text variant="body" color={semanticColors.errorText} style={styles.errorMessage}>
            {error}
          </Text>
          <Text variant="caption" color={semanticColors.errorText} style={styles.errorRetry}>
            El sistema reintentará automáticamente en breve...
          </Text>
          <Button onPress={handleRefresh} variant="primary" style={styles.retryButton}>
            Reintentar ahora
          </Button>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={isRefreshing} onRefresh={handleRefresh} />
        }
      >
        {/* Header */}
        <View style={styles.header}>
          <Text variant="h1" color={semanticColors.primary} style={styles.title}>
            Información General Del Cultivo
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
            Monitoreo de las variables y estado actual del sistema hidropónico
          </Text>
        </View>

        {/* Disconnection Banner */}
        {isPollingPaused && error && hasReadingsData && (
          <View style={styles.bannerContainer}>
            <DisconnectionBanner
              message={error}
              onRetry={handleRetryPolling}
            />
          </View>
        )}

        {/* Weather */}
        <WeatherSection
          weather={weather}
          isLoading={weatherLoading}
          error={weatherError}
        />

        {/* Last Update */}
        {lastUpdateTimestamp && hasReadingsData && (
          <LastUpdateBanner timestamp={lastUpdateTimestamp} />
        )}

        {/* Variables */}
        {hasReadingsData && systemStatus?.readings.readings && systemStatus.readings.readings.length > 0 && (
          <VariablesGrid
            readings={systemStatus.readings.readings}
            previousReadings={previousReadings}
            isArtificialLightActive={isArtificialLightActive()}
          />
        )}

        {/* Controller */}
        {systemStatus && lastUpdateTimestamp && hasReadingsData && (
          <ControllerSection
            lastUpdateTimestamp={lastUpdateTimestamp}
            jobStatus={systemStatus.jobStatus}
            stats={systemStatus.stats}
            internalRoutines={systemStatus.internalRoutines}
          />
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    paddingBottom: spacing.xl,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
  loadingText: {
    marginTop: spacing.md,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
  errorTitle: {
    marginTop: spacing.md,
    fontWeight: typography.fontWeight.bold,
  },
  errorMessage: {
    marginTop: spacing.sm,
    textAlign: 'center',
  },
  errorRetry: {
    marginTop: spacing.sm,
    textAlign: 'center',
  },
  retryButton: {
    marginTop: spacing.lg,
  },
  header: {
    padding: spacing.lg,
    backgroundColor: colors.hidro[50],
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.sm,
  },
  subtitle: {
    lineHeight: 20,
  },
  bannerContainer: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
});
