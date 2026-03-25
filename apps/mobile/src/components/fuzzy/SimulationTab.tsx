/**
 * SimulationTab — Tab de simulación del sistema fuzzy.
 * Sliders por variable de entrada + botón simular + cards de output.
 */
import React, { useState, useMemo } from 'react';
import { View, StyleSheet } from 'react-native';
import Slider from '@react-native-community/slider';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';
import { Card } from '../molecules/Card';
import { StatCard } from '../molecules/StatCard';
import { Alert } from '../molecules/Alert';
import { Spinner } from '../atoms/Spinner';
import type {
  FuzzySystemDetail,
  FuzzyVariable,
  SimulateInput,
  SimulateFuzzySystemResponse,
} from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, typography, useFuzzyStore } from '@hydroespinaca/shared';

export interface SimulationTabProps {
  detail: FuzzySystemDetail;
}

export function SimulationTab({ detail }: SimulationTabProps): React.ReactElement {
  const { variables } = detail;
  const systemId = detail.system.id;

  const {
    simulateSystem,
    simulationResult,
    simulationLoading,
    simulationError,
    clearSimulation,
  } = useFuzzyStore();

  const inputVars = useMemo(
    () => variables.filter((v) => v.variableType === 'input'),
    [variables],
  );

  // Initialize inputs with midpoints
  const [inputs, setInputs] = useState<Record<string, number>>(() => {
    const initial: Record<string, number> = {};
    inputVars.forEach((v) => {
      const min = v.universeMin ?? 0;
      const max = v.universeMax ?? 100;
      initial[v.referenceCode || v.id] = (min + max) / 2;
    });
    return initial;
  });

  const handleInputChange = (key: string, value: number) => {
    setInputs((prev) => ({ ...prev, [key]: Math.round(value * 100) / 100 }));
  };

  const handleSimulate = async () => {
    const simulateInputs: SimulateInput[] = inputVars.map((v) => ({
      referenceCode: v.referenceCode || v.id,
      value: inputs[v.referenceCode || v.id] ?? 0,
    }));

    await simulateSystem(systemId, { inputs: simulateInputs });
  };

  const handleReset = () => {
    clearSimulation();
    const initial: Record<string, number> = {};
    inputVars.forEach((v) => {
      const min = v.universeMin ?? 0;
      const max = v.universeMax ?? 100;
      initial[v.referenceCode || v.id] = (min + max) / 2;
    });
    setInputs(initial);
  };

  const getStrengthColor = (strength: number): string => {
    if (strength > 0.5) return colors.hidro[600];
    if (strength > 0.2) return colors.warning[600];
    return colors.gray[500];
  };

  return (
    <View style={styles.container}>
      {/* Input Sliders */}
      <Card variant="outlined" padding="md">
        <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
          Variables de Entrada
        </Text>
        {inputVars.length === 0 ? (
          <Text variant="caption" color={semanticColors.textTertiary}>
            No hay variables de entrada definidas
          </Text>
        ) : (
          inputVars.map((v: FuzzyVariable) => {
            const key = v.referenceCode || v.id;
            const min = v.universeMin ?? 0;
            const max = v.universeMax ?? 100;
            const value = inputs[key] ?? (min + max) / 2;

            return (
              <View key={v.id} style={styles.sliderContainer}>
                <View style={styles.sliderHeader}>
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.sliderLabel}>
                    {v.name}
                  </Text>
                  <Text variant="label" color={semanticColors.primary}>
                    {value.toFixed(1)}
                  </Text>
                </View>
                <Slider
                  value={value}
                  minimumValue={min}
                  maximumValue={max}
                  step={(max - min) / 100}
                  onValueChange={(val) => handleInputChange(key, val)}
                  minimumTrackTintColor={colors.hidro[500]}
                  maximumTrackTintColor={colors.gray[300]}
                  thumbTintColor={colors.hidro[600]}
                  style={styles.slider}
                />
                <View style={styles.sliderRange}>
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {min}
                  </Text>
                  {v.referenceCode && (
                    <Text variant="caption" color={semanticColors.textTertiary}>
                      {v.referenceCode}
                    </Text>
                  )}
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {max}
                  </Text>
                </View>
              </View>
            );
          })
        )}
      </Card>

      {/* Action Buttons */}
      <View style={styles.actionButtons}>
        <Button
          variant="primary"
          onPress={handleSimulate}
          loading={simulationLoading}
          disabled={inputVars.length === 0}
          fullWidth
          leftIcon={<Icon name="play" size={18} color={semanticColors.textInverse} />}
        >
          Simular
        </Button>
        {simulationResult && (
          <Button variant="outline" onPress={handleReset} fullWidth>
            Reiniciar
          </Button>
        )}
      </View>

      {/* Error */}
      {simulationError && (
        <Alert type="error" message={simulationError} />
      )}

      {/* Loading */}
      {simulationLoading && (
        <View style={styles.loadingContainer}>
          <Spinner size="lg" color={semanticColors.primary} />
          <Text variant="caption" color={semanticColors.textSecondary}>
            Simulando...
          </Text>
        </View>
      )}

      {/* Results */}
      {simulationResult && !simulationLoading && (
        <View style={styles.results}>
          {/* Final Outputs */}
          <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
            Salidas
          </Text>
          <View style={styles.outputGrid}>
            {simulationResult.finalOutputs.map((output) => (
              <StatCard
                key={output.referenceCode}
                label={output.variableName}
                value={output.crispValue.toFixed(2)}
                icon="arrow-up"
                iconColor={colors.hidro[500]}
                style={styles.outputCard}
              />
            ))}
          </View>

          {/* Activated Rules */}
          {simulationResult.activatedRules.length > 0 && (
            <>
              <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
                Reglas Activadas ({simulationResult.activatedRules.length})
              </Text>
              {simulationResult.activatedRules.map((rule) => (
                <Card key={rule.ruleId} variant="outlined" padding="sm">
                  <View style={styles.ruleRow}>
                    <Text variant="body" color={semanticColors.textPrimary} numberOfLines={1} style={styles.ruleNameText}>
                      {rule.ruleName}
                    </Text>
                    <View style={[styles.strengthBadge, { backgroundColor: `${getStrengthColor(rule.firingStrength)}15` }]}>
                      <Text variant="caption" color={getStrengthColor(rule.firingStrength)}>
                        {(rule.firingStrength * 100).toFixed(0)}%
                      </Text>
                    </View>
                  </View>
                </Card>
              ))}
            </>
          )}

          {/* Timestamp */}
          <Text variant="caption" color={semanticColors.textTertiary} align="center" style={styles.timestamp}>
            Simulado: {new Date(simulationResult.simulatedAt).toLocaleString('es-CO', {
              timeZone: 'America/Bogota',
              dateStyle: 'short',
              timeStyle: 'short',
            })}
          </Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  sliderContainer: {
    marginBottom: spacing.md,
  },
  sliderHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  sliderLabel: {
    fontWeight: typography.fontWeight.medium,
  },
  slider: {
    height: 40,
  },
  sliderRange: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  actionButtons: {
    gap: spacing.sm,
  },
  loadingContainer: {
    alignItems: 'center',
    padding: spacing.lg,
    gap: spacing.sm,
  },
  results: {
    gap: spacing.sm,
  },
  outputGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
  },
  outputCard: {
    flex: 1,
    minWidth: 140,
  },
  ruleRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  ruleNameText: {
    fontWeight: typography.fontWeight.medium,
    flex: 1,
    marginRight: spacing.sm,
  },
  strengthBadge: {
    paddingVertical: 2,
    paddingHorizontal: spacing.sm,
    borderRadius: 10,
  },
  timestamp: {
    marginTop: spacing.sm,
  },
});
