/**
 * Utilidades de fuzzy logic para la app móvil.
 * Cálculos de funciones de membresía, formateo y constantes.
 */
import type {
  FuzzySystemStatus,
  MembershipFunctionType,
  FuzzyTerm,
  FuzzyVariable,
  FuzzyRule,
  RuleCondition,
  RuleConsequent,
  IconName,
} from '@hydroespinaca/shared';
import {
  colors,
  chartColors,
  FUZZY_STATUS_LABELS,
  FUZZY_STATUS_COLORS,
  FUZZY_STATUS_ICONS,
  MEMBERSHIP_FUNCTION_LABELS,
  MF_PARAM_COUNTS,
  MF_PARAM_LABELS,
  VARIABLE_TYPE_LABELS,
} from '@hydroespinaca/shared';

// ─── Status helpers ────────────────────────────────────────────

export const getStatusLabel = (status: FuzzySystemStatus): string =>
  FUZZY_STATUS_LABELS[status] ?? status;

export const getStatusColor = (status: FuzzySystemStatus): string => {
  const colorKey = FUZZY_STATUS_COLORS[status] ?? 'gray';
  const colorMap: Record<string, { bg: string; text: string }> = {
    gray: { bg: colors.gray[100], text: colors.gray[600] },
    green: { bg: colors.hidro[100], text: colors.hidro[700] },
    red: { bg: colors.error[100], text: colors.error[700] },
    yellow: { bg: colors.warning[100], text: colors.warning[700] },
  };
  return colorMap[colorKey]?.bg ?? colors.gray[100];
};

export const getStatusTextColor = (status: FuzzySystemStatus): string => {
  const colorKey = FUZZY_STATUS_COLORS[status] ?? 'gray';
  const colorMap: Record<string, string> = {
    gray: colors.gray[600],
    green: colors.hidro[700],
    red: colors.error[700],
    yellow: colors.warning[700],
  };
  return colorMap[colorKey] ?? colors.gray[600];
};

export const getStatusIcon = (status: FuzzySystemStatus): IconName =>
  (FUZZY_STATUS_ICONS[status] ?? 'help') as IconName;

// ─── MF Parameter defaults ────────────────────────────────────

export const getDefaultMFParams = (
  type: MembershipFunctionType,
  universeMin: number,
  universeMax: number,
): number[] => {
  const range = universeMax - universeMin;
  const mid = (universeMin + universeMax) / 2;

  switch (type) {
    case 'triangular':
      return [universeMin, mid, universeMax];
    case 'trapezoidal':
      return [universeMin, universeMin + range * 0.3, universeMin + range * 0.7, universeMax];
    case 'gaussian':
      return [mid, range * 0.15];
    case 'bell':
      return [range * 0.25, 2, mid];
    case 'sigmoid':
      return [mid, 0.5];
    case 'z_shaped':
      return [universeMin + range * 0.3, universeMin + range * 0.7];
    case 's_shaped':
      return [universeMin + range * 0.3, universeMin + range * 0.7];
    case 'pi_shaped':
      return [universeMin, universeMin + range * 0.3, universeMin + range * 0.7, universeMax];
    case 'linear':
      return [0, 1];
    case 'constant':
      return [0.5];
    default:
      return Array(MF_PARAM_COUNTS[type] ?? 2).fill(0);
  }
};

// ─── MF Evaluation ────────────────────────────────────────────

const clamp = (v: number): number => Math.max(0, Math.min(1, v));

