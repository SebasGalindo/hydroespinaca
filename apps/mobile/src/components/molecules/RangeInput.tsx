import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ViewStyle } from 'react-native';

export interface RangeInputProps {
  /** Valor mínimo del rango */
  minimumValue?: number;
  /** Valor máximo del rango */
  maximumValue?: number;
  /** Valores iniciales del rango [min, max] */
  values?: [number, number];
  /** Paso del slider */
  step?: number;
  /** Callback cuando cambian los valores */
  onValuesChange?: (values: [number, number]) => void;
  /** Callback cuando termina el cambio */
  onValuesChangeFinish?: (values: [number, number]) => void;
  /** Etiqueta del componente */
  label?: string;
  /** Si está deshabilitado */
  disabled?: boolean;
  /** Variante del componente */
  variant?: 'default' | 'filled' | 'minimal';
  /** Tamaño del componente */
  size?: 'small' | 'medium' | 'large';
  /** Mostrar valores */
  showValues?: boolean;
  /** Prefijo para los valores */
  valuePrefix?: string;
  /** Sufijo para los valores */
  valueSuffix?: string;
  /** Color del track activo */
  activeTrackColor?: string;
  /** Color del track inactivo */
  inactiveTrackColor?: string;
  /** Color del thumb */
  thumbColor?: string;
}

