import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { 
  InfoField as InfoFieldType, 
  FieldType 
} from '@hydroespinaca/shared';

export interface InfoFieldProps {
  /** Datos del campo de información */
  field: InfoFieldType;
  /** Estilo personalizado para el contenedor */
  style?: any;
}

export function InfoField({ field, style }: InfoFieldProps): React.ReactElement {
  const renderValue = () => {
    switch (field.type) {
      case 'badge':
        return (
          <Badge 
            variant={field.variant === 'danger' ? 'error' : (field.variant || 'default')} 
            size="sm"
            style={styles.badgeValue}
          >
            {field.value}
          </Badge>
        );
      case 'number':
        return (
          <Text 
            variant="body" 
            color={semanticColors.textPrimary}
            style={styles.numberValue}
          >
            {field.value}
          </Text>
        );
      case 'date':
        return (
          <Text 
            variant="body" 
            color={semanticColors.textSecondary}
            style={styles.dateValue}
          >
            {field.value}
          </Text>
        );
      default: // 'text'
        return (
          <Text 
            variant="body" 
            color={semanticColors.textPrimary}
            style={styles.textValue}
          >
            {field.value}
          </Text>
        );
    }
  };

  return (
    <View style={[styles.container, style]}>
      <Text 
        variant="body" 
        color={semanticColors.textSecondary}
        style={styles.label}
      >
        {field.label}
      </Text>
      <View style={styles.valueContainer}>
        {renderValue()}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.xs,
  },

  label: {
    flex: 1,
    fontWeight: typography.fontWeight.medium as any,
    textTransform: 'uppercase',
    fontSize: 12,
    letterSpacing: 0.5,
  },

  valueContainer: {
    flex: 1,
    alignItems: 'flex-end',
  },

  textValue: {
    fontWeight: typography.fontWeight.semibold as any,
    textAlign: 'right',
  },

  numberValue: {
    fontWeight: typography.fontWeight.semibold as any,
    textAlign: 'right',
    fontVariant: ['tabular-nums'],
  },

  dateValue: {
    textAlign: 'right',
    fontSize: 12,
  },

  badgeValue: {
    // Badge ya tiene sus propios estilos
  },
});