export const evaluateMF = (
  type: MembershipFunctionType,
  params: number[],
  x: number,
): number => {
  switch (type) {
    case 'triangular': {
      const a = params[0]!;
      const b = params[1]!;
      const c = params[2]!;
      if (x <= a || x >= c) return 0;
      if (x <= b) return (x - a) / (b - a);
      return (c - x) / (c - b);
    }
    case 'trapezoidal': {
      const a = params[0]!;
      const b = params[1]!;
      const c = params[2]!;
      const d = params[3]!;
      if (x <= a || x >= d) return 0;
      if (x >= b && x <= c) return 1;
      if (x < b) return (x - a) / (b - a);
      return (d - x) / (d - c);
    }
    case 'gaussian': {
      const c = params[0]!;
      const sigma = params[1]!;
      return Math.exp(-0.5 * Math.pow((x - c) / sigma, 2));
    }
    case 'bell': {
      const a = params[0]!;
      const b = params[1]!;
      const c = params[2]!;
      return 1 / (1 + Math.pow(Math.abs((x - c) / a), 2 * b));
    }
    case 'sigmoid': {
      const c = params[0]!;
      const a = params[1]!;
      return 1 / (1 + Math.exp(-a * (x - c)));
    }
    case 'z_shaped': {
      const a = params[0]!;
      const b = params[1]!;
      if (x <= a) return 1;
      if (x >= b) return 0;
      const mid = (a + b) / 2;
      if (x <= mid) return 1 - 2 * Math.pow((x - a) / (b - a), 2);
      return 2 * Math.pow((b - x) / (b - a), 2);
    }
    case 's_shaped': {
      const a = params[0]!;
      const b = params[1]!;
      if (x <= a) return 0;
      if (x >= b) return 1;
      const mid = (a + b) / 2;
      if (x <= mid) return 2 * Math.pow((x - a) / (b - a), 2);
      return 1 - 2 * Math.pow((b - x) / (b - a), 2);
    }
    case 'pi_shaped': {
      const a = params[0]!;
      const b = params[1]!;
      const c = params[2]!;
      const d = params[3]!;
      const sVal = evaluateMF('s_shaped', [a, b], x);
      const zVal = evaluateMF('z_shaped', [c, d], x);
      return sVal * zVal;
    }
    case 'linear': {
      const slope = params[0]!;
      const intercept = params[1]!;
      return clamp(slope * x + intercept);
    }
    case 'constant': {
      return clamp(params[0] ?? 0);
    }
    default:
      return 0;
  }
};

// ─── Chart data generation ────────────────────────────────────

export const CHART_COLORS = [
  colors.hidro[500],
  colors.error[500],
  colors.warning[500],
  colors.info[500],
  chartColors.violet,
  chartColors.pink,
  chartColors.orange,
  chartColors.teal,
  chartColors.indigo,
  chartColors.lime,
  chartColors.cyan,
  chartColors.red,
];

export interface ChartDataPoint {
  x: number;
  y: number;
}

export const generateMFData = (
  term: FuzzyTerm,
  points: number = 100,
): ChartDataPoint[] => {
  const { functionType, parameters, universeMin, universeMax } = term.membershipFunction;
  const data: ChartDataPoint[] = [];
  const step = (universeMax - universeMin) / points;

  for (let i = 0; i <= points; i++) {
    const x = universeMin + i * step;
    const y = evaluateMF(functionType, parameters, x);
    data.push({ x: Math.round(x * 100) / 100, y: Math.round(y * 1000) / 1000 });
  }
  return data;
};

// ─── Rule text generation ─────────────────────────────────────

export const generateRuleText = (
  rule: FuzzyRule,
  variables: FuzzyVariable[],
  terms: FuzzyTerm[],
): string => {
  if (rule.ruleText) return rule.ruleText;

  const varMap = new Map(variables.map((v) => [v.id, v.name]));
  const termMap = new Map(terms.map((t) => [t.id, t.label]));

  const conditionParts = rule.conditions.map((c: RuleCondition, i: number) => {
    const varName = varMap.get(c.variableId) ?? '?';
    const termLabel = termMap.get(c.value) ?? '?';
    const op = c.operator === 'IS_NOT' ? 'NO ES' : 'ES';
    const prefix = i > 0 && rule.connectors[i - 1]
      ? ` ${rule.connectors[i - 1]} `
      : '';
    return `${prefix}${varName} ${op} ${termLabel}`;
  });

  const consequentParts = rule.consequents.map((c: RuleConsequent) => {
    const varName = varMap.get(c.variableId) ?? '?';
    const termLabels = c.terms.map((tid) => termMap.get(tid) ?? '?').join(', ');
    return `${varName} = ${termLabels}`;
  });

  return `SI ${conditionParts.join('')} ENTONCES ${consequentParts.join(', ')}`;
};

// ─── Variable helpers ─────────────────────────────────────────

export const getVariableTypeLabel = (type: 'input' | 'output'): string =>
  VARIABLE_TYPE_LABELS[type] ?? type;

export const getMFLabel = (type: MembershipFunctionType): string =>
  MEMBERSHIP_FUNCTION_LABELS[type] ?? type;

export const getMFParamLabels = (type: MembershipFunctionType): string[] =>
  MF_PARAM_LABELS[type] ?? [];

export const getMFParamCount = (type: MembershipFunctionType): number =>
  MF_PARAM_COUNTS[type] ?? 0;
