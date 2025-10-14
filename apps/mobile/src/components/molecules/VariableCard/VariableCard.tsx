import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text, Icon, Badge } from '../../atoms';
import { semanticColors, spacing, typography, colors } from '@hidroespinaca/shared';
import type { MetricData } from '@hidroespinaca/shared';

export interface VariableCardProps {
  metric: MetricData;
  optimalRange?: string;
  onPress?: () => void;
}

export const VariableCard: React.FC<VariableCardProps> = ({
  metric,
  optimalRange,
  onPress
}) => {
  const getStatusColor = (status: MetricData['status']) => {
    switch (status) {
      case 'optimal':
        return colors.success;
      case 'warning':
        return colors.warning;
      case 'critical':
        return colors.error;
      default:
        return semanticColors.textMuted;
    }
  };



  return (
    <View style={[styles.container, { borderLeftColor: getStatusColor(metric.status) }]}>
      {/* Header con título e icono */}
      <View style={styles.header}>
        <View style={styles.titleContainer}>
          <Icon 
            name={metric.iconType} 
            size={20} 
            color="#16a34a" 
          />
          <Text variant="body" color="textSecondary" style={styles.title}>
            {metric.title}
          </Text>
        </View>
        <Badge 
          variant={metric.status === 'optimal' ? 'success' : metric.status === 'warning' ? 'warning' : 'error'}
          size="sm"
        />
      </View>

      {/* Valor principal */}
      <View style={styles.valueContainer}>
        <Text variant="h2" color="textPrimary" style={styles.value}>
          {metric.value}
        </Text>
        <Text variant="body" color="textSecondary" style={styles.unit}>
          {metric.unit}
        </Text>
      </View>

      {/* Rango óptimo si se proporciona */}
      {optimalRange && (
        <View style={styles.rangeContainer}>
          <Text variant="caption" style={styles.optimalText}>
            Óptima: {optimalRange}
          </Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: 12,
    padding: spacing.md,
    shadowColor: '#9ca3af', // Color gris suave para el shadow
    shadowOffset: {
      width: 0,
      height: 1,
    },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
    marginBottom: spacing.sm,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  title: {
    marginLeft: spacing.xs,
    fontWeight: '500',
    fontSize: 16, // Aumentado de tamaño por defecto
  },
  valueContainer: {
    flexDirection: 'row',
    alignItems: 'baseline',
    marginBottom: spacing.xs,
  },
  value: {
    fontWeight: '700',
    marginRight: spacing.xs,
    fontSize: 24, // Valor principal más grande
  },
  unit: {
    fontWeight: '500',
    fontSize: 16, // Unidad más legible
  },
  rangeContainer: {
    marginBottom: spacing.xs,
  },
  optimalText: {
    color: '#16a34a', // Verde como en la web
    fontWeight: '500',
    fontSize: 14, // Texto óptimo más legible
  },
});