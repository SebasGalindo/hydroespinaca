import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { spacing, colors, semanticColors, borderRadius } from '@hydroespinaca/shared';
import type { GovernmentAlert } from '@hydroespinaca/shared';

export interface GovernmentAlertBannerProps {
  alerts: GovernmentAlert[];
  testID?: string;
}

function formatAlertDate(iso: string): string {
  const d = new Date(iso);
  return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
}

export function GovernmentAlertBanner({
  alerts,
  testID,
}: GovernmentAlertBannerProps): React.ReactElement | null {
  if (!alerts || alerts.length === 0) return null;

  return (
    <View style={styles.container} testID={testID}>
      {alerts.map((alert, idx) => (
        <View key={`${alert.event}-${idx}`} style={styles.banner}>
          <View style={styles.header}>
            <Text variant="body" style={styles.icon}>🏛️</Text>
            <Text variant="body" weight="bold" color={colors.error[800]} style={styles.event}>
              {alert.event}
            </Text>
          </View>
          <Text variant="caption" color={colors.error[700]} numberOfLines={3}>
            {alert.description}
          </Text>
          <View style={styles.meta}>
            <Text variant="caption" color={colors.error[600]}>
              {alert.senderName}
            </Text>
            <Text variant="caption" color={colors.error[600]}>
              {formatAlertDate(alert.start)} — {formatAlertDate(alert.end)}
            </Text>
          </View>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  banner: {
    backgroundColor: colors.error[50],
    borderLeftWidth: 4,
    borderLeftColor: colors.error[600],
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  icon: {
    marginRight: spacing.xs,
    fontSize: 16,
  },
  event: {
    flex: 1,
  },
  meta: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: spacing.xs,
  },
});
