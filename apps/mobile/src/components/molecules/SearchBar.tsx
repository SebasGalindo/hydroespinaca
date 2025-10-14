import React, { useMemo } from 'react';
import { View, ViewStyle, TextInput, TouchableOpacity, Text, ActivityIndicator } from 'react-native';
import { Icon } from '../atoms/Icon';

export interface SearchBarProps {
  value: string;
  placeholder?: string;
  loading?: boolean;
  disabled?: boolean;
  autoFocus?: boolean;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'default' | 'filled' | 'outline';
  showCancel?: boolean;
  cancelLabel?: string;
  style?: ViewStyle;
  onChangeText?: (text: string) => void;
  onSubmit?: (text: string) => void;
  onClear?: () => void;
  onCancel?: () => void;
  testID?: string;
}

export function SearchBar({
  value,
  placeholder = 'Buscar...',
  loading = false,
  disabled = false,
  autoFocus,
  size = 'md',
  variant = 'default',
  showCancel = false,
  cancelLabel = 'Cancelar',
  style,
  onChangeText,
  onSubmit,
  onClear,
  onCancel,
  testID = 'searchbar',
}: SearchBarProps): JSX.Element {
  const containerStyle = useMemo(() => ({
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  }) as ViewStyle, []);

  const inputContainerStyle = useMemo(() => ({
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: variant === 'filled' ? '#f3f4f6' : '#ffffff',
    borderRadius: 8,
    borderWidth: variant === 'outline' ? 1 : 0,
    borderColor: '#e5e7eb',
    paddingHorizontal: 12,
    paddingVertical: size === 'lg' ? 12 : size === 'sm' ? 6 : 8,
  }) as ViewStyle, [variant, size]);

  const inputStyle = useMemo(() => ({
    flex: 1,
    fontSize: size === 'lg' ? 18 : size === 'sm' ? 14 : 16,
    color: '#111827',
    marginLeft: 8,
  }), [size]);

  return (
    <View style={[containerStyle, style]} testID={testID}>
      <View style={inputContainerStyle}>
        <Icon 
          name="search" 
          size={size === 'lg' ? 20 : size === 'sm' ? 16 : 18} 
          color="#6b7280" 
        />
        <TextInput
          value={value}
          onChangeText={onChangeText}
          placeholder={placeholder}
          editable={!disabled}
          autoFocus={autoFocus}
          style={[inputStyle, disabled && { opacity: 0.5 }]}
          onSubmitEditing={() => onSubmit?.(value)}
          returnKeyType="search"
          testID={`${testID}-input`}
          placeholderTextColor="#9ca3af"
        />
        
        {loading && (
          <ActivityIndicator 
            size={size === 'lg' ? 'large' : 'small'} 
            color="#3b82f6" 
            style={{ marginLeft: 8 }}
          />
        )}
        
        {value?.length > 0 && !loading && !showCancel && (
          <TouchableOpacity
            onPress={onClear || (() => {})}
            style={{ marginLeft: 8, padding: 4 }}
            testID={`${testID}-clear`}
          >
            <Icon 
              name="close" 
              size={size === 'lg' ? 20 : size === 'sm' ? 16 : 18} 
              color="#6b7280" 
            />
          </TouchableOpacity>
        )}
      </View>

      {showCancel && (
        <TouchableOpacity
          onPress={onCancel || (() => {})}
          style={{ padding: 8 }}
          testID={`${testID}-cancel`}
        >
          <Text style={{ 
            fontSize: size === 'lg' ? 16 : size === 'sm' ? 14 : 15,
            color: '#3b82f6',
            fontWeight: '500'
          }}>
            {cancelLabel}
          </Text>
        </TouchableOpacity>
      )}
    </View>
  );
}