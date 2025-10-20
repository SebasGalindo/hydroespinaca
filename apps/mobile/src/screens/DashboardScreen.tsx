import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, StyleSheet, SafeAreaView, ScrollView, RefreshControl } from 'react-native';
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
  systemStatusService,
  formatNumericValue,
  type SystemStatusResponse,
  type ReadingItem,
  type WeatherSummary,
  type IconType,
} from '@hydroespinaca/shared';

type RootStackParamList = {
  Login: undefined;
  Dashboard: undefined;
};

type DashboardScreenNavigationProp = NativeStackNavigationProp<RootStackParamList, 'Dashboard'>;

// Configuración de alertas para variables (similar al web)
interface AlertConfig {
  warningThreshold: number;
  errorThreshold: number;
}

const getAlertConfig = (variableName: string): AlertConfig => {
  const lowerName = variableName.toLowerCase();

  if (lowerName.includes('ph')) {
    return { warningThreshold: 0.3, errorThreshold: 0.5 };
  }
  if (lowerName.includes('temperatura')) {
    return { warningThreshold: 2, errorThreshold: 4 };
  }
  if (lowerName.includes('humedad')) {
    return { warningThreshold: 10, errorThreshold: 15 };
  }
  if (lowerName.includes('luz') || lowerName.includes('luminosidad')) {
    return { warningThreshold: 5000, errorThreshold: 8000 };
  }
  if (lowerName.includes('conductividad') || lowerName.includes('ec')) {
    return { warningThreshold: 0.3, errorThreshold: 0.5 };
  }

  return { warningThreshold: 5, errorThreshold: 10 };
};

const calculateVariableStatus = (
  reading: ReadingItem,
  config: AlertConfig
): VariableStatus => {
  const { value, optimalMin, optimalMax } = reading;

  if (value >= optimalMin && value <= optimalMax) {
    return 'optimal';
  }

  const deviation = Math.min(
    Math.abs(value - optimalMin),
    Math.abs(value - optimalMax)
  );

  if (deviation >= config.errorThreshold) {
    return 'error';
  }

  if (deviation >= config.warningThreshold) {
    return 'warning';
  }

  return 'optimal';
};

const calculateTrend = (
  currentValue: number,
  previousValue: number | null
): TrendDirection => {
  if (previousValue === null) return 'stable';

  const diff = currentValue - previousValue;
  const threshold = 0.1;

  if (Math.abs(diff) < threshold) return 'stable';
  return diff > 0 ? 'up' : 'down';
};

const requiresArtificialLight = (variableName: string): boolean => {
  const lowerName = variableName.toLowerCase();
  return lowerName.includes('luz') || lowerName.includes('luminosidad');
};

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
  const navigationRef = useRef(navigation);

  const [systemStatus, setSystemStatus] = useState<SystemStatusResponse | null>(null);
  const [previousReadings, setPreviousReadings] = useState<ReadingItem[]>([]);
  const [lastUpdateTimestamp, setLastUpdateTimestamp] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [weather, setWeather] = useState<WeatherSummary | null>(null);
  const [weatherLoading, setWeatherLoading] = useState(false);
  const [weatherError, setWeatherError] = useState<string | null>(null);

  // Mantener navigationRef actualizado
  useEffect(() => {
    navigationRef.current = navigation;
  }, [navigation]);

  const fetchSystemStatus = useCallback(async (): Promise<string | null> => {
    try {
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
        navigationRef.current.replace('Login');
      }
      return null;
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [systemStatus, logout]);

  // Cargar datos iniciales
  useEffect(() => {
    fetchSystemStatus();
  }, []);

  // Auto-refresh dinámico basado en timestamp + 2 minutos
  const lastTimestampRef = useRef<string | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const followUpStartTimeRef = useRef<number | null>(null);

  useEffect(() => {
    if (!lastUpdateTimestamp) return;

    const READING_INTERVAL_MS = 2 * 60 * 1000; // 2 minutos
    const BUFFER_MS = 20 * 1000; // +20 segundos de margen
    const FOLLOW_UP_INTERVAL_MS = 20 * 1000; // 20 segundos
    const MAX_FOLLOW_UP_MS = 2 * 60 * 1000; // 2 minutos máximo

    const calculateNextFetchDelay = (readingTimestamp: string): number => {
      const readingTime = new Date(readingTimestamp).getTime();
      const nextReadingTime = readingTime + READING_INTERVAL_MS;
      const now = Date.now();
      const timeUntilNextReading = nextReadingTime - now;

      if (timeUntilNextReading <= 0) return 0;
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

  const handleRefresh = () => {
    setIsRefreshing(true);
    fetchSystemStatus();
  };

  const handleLogout = async () => {
    await logout();
    navigation.replace('Login');
  };

  if (isLoading && !systemStatus) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Spinner size="large" color={semanticColors.primary} />
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

        {/* Last Update Info */}
        {lastUpdateTimestamp && (
          <View style={styles.updateInfo}>
            <Text style={styles.updateEmoji}>🔄</Text>
            <View style={styles.updateTextContainer}>
              <Text variant="body" color="#15803d" style={styles.updateText}>
                Última actualización: {formatColombiaDateTime(lastUpdateTimestamp)}
              </Text>
              <Text variant="caption" color="#15803d" style={styles.updateSubtext}>
                Las lecturas se actualizan automáticamente cada 2 minutos
              </Text>
            </View>
          </View>
        )}

        {/* Variables Section */}
        <View style={styles.section}>
          <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
            Variables del Sistema
          </Text>
          <View style={styles.variablesGrid}>
            {systemStatus?.readings.readings.map((reading) => {
              const alertConfig = getAlertConfig(reading.name);
              const status = calculateVariableStatus(reading, alertConfig);
              const previousValue = getPreviousValue(reading.name);
              const trend = calculateTrend(reading.value, previousValue);
              const needsLightCheck = requiresArtificialLight(reading.name);
              const lightActive = isArtificialLightActive();
              const showLightAlert = needsLightCheck && reading.value < reading.optimalMin;

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
                    showArtificialLightAlert={showLightAlert}
                  />
                </View>
              );
            })}
          </View>
        </View>

        {/* Controller Status Section */}
        {systemStatus && lastUpdateTimestamp && (
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
    backgroundColor: colors.hidro.bgLight,
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
    backgroundColor: colors.hidro.bgLight,
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
    gap: spacing.sm,
    marginHorizontal: spacing.lg,
    marginBottom: spacing.lg,
    padding: spacing.md,
    backgroundColor: '#dcfce7',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#86efac',
  },
  updateEmoji: {
    fontSize: 24,
  },
  updateTextContainer: {
    flex: 1,
  },
  updateText: {
    fontWeight: '500',
  },
  updateSubtext: {
    marginTop: spacing.xs,
  },
  variablesGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  variableCardWrapper: {
    width: '48%',
  },
});
