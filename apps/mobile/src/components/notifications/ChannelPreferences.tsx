import React, { useCallback } from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { ChannelToggleRow } from '../molecules/ChannelToggleRow';
import { spacing, semanticColors, typography } from '@hydroespinaca/shared';
import type { ChannelPreference, NotificationChannel } from '@hydroespinaca/shared';

export interface ChannelPreferencesProps {
  channels: ChannelPreference[];
  loading?: boolean;
  error?: string | null;
  onToggle: (channel: NotificationChannel, enabled: boolean) => void;
}

export function ChannelPreferences({
  channels,
  loading,
  error,
  onToggle,
}: ChannelPreferencesProps): React.ReactElement {
  if (loading && channels.length === 0) {
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
        Canales de Notificación
      </Text>
      {channels.length === 0 ? (
        <Text variant="body" color={semanticColors.textSecondary}>
          No hay canales configurados.
        </Text>
      ) : (
        channels.map(ch => (
          <ChannelToggleRow
            key={ch.channel}
            channel={ch.channel}
            enabled={ch.enabled}
            subtitle={ch.target ?? undefined}
            onToggle={onToggle}
            testID={`channel-pref-${ch.channel}`}
          />
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
});
