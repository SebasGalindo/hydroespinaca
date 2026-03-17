import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, semanticColors } from '@hydroespinaca/shared';

export interface StatusIndicatorProps {
  status: 'online' | 'offline' | 'warning' | 'idle';
  size?: 'sm' | 'md' | 'lg';
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

const statusColors = {
  online: colors.hidro[500],
  offline: colors.error[500],
  warning: colors.warning[500],
  idle: colors.gray[400],
} as const;

const sizeMap = {
  sm: 8,
  md: 12,
  lg: 16,
} as const;

const statusLabels = {
    online: 'En línea',
    offline: 'Sin conexión',
    warning: 'Advertencia',
    idle: 'Inactivo',
};

export const StatusIndicator = React.memo(function StatusIndicator({
  status,
  size = 'md',
  style,
  testID,
  accessibilityLabel,
}: StatusIndicatorProps): React.ReactElement {
  const dimension = sizeMap[size];
  const color = statusColors[status];

  return (
    <View
      style={[
        styles.outer,
        {
          width: dimension + 4,
          height: dimension + 4,
          borderRadius: (dimension + 4) / 2,
        },
        style,
      ]}
      testID={testID}
      accessibilityRole="text"
      accessibilityLabel={accessibilityLabel || statusLabels[status]}
    >
      <View
        style={[
          styles.dot,
          {
            width: dimension,
            height: dimension,
            borderRadius: dimension / 2,
            backgroundColor: color,
          },
        ]}
      />
    </View>
  );
});

const styles = StyleSheet.create({
  outer: {
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: semanticColors.overlayWhiteStrong,
  },
  dot: {},
});
