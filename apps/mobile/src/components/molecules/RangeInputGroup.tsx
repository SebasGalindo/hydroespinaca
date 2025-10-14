import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms';
import { FormField } from '../molecules';
import { colors, semanticColors, spacing, typography } from '@hidroespinaca/shared';

export interface RangeInputGroupProps {
  /** Título de la sección */
  title: string;
  /** Valor mínimo del rango general */
  generalMinValue?: string;
  /** Valor máximo del rango general */
  generalMaxValue?: string;
  /** Valor mínimo del rango óptimo */
  optimalMinValue?: string;
  /** Valor máximo del rango óptimo */
  optimalMaxValue?: string;
  /** Callback cuando cambian los valores del rango general */
  onGeneralRangeChange?: (min: string, max: string) => void;
  /** Callback cuando cambian los valores del rango óptimo */
  onOptimalRangeChange?: (min: string, max: string) => void;
  /** Unidad de medida */
  unit?: string;
  /** Si está deshabilitado */
  disabled?: boolean;
  /** Errores de validación */
  errors?: {
    generalMin?: string | undefined;
    generalMax?: string | undefined;
    optimalMin?: string | undefined;
    optimalMax?: string | undefined;
  };
}

export const RangeInputGroup: React.FC<RangeInputGroupProps> = ({
  title,
  generalMinValue = '0',
  generalMaxValue = '100',
  optimalMinValue = '20',
  optimalMaxValue = '80',
  onGeneralRangeChange,
  onOptimalRangeChange,
  unit = '',
  disabled = false,
  errors = {},
}) => {
  const [generalMin, setGeneralMin] = useState(generalMinValue);
  const [generalMax, setGeneralMax] = useState(generalMaxValue);
  const [optimalMin, setOptimalMin] = useState(optimalMinValue);
  const [optimalMax, setOptimalMax] = useState(optimalMaxValue);

  // Validaciones
  const validateGeneralRange = (min: string, max: string) => {
    const minNum = parseFloat(min);
    const maxNum = parseFloat(max);
    
    if (isNaN(minNum) || isNaN(maxNum)) return false;
    if (minNum >= maxNum) return false;
    
    return true;
  };

  const validateOptimalRange = (min: string, max: string, generalMinNum: number, generalMaxNum: number) => {
    const minNum = parseFloat(min);
    const maxNum = parseFloat(max);
    
    if (isNaN(minNum) || isNaN(maxNum)) return false;
    if (minNum >= maxNum) return false;
    if (minNum < generalMinNum || maxNum > generalMaxNum) return false;
    
    return true;
  };

  // Handlers para rango general
  const handleGeneralMinChange = (value: string) => {
    setGeneralMin(value);
    const minNum = parseFloat(value);
    const maxNum = parseFloat(generalMax);
    
    if (!isNaN(minNum) && !isNaN(maxNum) && validateGeneralRange(value, generalMax)) {
      onGeneralRangeChange?.(value, generalMax);
    }
  };

  const handleGeneralMaxChange = (value: string) => {
    setGeneralMax(value);
    const minNum = parseFloat(generalMin);
    const maxNum = parseFloat(value);
    
    if (!isNaN(minNum) && !isNaN(maxNum) && validateGeneralRange(generalMin, value)) {
      onGeneralRangeChange?.(generalMin, value);
    }
  };

  // Handlers para rango óptimo
  const handleOptimalMinChange = (value: string) => {
    setOptimalMin(value);
    const minNum = parseFloat(value);
    const maxNum = parseFloat(optimalMax);
    const generalMinNum = parseFloat(generalMin);
    const generalMaxNum = parseFloat(generalMax);
    
    if (!isNaN(minNum) && !isNaN(maxNum) && !isNaN(generalMinNum) && !isNaN(generalMaxNum) && 
        validateOptimalRange(value, optimalMax, generalMinNum, generalMaxNum)) {
      onOptimalRangeChange?.(value, optimalMax);
    }
  };

  const handleOptimalMaxChange = (value: string) => {
    setOptimalMax(value);
    const minNum = parseFloat(optimalMin);
    const maxNum = parseFloat(value);
    const generalMinNum = parseFloat(generalMin);
    const generalMaxNum = parseFloat(generalMax);
    
    if (!isNaN(minNum) && !isNaN(maxNum) && !isNaN(generalMinNum) && !isNaN(generalMaxNum) && 
        validateOptimalRange(optimalMin, value, generalMinNum, generalMaxNum)) {
      onOptimalRangeChange?.(optimalMin, value);
    }
  };

  // Actualizar valores cuando cambien las props
  useEffect(() => {
    setGeneralMin(generalMinValue);
    setGeneralMax(generalMaxValue);
    setOptimalMin(optimalMinValue);
    setOptimalMax(optimalMaxValue);
  }, [generalMinValue, generalMaxValue, optimalMinValue, optimalMaxValue]);

  return (
    <View style={styles.container}>
      <Text style={styles.sectionTitle}>{title}</Text>
      
      {/* Rango General de Medición */}
      <View style={styles.rangeSection}>
        <Text style={styles.rangeTitle}>Rango General de Medición</Text>
        <Text style={styles.rangeDescription}>
          Valor mínimo que puede medir el sensor
        </Text>
        
        <View style={styles.rangeInputs}>
          <View style={styles.inputContainer}>
            <FormField
              label="Valor Mínimo"
              value={generalMin}
              onChangeText={handleGeneralMinChange}
              placeholder="0"
              disabled={disabled}
              {...(errors.generalMin && { errorText: errors.generalMin })}
              inputProps={{ keyboardType: "numeric" }}
            />
          </View>
          
          <View style={styles.inputContainer}>
            <FormField
              label="Valor Máximo"
              value={generalMax}
              onChangeText={handleGeneralMaxChange}
              placeholder="100"
              disabled={disabled}
              {...(errors.generalMax && { errorText: errors.generalMax })}
              inputProps={{ keyboardType: "numeric" }}
            />
          </View>
        </View>
        
        <Text style={styles.helpText}>
          Valor máximo que puede medir el sensor
        </Text>
      </View>

      {/* Rango Óptimo para el Cultivo */}
      <View style={styles.rangeSection}>
        <Text style={styles.rangeTitle}>Rango Óptimo para el Cultivo</Text>
        
        <View style={styles.rangeInputs}>
          <View style={styles.inputContainer}>
            <FormField
              label="Óptimo Mínimo"
              value={optimalMin}
              onChangeText={handleOptimalMinChange}
              placeholder="20"
              disabled={disabled}
              {...(errors.optimalMin && { errorText: errors.optimalMin })}
              inputProps={{ keyboardType: "numeric" }}
            />
          </View>
          
          <View style={styles.inputContainer}>
            <FormField
              label="Óptimo Máximo"
              value={optimalMax}
              onChangeText={handleOptimalMaxChange}
              placeholder="80"
              disabled={disabled}
              {...(errors.optimalMax && { errorText: errors.optimalMax })}
              inputProps={{ keyboardType: "numeric" }}
            />
          </View>
        </View>
        
        <Text style={styles.helpText}>
          Valor mínimo del rango óptimo
        </Text>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.semibold as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  rangeSection: {
    marginBottom: spacing.lg,
  },
  rangeTitle: {
    fontSize: typography.fontSize.md,
    fontWeight: typography.fontWeight.medium as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },
  rangeDescription: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
    marginBottom: spacing.md,
  },
  rangeInputs: {
    flexDirection: 'row',
    gap: spacing.md,
    marginBottom: spacing.sm,
  },
  inputContainer: {
    flex: 1,
  },
  helpText: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textMuted,
    fontStyle: 'italic',
  },
});

export default RangeInputGroup;