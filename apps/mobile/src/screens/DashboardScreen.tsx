import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, StyleSheet, ScrollView, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { Icon } from '../components/atoms/Icon';
import { Spinner } from '../components/atoms/Spinner';
import { VariableCard, WeatherCard, ControllerStatus } from '../components/dashboard';
import type { TrendDirection, VariableStatus } from '../components/dashboard';
import { useAuth } from '../context/AuthProvider';
import {
  semanticColors,
  spacing,
  colors,
  borderRadius,
  systemStatusService,
  formatNumericValue,
  calculateVariableStatus as sharedCalculateVariableStatus,
  calculateTrend as sharedCalculateTrend,
  getAlertConfig as sharedGetAlertConfig,
  type SystemStatusResponse,
  type ReadingItem,
  type WeatherSummary,
  type IconType,
  type VariableStatus as SharedVariableStatus,
  type TrendDirection as SharedTrendDirection,
} from '@hydroespinaca/shared';

type RootStackParamList = {
  Login: undefined;
  Dashboard: undefined;
};

type DashboardScreenNavigationProp = NativeStackNavigationProp<RootStackParamList, 'Dashboard'>;

const getIconType = (name: string): IconType => {
  const lowerName = name.toLowerCase();

  if (lowerName.includes('temperatura') && lowerName.includes('agua')) return 'water';
  if (lowerName.includes('temperatura')) return 'temperature';
  if (lowerName.includes('humedad')) return 'humidity';
  if (lowerName.includes('luz') || lowerName.includes('luminosidad')) return 'sun';
  if (lowerName.includes('conductividad') || lowerName.includes('ec')) return 'electric';
  if (lowerName.includes('nivel')) return 'ruler';
  if (lowerName.includes('ph')) return 'ph';

  return 'temperature';
};

