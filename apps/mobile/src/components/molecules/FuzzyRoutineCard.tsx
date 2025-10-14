import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import type { SimpleFuzzyRoutine } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { Card } from './Card';

export interface FuzzyRoutineCardProps {
  routine: SimpleFuzzyRoutine;
  onPress?: (routine: SimpleFuzzyRoutine) => void;
}

export const FuzzyRoutineCard: React.FC<FuzzyRoutineCardProps> = ({
  routine,
  onPress,
}) => {
  const handlePress = () => {
    onPress?.(routine);
  };

  return (
    <Card onPress={handlePress} style={styles.card}>
      <View style={styles.header}>
        <Text variant="h4" style={styles.title}>
          {routine.routine_name}
        </Text>
      </View>

      {routine.steps && routine.steps.length > 0 && (
        <View style={styles.stepsContainer}>
          <Text variant="body" color={semanticColors.textMuted} style={styles.stepsLabel}>
            Pasos ({routine.steps.length}):
          </Text>
          
          {routine.steps.map((step, index) => (
            <View key={index} style={styles.stepItem}>
              <View style={styles.stepHeader}>
                <Badge variant="info" size="sm">
                  Paso {index + 1}
                </Badge>
              </View>
              
              <View style={styles.stepDetails}>
                <View style={styles.stepRow}>
                  <Text variant="body" color={semanticColors.textMuted} style={styles.stepLabel}>
                    Potencia:
                  </Text>
                  <Text variant="body" style={styles.stepValue}>
                    {step.power_term_id}
                  </Text>
                </View>
                
                <View style={styles.stepRow}>
                  <Text variant="body" color={semanticColors.textMuted} style={styles.stepLabel}>
                    Duración:
                  </Text>
                  <Text variant="body" style={styles.stepValue}>
                    {step.duration_term_id}
                  </Text>
                </View>
              </View>
            </View>
          ))}
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
    marginBottom: spacing.sm,
  },
  title: {
    fontWeight: '600',
    fontSize: 18,
  },
  description: {
    marginBottom: spacing.md,
  },
  stepsContainer: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },
  stepsLabel: {
    marginBottom: spacing.sm,
    fontWeight: '600',
    fontSize: 16,
  },
  stepItem: {
    marginBottom: spacing.md,
    padding: spacing.md,
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
  },
  stepHeader: {
    marginBottom: spacing.sm,
  },
  stepDetails: {
    gap: spacing.sm,
  },
  stepRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  stepLabel: {
    fontSize: 15,
    fontWeight: '500',
  },
  stepValue: {
    fontWeight: '600',
    fontSize: 15,
  },
});