'use client';

import React, { useEffect, useMemo, useState } from 'react';
import BaseCard from '@/components/ui/BaseCard';
import Button from '@/components/ui/Button';
import StatCard from '@/components/ui/StatCard';
import Badge from '@/components/ui/Badge';
import {
  useFuzzyStore,
  type FuzzySystemDetail,
  type FuzzyEvaluation,
  type FuzzyEvaluationListParams,
  type FuzzyRule,
  type FuzzyVariable,
} from '@hydroespinaca/shared';

interface EvaluationHistorySectionProps {
  detail: FuzzySystemDetail;
}

// `hours: null` means "all" (no date filter). `statsDays` is what we pass to the
// stats endpoint — it accepts a positive integer, so for "todas" we send a large value.
type WindowOption = { hours: number | null; label: string; statsDays: number };

const HOUR_OPTIONS: WindowOption[] = [
  { hours: 24, label: 'Últimas 24 h', statsDays: 1 },
  { hours: 72, label: 'Últimos 3 días', statsDays: 3 },
  { hours: 168, label: 'Últimos 7 días', statsDays: 7 },
  { hours: 720, label: 'Último mes', statsDays: 30 },
  { hours: null, label: 'Todas', statsDays: 365 },
];

const PAGE_SIZE = 10;
const MAX_CHART_DAYS = 30;

