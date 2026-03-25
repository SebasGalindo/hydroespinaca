import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { semanticColors, spacing, borderRadius, colors, typography } from '@hydroespinaca/shared';
import type { WeatherSummary } from '@hydroespinaca/shared';

interface WeatherCardProps {
  weather: WeatherSummary | null;
  isLoading?: boolean;
  error?: string | null;
}

export function WeatherCard({
  weather,
  isLoading = false,
  error = null,
}: WeatherCardProps): React.ReactElement {

  const getWeatherEmoji = (icon: string): string => {
    const iconMap: { [key: string]: string } = {
      '01d': '☀️', '01n': '🌙',
      '02d': '🌤️', '02n': '☁️',
      '03d': '☁️', '03n': '☁️',
      '04d': '☁️', '04n': '☁️',
      '09d': '🌧️', '09n': '🌧️',
      '10d': '🌦️', '10n': '🌧️',
      '11d': '⛈️', '11n': '⛈️',
      '13d': '❄️', '13n': '❄️',
      '50d': '🌫️', '50n': '🌫️',
    };
    return iconMap[icon] || '🌤️';
  };

  const formatTime = (isoString: string): string => {
    try {
      const date = new Date(isoString);
      return date.toLocaleTimeString('es-CO', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: true
      });
    } catch {
      return 'N/A';
    }
  };

  // Error state
  if (error) {
    return (
      <View style={[styles.container, styles.errorContainer]}>
        <Text variant="label" color={semanticColors.errorText} style={styles.headerTitle}>
          Clima en Mosquera, Cundinamarca
        </Text>
        <View style={styles.errorContent}>
          <Text variant="caption" color={semanticColors.errorText} style={styles.errorText}>
            {error}
          </Text>
        </View>
      </View>
    );
  }

  // Loading state
  if (isLoading || !weather) {
    return (
      <View style={styles.container}>
        <Text variant="label" color={semanticColors.textPrimary} style={styles.headerTitle}>
          Clima en Mosquera, Cundinamarca
        </Text>
        <View style={styles.loadingContent}>
          <Spinner size="lg" color={semanticColors.primary} />
          <Text variant="caption" color={semanticColors.textSecondary} style={styles.loadingText}>
            Cargando datos del clima...
          </Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <Text variant="label" color={semanticColors.textPrimary} style={styles.headerTitle}>
            Clima en Mosquera, Cundinamarca
          </Text>
          <Text variant="caption" color={semanticColors.textSecondary} style={styles.description}>
            {getWeatherEmoji(weather.icon)} {weather.description}
          </Text>
        </View>
        <Text style={styles.mainEmoji}>
          {getWeatherEmoji(weather.icon)}
        </Text>
      </View>

      {/* Main temperature */}
      <View style={styles.mainTempContainer}>
        <Text variant="h1" color={semanticColors.textPrimary} style={styles.mainTemp}>
          {weather.temperature.toFixed(1)}°C
        </Text>
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.feelsLike}>
          Sensación: {weather.feelsLike.toFixed(1)}°C
        </Text>
      </View>

      {/* Weather metrics grid */}
      <View style={styles.metricsGrid}>
        <View style={styles.metricCard}>
          <Text style={styles.metricEmoji}>💧</Text>
          <View>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Humedad
            </Text>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.metricValue}>
              {weather.humidity}%
            </Text>
          </View>
        </View>

        <View style={styles.metricCard}>
          <Text style={styles.metricEmoji}>💨</Text>
          <View>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Viento
            </Text>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.metricValue}>
              {weather.windSpeed.toFixed(1)} m/s
            </Text>
          </View>
        </View>

        <View style={styles.metricCard}>
          <Text style={styles.metricEmoji}>☁️</Text>
          <View>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Nubosidad
            </Text>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.metricValue}>
              {weather.cloudiness}%
            </Text>
          </View>
        </View>

        <View style={styles.metricCard}>
          <Text style={styles.metricEmoji}>🌧️</Text>
          <View>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Lluvia (1h)
            </Text>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.metricValue}>
              {weather.rain1h !== null ? `${weather.rain1h.toFixed(1)} mm` : '0.0 mm'}
            </Text>
          </View>
        </View>
      </View>

      {/* Sunrise/Sunset */}
      <View style={styles.sunTimesContainer}>
        <View style={[styles.sunTimeCard, styles.sunriseCard]}>
          <Text style={styles.sunTimeEmoji}>🌅</Text>
          <View>
            <Text variant="caption" color={colors.warning[600]} style={styles.sunTimeLabel}>
              Amanecer
            </Text>
            <Text variant="body" color={colors.warning[700]} style={styles.sunTimeValue}>
              {formatTime(weather.sunrise)}
            </Text>
          </View>
        </View>

        <View style={[styles.sunTimeCard, styles.sunsetCard]}>
          <Text style={styles.sunTimeEmoji}>🌇</Text>
          <View>
            <Text variant="caption" color={colors.info[600]} style={styles.sunTimeLabel}>
              Atardecer
            </Text>
            <Text variant="body" color={colors.info[700]} style={styles.sunTimeValue}>
              {formatTime(weather.sunset)}
            </Text>
          </View>
        </View>
      </View>

      {/* Footer */}
      <View style={styles.footer}>
        <Text variant="caption" color={semanticColors.textSecondary}>
          ⏱ Actualizado: {formatTime(weather.lastUpdate)}
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.info[50],
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.info[200],
  },
  errorContainer: {
    backgroundColor: colors.error[50],
    borderColor: colors.error[200],
  },
  errorContent: {
    paddingVertical: spacing.md,
    alignItems: 'center',
  },
  errorText: {
    textAlign: 'center',
  },
  loadingContent: {
    paddingVertical: spacing.xl,
    alignItems: 'center',
  },
  loadingText: {
    marginTop: spacing.sm,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.md,
  },
  headerLeft: {
    flex: 1,
  },
  headerTitle: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.xs,
  },
  description: {
    textTransform: 'capitalize',
  },
  mainEmoji: {
    fontSize: typography.fontSize['5xl'],
  },
  mainTempContainer: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  mainTemp: {
    fontSize: 40,
    fontWeight: typography.fontWeight.bold,
  },
  feelsLike: {
    marginTop: spacing.xs,
  },
  metricsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  metricCard: {
    flex: 1,
    minWidth: '45%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: semanticColors.overlayWhite,
    padding: spacing.sm,
    borderRadius: borderRadius.md,
  },
  metricEmoji: {
    fontSize: typography.fontSize['2xl'],
  },
  metricValue: {
    fontWeight: typography.fontWeight.semibold,
  },
  sunTimesContainer: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  sunTimeCard: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.sm,
    borderRadius: borderRadius.md,
  },
  sunriseCard: {
    backgroundColor: colors.warning[50],
  },
  sunsetCard: {
    backgroundColor: colors.info[100],
  },
  sunTimeEmoji: {
    fontSize: 28,
  },
  sunTimeLabel: {
    fontWeight: typography.fontWeight.semibold,
  },
  sunTimeValue: {
    fontWeight: typography.fontWeight.bold,
  },
  footer: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.info[200],
    alignItems: 'center',
  },
});