export function DashboardScreen(): React.ReactElement {
  const navigation = useNavigation<DashboardScreenNavigationProp>();
  const { logout } = useAuth();

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

  const formatColombiaDateTime = (isoString: string): string => {
    const date = new Date(isoString);
    const options: Intl.DateTimeFormatOptions = {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      timeZone: 'America/Bogota',
    };
    return new Intl.DateTimeFormat('es-CO', options).format(date);
  };

  const isArtificialLightActive = (): boolean => {
    if (!systemStatus?.jobStatus.queue) return false;

    return systemStatus.jobStatus.queue.some(job =>
      job.commandId.toLowerCase().includes('luz') ||
      job.commandId.toLowerCase().includes('light') ||
      job.commandId.toLowerCase().includes('amplio-espectro')
    );
  };

  const getPreviousValue = (variableName: string): number | null => {
    const previous = previousReadings.find(
      r => r.name.toLowerCase() === variableName.toLowerCase()
    );
    return previous?.value || null;
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

  const handleLogout = async () => {
    await logout();
    navigation.replace('Login');
  };

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
          <View style={styles.headerTop}>
            <Text variant="h1" color={semanticColors.primary} style={styles.title}>
              Información General Del Cultivo
            </Text>
            <Button
              onPress={handleLogout}
              variant="outline"
              size="sm"
              leftIcon={<Icon name="log-out" size={16} color={semanticColors.primary} />}
            >
              Salir
            </Button>
          </View>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
            Monitoreo de las variables y estado actual del sistema hidropónico
          </Text>
        </View>

        {/* Banner de advertencia si el polling está pausado */}
        {isPollingPaused && error && hasReadingsData && (
          <View style={styles.warningBanner}>
            <View style={styles.warningContent}>
              <Icon name="alert-triangle" size={20} color="#ea580c" />
              <View style={styles.warningTextContainer}>
                <Text variant="label" color="#9a3412" style={styles.warningTitle}>
                  Actualizaciones automáticas pausadas
                </Text>
                <Text variant="caption" color="#c2410c" style={styles.warningMessage}>
                  {error}
                </Text>
                <Button
                  onPress={handleRetryPolling}
                  variant="primary"
                  size="sm"
                  style={styles.warningButton}
                  leftIcon={<Icon name="refresh" size={16} color="#FFFFFF" />}
                >
                  Reintentar ahora
                </Button>
              </View>
            </View>
          </View>
        )}

        {/* Weather Section */}
        <View style={styles.section}>
          <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
            Condiciones Climáticas
          </Text>
          <WeatherCard
            weather={weather}
            isLoading={weatherLoading}
            error={weatherError}
          />
        </View>

        {/* Last Update Info - Solo mostrar si hay datos válidos */}
        {lastUpdateTimestamp && hasReadingsData && (
          <View style={styles.updateInfo}>
            <Text style={styles.updateEmoji}>🔄</Text>
            <View style={styles.updateTextContainer}>
              <Text variant="caption" color="#15803d" style={styles.updateText}>
                Última actualización: {formatColombiaDateTime(lastUpdateTimestamp)}
              </Text>
              <Text variant="caption" color="#15803d" style={styles.updateSubtext}>
                Actualiza cada 2 minutos
              </Text>
            </View>
          </View>
        )}

        {/* Variables Section - Solo mostrar si hay datos válidos */}
        {hasReadingsData && systemStatus?.readings.readings && systemStatus.readings.readings.length > 0 && (
          <View style={styles.section}>
            <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
              Variables del Sistema
            </Text>
            <View style={styles.variablesGrid}>
              {systemStatus.readings.readings.map((reading) => {
                const alertConfig = sharedGetAlertConfig(reading.name);
                const status = sharedCalculateVariableStatus(reading, alertConfig);
                const previousValue = getPreviousValue(reading.name);
                const trend = sharedCalculateTrend(reading.value, previousValue);
                const lightActive = isArtificialLightActive();

                return (
                  <View key={reading.name} style={styles.variableCardWrapper}>
                    <VariableCard
                      title={reading.name}
                      value={`${formatNumericValue(reading.value)} ${reading.unit}`}
                      optimal={`Óptima: ${formatNumericValue(reading.optimalMin)} – ${formatNumericValue(reading.optimalMax)} ${reading.unit}`}
                      iconType={getIconType(reading.name)}
                      status={status}
                      trend={trend}
                      artificialLightActive={lightActive}
                    />
                  </View>
                );
              })}
            </View>
          </View>
        )}

        {/* Controller Status Section - Solo mostrar si hay datos válidos */}
        {systemStatus && lastUpdateTimestamp && hasReadingsData && (
          <View style={styles.section}>
            <ControllerStatus
              timeSinceUpdate={Math.floor(
                (new Date().getTime() - new Date(lastUpdateTimestamp).getTime()) / 1000
              )}
              jobStatus={systemStatus.jobStatus}
              stats={systemStatus.stats}
              internalRoutines={systemStatus.internalRoutines}
            />
          </View>
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
    fontWeight: 'bold',
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
  headerTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  title: {
    flex: 1,
    fontWeight: 'bold',
  },
  subtitle: {
    lineHeight: 20,
  },
  section: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontWeight: 'bold',
    marginBottom: spacing.md,
  },
  updateInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginHorizontal: spacing.lg,
    marginBottom: spacing.lg,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: '#dcfce7',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#86efac',
  },
  updateEmoji: {
    fontSize: 20,
    flexShrink: 0,
  },
  updateTextContainer: {
    flex: 1,
    flexShrink: 1,
  },
  updateText: {
    fontWeight: '500',
    fontSize: 12,
    lineHeight: 16,
    flexWrap: 'wrap',
  },
  updateSubtext: {
    marginTop: 2,
    fontSize: 11,
    lineHeight: 14,
  },
  variablesGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  variableCardWrapper: {
    width: '48%',
  },
  warningBanner: {
    marginHorizontal: spacing.lg,
    marginBottom: spacing.lg,
    backgroundColor: '#fff7ed',
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: '#fed7aa',
    padding: spacing.md,
  },
  warningContent: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
  },
  warningTextContainer: {
    flex: 1,
  },
  warningTitle: {
    fontWeight: '600',
    marginBottom: spacing.xs,
  },
  warningMessage: {
    lineHeight: 16,
    marginBottom: spacing.md,
  },
  warningButton: {
    alignSelf: 'flex-start',
  },
});
