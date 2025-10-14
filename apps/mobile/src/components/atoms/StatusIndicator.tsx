import React, { useRef, useEffect } from 'react';
import { View, ViewStyle, Animated } from 'react-native';
import { semanticColors } from '@hidroespinaca/shared';

export interface StatusIndicatorProps {
  status: 'online' | 'offline' | 'warning' | 'error' | 'loading' | 'success';
  size?: 'sm' | 'md' | 'lg' | number;
  animated?: boolean;
  style?: ViewStyle;
  testID?: string;
}

const sizeStyles = {
  sm: 8,
  md: 12,
  lg: 16,
} as const;

const statusColors = {
  online: semanticColors.successText,
  offline: semanticColors.textMuted,
  warning: semanticColors.warningText,
  error: semanticColors.errorText,
  loading: semanticColors.primary,
  success: semanticColors.successText,
} as const;

export function StatusIndicator({
  status,
  size = 'md',
  animated = false,
  style,
  testID,
}: StatusIndicatorProps): React.ReactElement {
  const pulseValue = useRef(new Animated.Value(1)).current;
  const indicatorSize = typeof size === 'number' ? size : sizeStyles[size];
  
  useEffect(() => {
    if (animated && (status === 'loading' || status === 'online')) {
      const pulseAnimation = Animated.loop(
        Animated.sequence([
          Animated.timing(pulseValue, {
            toValue: 1.3,
            duration: 800,
            useNativeDriver: true,
          }),
          Animated.timing(pulseValue, {
            toValue: 1,
            duration: 800,
            useNativeDriver: true,
          }),
        ])
      );
      
      pulseAnimation.start();
      
      return () => {
        pulseAnimation.stop();
      };
    } else {
      pulseValue.setValue(1);
      return undefined;
    }
  }, [animated, status, pulseValue]);
  
  const getIndicatorStyle = (): ViewStyle => ({
    width: indicatorSize,
    height: indicatorSize,
    borderRadius: indicatorSize / 2,
    backgroundColor: statusColors[status],
    shadowColor: statusColors[status],
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.3,
    shadowRadius: 3,
    elevation: 3,
  });
  
  const getAccessibilityLabel = (): string => {
    switch (status) {
      case 'online':
        return 'Online status';
      case 'offline':
        return 'Offline status';
      case 'warning':
        return 'Warning status';
      case 'error':
        return 'Error status';
      case 'loading':
        return 'Loading status';
      case 'success':
        return 'Success status';
      default:
        return 'Status indicator';
    }
  };
  
  return (
    <View
      style={[
        {
          alignItems: 'center',
          justifyContent: 'center',
        },
        style,
      ]}
      testID={testID}
      accessibilityRole="image"
      accessibilityLabel={getAccessibilityLabel()}
    >
      <Animated.View
        style={[
          getIndicatorStyle(),
          animated && {
            transform: [{ scale: pulseValue }],
          },
        ]}
      />
    </View>
  );
}