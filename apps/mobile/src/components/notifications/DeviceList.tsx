import React from 'react';
import { View, StyleSheet, TouchableOpacity } from 'react-native';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { Spinner } from '../atoms/Spinner';
import { spacing, semanticColors, typography, colors, borderRadius } from '@hydroespinaca/shared';
import type { PushSubscriptionInfo } from '@hydroespinaca/shared';

export interface DeviceListProps {
  devices: PushSubscriptionInfo[];
  loading?: boolean;
  error?: string | null;
  onRemove: (subscriptionId: string) => void;
}

export function DeviceList({
  devices,
  loading,
  error,
  onRemove,
}: DeviceListProps): React.ReactElement {
  if (loading && devices.length === 0) {
    return (
      <View style={styles.center}>
        <Spinner size="sm" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.section}>
        <Text variant="caption" color={semanticColors.error}>{error}</Text>
      </View>
    );
  }

  return (
    <View style={styles.section}>
      <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
        Dispositivos Push
      </Text>
      {devices.length === 0 ? (
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          No hay dispositivos registrados.
        </Text>
      ) : (
        devices.map(device => (
          <View key={device.id} style={styles.deviceRow}>
            <View style={styles.deviceInfo}>
              <Text variant="body" weight="medium" color={semanticColors.textPrimary}>
                {device.deviceName ?? device.platform}
              </Text>
              <View style={styles.deviceMeta}>
                <Badge
                  variant={device.isActive ? 'success' : 'default'}
                  size="sm"
                >
                  {device.isActive ? 'Activo' : 'Inactivo'}
                </Badge>
                <Text variant="caption" color={semanticColors.textTertiary} style={styles.platform}>
                  {device.platform}
                </Text>
              </View>
            </View>
            <TouchableOpacity
              onPress={() => device.id && onRemove(device.id)}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              accessibilityLabel="Eliminar dispositivo"
            >
              <Text variant="body" color={semanticColors.error}>Eliminar</Text>
            </TouchableOpacity>
          </View>
        ))
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    marginBottom: spacing.lg,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
  },
  center: {
    alignItems: 'center',
    paddingVertical: spacing.xl,
  },
  empty: {
    paddingHorizontal: spacing.md,
  },
  deviceRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
  },
  deviceInfo: {
    flex: 1,
  },
  deviceMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 2,
  },
  platform: {
    marginLeft: spacing.xs,
  },
});
