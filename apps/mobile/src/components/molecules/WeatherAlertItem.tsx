import React from 'react';
import { View, StyleSheet, ViewStyle, TouchableOpacity } from 'react-native';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import {
  semanticColors, spacing, colors, borderRadius,
  ALERT_TYPE_LABELS, ALERT_TYPE_ICONS, ALERT_SEVERITY_COLORS,
  type WeatherAlert,
} from '@hydroespinaca/shared';

export interface WeatherAlertItemProps {
  alert: WeatherAlert;
  userId: string;
  onPress?: (alert: WeatherAlert) => void;
  onMarkRead?: (alertId: string) => void;
  style?: ViewStyle;
  testID?: string;
}

function getSeverityColor(severity: string): string {
  switch (severity) {
    case 'critical': return colors.error[500];
    case 'warning': return colors.warning[500];
    default: return colors.info[500];
  }
}

function isAlertRead(alert: WeatherAlert, userId: string): boolean {
  return alert.notifiedUsers?.some(u => u.userId === userId && u.isRead) ?? false;
}

export function WeatherAlertItem({
  alert,
  userId,
  onPress,
  onMarkRead,
  style,
  testID,
}: WeatherAlertItemProps): React.ReactElement {
  const read = isAlertRead(alert, userId);
  const severityColor = getSeverityColor(alert.severity);
  const icon = ALERT_TYPE_ICONS[alert.alertType] ?? '⚠️';
  const label = ALERT_TYPE_LABELS[alert.alertType] ?? alert.alertType;
  const date = new Date(alert.createdAt).toLocaleDateString('es-CO', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
  });

  return (
    <TouchableOpacity
      style={[styles.container, { borderLeftColor: severityColor }, !read && styles.unread, style]}
      onPress={() => onPress?.(alert)}
      activeOpacity={0.7}
      testID={testID}
    >
      <View style={styles.header}>
        <Text variant="body" style={styles.icon}>{icon}</Text>
        <View style={styles.titleSection}>
          <Text variant="body" weight={read ? 'medium' : 'bold'} color={semanticColors.textPrimary} numberOfLines={1}>
            {alert.title}
          </Text>
          <Text variant="caption" color={semanticColors.textSecondary}>{date}</Text>
        </View>
        <Badge variant={alert.severity === 'critical' ? 'error' : 'warning'} size="sm">
          {label}
        </Badge>
      </View>
      <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={2} style={styles.message}>
        {alert.message}
      </Text>
      {!read && onMarkRead && (
        <TouchableOpacity onPress={() => onMarkRead(alert.id)} style={styles.markReadBtn}>
          <Text variant="caption" color={semanticColors.primary}>Marcar como leída</Text>
        </TouchableOpacity>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderLeftWidth: 4,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  unread: {
    backgroundColor: colors.info[50],
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  icon: {
    marginRight: spacing.sm,
    fontSize: 18,
  },
  titleSection: {
    flex: 1,
    marginRight: spacing.sm,
  },
  message: {
    marginTop: spacing.xs,
    marginLeft: spacing.lg + spacing.sm,
  },
  markReadBtn: {
    marginTop: spacing.xs,
    alignSelf: 'flex-end',
  },
});
