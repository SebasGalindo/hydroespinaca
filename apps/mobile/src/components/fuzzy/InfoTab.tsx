/**
 * InfoTab — Tab de resumen del sistema fuzzy.
 * Cards de variables entrada/salida, preview de reglas, operadores.
 */
import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Card } from '../molecules/Card';
import { StatCard } from '../molecules/StatCard';
import type { FuzzySystemDetail, FuzzyVariable, FuzzyRule, IconName } from '@hydroespinaca/shared';
import {
  colors,
  spacing,
  semanticColors,
  typography,
  DEFUZZIFICATION_METHODS,
  AND_METHODS,
  OR_METHODS,
  NOT_METHODS,
} from '@hydroespinaca/shared';
import { generateRuleText } from '../../utils/fuzzyHelpers';

export interface InfoTabProps {
  detail: FuzzySystemDetail;
}

export function InfoTab({ detail }: InfoTabProps): React.ReactElement {
  const { system, variables, terms, rules } = detail;

  const inputVars = variables.filter((v) => v.variableType === 'input');
  const outputVars = variables.filter((v) => v.variableType === 'output');

  const getMethodLabel = (value: string, options: readonly { value: string; label: string }[]): string =>
    options.find((o) => o.value === value)?.label ?? value;

  return (
    <View style={styles.container}>
      {/* Stats Row */}
      <View style={styles.statsRow}>
        <StatCard
          label="Entradas"
          value={inputVars.length}
          icon="arrow-down"
          iconColor={colors.info[500]}
          style={styles.statCard}
        />
        <StatCard
          label="Salidas"
          value={outputVars.length}
          icon="arrow-up"
          iconColor={colors.hidro[500]}
          style={styles.statCard}
        />
        <StatCard
          label="Reglas"
          value={rules.length}
          icon="git-branch"
          iconColor={colors.warning[500]}
          style={styles.statCard}
        />
      </View>

      {/* Operators Card */}
      <Card variant="outlined" padding="md" style={styles.section}>
        <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
          Configuración
        </Text>
        <View style={styles.operatorRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>Defuzzificación:</Text>
          <Text variant="caption" color={semanticColors.textPrimary}>
            {getMethodLabel(system.defuzzificationMethod, DEFUZZIFICATION_METHODS)}
          </Text>
        </View>
        <View style={styles.operatorRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>AND:</Text>
          <Text variant="caption" color={semanticColors.textPrimary}>
            {getMethodLabel(system.operators?.andMethod || 'min', AND_METHODS)}
          </Text>
        </View>
        <View style={styles.operatorRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>OR:</Text>
          <Text variant="caption" color={semanticColors.textPrimary}>
            {getMethodLabel(system.operators?.orMethod || 'max', OR_METHODS)}
          </Text>
        </View>
        <View style={styles.operatorRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>NOT:</Text>
          <Text variant="caption" color={semanticColors.textPrimary}>
            {getMethodLabel(system.operators?.notMethod || 'complement', NOT_METHODS)}
          </Text>
        </View>
      </Card>

      {/* Variables Summary */}
      {inputVars.length > 0 && (
        <Card variant="outlined" padding="md" style={styles.section}>
          <View style={styles.sectionHeader}>
            <Icon name="arrow-down" size={16} color={colors.info[500]} />
            <Text variant="label" color={semanticColors.textPrimary}>
              Variables de Entrada
            </Text>
          </View>
          {inputVars.map((v: FuzzyVariable) => {
            const varTerms = terms.filter((t) => t.variableId === v.id);
            return (
              <View key={v.id} style={styles.varRow}>
                <Text variant="body" color={semanticColors.textPrimary} numberOfLines={1} style={styles.varName}>
                  {v.name}
                </Text>
                <Text variant="caption" color={semanticColors.textSecondary}>
                  [{v.universeMin ?? 0} - {v.universeMax ?? 100}] · {varTerms.length} término{varTerms.length !== 1 ? 's' : ''}
                </Text>
              </View>
            );
          })}
        </Card>
      )}

      {outputVars.length > 0 && (
        <Card variant="outlined" padding="md" style={styles.section}>
          <View style={styles.sectionHeader}>
            <Icon name="arrow-up" size={16} color={colors.hidro[500]} />
            <Text variant="label" color={semanticColors.textPrimary}>
              Variables de Salida
            </Text>
          </View>
          {outputVars.map((v: FuzzyVariable) => {
            const varTerms = terms.filter((t) => t.variableId === v.id);
            return (
              <View key={v.id} style={styles.varRow}>
                <Text variant="body" color={semanticColors.textPrimary} numberOfLines={1} style={styles.varName}>
                  {v.name}
                </Text>
                <Text variant="caption" color={semanticColors.textSecondary}>
                  [{v.universeMin ?? 0} - {v.universeMax ?? 100}] · {varTerms.length} término{varTerms.length !== 1 ? 's' : ''}
                  {v.actuatorType ? ` · ${v.actuatorType}` : ''}
                </Text>
              </View>
            );
          })}
        </Card>
      )}

      {/* Rules Preview */}
      {rules.length > 0 && (
        <Card variant="outlined" padding="md" style={styles.section}>
          <View style={styles.sectionHeader}>
            <Icon name="git-branch" size={16} color={colors.warning[500]} />
            <Text variant="label" color={semanticColors.textPrimary}>
              Reglas ({rules.length})
            </Text>
          </View>
          {rules.slice(0, 5).map((r: FuzzyRule) => (
            <View key={r.id} style={styles.rulePreview}>
              <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={2}>
                {r.ruleText || generateRuleText(r, variables, terms)}
              </Text>
            </View>
          ))}
          {rules.length > 5 && (
            <Text variant="caption" color={semanticColors.primary} style={styles.moreRules}>
              + {rules.length - 5} regla{rules.length - 5 !== 1 ? 's' : ''} más
            </Text>
          )}
        </Card>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  statsRow: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  statCard: {
    flex: 1,
  },
  section: {
    gap: spacing.xs,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginBottom: spacing.xs,
  },
  operatorRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 2,
  },
  varRow: {
    paddingVertical: spacing.xs,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.gray[100],
  },
  varName: {
    fontWeight: typography.fontWeight.medium,
    marginBottom: 2,
  },
  rulePreview: {
    paddingVertical: spacing.xs,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.gray[100],
  },
  moreRules: {
    marginTop: spacing.xs,
    textAlign: 'center',
  },
});
