import React from 'react';
import { TouchableOpacity, View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius, semanticColors } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Divider } from '../atoms/Divider';

export interface ListItemProps {
  title: string;
  subtitle?: string;
  leftIcon?: string;
  leftIconColor?: string;
  rightContent?: React.ReactNode;
  showChevron?: boolean;
  showDivider?: boolean;
  onPress?: () => void;
  disabled?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

export function ListItem({
  title,
  subtitle,
  leftIcon,
  leftIconColor = semanticColors.textSecondary,
  rightContent,
  showChevron = true,
  showDivider = false,
  onPress,
  disabled = false,
  style,
  testID,
  accessibilityLabel,
}: ListItemProps): React.ReactElement {
  const content = (
    <>
      <View style={[styles.container, style]}>
        {leftIcon && (
          <View style={styles.iconContainer}>
            <Icon name={leftIcon as any} size={22} color={leftIconColor} />
          </View>
        )}
        <View style={styles.textContainer}>
          <Text
            variant="body"
            color={disabled ? semanticColors.textTertiary : semanticColors.textPrimary}
            numberOfLines={1}
          >
            {title}
          </Text>
          {subtitle && (
            <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={2}>
              {subtitle}
            </Text>
          )}
        </View>
        {rightContent}
        {showChevron && !rightContent && (
          <Icon name="chevron-right" size={18} color={semanticColors.textTertiary} />
        )}
      </View>
      {showDivider && <Divider spacing={0} />}
    </>
  );

  if (onPress) {
    return (
      <TouchableOpacity
        onPress={onPress}
        disabled={disabled}
        activeOpacity={0.7}
        testID={testID}
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel || title}
        accessibilityState={{ disabled }}
      >
        {content}
      </TouchableOpacity>
    );
  }

  return <View testID={testID}>{content}</View>;
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.md,
    gap: spacing.md,
  },
  iconContainer: {
    width: 36,
    height: 36,
    borderRadius: borderRadius.lg,
    backgroundColor: colors.gray[50],
    justifyContent: 'center',
    alignItems: 'center',
  },
  textContainer: {
    flex: 1,
    gap: 2,
  },
});
