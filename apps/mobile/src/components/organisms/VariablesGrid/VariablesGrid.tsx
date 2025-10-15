import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text } from '../../atoms';
import { MetricsGrid } from '../MetricsGrid';
import { semanticColors, spacing, typography, colors } from '@hydroespinaca/shared';
import type { MetricData } from '@hydroespinaca/shared';

export interface VariablesGridProps {
  aiVariables: MetricData[];
  manualVariables?: MetricData[];
  onVariablePress?: (metric: MetricData) => void;
  showSections?: boolean;
}

export const VariablesGrid: React.FC<VariablesGridProps> = ({
  aiVariables,
  manualVariables = [],
  onVariablePress,
  showSections = true
}) => {
  if (!showSections) {
    // Si no se muestran secciones, combinar todas las variables
    const allVariables = [...aiVariables, ...manualVariables];
    return (
      <MetricsGrid
        metrics={allVariables}
        {...(onVariablePress && { onVariablePress })}
      />
    );
  }

  return (
    <ScrollView 
      style={styles.container}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={styles.scrollContent}
    >
      {/* Sección Variables Controladas Mediante IA */}
      {aiVariables.length > 0 && (
        <View style={styles.section}>
          <View style={styles.sectionHeader}>
            <Text variant="h4" color="textPrimary" style={styles.sectionTitle}>
              Variables Controladas Mediante IA
            </Text>
          </View>
          <View style={styles.sectionContent}>
            {aiVariables.map((metric, index) => (
              <View key={metric.title} style={styles.variableCard}>
                <MetricsGrid
                  metrics={[metric]}
                  {...(onVariablePress && { onVariablePress })}
                />
              </View>
            ))}
          </View>
        </View>
      )}

      {/* Sección Variables Controladas Manualmente */}
      {manualVariables.length > 0 && (
        <View style={styles.section}>
          <View style={styles.sectionHeader}>
            <Text variant="h4" color="textPrimary" style={styles.sectionTitle}>
              Variables Controladas Manualmente
            </Text>
          </View>
          <View style={styles.sectionContent}>
            {manualVariables.map((metric, index) => (
              <View key={metric.title} style={styles.variableCard}>
                <MetricsGrid
                  metrics={[metric]}
                  {...(onVariablePress && { onVariablePress })}
                />
              </View>
            ))}
          </View>
        </View>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  scrollContent: {
    paddingBottom: spacing.xl,
  },
  section: {
    marginBottom: spacing.lg,
  },
  sectionHeader: {
    backgroundColor: colors.hidro.bgLight,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    marginBottom: spacing.sm,
    alignItems: 'center', // Centrar el contenido
  },
  sectionTitle: {
    fontWeight: '600',
    color: semanticColors.primary,
    textAlign: 'center', // Centrar el texto
  },
  sectionContent: {
    paddingHorizontal: spacing.md,
  },
  variableCard: {
    marginBottom: spacing.xs / 4,
  },
});