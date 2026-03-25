import React, { useCallback } from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { Text } from '../atoms/Text';
import { Switch } from '../atoms/Switch';
import {
  semanticColors, spacing, colors,
  CHANNEL_LABELS, CHANNEL_ICONS,
  type NotificationChannel,
} from '@hydroespinaca/shared';

export interface ChannelToggleRowProps {
  channel: NotificationChannel;
  enabled: boolean;
  onToggle: (channel: NotificationChannel, enabled: boolean) => void;
  subtitle?: string;
  style?: ViewStyle;
  testID?: string;
}

export const ChannelToggleRow = React.memo(function ChannelToggleRow({
  channel,
  enabled,
  onToggle,
  subtitle,
  style,
  testID,
}: ChannelToggleRowProps): React.ReactElement {
  const label = CHANNEL_LABELS[channel] ?? channel;
  const icon = CHANNEL_ICONS[channel] ?? '📬';

  const handleToggle = useCallback(
    (val: boolean) => onToggle(channel, val),
    [onToggle, channel],
  );

  return (
    <View style={[styles.container, style]} testID={testID}>
      <Text variant="body" style={styles.icon}>{icon}</Text>
      <View style={styles.content}>
        <Text variant="body" weight="medium" color={semanticColors.textPrimary}>
          {label}
        </Text>
        {subtitle && (
          <Text variant="caption" color={semanticColors.textSecondary}>{subtitle}</Text>
        )}
      </View>
      <Switch
        value={enabled}
        onValueChange={handleToggle}
        testID={`${testID}-switch`}
      />
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
  },
  icon: {
    marginRight: spacing.sm,
    fontSize: 20,
  },
  content: {
    flex: 1,
  },
});
