import React, { useMemo, useRef, useState, useCallback } from 'react';
import { View, PanResponder, ViewStyle, AccessibilityActionEvent, Animated } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface SliderProps {
  value?: number;
  defaultValue?: number;
  minimumValue?: number;
  maximumValue?: number;
  step?: number;
  disabled?: boolean;
  showValue?: boolean;
  trackColor?: string;
  activeTrackColor?: string;
  thumbColor?: string;
  size?: 'sm' | 'md' | 'lg';
  allowTouchTrack?: boolean;
  thumbTouchSize?: { width: number; height: number };
  style?: ViewStyle;
  onValueChange?: (value: number) => void;
  onSlidingStart?: (value: number) => void;
  onSlidingComplete?: (value: number) => void;
  testID?: string;
}

const sizeStyles = {
  sm: { trackHeight: 4, thumbSize: 16, containerHeight: 40 },
  md: { trackHeight: 6, thumbSize: 20, containerHeight: 48 },
  lg: { trackHeight: 8, thumbSize: 24, containerHeight: 56 },
} as const;

const DEFAULT_THUMB_TOUCH_SIZE = { width: 40, height: 40 };

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function roundToStep(value: number, step: number, min: number, max: number): number {
  if (step <= 0) return clamp(value, min, max);
  const rounded = Math.round((value - min) / step) * step + min;
  return clamp(Number(rounded.toFixed(10)), min, max);
}

