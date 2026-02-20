import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, typography } from '@hydroespinaca/shared';
import { Text } from './Text';
import { Icon } from './Icon';

export interface AvatarProps {
  name?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  style?: ViewStyle;
  testID?: string;
}

const sizeMap = {
  sm: 32,
  md: 40,
  lg: 56,
  xl: 80,
} as const;

const fontSizeMap = {
  sm: typography.fontSize.xs as number,
  md: typography.fontSize.sm as number,
  lg: typography.fontSize.lg as number,
  xl: typography.fontSize['2xl'] as number,
} as const;

const getInitials = (name?: string): string => {
  if (!name) return '';
  const parts = name.trim().split(' ').filter(Boolean);
  if (parts.length === 0) return '';
  const first = parts[0]?.[0] ?? '';
  if (parts.length === 1) return first.toUpperCase();
  const last = parts[parts.length - 1]?.[0] ?? '';
  return (first + last).toUpperCase();
};

export function Avatar({
  name,
  size = 'md',
  style,
  testID,
}: AvatarProps): React.ReactElement {
  const dimension = sizeMap[size];
  const initials = getInitials(name);
  const fontSize = fontSizeMap[size];

  return (
    <View
      style={[
        styles.container,
        {
          width: dimension,
          height: dimension,
          borderRadius: dimension / 2,
        },
        style,
      ]}
      testID={testID}
      accessibilityRole="image"
      accessibilityLabel={name ? `Avatar de ${name}` : 'Avatar'}
    >
      {initials ? (
        <Text
          variant="body"
          color={colors.hidro[700]}
          style={{ fontSize, fontWeight: typography.fontWeight.semibold as any }}
        >
          {initials}
        </Text>
      ) : (
        <Icon name="user" size={dimension * 0.5} color={colors.hidro[600]} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.hidro[100],
    justifyContent: 'center',
    alignItems: 'center',
  },
});
