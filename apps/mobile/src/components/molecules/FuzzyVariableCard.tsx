import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, useFuzzyStore } from '@hydroespinaca/shared';
import type { SimpleFuzzyVariable } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { Card } from './Card';
import { MembershipChart } from './MembershipChart';

export interface FuzzyVariableCardProps {
  variable: SimpleFuzzyVariable;
  onPress?: (variable: SimpleFuzzyVariable) => void;
}

export const FuzzyVariableCard: React.FC<FuzzyVariableCardProps> = ({
  variable,
  onPress,
}) => {
  const { getTermsByVariableId } = useFuzzyStore();
  const terms = getTermsByVariableId(variable.id);

  const handlePress = () => {
    onPress?.(variable);
  };

  // Función para formatear el rango de una función de membresía
  const formatRange = (membershipFunction: any): string => {
    return `[${membershipFunction.universe_min}, ${membershipFunction.universe_max}]`;
  };

  // Función para describir la función de membresía
  const describeMembershipFunction = (membershipFunction: any): string => {
    const { function_type, parameters } = membershipFunction;
    switch (function_type) {
      case 'triangular':
        return `Triangular (${parameters[0]}, ${parameters[1]}, ${parameters[2]})`;
      case 'trapezoidal':
        return `Trapezoidal (${parameters[0]}, ${parameters[1]}, ${parameters[2]}, ${parameters[3]})`;
      case 'gaussian':
        return `Gaussiana (μ=${parameters[0]}, σ=${parameters[1]})`;
      default:
        return `${function_type} (parámetros: ${parameters.join(', ')})`;
    }
  };

  return (
    <Card onPress={handlePress} style={styles.card}>
      <View style={styles.header}>
        <Text variant="h4" style={styles.title}>
          {variable.name}
        </Text>
        <Badge 
          variant={variable.variable_type === 'input' ? 'info' : 'success'} 
          size="sm"
        >
          {variable.variable_type === 'input' ? 'Entrada' : 'Salida'}
        </Badge>
      </View>

      <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
        {variable.description}
      </Text>

      {/* Información adicional */}
      <View style={styles.infoContainer}>
        <View style={styles.infoRow}>
          <Text variant="caption" color={semanticColors.textMuted}>
            Dispositivo:
          </Text>
          <Text variant="body" style={styles.infoValue}>
            {variable.device_id}
          </Text>
        </View>
        
        <View style={styles.infoRow}>
          <Text variant="caption" color={semanticColors.textMuted}>
            Términos:
          </Text>
          <Text variant="body" style={styles.infoValue}>
            {terms.length}
          </Text>
        </View>

        {terms.length > 0 && terms[0] && (
          <View style={styles.infoRow}>
            <Text variant="caption" color={semanticColors.textMuted}>
              Rango:
            </Text>
            <Text variant="body" style={styles.infoValue}>
              {formatRange(terms[0].membership_function)}
            </Text>
          </View>
        )}
      </View>

      {/* Términos lingüísticos */}
      {terms.length > 0 && (
        <View style={styles.termsSection}>
          <Text variant="body" style={styles.termsTitle}>
            Términos Lingüísticos
          </Text>
          
          <View style={styles.termsContainer}>
            {terms.map((term: any, index: number) => (
              <View key={term.id} style={styles.termItem}>
                <Text variant="body" style={styles.termLabel}>
                  {term.label}
                </Text>
                <Text variant="caption" color={semanticColors.textMuted} style={styles.termFunction}>
                  {describeMembershipFunction(term.membership_function)}
                </Text>
              </View>
            ))}
          </View>
        </View>
      )}

      {/* Gráfica de funciones de membresía */}
      {terms.length > 0 && (
        <View style={styles.chartSection}>
          <MembershipChart variable={variable} />
        </View>
      )}
    </Card>
  );
};

const styles = StyleSheet.create({
  card: {
    marginBottom: spacing.md,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.sm,
  },
  title: {
    flex: 1,
    fontWeight: '600',
    marginRight: spacing.sm,
  },
  description: {
    marginBottom: spacing.md,
    lineHeight: 20,
  },
  infoContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    padding: spacing.sm,
    borderRadius: 8,
    marginBottom: spacing.md,
    gap: spacing.xs,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  infoValue: {
    fontWeight: '500',
    fontSize: 14,
  },
  termsSection: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
    marginBottom: spacing.md,
  },
  termsTitle: {
    fontWeight: '600',
    marginBottom: spacing.sm,
    fontSize: 16,
  },
  termsContainer: {
    gap: spacing.sm,
  },
  termItem: {
    backgroundColor: semanticColors.backgroundSecondary,
    padding: spacing.sm,
    borderRadius: 8,
  },
  termLabel: {
    fontWeight: '600',
    marginBottom: spacing.xs,
    fontSize: 15,
  },
  termFunction: {
    fontSize: 13,
    lineHeight: 18,
  },
  chartSection: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },
});