export function Slider({
  value,
  defaultValue = 0,
  minimumValue = 0,
  maximumValue = 100,
  step = 1,
  disabled = false,
  showValue = true,
  trackColor = semanticColors.backgroundSecondary,
  activeTrackColor = semanticColors.primary,
  thumbColor = semanticColors.primary,
  size = 'lg',
  allowTouchTrack = true,
  thumbTouchSize = DEFAULT_THUMB_TOUCH_SIZE,
  style,
  onValueChange,
  onSlidingStart,
  onSlidingComplete,
  testID,
}: SliderProps): React.ReactElement {
  // Controlled vs uncontrolled state management
  const isControlled = value !== undefined;
  const [internalValue, setInternalValue] = useState(() => 
    clamp(defaultValue, minimumValue, maximumValue)
  );
  
  const currentValue = useMemo(() => 
    clamp(isControlled ? (value as number) : internalValue, minimumValue, maximumValue),
    [value, internalValue, minimumValue, maximumValue, isControlled]
  );

  // Track dimensions
  const [trackLayout, setTrackLayout] = useState({ width: 0, x: 0 });
  const sizeCfg = sizeStyles[size];
  
  // Animation values
  const thumbPosition = useRef(new Animated.Value(0)).current;
  const thumbScale = useRef(new Animated.Value(1)).current;
  
  // Gesture state
  const sliderState = useRef({
    isSliding: false,
    startValue: 0,
    startX: 0,
  });

  // Calculate positions
  const range = maximumValue - minimumValue;
  const fraction = range > 0 ? (currentValue - minimumValue) / range : 0;
  const trackWidth = Math.max(0, trackLayout.width - sizeCfg.thumbSize);
  const thumbLeft = fraction * trackWidth;

  // Update thumb position when value changes
  React.useEffect(() => {
    if (!sliderState.current.isSliding) {
      Animated.timing(thumbPosition, {
        toValue: thumbLeft,
        duration: 150,
        useNativeDriver: false,
      }).start();
    }
  }, [thumbLeft, thumbPosition]);

  // Value update function
  const updateValue = useCallback((newValue: number, eventType: 'change' | 'complete') => {
    const clampedValue = clamp(newValue, minimumValue, maximumValue);
    const steppedValue = roundToStep(clampedValue, step, minimumValue, maximumValue);
    
    if (!isControlled) {
      setInternalValue(steppedValue);
    }
    
    if (eventType === 'change') {
      onValueChange?.(steppedValue);
    } else {
      onSlidingComplete?.(steppedValue);
    }
    
    return steppedValue;
  }, [minimumValue, maximumValue, step, isControlled, onValueChange, onSlidingComplete]);

  // Convert X position to value
  const getValueFromPosition = useCallback((x: number): number => {
    if (trackWidth <= 0) return currentValue;
    
    const relativeX = clamp(x, 0, trackWidth);
    const fraction = relativeX / trackWidth;
    const rawValue = minimumValue + fraction * range;
    
    return roundToStep(rawValue, step, minimumValue, maximumValue);
  }, [trackWidth, minimumValue, range, step, currentValue]);

  // Handle track press for tap-to-set
  const handleTrackPress = useCallback((evt: any) => {
    if (disabled || !allowTouchTrack) return;
    
    const newValue = getValueFromPosition(evt.nativeEvent.locationX);
    updateValue(newValue, 'change');
    updateValue(newValue, 'complete');
  }, [disabled, allowTouchTrack, getValueFromPosition, updateValue]);

  // Pan responder for gesture handling
  const panResponder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => !disabled,
      onMoveShouldSetPanResponder: (_, gestureState) => {
        if (disabled) return false;
        // Only capture horizontal movements that are more significant than vertical
        return Math.abs(gestureState.dx) > Math.abs(gestureState.dy) && Math.abs(gestureState.dx) > 2;
      },
      onPanResponderTerminationRequest: () => false,
      onShouldBlockNativeResponder: () => true,

      onPanResponderGrant: (evt) => {
        if (disabled) return;
        
        sliderState.current.isSliding = true;
        sliderState.current.startValue = currentValue;
        sliderState.current.startX = evt.nativeEvent.locationX;
        
        // Scale animation for visual feedback
        Animated.spring(thumbScale, {
          toValue: 1.2,
          useNativeDriver: false,
        }).start();
        
        onSlidingStart?.(currentValue);
      },

      onPanResponderMove: (evt, gestureState) => {
        if (disabled || !sliderState.current.isSliding || trackWidth <= 0) return;
        
        // Calculate new position based on current touch location
        const currentX = evt.nativeEvent.locationX;
        const relativeX = clamp(currentX, 0, trackWidth);
        const fraction = relativeX / trackWidth;
        const newValue = minimumValue + fraction * range;
        
        updateValue(newValue, 'change');
      },

      onPanResponderRelease: () => {
        if (disabled || !sliderState.current.isSliding) return;
        
        sliderState.current.isSliding = false;
        
        // Reset scale animation
        Animated.spring(thumbScale, {
          toValue: 1,
          useNativeDriver: false,
        }).start();
        
        updateValue(currentValue, 'complete');
      },

      onPanResponderTerminate: () => {
        sliderState.current.isSliding = false;
        Animated.spring(thumbScale, {
          toValue: 1,
          useNativeDriver: false,
        }).start();
        onSlidingComplete?.(currentValue);
      },
    })
  ).current;

  // Styles
  const containerStyle: ViewStyle = useMemo(() => ({
    height: sizeCfg.containerHeight,
    justifyContent: 'center',
    opacity: disabled ? 0.5 : 1,
    paddingHorizontal: sizeCfg.thumbSize / 2,
  }), [sizeCfg, disabled]);

  const trackStyle: ViewStyle = useMemo(() => ({
    height: sizeCfg.trackHeight,
    backgroundColor: trackColor,
    borderRadius: sizeCfg.trackHeight / 2,
    position: 'relative',
  }), [sizeCfg, trackColor]);

  const activeTrackStyle: ViewStyle = useMemo(() => ({
    height: sizeCfg.trackHeight,
    width: thumbLeft + sizeCfg.thumbSize / 2,
    backgroundColor: activeTrackColor,
    borderRadius: sizeCfg.trackHeight / 2,
    position: 'absolute',
    left: 0,
    top: 0,
  }), [sizeCfg, activeTrackColor, thumbLeft]);

  const thumbContainerStyle: ViewStyle = useMemo(() => ({
    position: 'absolute',
    width: thumbTouchSize.width,
    height: thumbTouchSize.height,
    justifyContent: 'center',
    alignItems: 'center',
    left: thumbLeft - (thumbTouchSize.width - sizeCfg.thumbSize) / 2,
    top: -(thumbTouchSize.height - sizeCfg.trackHeight) / 2,
  }), [thumbLeft, thumbTouchSize, sizeCfg]);

  const thumbStyle: ViewStyle = useMemo(() => ({
    width: sizeCfg.thumbSize,
    height: sizeCfg.thumbSize,
    backgroundColor: semanticColors.surface,
    borderRadius: sizeCfg.thumbSize / 2,
    borderWidth: 2,
    borderColor: thumbColor,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.2,
    shadowRadius: 4,
    elevation: 3,
  }), [sizeCfg, thumbColor]);

  return (
    <View style={[containerStyle, style]} testID={testID}>
      {showValue && (
        <View style={{ marginBottom: spacing.sm, alignItems: 'center' }}>
          <Text size="lg" weight="semibold" color={semanticColors.textPrimary}>
            {String(currentValue.toFixed(step < 1 ? 1 : 0))}
          </Text>
        </View>
      )}

      <View
        style={trackStyle}
        onLayout={(event) => {
          const { width, x } = event.nativeEvent.layout;
          setTrackLayout({ width, x });
        }}
        onTouchEnd={handleTrackPress}
        accessible
        accessibilityRole="adjustable"
        accessibilityLabel={`Slider, current value ${currentValue}`}
        accessibilityState={{ disabled }}
        accessibilityActions={[
          { name: 'increment', label: 'Increase value' },
          { name: 'decrement', label: 'Decrease value' }
        ]}
        onAccessibilityAction={(event: AccessibilityActionEvent) => {
          if (disabled) return;
          const delta = event.nativeEvent.actionName === 'increment' ? step : -step;
          updateValue(currentValue + delta, 'change');
        }}
      >
        <View style={activeTrackStyle} />
        
        <Animated.View 
          style={[
            thumbContainerStyle,
            { transform: [{ scale: thumbScale }] }
          ]}
          {...panResponder.panHandlers}
        >
          <View style={thumbStyle} />
        </Animated.View>
      </View>
    </View>
  );
}