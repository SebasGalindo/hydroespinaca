import React, { useRef } from 'react';
import { 
  TouchableOpacity, 
  ViewStyle, 
  Animated, 
  GestureResponderEvent,
  View
} from 'react-native';
import { semanticColors } from '@hydroespinaca/shared';

export interface FloatingActionButtonProps {
  onPress: (event: GestureResponderEvent) => void;
  icon: React.ReactNode;
  size?: 'small' | 'medium' | 'large';
  color?: string;
  position?: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left' | 'center';
  disabled?: boolean;
  style?: ViewStyle;
  testID?: string;
}

export function FloatingActionButton({
  onPress,
  icon,
  size = 'medium',
  color = semanticColors.primary,
  position = 'bottom-right',
  disabled = false,
  style,
  testID,
}: FloatingActionButtonProps): React.ReactElement {
  const scaleAnim = useRef(new Animated.Value(1)).current;
  
  const getSizeStyles = () => {
    switch (size) {
      case 'small':
        return {
          width: 40,
          height: 40,
          borderRadius: 20,
        };
      case 'large':
        return {
          width: 64,
          height: 64,
          borderRadius: 32,
        };
      default: // medium
        return {
          width: 56,
          height: 56,
          borderRadius: 28,
        };
    }
  };
  
  const getPositionStyles = (): ViewStyle => {
    const margin = 16;
    
    switch (position) {
      case 'bottom-left':
        return {
          position: 'absolute',
          bottom: margin,
          left: margin,
        };
      case 'top-right':
        return {
          position: 'absolute',
          top: margin,
          right: margin,
        };
      case 'top-left':
        return {
          position: 'absolute',
          top: margin,
          left: margin,
        };
      case 'center':
        return {
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: [
            { translateX: -getSizeStyles().width / 2 },
            { translateY: -getSizeStyles().height / 2 },
          ],
        };
      default: // bottom-right
        return {
          position: 'absolute',
          bottom: margin,
          right: margin,
        };
    }
  };
  
  const handlePressIn = () => {
    Animated.spring(scaleAnim, {
      toValue: 0.95,
      useNativeDriver: true,
    }).start();
  };
  
  const handlePressOut = () => {
    Animated.spring(scaleAnim, {
      toValue: 1,
      useNativeDriver: true,
    }).start();
  };
  
  const getButtonStyle = (): ViewStyle => ({
    ...getSizeStyles(),
    backgroundColor: disabled ? semanticColors.backgroundSecondary : color,
    alignItems: 'center',
    justifyContent: 'center',
    elevation: disabled ? 0 : 6,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 3,
    },
    shadowOpacity: disabled ? 0 : 0.27,
    shadowRadius: disabled ? 0 : 4.65,
  });
  
  return (
    <View style={[getPositionStyles(), style]}>
      <Animated.View
        style={{
          transform: [{ scale: scaleAnim }],
        }}
      >
        <TouchableOpacity
          onPress={onPress}
          onPressIn={handlePressIn}
          onPressOut={handlePressOut}
          disabled={disabled}
          style={getButtonStyle()}
          testID={testID}
          accessibilityRole="button"
          accessibilityState={{ disabled }}
          activeOpacity={0.8}
        >
          <View
            style={{
              opacity: disabled ? 0.5 : 1,
            }}
          >
            {icon}
          </View>
        </TouchableOpacity>
      </Animated.View>
    </View>
  );
}