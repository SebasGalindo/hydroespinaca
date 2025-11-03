/**
 * Icon type definitions
 */

// Using string literal union type
export type IconName =
  | 'home'
  | 'user'
  | 'settings'
  | 'logout'
  | 'login'
  | 'dashboard'
  | 'chart'
  | 'analytics'
  | 'plant'
  | 'water'
  | 'temperature'
  | 'humidity'
  | 'light'
  | 'ph'
  | 'add'
  | 'edit'
  | 'delete'
  | 'close'
  | 'check'
  | 'check-circle'
  | 'alert'
  | 'alert-triangle'
  | 'info'
  | 'warning'
  | 'error'
  | 'success'
  | 'x-circle'
  | 'log-out'
  | 'arrow-left'
  | 'arrow-right'
  | 'arrow-up'
  | 'arrow-down'
  | 'chevron-left'
  | 'chevron-right'
  | 'chevron-up'
  | 'chevron-down'
  | 'menu'
  | 'more'
  | 'search'
  | 'filter'
  | 'refresh'
  | 'calendar'
  | 'clock'
  | 'download'
  | 'upload'
  | 'eye'
  | 'eye-off'
  | 'heart'
  | 'star'
  | 'bell'
  | 'mail'
  | 'phone'
  | 'location'
  | 'wifi'
  | 'bluetooth'
  | 'battery'
  | 'camera'
  | 'microphone'
  | 'speaker'
  | 'lock'
  | 'unlock'
  | 'key'
  | 'shield'
  | 'sun'
  | 'moon'
  | 'cloud'
  | 'rain'
  | 'snow'
  | 'wind'
  | 'droplet'
  | 'thermometer'
  | 'activity'
  | 'zap'
  | 'maximize';

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
