import React from 'react';
import { View, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius, typography } from '@hydroespinaca/shared';
import { Pressable, Text, Icon, Avatar, Badge } from '../atoms';

export interface ListItemProps {
  title: string;
  subtitle?: string;
  left?: React.ReactNode; // e.g., <Avatar/>, <Icon/>
  right?: React.ReactNode; // e.g., value, <Badge/>, actions
  onPress?: () => void;
  disabled?: boolean;
  dense?: boolean;
  divider?: boolean;
  style?: ViewStyle;
  testID?: string;
}

export function ListItem({
  title,
  subtitle,
  left,
  right,
  onPress,
  disabled,
  dense,
  divider = true,
  style,
  testID = 'list-item',
}: ListItemProps): JSX.Element {
  const paddingY = dense ? spacing.xs : spacing.sm;
  const titleSize = dense ? 'md' : 'lg';
  const subtitleColor = semanticColors.textSecondary;

  const content = (
    <View style={{ flexDirection: 'row', alignItems: 'center', paddingVertical: paddingY }}>
      {/* Left accessory */}
      {left && (
        <View style={{ marginRight: spacing.sm }}>
          {left}
        </View>
      )}

      {/* Texts */}
      <View style={{ flex: 1, minHeight: dense ? 36 : 44, justifyContent: 'center' }}>
        <Text size={titleSize as any} numberOfLines={1} style={{ fontWeight: typography.fontWeight.medium as any }}>
          {title}
        </Text>
        {subtitle ? (
          <Text size="sm" color={subtitleColor} numberOfLines={2}>
            {subtitle}
          </Text>
        ) : null}
      </View>

      {/* Right accessory */}
      {right && (
        <View style={{ marginLeft: spacing.sm }}>
          {right}
        </View>
      )}
    </View>
  );

  return (
    <View style={[{ paddingHorizontal: spacing.md, backgroundColor: semanticColors.backgroundPrimary }, style]} testID={testID}>
      {onPress ? (
        <Pressable onPress={onPress} disabled={disabled} style={{ paddingVertical: 0 }}>
          {content}
        </Pressable>
      ) : (
        content
      )}
      {divider && (
        <View style={{ height: 1, backgroundColor: semanticColors.borderMuted }} />
      )}
    </View>
  );
}