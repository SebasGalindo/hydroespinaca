'use client';

import React, { useMemo } from 'react';
import dynamic from 'next/dynamic';
import type { FuzzyVariable, FuzzyTerm, MembershipFunctionType } from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface MembershipFunctionChartProps {
  variable: FuzzyVariable;
  terms: FuzzyTerm[];
  height?: string;
  showTitle?: boolean;
}

const CHART_COLORS = [
  '#3B82F6', '#EF4444', '#10B981', '#F59E0B',
  '#8B5CF6', '#EC4899', '#06B6D4', '#84CC16',
  '#F97316', '#6366F1', '#14B8A6', '#E11D48',
];

// ─── Membership Function Calculators ─────────────────────────

const calcTriangular = (x: number, a: number, b: number, c: number): number => {
  if (x <= a || x >= c) return 0;
  if (x === b) return 1;
  if (x < b) return (x - a) / (b - a);
  return (c - x) / (c - b);
};

const calcTrapezoidal = (x: number, a: number, b: number, c: number, d: number): number => {
  if (x <= a || x >= d) return 0;
  if (x >= b && x <= c) return 1;
  if (x < b) return (x - a) / (b - a);
  return (d - x) / (d - c);
};

const calcGaussian = (x: number, mean: number, sigma: number): number => {
  return Math.exp(-0.5 * ((x - mean) / sigma) ** 2);
};

const calcGaussian2 = (x: number, mean1: number, sigma1: number, mean2: number, sigma2: number): number => {
  const left = x <= mean1 ? Math.exp(-0.5 * ((x - mean1) / sigma1) ** 2) : 1;
  const right = x >= mean2 ? Math.exp(-0.5 * ((x - mean2) / sigma2) ** 2) : 1;
  return left * right;
};

const calcBell = (x: number, a: number, b: number, c: number): number => {
  return 1 / (1 + Math.abs((x - c) / a) ** (2 * b));
};

const calcSigmoid = (x: number, a: number, c: number): number => {
  return 1 / (1 + Math.exp(-a * (x - c)));
};

const calcDSigmoid = (x: number, a1: number, c1: number, a2: number, c2: number): number => {
  const s1 = 1 / (1 + Math.exp(-a1 * (x - c1)));
  const s2 = 1 / (1 + Math.exp(-a2 * (x - c2)));
  return Math.abs(s1 - s2);
};

const calcPSigmoid = (x: number, a1: number, c1: number, a2: number, c2: number): number => {
  const s1 = 1 / (1 + Math.exp(-a1 * (x - c1)));
  const s2 = 1 / (1 + Math.exp(-a2 * (x - c2)));
  return s1 * s2;
};

const calcZMF = (x: number, a: number, b: number): number => {
  if (x <= a) return 1;
  if (x >= b) return 0;
  const mid = (a + b) / 2;
  if (x <= mid) return 1 - 2 * ((x - a) / (b - a)) ** 2;
  return 2 * ((x - b) / (b - a)) ** 2;
};

const calcSMF = (x: number, a: number, b: number): number => {
  if (x <= a) return 0;
  if (x >= b) return 1;
  const mid = (a + b) / 2;
  if (x <= mid) return 2 * ((x - a) / (b - a)) ** 2;
  return 1 - 2 * ((x - b) / (b - a)) ** 2;
};

const calcPiMF = (x: number, a: number, b: number, c: number, d: number): number => {
  const sVal = calcSMF(x, a, b);
  const zVal = calcZMF(x, c, d);
  return sVal * zVal;
};

