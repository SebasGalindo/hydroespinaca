import React from 'react';
import { Ionicons } from '@expo/vector-icons';
import { IconName } from '@hydroespinaca/shared';

export interface IconProps {
  name: IconName;
  size?: number;
  color?: string;
  stroke?: string;
  strokeWidth?: number;
  fill?: string;
  style?: any;
  testID?: string;
}

// Mapeo de nombres de íconos personalizados a Ionicons
const iconMap: Record<string, keyof typeof Ionicons.glyphMap> = {
  // User & Auth
  'user': 'person-outline',
  'lock': 'lock-closed-outline',
  'mail': 'mail-outline',
  'eye': 'eye-outline',
  'eye-off': 'eye-off-outline',
  'log-out': 'log-out-outline',

  // Alerts & Status
  'warning': 'warning-outline',
  'alert-triangle': 'alert-circle-outline',
  'close': 'close-outline',
  'check': 'checkmark-outline',

  // Variables & Sensors
  'temperature': 'thermometer-outline',
  'water': 'water-outline',
  'humidity': 'water-outline',
  'sun': 'sunny-outline',
  'electric': 'flash-outline',
  'ruler': 'resize-outline',
  'ph': 'flask-outline',

  // Trends
  'trending-up': 'trending-up-outline',
  'trending-down': 'trending-down-outline',
  'minus': 'remove-outline',

  // Other
  'refresh': 'refresh-outline',
  'settings': 'settings-outline',
  'menu': 'menu-outline',
  'search': 'search-outline',
  'calendar': 'calendar-outline',
  'clock': 'time-outline',
};

export function Icon({
  name,
  size = 24,
  color = '#000000',
  style,
  testID,
}: IconProps): React.ReactElement {
  const ionIconName = iconMap[name] || 'help-circle-outline';

  return (
    <Ionicons
      name={ionIconName}
      size={size}
      color={color}
      style={style}
      testID={testID}
    />
  );
}