import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, borderRadius, semanticColors } from '@hydroespinaca/shared';

export interface ProgressBarProps {
  /** Value between 0 and 1 */
  value: number;
  /** Color of the filled portion */
  color?: string;
  /** Background track color */
  trackColor?: string;
  height?: number;
  style?: ViewStyle;
  testID?: string;
}

export function ProgressBar({
  value,
  color = semanticColors.primary,
  trackColor = colors.gray[200],
  height = 6,
  style,
  testID,
}: ProgressBarProps): React.ReactElement {
  const clampedValue = Math.min(1, Math.max(0, value));

  return (
    <View
      style={[styles.track, { height, backgroundColor: trackColor, borderRadius: height / 2 }, style]}
      testID={testID}
      accessibilityRole="progressbar"
      accessibilityValue={{ min: 0, max: 100, now: Math.round(clampedValue * 100) }}
    >
      <View
        style={[
          styles.fill,
          { width: `${clampedValue * 100}%`, backgroundColor: color, borderRadius: height / 2 },
        ]}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  track: {
    width: '100%',
    overflow: 'hidden',
  },
  fill: {
    height: '100%',
  },
});
