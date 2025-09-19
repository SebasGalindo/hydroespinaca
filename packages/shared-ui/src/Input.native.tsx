import React from 'react';
import { View, Text, TextInput, TextStyle, ViewStyle } from 'react-native';

export interface InputProps {
  label?: string;
  placeholder?: string;
  value?: string;
  onChangeText?: (text: string) => void;
  onFocus?: () => void;
  onBlur?: () => void;
  secureTextEntry?: boolean;
  keyboardType?: 'default' | 'email-address' | 'numeric' | 'phone-pad';
  autoComplete?: 'email' | 'password' | 'current-password' | 'name' | 'off';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  required?: boolean;
  error?: string;
  helperText?: string;
  fullWidth?: boolean;
  style?: ViewStyle;
}

export const Input: React.FC<InputProps> = ({
  label,
  placeholder,
  value,
  onChangeText,
  onFocus,
  onBlur,
  secureTextEntry = false,
  keyboardType = 'default',
  autoComplete,
  size = 'md',
  disabled = false,
  required = false,
  error,
  helperText,
  fullWidth = true,
  style,
}) => {
  const getFontSize = (size: string): number => {
    switch (size) {
      case 'sm': return 14;
      case 'md': return 16;
      case 'lg': return 18;
      default: return 16;
    }
  };

  const getPadding = (size: string): number => {
    switch (size) {
      case 'sm': return 8;
      case 'md': return 12;
      case 'lg': return 16;
      default: return 12;
    }
  };

  const hasError = Boolean(error);
  
  const containerStyle: ViewStyle = {
    width: fullWidth ? '100%' : 'auto',
    marginBottom: 4,
    ...style
  };

  const labelStyle: TextStyle = {
    fontSize: 14,
    fontWeight: '500',
    color: hasError ? '#dc2626' : '#374151',
  };

  const inputStyle: TextStyle = {
    borderWidth: 1,
    borderColor: hasError ? '#dc2626' : '#d1d5db',
    borderRadius: 6,
    padding: getPadding(size),
    fontSize: getFontSize(size),
    backgroundColor: disabled ? '#f9fafb' : '#ffffff',
    color: disabled ? '#9ca3af' : '#111827',
  };

  const helperStyle: TextStyle = {
    fontSize: 12,
    color: hasError ? '#dc2626' : '#6b7280',
    marginTop: 4,
  };

  return (
    <View style={containerStyle}>
      {label && (
        <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 6 }}>
          <Text style={labelStyle}>{label}</Text>
          {required && <Text style={{ color: '#dc2626' }}> *</Text>}
        </View>
      )}
      
      <TextInput
        style={inputStyle}
        value={value}
        onChangeText={onChangeText}
        onFocus={onFocus}
        onBlur={onBlur}
        placeholder={placeholder}
        placeholderTextColor="#9ca3af"
        secureTextEntry={secureTextEntry}
        keyboardType={keyboardType}
        autoComplete={autoComplete}
        editable={!disabled}
      />
      
      {(error || helperText) && (
        <Text style={helperStyle}>
          {error || helperText}
        </Text>
      )}
    </View>
  );
};