const evaluateMF = (x: number, fnType: MembershipFunctionType, params: number[]): number => {
  switch (fnType) {
    case 'triangular': return calcTriangular(x, params[0]!, params[1]!, params[2]!);
    case 'trapezoidal': return calcTrapezoidal(x, params[0]!, params[1]!, params[2]!, params[3]!);
    case 'gaussian': return calcGaussian(x, params[0]!, params[1]!);
    case 'bell': return calcBell(x, params[0]!, params[1]!, params[2]!);
    case 'sigmoid': return calcSigmoid(x, params[0]!, params[1]!);
    case 'z_shaped': return calcZMF(x, params[0]!, params[1]!);
    case 's_shaped': return calcSMF(x, params[0]!, params[1]!);
    case 'pi_shaped': return calcPiMF(x, params[0]!, params[1]!, params[2]!, params[3]!);
    case 'linear': return params.length >= 2 ? Math.max(0, Math.min(1, params[0]! * x + params[1]!)) : 0;
    case 'constant': return params.length >= 1 ? params[0]! : 0;
    default: return 0;
  }
};

// ─── Component ───────────────────────────────────────────────

const MembershipFunctionChart = React.memo(function MembershipFunctionChart({
  variable,
  terms,
  height = '380px',
  showTitle = true,
}: MembershipFunctionChartProps) {
  const chartData = useMemo(() => {
    if (terms.length === 0) return [];

    const uMin = variable.universeMin ?? terms[0]?.membershipFunction.universeMin ?? 0;
    const uMax = variable.universeMax ?? terms[0]?.membershipFunction.universeMax ?? 100;
    const step = (uMax - uMin) / 200;
    const xValues: number[] = [];
    for (let x = uMin; x <= uMax; x += step) xValues.push(x);

    return terms.map((term, idx) => {
      const yValues = xValues.map((x) =>
        evaluateMF(x, term.membershipFunction.functionType, term.membershipFunction.parameters)
      );

      const color = CHART_COLORS[idx % CHART_COLORS.length]!;

      return {
        x: xValues,
        y: yValues,
        type: 'scatter' as const,
        mode: 'lines' as const,
        name: term.label,
        line: { color, width: 2 },
        fill: 'tozeroy' as const,
        fillcolor: color + '18',
      };
    });
  }, [variable, terms]);

  const uMin = variable.universeMin ?? terms[0]?.membershipFunction.universeMin ?? 0;
  const uMax = variable.universeMax ?? terms[0]?.membershipFunction.universeMax ?? 100;

  const layout = {
    ...(showTitle && {
      title: {
        text: `Funciones de Membresía — ${variable.name}`,
        font: { size: 15, family: 'Inter, sans-serif', color: '#1f2937' },
      },
    }),
    xaxis: {
      title: { text: variable.name, font: { family: 'Inter, sans-serif', size: 12 } },
      range: [uMin, uMax],
      gridcolor: '#E5E7EB',
      font: { family: 'Inter, sans-serif' },
    },
    yaxis: {
      title: { text: 'Grado de Membresía', font: { family: 'Inter, sans-serif', size: 12 } },
      range: [0, 1.1],
      gridcolor: '#E5E7EB',
      font: { family: 'Inter, sans-serif' },
    },
    plot_bgcolor: '#FFFFFF',
    paper_bgcolor: '#FFFFFF',
    font: { family: 'Inter, sans-serif' },
    legend: {
      orientation: 'h' as const,
      x: 0,
      y: -0.22,
      font: { family: 'Inter, sans-serif', size: 11 },
    },
    margin: { l: 55, r: 30, t: showTitle ? 50 : 20, b: 75 },
    hovermode: 'x unified' as const,
  };

  const config = {
    displayModeBar: true,
    displaylogo: false,
    modeBarButtonsToRemove: [
      'pan2d', 'lasso2d', 'select2d', 'autoScale2d',
      'hoverClosestCartesian', 'hoverCompareCartesian', 'toggleSpikelines',
    ] as any,
    responsive: true,
  };

  if (terms.length === 0) {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-400 text-sm">
        No hay términos definidos para esta variable
      </div>
    );
  }

  return (
    <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
      <Plot
        data={chartData}
        layout={layout}
        config={config}
        style={{ width: '100%', height }}
      />
    </div>
  );
});

export default MembershipFunctionChart;
