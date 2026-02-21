import React from 'react';
import { View, StyleSheet, ViewStyle, TextInput } from 'react-native';
import { Text } from '../atoms/Text';
import { Switch } from '../atoms/Switch';
import { ListItem } from './ListItem';
import {
  semanticColors, spacing, colors,
  ALERT_TYPE_LABELS, ALERT_TYPE_ICONS,
  type AlertThreshold, type AlertType,
} from '@hydroespinaca/shared';

export interface AlertThresholdRowProps {
  threshold: AlertThreshold;
  onToggle: (type: AlertType, enabled: boolean) => void;
  onValueChange?: (type: AlertType, value: number) => void;
  style?: ViewStyle;
  testID?: string;
}

export function AlertThresholdRow({
  threshold,
  onToggle,
  onValueChange,
  style,
  testID,
}: AlertThresholdRowProps): React.ReactElement {
  const label = ALERT_TYPE_LABELS[threshold.type] ?? threshold.type;
  const icon = ALERT_TYPE_ICONS[threshold.type] ?? '⚠️';

  return (
    <View style={[styles.container, style]} testID={testID}>
      <View style={styles.row}>
        <Text variant="body" style={styles.icon}>{icon}</Text>
        <View style={styles.content}>
          <Text variant="body" weight="medium" color={semanticColors.textPrimary}>
            {label}
          </Text>
          {threshold.thresholdValue != null && threshold.comparison && (
            <Text variant="caption" color={semanticColors.textSecondary}>
              {threshold.comparison === 'gt' ? '> ' : '< '}
              {threshold.thresholdValue}
            </Text>
          )}
        </View>
        <Switch
          value={threshold.enabled}
          onValueChange={(val) => onToggle(threshold.type, val)}
          testID={`${testID}-switch`}
        />
      </View>
      {threshold.enabled && threshold.thresholdValue != null && onValueChange && (
        <View style={styles.valueRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>Umbral:</Text>
          <TextInput
            value={String(threshold.thresholdValue)}
            onChangeText={(text) => {
              const num = parseFloat(text);
              if (!isNaN(num)) onValueChange(threshold.type, num);
            }}
            keyboardType="numeric"
            style={styles.input}
            testID={`${testID}-input`}
          />
        </View>
      )}
      {threshold.enabled && threshold.recommendation && (
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.recommendation} numberOfLines={2}>
          {threshold.recommendation}
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  icon: {
    marginRight: spacing.sm,
    fontSize: 20,
  },
  content: {
    flex: 1,
  },
  valueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    marginTop: spacing.xs,
    marginLeft: spacing.xl,
  },
  input: {
    borderWidth: 1,
    borderColor: colors.gray[300],
    borderRadius: 6,
    paddingHorizontal: spacing.sm,
    paddingVertical: 4,
    width: 80,
    fontSize: 14,
  },
  recommendation: {
    marginTop: spacing.xs,
    marginLeft: spacing.xl,
    fontStyle: 'italic',
  },
});
