// Cross-platform Button component types
export interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  // Platform-specific props
  onClick?: any;
  onPress?: any;
  type?: any;
  className?: string;
  style?: any;
}