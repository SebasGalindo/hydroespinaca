/**
 * EvaluationHistoryTab — Historial de evaluaciones fuzzy persistidas (RF-F07).
 * Muestra estadísticas, conteo diario y un listado expandible de inferencias.
 */
import React, { useEffect, useMemo, useState } from 'react';
import { View, StyleSheet, Pressable, ScrollView } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Spinner } from '../atoms/Spinner';
import { ProgressBar } from '../atoms/ProgressBar';
import { Card } from '../molecules/Card';
import { StatCard } from '../molecules/StatCard';
import { Alert } from '../molecules/Alert';
import { Button } from '../atoms/Button';
import type {
  FuzzySystemDetail,
  FuzzyEvaluation,
  FuzzyRule,
  FuzzyVariable,
} from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, typography, borderRadius, useFuzzyStore } from '@hydroespinaca/shared';

export interface EvaluationHistoryTabProps {
  detail: FuzzySystemDetail;
}

const HOUR_OPTIONS: { hours: number; label: string }[] = [
  { hours: 24, label: '24h' },
  { hours: 72, label: '3 días' },
  { hours: 168, label: '7 días' },
];

const PAGE_SIZE = 10;

export function EvaluationHistoryTab({ detail }: EvaluationHistoryTabProps): React.ReactElement {
  const systemId = detail.system.id;
  const variables = detail.variables;
  const rules = detail.rules;

  const {
    evaluations,
    evaluationsLoading,
    evaluationsError,
    evaluationsTotalCount,
    evaluationsPage,
    evaluationsTotalPages,
    evaluationsHasNext,
    evaluationsHasPrevious,
    evaluationStats,
    evaluationStatsLoading,
    evaluationStatsError,
    fetchEvaluations,
    fetchEvaluationStats,
    clearEvaluations,
  } = useFuzzyStore();

  const [selectedHours, setSelectedHours] = useState<number>(24);
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const days = Math.max(1, Math.ceil(selectedHours / 24));

  useEffect(() => {
    setPage(1);
  }, [selectedHours, systemId]);

  useEffect(() => {
    const startDate = new Date(Date.now() - selectedHours * 60 * 60 * 1000).toISOString();
    fetchEvaluations({
      systemId,
      startDate,
      page,
      pageSize: PAGE_SIZE,
      sortBy: 'timestamp',
      sortOrder: 'desc',
    });
    fetchEvaluationStats(systemId, days);
  }, [systemId, selectedHours, page, days, fetchEvaluations, fetchEvaluationStats]);

  useEffect(() => {
    return () => {
      clearEvaluations();
    };
  }, [clearEvaluations]);

  const variableBySensorId = useMemo(() => {
    const map = new Map<string, FuzzyVariable>();
    variables.forEach((v) => {
      if (v.referenceCode) map.set(v.referenceCode, v);
      map.set(v.id, v);
    });
    return map;
  }, [variables]);

  const ruleById = useMemo(() => {
    const map = new Map<string, FuzzyRule>();
    rules.forEach((r) => map.set(r.id, r));
    return map;
  }, [rules]);

  const dailyTotals = useMemo(() => {
    if (!evaluationStats) return [] as { date: string; count: number }[];
    return Object.entries(evaluationStats.dailyStats)
      .map(([date, count]) => ({ date, count }))
      .sort((a, b) => a.date.localeCompare(b.date))
      .slice(-7);
  }, [evaluationStats]);

  const maxDaily = useMemo(
    () => dailyTotals.reduce((max, d) => Math.max(max, d.count), 0) || 1,
    [dailyTotals],
  );

  const avgRulesFired = useMemo(() => {
    if (evaluations.length === 0) return 0;
    const total = evaluations.reduce((acc, e) => acc + e.activatedRules.length, 0);
    return total / evaluations.length;
  }, [evaluations]);

  const formatTimestamp = (iso: string | null): string => {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('es-CO', {
      timeZone: 'America/Bogota',
      dateStyle: 'short',
      timeStyle: 'short',
    });
  };

  const getStrengthColor = (strength: number): string => {
    if (strength > 0.5) return colors.hidro[600];
    if (strength > 0.2) return colors.warning[600];
    return colors.gray[500];
  };

  const renderEvaluation = (evaluation: FuzzyEvaluation) => {
    const id = evaluation.id ?? `${evaluation.systemId}-${evaluation.timestamp}`;
    const isExpanded = expandedId === id;
    const inputCount = evaluation.inputs.length;
    const ruleCount = evaluation.activatedRules.length;
    const topRule = [...evaluation.activatedRules].sort(
      (a, b) => b.firingStrength - a.firingStrength,
    )[0];

    return (
      <Card key={id} variant="outlined" padding="none" style={styles.evalCard}>
        <Pressable
          onPress={() => setExpandedId(isExpanded ? null : id)}
          style={styles.evalHeader}
          accessibilityRole="button"
          accessibilityState={{ expanded: isExpanded }}
        >
          <View style={styles.evalHeaderLeft}>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.evalTimestamp}>
              {formatTimestamp(evaluation.timestamp)}
            </Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {inputCount} entrada{inputCount !== 1 ? 's' : ''} · {ruleCount} regla{ruleCount !== 1 ? 's' : ''}
            </Text>
          </View>
          <View style={styles.evalHeaderRight}>
            {topRule && (
              <View style={[styles.maxBadge, { backgroundColor: `${getStrengthColor(topRule.firingStrength)}15` }]}>
                <Text variant="caption" color={getStrengthColor(topRule.firingStrength)}>
                  máx {(topRule.firingStrength * 100).toFixed(0)}%
                </Text>
              </View>
            )}
            <Icon
              name={isExpanded ? 'chevron-up' : 'chevron-down'}
              size={16}
              color={semanticColors.textTertiary}
            />
          </View>
        </Pressable>

        {isExpanded && (
          <View style={styles.evalBody}>
            {/* Inputs */}
            <Text variant="caption" color={semanticColors.textSecondary} style={styles.subTitle}>
              Entradas
            </Text>
            <View style={styles.inputsList}>
              {evaluation.inputs.map((input, i) => {
                const variable = variableBySensorId.get(input.sensorId);
                return (
                  <View key={`${input.sensorId}-${i}`} style={styles.inputRow}>
                    <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={1} style={styles.inputName}>
                      {variable?.name ?? input.sensorId}
                    </Text>
                    <Text variant="label" color={semanticColors.textPrimary}>
                      {input.value.toFixed(2)}
                    </Text>
                  </View>
                );
              })}
            </View>

            {/* Activated rules */}
            {evaluation.activatedRules.length > 0 && (
              <>
                <Text variant="caption" color={semanticColors.textSecondary} style={styles.subTitle}>
                  Reglas activadas
                </Text>
                {[...evaluation.activatedRules]
                  .sort((a, b) => b.firingStrength - a.firingStrength)
                  .map((activation, i) => {
                    const rule = ruleById.get(activation.ruleId);
                    const pct = activation.firingStrength * 100;
                    return (
                      <View key={`${activation.ruleId}-${i}`} style={styles.ruleRow}>
                        <View style={styles.ruleHeader}>
                          <Text variant="caption" color={semanticColors.textPrimary} numberOfLines={1} style={styles.ruleName}>
                            {rule?.name ?? `Regla ${activation.ruleId.slice(0, 8)}…`}
                          </Text>
                          <Text variant="caption" color={semanticColors.textSecondary}>
                            {pct.toFixed(0)}%
                          </Text>
                        </View>
                        <ProgressBar
                          value={activation.firingStrength}
                          color={getStrengthColor(activation.firingStrength)}
                          height={4}
                        />
                      </View>
                    );
                  })}
              </>
            )}
          </View>
        )}
      </Card>
    );
  };

  return (
    <View style={styles.container}>
      {/* Range chips */}
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.chipsRow}
      >
        {HOUR_OPTIONS.map((opt) => {
          const active = selectedHours === opt.hours;
          return (
            <Pressable
              key={opt.hours}
              onPress={() => setSelectedHours(opt.hours)}
              style={[styles.chip, active && styles.chipActive]}
            >
              <Text
                variant="caption"
                color={active ? colors.white : semanticColors.textSecondary}
                style={active ? styles.chipLabelActive : undefined}
              >
                {opt.label}
              </Text>
            </Pressable>
          );
        })}
      </ScrollView>

      {/* Stats */}
      <View style={styles.statsRow}>
        <StatCard
          label="Total"
          value={evaluationStats?.totalEvaluations ?? (evaluationStatsLoading ? '…' : 0)}
          icon="bar-chart"
          iconColor={colors.hidro[500]}
          style={styles.statCard}
        />
        <StatCard
          label="Promedio/día"
          value={
            evaluationStats
              ? evaluationStats.avgEvaluationsPerDay.toFixed(1)
              : evaluationStatsLoading
                ? '…'
                : '0'
          }
          icon="trending-up"
          iconColor={colors.info[500]}
          style={styles.statCard}
        />
        <StatCard
          label="Reglas/inf."
          value={evaluations.length > 0 ? avgRulesFired.toFixed(1) : '0'}
          icon="git-branch"
          iconColor={colors.warning[500]}
          style={styles.statCard}
        />
      </View>

      {/* Daily mini-chart */}
      {dailyTotals.length > 0 && (
        <Card variant="outlined" padding="md">
          <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
            Evaluaciones por día
          </Text>
          <View style={styles.barChart}>
            {dailyTotals.map((d) => {
              const heightPct = (d.count / maxDaily) * 100;
              const day = d.date.slice(5);
              return (
                <View key={d.date} style={styles.barColumn}>
                  <View style={styles.barWrapper}>
                    <View
                      style={[
                        styles.bar,
                        { height: `${Math.max(6, heightPct)}%` },
                      ]}
                    />
                  </View>
                  <Text variant="caption" color={semanticColors.textTertiary} style={styles.barLabel}>
                    {day}
                  </Text>
                  <Text variant="caption" color={semanticColors.textPrimary} style={styles.barValue}>
                    {d.count}
                  </Text>
                </View>
              );
            })}
          </View>
        </Card>
      )}

      {evaluationStatsError && <Alert type="error" message={evaluationStatsError} />}

      {/* List */}
      <Card variant="outlined" padding="md">
        <View style={styles.listHeader}>
          <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
            Inferencias recientes
          </Text>
          {evaluationsTotalCount > 0 && (
            <Text variant="caption" color={semanticColors.textSecondary}>
              {evaluationsTotalCount} en total
            </Text>
          )}
        </View>

        {evaluationsLoading && evaluations.length === 0 ? (
          <View style={styles.loading}>
            <Spinner size="md" color={semanticColors.primary} />
            <Text variant="caption" color={semanticColors.textSecondary}>
              Cargando historial…
            </Text>
          </View>
        ) : evaluationsError ? (
          <Alert type="error" message={evaluationsError} />
        ) : evaluations.length === 0 ? (
          <View style={styles.emptyState}>
            <Icon name="file-text" size={32} color={semanticColors.textTertiary} />
            <Text variant="caption" color={semanticColors.textSecondary} align="center" style={styles.emptyText}>
              No hay evaluaciones registradas en esta ventana.
            </Text>
          </View>
        ) : (
          <View style={styles.evalList}>{evaluations.map(renderEvaluation)}</View>
        )}

        {evaluationsTotalPages > 1 && (
          <View style={styles.pagination}>
            <Button
              variant="outline"
              size="sm"
              onPress={() => setPage((p) => Math.max(1, p - 1))}
              disabled={!evaluationsHasPrevious || evaluationsLoading}
            >
              Anterior
            </Button>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {evaluationsPage} / {evaluationsTotalPages}
            </Text>
            <Button
              variant="outline"
              size="sm"
              onPress={() => setPage((p) => p + 1)}
              disabled={!evaluationsHasNext || evaluationsLoading}
            >
              Siguiente
            </Button>
          </View>
        )}
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  chipsRow: {
    gap: spacing.sm,
    paddingVertical: spacing.xs,
  },
  chip: {
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.md,
    borderRadius: 999,
    backgroundColor: colors.white,
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  chipActive: {
    backgroundColor: semanticColors.primary,
    borderColor: semanticColors.primary,
  },
  chipLabelActive: {
    fontWeight: typography.fontWeight.semibold,
  },
  statsRow: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  statCard: {
    flex: 1,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  barChart: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.xs,
    height: 100,
    marginTop: spacing.xs,
  },
  barColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  barWrapper: {
    flex: 1,
    width: '100%',
    justifyContent: 'flex-end',
  },
  bar: {
    width: '100%',
    backgroundColor: colors.hidro[400],
    borderTopLeftRadius: borderRadius.sm,
    borderTopRightRadius: borderRadius.sm,
  },
  barLabel: {
    fontSize: 10,
  },
  barValue: {
    fontSize: 10,
    fontWeight: typography.fontWeight.semibold,
  },
  listHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  loading: {
    alignItems: 'center',
    padding: spacing.lg,
    gap: spacing.sm,
  },
  emptyState: {
    alignItems: 'center',
    padding: spacing.lg,
    gap: spacing.sm,
  },
  emptyText: {
    paddingHorizontal: spacing.md,
  },
  evalList: {
    gap: spacing.sm,
  },
  evalCard: {
    overflow: 'hidden',
  },
  evalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: spacing.md,
  },
  evalHeaderLeft: {
    flex: 1,
    gap: 2,
  },
  evalHeaderRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  evalTimestamp: {
    fontWeight: typography.fontWeight.medium,
  },
  maxBadge: {
    paddingVertical: 2,
    paddingHorizontal: spacing.sm,
    borderRadius: 10,
  },
  evalBody: {
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.md,
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.gray[200],
    backgroundColor: colors.gray[50],
  },
  subTitle: {
    fontWeight: typography.fontWeight.semibold,
    textTransform: 'uppercase',
    fontSize: 10,
    marginTop: spacing.sm,
  },
  inputsList: {
    gap: spacing.xs,
  },
  inputRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.sm,
    backgroundColor: colors.white,
    borderRadius: borderRadius.sm,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.gray[200],
  },
  inputName: {
    flex: 1,
    marginRight: spacing.sm,
  },
  ruleRow: {
    backgroundColor: colors.white,
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.sm,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.gray[200],
    gap: spacing.xs,
  },
  ruleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  ruleName: {
    flex: 1,
    marginRight: spacing.sm,
    fontWeight: typography.fontWeight.medium,
  },
  pagination: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: spacing.md,
    paddingTop: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.gray[200],
  },
});