const EvaluationHistorySection = React.memo(function EvaluationHistorySection({
  detail,
}: EvaluationHistorySectionProps) {
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

  const [selectedHours, setSelectedHours] = useState<number | null>(24);
  const [page, setPage] = useState(1);
  const [expanded, setExpanded] = useState<string | null>(null);

  const statsDays = useMemo(
    () => HOUR_OPTIONS.find((o) => o.hours === selectedHours)?.statsDays ?? 7,
    [selectedHours],
  );

  // Initial + filter changes
  useEffect(() => {
    setPage(1);
  }, [selectedHours, systemId]);

  useEffect(() => {
    const params: FuzzyEvaluationListParams = {
      systemId,
      page,
      pageSize: PAGE_SIZE,
      sortBy: 'timestamp',
      sortOrder: 'desc',
    };
    // Only send startDate when a finite window is selected. For "Todas" we
    // omit it so the backend returns all evaluations for the system.
    if (selectedHours !== null) {
      params.startDate = new Date(Date.now() - selectedHours * 60 * 60 * 1000).toISOString();
    }
    fetchEvaluations(params);
    fetchEvaluationStats(systemId, statsDays);
  }, [systemId, selectedHours, page, statsDays, fetchEvaluations, fetchEvaluationStats]);

  useEffect(() => {
    return () => {
      clearEvaluations();
    };
  }, [clearEvaluations]);

  // Derived: variables and rules indexed for quick lookup
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

    let entries = Object.entries(evaluationStats.dailyStats)
      .map(([date, count]) => ({ date, count }))
      .sort((a, b) => a.date.localeCompare(b.date));

    // Restrict the chart to days inside the selected window so it doesn't show
    // months-old buckets that the backend may include.
    if (selectedHours !== null) {
      const cutoff = new Date(Date.now() - selectedHours * 60 * 60 * 1000)
        .toISOString()
        .slice(0, 10);
      entries = entries.filter((e) => e.date >= cutoff);
    }

    // Cap at 30 buckets so the bars stay readable for long windows.
    return entries.slice(-MAX_CHART_DAYS);
  }, [evaluationStats, selectedHours]);

  const maxDaily = useMemo(
    () => dailyTotals.reduce((max, d) => Math.max(max, d.count), 0) || 1,
    [dailyTotals],
  );

  const avgRulesFired = useMemo(() => {
    if (evaluations.length === 0) return 0;
    const total = evaluations.reduce((acc, e) => acc + e.activatedRules.length, 0);
    return total / evaluations.length;
  }, [evaluations]);

  // ─── Render ─────────────────────────────────────────────

  const formatTimestamp = (iso: string | null): string => {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('es-CO', {
      timeZone: 'America/Bogota',
      dateStyle: 'short',
      timeStyle: 'medium',
    });
  };

  const renderEvaluationCard = (evaluation: FuzzyEvaluation) => {
    const isExpanded = expanded === evaluation.id;
    const inputCount = evaluation.inputs.length;
    const ruleCount = evaluation.activatedRules.length;
    const topRule = [...evaluation.activatedRules].sort(
      (a, b) => b.firingStrength - a.firingStrength,
    )[0];

    return (
      <div
        key={evaluation.id ?? `${evaluation.systemId}-${evaluation.timestamp}`}
        className="bg-white rounded-lg border border-gray-200 overflow-hidden"
      >
        <button
          type="button"
          onClick={() => setExpanded(isExpanded ? null : evaluation.id)}
          className="w-full text-left px-4 py-3 hover:bg-gray-50 transition-colors"
          aria-expanded={isExpanded}
        >
          <div className="flex items-center justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-gray-900 font-inter">
                {formatTimestamp(evaluation.timestamp)}
              </p>
              <p className="text-xs text-gray-500 font-inter mt-0.5">
                {inputCount} entrada{inputCount !== 1 ? 's' : ''} · {ruleCount} regla{ruleCount !== 1 ? 's' : ''} activada{ruleCount !== 1 ? 's' : ''}
              </p>
            </div>
            <div className="flex items-center gap-2 flex-shrink-0">
              {topRule && (
                <Badge variant={topRule.firingStrength > 0.5 ? 'success' : 'warning'} size="sm">
                  máx {(topRule.firingStrength * 100).toFixed(0)}%
                </Badge>
              )}
              <span className="text-gray-400 text-sm">{isExpanded ? '▴' : '▾'}</span>
            </div>
          </div>
        </button>

        {isExpanded && (
          <div className="px-4 pb-4 pt-2 bg-gray-50 border-t border-gray-100 space-y-3">
            {/* Inputs */}
            <div>
              <p className="text-xs font-semibold text-gray-600 uppercase tracking-wider mb-2 font-inter">
                Entradas
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {evaluation.inputs.map((input, i) => {
                  const variable = variableBySensorId.get(input.sensorId);
                  return (
                    <div
                      key={`${input.sensorId}-${i}`}
                      className="flex items-center justify-between bg-white rounded px-3 py-2 border border-gray-200"
                    >
                      <span className="text-xs text-gray-600 font-inter truncate">
                        {variable?.name ?? input.sensorId}
                      </span>
                      <span className="text-sm font-mono font-semibold text-gray-900">
                        {input.value.toFixed(2)}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Activated rules */}
            {evaluation.activatedRules.length > 0 && (
              <div>
                <p className="text-xs font-semibold text-gray-600 uppercase tracking-wider mb-2 font-inter">
                  Reglas activadas
                </p>
                <div className="space-y-1.5">
                  {evaluation.activatedRules
                    .slice()
                    .sort((a, b) => b.firingStrength - a.firingStrength)
                    .map((activation, i) => {
                      const rule = ruleById.get(activation.ruleId);
                      const pct = activation.firingStrength * 100;
                      const color =
                        activation.firingStrength > 0.5
                          ? 'bg-green-500'
                          : activation.firingStrength > 0.2
                            ? 'bg-amber-400'
                            : 'bg-gray-300';
                      return (
                        <div
                          key={`${activation.ruleId}-${i}`}
                          className="bg-white rounded px-3 py-2 border border-gray-200"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-xs font-medium text-gray-800 font-inter truncate">
                              {rule?.name ?? `Regla ${activation.ruleId.slice(0, 8)}…`}
                            </span>
                            <span className="text-xs font-mono text-gray-700 flex-shrink-0">
                              {pct.toFixed(1)}%
                            </span>
                          </div>
                          <div className="mt-1.5 h-1.5 bg-gray-100 rounded-full overflow-hidden">
                            <div
                              className={`h-full ${color} rounded-full transition-all`}
                              style={{ width: `${Math.min(100, pct)}%` }}
                            />
                          </div>
                        </div>
                      );
                    })}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <div className="border-b border-gray-200 pb-4">
        <h2 className="text-xl font-semibold text-gray-900 font-inter">Historial de evaluaciones</h2>
        <p className="text-sm text-gray-600 font-inter mt-1">
          Inferencias persistidas del motor fuzzy para este sistema, con sus entradas y reglas disparadas.
        </p>
      </div>

      {/* Range selector */}
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-xs text-gray-500 font-inter mr-1">Ventana:</span>
        {HOUR_OPTIONS.map((opt) => {
          const active = selectedHours === opt.hours;
          return (
            <button
              key={opt.hours ?? 'all'}
              type="button"
              onClick={() => setSelectedHours(opt.hours)}
              className={`px-3 py-1.5 rounded-full text-xs font-medium font-inter border transition-colors ${
                active
                  ? 'bg-hidro-green-primary text-white border-hidro-green-primary'
                  : 'bg-white text-gray-600 border-gray-200 hover:bg-gray-50'
              }`}
            >
              {opt.label}
            </button>
          );
        })}
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatCard
          variant="success"
          label="Evaluaciones en ventana"
          value={evaluationStats?.totalEvaluations ?? (evaluationStatsLoading ? '…' : 0)}
        />
        <StatCard
          variant="info"
          label="Promedio diario"
          value={
            evaluationStats
              ? evaluationStats.avgEvaluationsPerDay.toFixed(1)
              : evaluationStatsLoading
                ? '…'
                : '0'
          }
        />
        <StatCard
          variant="default"
          label="Reglas disparadas / inferencia"
          value={evaluations.length > 0 ? avgRulesFired.toFixed(1) : '0'}
        />
      </div>

      {/* Daily mini-chart */}
      {dailyTotals.length > 0 && (
        <BaseCard hover={false} className="border border-gray-200">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-gray-700 font-inter">
              Evaluaciones por día
            </h3>
            <span className="text-xs text-gray-400 font-inter">{dailyTotals.length} día{dailyTotals.length !== 1 ? 's' : ''}</span>
          </div>
          <div className="flex gap-2 h-32">
            {dailyTotals.map((d) => {
              const heightPct = (d.count / maxDaily) * 100;
              const day = d.date.slice(5);
              return (
                <div
                  key={d.date}
                  className="flex-1 h-full flex flex-col items-center min-w-0"
                >
                  <div className="flex-1 w-full flex items-end">
                    <div
                      className="w-full bg-emerald-400 hover:bg-emerald-500 rounded-t transition-colors"
                      style={{ height: `${Math.max(4, heightPct)}%` }}
                      title={`${d.count} evaluaciones`}
                    />
                  </div>
                  <span className="text-[10px] text-gray-500 font-inter mt-1">{day}</span>
                  <span className="text-[10px] font-mono text-gray-700">{d.count}</span>
                </div>
              );
            })}
          </div>
        </BaseCard>
      )}

      {/* Stats error */}
      {evaluationStatsError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-xs text-red-700 font-inter">Estadísticas: {evaluationStatsError}</p>
        </div>
      )}

      {/* List */}
      <BaseCard hover={false} className="border border-gray-200">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-semibold text-gray-700 font-inter">
            Inferencias recientes
          </h3>
          {evaluationsTotalCount > 0 && (
            <span className="text-xs text-gray-500 font-inter">
              {evaluationsTotalCount} en total
            </span>
          )}
        </div>

        {evaluationsLoading && evaluations.length === 0 ? (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto mb-3" />
            <p className="text-xs text-gray-500 font-inter">Cargando historial…</p>
          </div>
        ) : evaluationsError ? (
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-700 font-inter">{evaluationsError}</p>
          </div>
        ) : evaluations.length === 0 ? (
          <div className="text-center py-10">
            <div className="w-12 h-12 mx-auto bg-gray-100 rounded-full flex items-center justify-center mb-3">
              <span className="text-xl">📭</span>
            </div>
            <p className="text-sm text-gray-600 font-inter">
              No hay evaluaciones registradas en esta ventana de tiempo.
            </p>
            <p className="text-xs text-gray-400 font-inter mt-1">
              Las inferencias se registran automáticamente cuando el sistema está activo.
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {evaluations.map(renderEvaluationCard)}
          </div>
        )}

        {/* Pagination */}
        {evaluationsTotalPages > 1 && (
          <div className="flex items-center justify-between mt-4 pt-3 border-t border-gray-100">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={!evaluationsHasPrevious || evaluationsLoading}
            >
              ← Anterior
            </Button>
            <span className="text-xs text-gray-500 font-inter">
              Página {evaluationsPage} de {evaluationsTotalPages}
            </span>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setPage((p) => p + 1)}
              disabled={!evaluationsHasNext || evaluationsLoading}
            >
              Siguiente →
            </Button>
          </div>
        )}
      </BaseCard>
    </div>
  );
});

export default EvaluationHistorySection;
