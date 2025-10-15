import React, { useRef, useEffect } from 'react';
import { View, ViewStyle, Animated } from 'react-native';
import { semanticColors, borderRadius } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface ProgressBarProps {
  progress: number; // 0-100
  height?: number;
  color?: string;
  backgroundColor?: string;
  showLabel?: boolean;
  labelPosition?: 'top' | 'bottom' | 'center';
  animated?: boolean;
  style?: ViewStyle;
  testID?: string;
}

export function ProgressBar({
  progress,
  height = 8,
  color = semanticColors.primary,
  backgroundColor = semanticColors.backgroundSecondary,
  showLabel = false,
  labelPosition = 'top',
  animated = true,
  style,
  testID,
}: ProgressBarProps): React.ReactElement {
  const animatedProgress = useRef(new Animated.Value(0)).current;
  
  // Asegurar que el progreso esté entre 0 y 100
  const clampedProgress = Math.max(0, Math.min(100, progress));
  
  useEffect(() => {
    if (animated) {
      Animated.timing(animatedProgress, {
        toValue: clampedProgress,
        duration: 300,
        useNativeDriver: false,
      }).start();
    } else {
      animatedProgress.setValue(clampedProgress);
    }
  }, [clampedProgress, animated, animatedProgress]);
  
  const getContainerStyle = (): ViewStyle => ({
    height,
    backgroundColor,
    borderRadius: height / 2,
    overflow: 'hidden',
  });
  
  const getProgressStyle = () => ({
    height,
    backgroundColor: color,
    borderRadius: height / 2,
    width: animatedProgress.interpolate({
      inputRange: [0, 100],
      outputRange: ['0%', '100%'],
      extrapolate: 'clamp',
    }),
  });
  
  const renderLabel = () => {
    if (!showLabel) return null;
    
    const labelText = `${Math.round(clampedProgress)}%`;
    
    if (labelPosition === 'center') {
      return (
        <View
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Text
            style={{
              fontSize: height * 0.7,
              color: semanticColors.textPrimary,
              fontWeight: 'bold',
            }}
          >
            {labelText}
          </Text>
        </View>
      );
    }
    
    return (
      <Text
        style={{
          fontSize: 12,
          color: semanticColors.textSecondary,
          textAlign: 'right',
          marginTop: labelPosition === 'bottom' ? 4 : 0,
          marginBottom: labelPosition === 'top' ? 4 : 0,
        }}
      >
        {labelText}
      </Text>
    );
  };
  
  return (
    <View
      style={style}
      testID={testID}
      accessibilityRole="progressbar"
      accessibilityValue={{ min: 0, max: 100, now: clampedProgress }}
      accessibilityLabel={`Progress: ${Math.round(clampedProgress)} percent`}
    >
      {labelPosition === 'top' && renderLabel()}
      
      <View style={getContainerStyle()}>
        <Animated.View style={getProgressStyle()} />
        {labelPosition === 'center' && renderLabel()}
      </View>
      
      {labelPosition === 'bottom' && renderLabel()}
    </View>
  );
}