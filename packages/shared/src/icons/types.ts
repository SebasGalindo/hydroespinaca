export interface IconProps {
  size?: number;
  color?: string;
  className?: string; // Para web
  style?: any; // Para React Native
  testID?: string; // Para testing
}

export type IconName = 
  | 'home'
  | 'settings'
  | 'user'
  | 'close'
  | 'menu'
  | 'search'
  | 'plus'
  | 'minus'
  | 'check'
  | 'arrow-left'
  | 'arrow-right'
  | 'heart'
  | 'star'
  | 'bell'
  | 'mail'
  | 'phone'
  | 'camera'
  | 'edit'
  | 'delete'
  | 'save'
  | 'refresh'
  | 'download'
  | 'upload'
  | 'lock'
  | 'unlock'
  | 'eye'
  | 'eye-off'
  | 'calendar'
  | 'clock'
  | 'location'
  | 'wifi'
  | 'battery'
  | 'power'
  | 'warning'
  | 'info'
  | 'error'
  | 'success'
  | 'chevron-back'
  | 'chevron-forward'
  | 'play-skip-back'
  | 'play-skip-forward'
  | 'chart'
  | 'book'
  | 'trending-up'
  | 'brain'
  | 'temperature'
  | 'light'
  | 'humidity'
  | 'ph'
  | 'sun'
  | 'electric'
  | 'ruler'
  | 'water';

export interface IconComponent {
  (props: IconProps): React.ReactElement;
}