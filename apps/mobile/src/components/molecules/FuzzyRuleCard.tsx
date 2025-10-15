import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import type { SimpleFuzzyRule } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Card } from './Card';

export interface FuzzyRuleCardProps {
  rule: SimpleFuzzyRule;
  onPress?: (rule: SimpleFuzzyRule) => void;
}

export const FuzzyRuleCard: React.FC<FuzzyRuleCardProps> = ({
  rule,
  onPress,
}) => {
  const handlePress = () => {
    onPress?.(rule);
  };

  return (
    <Card onPress={handlePress} style={styles.card}>
      <View style={styles.content}>
        <Text 
          variant="body" 
          color={semanticColors.textSecondary}
          style={styles.description}
        >
          {rule.description}
        </Text>
      </View>
    </Card>
  );
};

const styles = StyleSheet.create({
  card: {
    marginBottom: spacing.md,
  },
  content: {
    gap: spacing.sm,
  },
  description: {
    lineHeight: 20,
  },
  consequenceContainer: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },
  consequence: {
    fontWeight: '500',
    marginTop: spacing.xs,
    color: semanticColors.primary,
  },
});