export const RangeInput: React.FC<RangeInputProps> = ({
  minimumValue = 0,
  maximumValue = 100,
  values = [20, 80],
  step = 1,
  onValuesChange,
  onValuesChangeFinish,
  label,
  disabled = false,
  variant = 'default',
  size = 'medium',
  showValues = true,
  valuePrefix = '',
  valueSuffix = '',
  activeTrackColor,
  inactiveTrackColor,
  thumbColor,
}) => {
  const [currentValues, setCurrentValues] = useState<[number, number]>(values);

  const handleMinValueChange = useCallback((value: number) => {
    const newValues: [number, number] = [
      Math.min(value, currentValues[1] - step),
      currentValues[1]
    ];
    setCurrentValues(newValues);
    onValuesChange?.(newValues);
  }, [currentValues, step, onValuesChange]);

  const handleMaxValueChange = useCallback((value: number) => {
    const newValues: [number, number] = [
      currentValues[0],
      Math.max(value, currentValues[0] + step)
    ];
    setCurrentValues(newValues);
    onValuesChange?.(newValues);
  }, [currentValues, step, onValuesChange]);

  const handleMinValueChangeComplete = useCallback((value: number) => {
    const newValues: [number, number] = [
      Math.min(value, currentValues[1] - step),
      currentValues[1]
    ];
    onValuesChangeFinish?.(newValues);
  }, [currentValues, step, onValuesChangeFinish]);

  const handleMaxValueChangeComplete = useCallback((value: number) => {
    const newValues: [number, number] = [
      currentValues[0],
      Math.max(value, currentValues[0] + step)
    ];
    onValuesChangeFinish?.(newValues);
  }, [currentValues, step, onValuesChangeFinish]);

  const getContainerStyle = (): ViewStyle[] => {
    const baseStyle: ViewStyle[] = [styles.container];
    
    if (variant === 'filled') {
      baseStyle.push(styles.filledContainer);
    } else if (variant === 'minimal') {
      baseStyle.push(styles.minimalContainer);
    }

    if (size === 'small') {
      baseStyle.push(styles.smallContainer);
    } else if (size === 'large') {
      baseStyle.push(styles.largeContainer);
    }

    if (disabled) {
      baseStyle.push(styles.disabledContainer);
    }

    return baseStyle;
  };

  const getSliderStyle = (): ViewStyle[] => {
    const baseStyle: ViewStyle[] = [styles.slider];
    
    if (size === 'small') {
      baseStyle.push(styles.smallSlider);
    } else if (size === 'large') {
      baseStyle.push(styles.largeSlider);
    }

    return baseStyle;
  };

  const getTrackColors = () => {
    if (disabled) {
      return {
        minimumTrackTintColor: '#d1d5db',
        maximumTrackTintColor: '#e5e7eb',
        thumbTintColor: '#9ca3af',
      };
    }

    return {
      minimumTrackTintColor: activeTrackColor || '#3b82f6',
      maximumTrackTintColor: inactiveTrackColor || '#d1d5db',
      thumbTintColor: thumbColor || '#2563eb',
    };
  };

  const formatValue = (value: number) => {
    return `${valuePrefix}${value}${valueSuffix}`;
  };

  return (
    <View style={getContainerStyle()}>
      {label && (
        <Text style={[styles.label, disabled && styles.disabledLabel]}>
          {label}
        </Text>
      )}
      
      {showValues && (
        <View style={styles.valuesContainer}>
          <Text style={[styles.valueText, disabled && styles.disabledText]}>
            {formatValue(currentValues[0])}
          </Text>
          <Text style={[styles.valueText, disabled && styles.disabledText]}>
            {formatValue(currentValues[1])}
          </Text>
        </View>
      )}

      <View style={styles.slidersContainer}>
        {/* Indicador visual del rango */}
        <View style={styles.rangeIndicator}>
          <View 
            style={[
              styles.rangeTrack,
              {
                left: `${((currentValues[0] - minimumValue) / (maximumValue - minimumValue)) * 100}%`,
                width: `${((currentValues[1] - currentValues[0]) / (maximumValue - minimumValue)) * 100}%`,
                backgroundColor: disabled ? '#d1d5db' : (activeTrackColor || '#3b82f6'),
              }
            ]}
          />
        </View>
        
        {/* Controles mejorados para ajustar valores */}
        <View style={styles.controlsContainer}>
          <View style={styles.controlGroup}>
            <Text style={[styles.controlLabel, disabled && styles.disabledText]}>
              Mínimo
            </Text>
            <View style={styles.buttonGroup}>
              <TouchableOpacity 
                style={[styles.button, styles.decreaseButton, disabled && styles.disabledButton]}
                onPress={() => !disabled && handleMinValueChange(Math.max(minimumValue, currentValues[0] - step))}
                disabled={disabled}
                accessibilityLabel="Disminuir valor mínimo"
              >
                <Text style={[styles.buttonText, disabled && styles.disabledText]}>−</Text>
              </TouchableOpacity>
              
              <TouchableOpacity 
                style={[styles.button, styles.increaseButton, disabled && styles.disabledButton]}
                onPress={() => !disabled && handleMinValueChange(Math.min(currentValues[1] - step, currentValues[0] + step))}
                disabled={disabled}
                accessibilityLabel="Aumentar valor mínimo"
              >
                <Text style={[styles.buttonText, disabled && styles.disabledText]}>+</Text>
              </TouchableOpacity>
            </View>
          </View>
          
          <View style={styles.controlGroup}>
            <Text style={[styles.controlLabel, disabled && styles.disabledText]}>
              Máximo
            </Text>
            <View style={styles.buttonGroup}>
              <TouchableOpacity 
                style={[styles.button, styles.decreaseButton, disabled && styles.disabledButton]}
                onPress={() => !disabled && handleMaxValueChange(Math.max(currentValues[0] + step, currentValues[1] - step))}
                disabled={disabled}
                accessibilityLabel="Disminuir valor máximo"
              >
                <Text style={[styles.buttonText, disabled && styles.disabledText]}>−</Text>
              </TouchableOpacity>
              
              <TouchableOpacity 
                style={[styles.button, styles.increaseButton, disabled && styles.disabledButton]}
                onPress={() => !disabled && handleMaxValueChange(Math.min(maximumValue, currentValues[1] + step))}
                disabled={disabled}
                accessibilityLabel="Aumentar valor máximo"
              >
                <Text style={[styles.buttonText, disabled && styles.disabledText]}>+</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    paddingVertical: 16,
    marginBottom: 32,
    marginTop: 8,
  },
  filledContainer: {
    backgroundColor: '#f9fafb',
    borderRadius: 8,
    padding: 16,
  },
  minimalContainer: {
    paddingVertical: 8,
  },
  smallContainer: {
    paddingVertical: 8,
  },
  largeContainer: {
    paddingVertical: 24,
  },
  disabledContainer: {
    opacity: 0.6,
  },
  label: {
    fontSize: 16,
    fontWeight: '500',
    color: '#374151',
    marginBottom: 8,
  },
  disabledLabel: {
    color: '#9ca3af',
  },
  valuesContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  valueText: {
    fontSize: 12,
    fontWeight: '500',
    color: '#6b7280',
  },
  disabledText: {
    color: '#9ca3af',
  },
  slidersContainer: {
    position: 'relative',
    height: 40,
  },
  slider: {
    position: 'absolute',
    width: '100%',
    height: 40,
  },
  smallSlider: {
    height: 30,
  },
  largeSlider: {
    height: 50,
  },
  maxSlider: {
    opacity: 0.8,
  },
  rangeIndicator: {
    position: 'absolute',
    bottom: 31,
    left: 0,
    right: 0,
    height: 4,
    backgroundColor: '#e5e7eb',
    borderRadius: 2,
  },
  rangeTrack: {
    position: 'absolute',
    height: '100%',
    borderRadius: 2,
  },
  controlsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 16,
    paddingHorizontal: 8,
  },
  controlGroup: {
    alignItems: 'center',
    flex: 1,
    marginHorizontal: 8,
  },
  controlLabel: {
    fontSize: 12,
    fontWeight: '500',
    color: '#6b7280',
    marginBottom: 8,
    textAlign: 'center',
  },
  buttonGroup: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginVertical: 4,
  },
  button: {
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    minWidth: 40,
    minHeight: 40,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 1,
    },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  decreaseButton: {
    backgroundColor: '#ef4444',
  },
  increaseButton: {
    backgroundColor: '#22c55e',
  },
  disabledButton: {
    backgroundColor: '#d1d5db',
    shadowOpacity: 0,
    elevation: 0,
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 20,
    fontWeight: '700',
    lineHeight: 20,
  },
});

export default RangeInput;