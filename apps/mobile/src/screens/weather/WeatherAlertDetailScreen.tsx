/**
 * WeatherAlertDetailScreen — Vista detallada de una alerta meteorológica.
 */
import React, { useCallback, useMemo } from 'react';
import { View, StyleSheet } from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import { ScreenLayout } from '../../components/organisms/ScreenLayout';
import { Text } from '../../components/atoms/Text';
import { Badge } from '../../components/atoms/Badge';
import { Button } from '../../components/atoms/Button';
import {
  useWeatherStore,
  useAuthStore,
  colors,
  spacing,
  semanticColors,
  typography,
  borderRadius,
  ALERT_TYPE_LABELS,
  ALERT_TYPE_ICONS,
  ALERT_SEVERITY_COLORS,
} from '@hydroespinaca/shared';
import type { WeatherStackParamList } from '../../navigation/types';
import type { AlertSeverity } from '@hydroespinaca/shared';

type DetailRoute = RouteProp<WeatherStackParamList, 'WeatherAlertDetail'>;

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString('es-CO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function WeatherAlertDetailScreen(): React.ReactElement {
  const route = useRoute<DetailRoute>();
  const navigation = useNavigation();
  const { alertId } = route.params;

  const userId = useAuthStore(s => s.user?.id) ?? '';
  const alert = useWeatherStore(s => s.alerts.find(a => a.id === alertId));
  const markAlertRead = useWeatherStore(s => s.markAlertRead);

  const isRead = useMemo(() => {
    if (!alert) return false;
    return alert.notifiedUsers.some(u => u.userId === userId && u.isRead);
  }, [alert, userId]);

  const handleMarkRead = useCallback(() => {
    if (alert) markAlertRead(alert.id, userId);
  }, [alert, userId, markAlertRead]);

  if (!alert) {
    return (
      <ScreenLayout title="Alerta" testID="alert-detail-not-found">
        <View style={styles.center}>
          <Text variant="body" color={semanticColors.textSecondary}>
            Alerta no encontrada.
          </Text>
          <Button
            variant="outline"
            onPress={() => navigation.goBack()}
            style={styles.backButton}
          >
            Volver
          </Button>
        </View>
      </ScreenLayout>
    );
  }

  const icon = ALERT_TYPE_ICONS[alert.alertType] ?? '⚠️';
  const label = ALERT_TYPE_LABELS[alert.alertType] ?? alert.alertType;
  const severityColor = ALERT_SEVERITY_COLORS[alert.severity as AlertSeverity] ?? 'gray';
  const badgeVariant: 'error' | 'warning' | 'info' =
    severityColor === 'red' ? 'error' :
    severityColor === 'amber' ? 'warning' :
    'info';

  return (
    <ScreenLayout title="Detalle de Alerta" testID="alert-detail-screen">
      <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text variant="h1" style={styles.icon}>{icon}</Text>
          <View style={styles.headerText}>
            <Text variant="h2" color={semanticColors.textPrimary}>{alert.title}</Text>
            <View style={styles.badges}>
              <Badge variant={badgeVariant} size="sm">{label}</Badge>
              <Badge
                variant={badgeVariant}
                size="sm"
              >
                {alert.severity.toUpperCase()}
              </Badge>
              {!isRead && (
                <Badge variant="info" size="sm">No leída</Badge>
              )}
            </View>
          </View>
        </View>

        {/* Dates */}
        <View style={styles.section}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Creada: {formatDateTime(alert.createdAt)}
          </Text>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Pronóstico: {formatDateTime(alert.forecastDatetime)}
          </Text>
        </View>

        {/* Message */}
        <View style={styles.section}>
          <Text variant="h3" color={semanticColors.textPrimary} style={styles.sectionTitle}>
            Mensaje
          </Text>
          <Text variant="body" color={semanticColors.textPrimary}>
            {alert.message}
          </Text>
        </View>

        {/* Recommendation */}
        {alert.recommendation && (
          <View style={[styles.section, styles.recommendationBox]}>
            <Text variant="h3" color={colors.info[700]} style={styles.sectionTitle}>
              💡 Recomendación
            </Text>
            <Text variant="body" color={colors.info[800]}>
              {alert.recommendation}
            </Text>
          </View>
        )}

        {/* Forecast value */}
        {alert.forecastValue != null && (
          <View style={styles.section}>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Valor pronosticado: {alert.forecastValue}
              {alert.forecastCondition ? ` — ${alert.forecastCondition}` : ''}
            </Text>
          </View>
        )}

        {/* Actions */}
        {!isRead && (
          <Button
            onPress={handleMarkRead}
            style={styles.actionButton}
            testID="mark-read-btn"
          >
            Marcar como leída
          </Button>
        )}

        <Button
          variant="outline"
          onPress={() => navigation.goBack()}
          style={styles.actionButton}
        >
          Volver
        </Button>
      </View>
    </ScreenLayout>
  );
}

const styles = StyleSheet.create({
  container: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl * 2,
  },
  center: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xl * 2,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: spacing.lg,
  },
  icon: {
    fontSize: 40,
    marginRight: spacing.md,
  },
  headerText: {
    flex: 1,
  },
  badges: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
    marginTop: spacing.xs,
  },
  section: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.xs,
  },
  recommendationBox: {
    backgroundColor: colors.info[50],
    padding: spacing.md,
    borderRadius: borderRadius.md,
  },
  actionButton: {
    marginBottom: spacing.sm,
  },
  backButton: {
    marginTop: spacing.md,
  },
});
