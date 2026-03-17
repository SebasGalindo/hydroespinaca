import React from 'react';
import { Ionicons } from '@expo/vector-icons';
import { IconName, colors } from '@hydroespinaca/shared';

export interface IconProps {
  name: IconName;
  size?: number;
  color?: string;
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
  'x': 'close-outline',
  'check': 'checkmark-outline',
  'check-circle': 'checkmark-circle-outline',
  'x-circle': 'close-circle-outline',
  'info': 'information-circle-outline',

  // Variables & Sensors
  'thermometer': 'thermometer-outline',
  'temperature': 'thermometer-outline',
  'water': 'water-outline',
  'droplet': 'water-outline',
  'humidity': 'water-outline',
  'sun': 'sunny-outline',
  'electric': 'flash-outline',
  'zap': 'flash-outline',
  'ruler': 'resize-outline',
  'maximize': 'expand-outline',
  'ph': 'flask-outline',
  'activity': 'pulse-outline',

  // Trends
  'trending-up': 'trending-up-outline',
  'trending-down': 'trending-down-outline',
  'minus': 'remove-outline',

  // Actions
  'edit': 'create-outline',
  'file-text': 'document-text-outline',

  // Other
  'refresh': 'refresh-outline',
  'settings': 'settings-outline',
  'menu': 'menu-outline',
  'search': 'search-outline',
  'calendar': 'calendar-outline',
  'clock': 'time-outline',
  'bell': 'notifications-outline',

  // Analytics
  'bar-chart': 'bar-chart-outline',
  'pie-chart': 'pie-chart-outline',
  'stats': 'stats-chart-outline',
  'share': 'share-outline',
  'flash': 'flash-outline',
  'funnel': 'funnel-outline',
  'swap': 'swap-horizontal-outline',
  'download': 'download-outline',
  'filter': 'filter-outline',

  // BI
  'bolt': 'flash-outline',
  'plant': 'leaf-outline',
  'leaf': 'leaf-outline',
  'cash': 'cash-outline',
  'wallet': 'wallet-outline',
  'trash': 'trash-outline',
  'add': 'add-outline',
  'chevron-right': 'chevron-forward-outline',
  'chevron-up': 'chevron-up-outline',
  'calculator': 'calculator-outline',
  'receipt': 'receipt-outline',
  'pricetag': 'pricetag-outline',
  'cube': 'cube-outline',
  'arrow-up': 'arrow-up-outline',
  'arrow-down': 'arrow-down-outline',

  // Fuzzy
  'copy': 'copy-outline',
  'play': 'play-outline',
  'code': 'code-slash-outline',
  'list': 'list-outline',
  'layers': 'layers-outline',
  'git-branch': 'git-branch-outline',
  'document': 'document-outline',
  'chevron-down': 'chevron-down-outline',
  'radio-on': 'radio-button-on-outline',
  'radio-off': 'radio-button-off-outline',
  'ellipsis': 'ellipsis-horizontal-outline',
  'power': 'power-outline',
  'flask': 'flask-outline',
  'file-edit': 'document-outline',
  'upload': 'cloud-upload-outline',
};

export const Icon = React.memo(function Icon({
  name,
  size = 24,
  color = colors.black,
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
});