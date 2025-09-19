// Cross-platform Input component types
export interface InputProps {
  label?: string;
  placeholder?: string;
  value?: string;
  // Platform-specific props will be handled by each implementation
  onChange?: any;
  onChangeText?: any;
  onFocus?: any;
  onBlur?: any;
  type?: any;
  secureTextEntry?: boolean;
  keyboardType?: any;
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  required?: boolean;
  error?: string;
  helperText?: string;
  fullWidth?: boolean;
  id?: string;
  name?: string;
  autoComplete?: any;
  className?: string;
  style?: any;
}