import React, { useState } from 'react';
import { View, Image, ViewStyle, ImageStyle, ImageSourcePropType, TouchableOpacity } from 'react-native';
import { semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface AvatarProps {
  source?: ImageSourcePropType;
  size?: 'sm' | 'md' | 'lg' | number;
  fallback?: string;
  backgroundColor?: string;
  textColor?: string;
  style?: ViewStyle;
  onPress?: () => void;
  testID?: string;
}

const sizeStyles = {
  sm: { size: 32, fontSize: typography.fontSize.sm },
  md: { size: 48, fontSize: typography.fontSize.md },
  lg: { size: 64, fontSize: typography.fontSize.lg },
} as const;

export function Avatar({
  source,
  size = 'md',
  fallback,
  backgroundColor = semanticColors.primary,
  textColor = semanticColors.backgroundPrimary,
  style,
  onPress,
  testID,
}: AvatarProps): React.ReactElement {
  const [imageError, setImageError] = useState(false);
  
  const avatarSize = typeof size === 'number' ? size : sizeStyles[size].size;
  const fontSize = typeof size === 'number' ? Math.round(size * 0.42) : sizeStyles[size].fontSize;
  
  const getContainerStyle = (): ViewStyle => ({
    width: avatarSize,
    height: avatarSize,
    borderRadius: avatarSize / 2,
    backgroundColor,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  });
  
  const getImageStyle = (): ImageStyle => ({
    width: avatarSize,
    height: avatarSize,
    borderRadius: avatarSize / 2,
  });
  
  const getFallbackText = (): string => {
    if (!fallback) return '?';
    
    // Si es un nombre completo, tomar las iniciales
    const words = fallback.trim().split(' ').filter(word => word.length > 0);
    if (words.length >= 2 && words[0] && words[1] && words[0][0] && words[1][0]) {
      return (words[0][0] + words[1][0]).toUpperCase();
    }
    
    // Si es una sola palabra, tomar las primeras dos letras
    return fallback.substring(0, 2).toUpperCase();
  };
  
  const renderContent = () => {
    if (source && !imageError) {
      return (
        <Image
          source={source}
          style={getImageStyle()}
          onError={() => setImageError(true)}
          accessibilityRole="image"
          accessibilityLabel="Profile picture"
        />
      );
    }
    
    return (
      <Text
        style={{
          fontSize,
          color: textColor,
          fontWeight: '600',
          lineHeight: Math.round(fontSize * 1.1),
          letterSpacing: 0.5,
        }}
      >
        {getFallbackText()}
      </Text>
    );
  };
  
  if (onPress) {
    return (
      <TouchableOpacity
        style={[getContainerStyle(), style]}
        onPress={onPress}
        testID={testID}
        accessibilityRole="button"
        accessibilityLabel="Avatar button"
      >
        {renderContent()}
      </TouchableOpacity>
    );
  }
  
  return (
    <View
      style={[getContainerStyle(), style]}
      testID={testID}
      accessibilityRole="image"
      accessibilityLabel="Avatar"
    >
      {renderContent()}
    </View>
  );
}