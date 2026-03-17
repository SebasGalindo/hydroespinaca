'use client';

import React, { useState, useEffect, useMemo } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';
import dynamic from 'next/dynamic';
import type {
  FuzzyTerm,
  FuzzyVariable,
  CreateFuzzyTermRequest,
  UpdateFuzzyTermRequest,
  MembershipFunctionType,
  MembershipFunction,
} from '@hydroespinaca/shared';
import {
  MEMBERSHIP_FUNCTION_LABELS,
  MF_PARAM_COUNTS,
  MF_PARAM_LABELS,
} from '@hydroespinaca/shared';

const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

// ─── MF Evaluator (reuse from MembershipFunctionChart logic) ─

const evaluateMF = (x: number, fnType: MembershipFunctionType, params: number[]): number => {
  switch (fnType) {
    case 'triangular': {
      const [a, b, c] = params;
      if (x <= a! || x >= c!) return 0;
      if (x === b!) return 1;
      if (x < b!) return (x - a!) / (b! - a!);
      return (c! - x) / (c! - b!);
    }
    case 'trapezoidal': {
      const [a, b, c, d] = params;
      if (x <= a! || x >= d!) return 0;
      if (x >= b! && x <= c!) return 1;
      if (x < b!) return (x - a!) / (b! - a!);
      return (d! - x) / (d! - c!);
    }
    case 'gaussian':
      return Math.exp(-0.5 * ((x - params[0]!) / params[1]!) ** 2);
    case 'bell':
      return 1 / (1 + Math.abs((x - params[2]!) / params[0]!) ** (2 * params[1]!));
    case 'sigmoid':
      return 1 / (1 + Math.exp(-params[0]! * (x - params[1]!)));
    default:
      return 0;
  }
};

// ─── Chart colors ────────────────────────────────────────────

const CHART_COLORS = [
  '#3B82F6', '#EF4444', '#10B981', '#F59E0B',
  '#8B5CF6', '#EC4899', '#06B6D4', '#84CC16',
];

// ─── Props ───────────────────────────────────────────────────

interface TermFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateFuzzyTermRequest | UpdateFuzzyTermRequest) => Promise<void>;
  isLoading?: boolean;
  variable: FuzzyVariable;
  /** Existing terms for this variable — used for overlay chart */
  existingTerms: FuzzyTerm[];
  /** If provided, the form is in edit mode */
  term?: FuzzyTerm | null;
}

// ─── MF type options ─────────────────────────────────────────

const mfTypeOptions = Object.entries(MEMBERSHIP_FUNCTION_LABELS).map(([value, label]) => ({
  value,
  label,
}));

// ─── Component ───────────────────────────────────────────────

const TermForm = React.memo(function TermForm({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
  variable,
  existingTerms,
  term = null,
}: TermFormProps) {
  const isEdit = !!term;

  const [label, setLabel] = useState('');
  const [mfType, setMfType] = useState<MembershipFunctionType>('triangular');
  const [params, setParams] = useState<string[]>(['0', '50', '100']);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Compute effective universe range: use variable's values or infer from existing term params
  const uMin = useMemo(() => {
    const allParams = existingTerms.flatMap((t) => t.membershipFunction.parameters);
    const minParam = allParams.length > 0 ? Math.min(...allParams) : 0;
    if (variable.universeMin != null) return Math.min(variable.universeMin, minParam);
    return allParams.length > 0 ? Math.floor(minParam * 0.9) : 0;
  }, [variable.universeMin, existingTerms]);
  const uMax = useMemo(() => {
    const allParams = existingTerms.flatMap((t) => t.membershipFunction.parameters);
    const maxParam = allParams.length > 0 ? Math.max(...allParams) : 100;
    if (variable.universeMax != null) return Math.max(variable.universeMax, maxParam);
    return allParams.length > 0 ? Math.ceil(maxParam * 1.1) : 100;
  }, [variable.universeMax, existingTerms]);

  // Populate form when editing
  useEffect(() => {
    if (term) {
      setLabel(term.label);
      setMfType(term.membershipFunction.functionType);
      setParams(term.membershipFunction.parameters.map(String));
    } else {
      setLabel('');
      setMfType('triangular');
      // Default params based on universe
      const mid = (uMin + uMax) / 2;
      setParams([String(uMin), String(mid), String(uMax)]);
    }
    setErrors({});
  }, [term, isOpen, uMin, uMax]);

  // Update param count when MF type changes
  const handleMfTypeChange = (newType: string) => {
    const t = newType as MembershipFunctionType;
    setMfType(t);
    const count = MF_PARAM_COUNTS[t] ?? 3;
    const mid = (uMin + uMax) / 2;
    const range = uMax - uMin;

    // Generate sensible defaults per type
    let newParams: number[];
    switch (t) {
      case 'triangular':
        newParams = [uMin, mid, uMax];
        break;
      case 'trapezoidal':
        newParams = [uMin, uMin + range * 0.3, uMin + range * 0.7, uMax];
        break;
      case 'gaussian':
        newParams = [mid, range * 0.15];
        break;
      case 'sigmoid':
        newParams = [mid, 0.5];
        break;
      case 'bell':
        newParams = [range * 0.25, 2, mid];
        break;
      default:
        newParams = Array(count).fill(0);
    }
    setParams(newParams.map(String));
  };

  const paramLabels = MF_PARAM_LABELS[mfType] ?? [];
  const paramCount = MF_PARAM_COUNTS[mfType] ?? params.length;

  const handleParamChange = (index: number, value: string) => {
    const updated = [...params];
    updated[index] = value;
    setParams(updated);
  };

  // ─── Live chart preview ──────────────────────────────────

  const chartData = useMemo(() => {
    const step = (uMax - uMin) / 200;
    const xValues: number[] = [];
    for (let x = uMin; x <= uMax; x += step) xValues.push(x);

    const traces: any[] = [];

    // Existing terms (semi-transparent)
    existingTerms
      .filter((t) => !isEdit || t.id !== term?.id)
      .forEach((t, idx) => {
        const yVals = xValues.map((x) =>
          evaluateMF(x, t.membershipFunction.functionType, t.membershipFunction.parameters)
        );
        traces.push({
          x: xValues,
          y: yVals,
          type: 'scatter',
          mode: 'lines',
          name: t.label,
          line: { color: CHART_COLORS[idx % CHART_COLORS.length], width: 1.5, dash: 'dot' },
          opacity: 0.4,
        });
      });

    // Current term being edited (bold)
    const numericParams = params.map(Number).filter((n) => !isNaN(n));
    if (numericParams.length >= paramCount) {
      const yVals = xValues.map((x) => evaluateMF(x, mfType, numericParams));
      traces.push({
        x: xValues,
        y: yVals,
        type: 'scatter',
        mode: 'lines',
        name: label || 'Nuevo término',
        line: { color: '#16a34a', width: 3 },
        fill: 'tozeroy',
        fillcolor: 'rgba(22, 163, 74, 0.12)',
      });
    }

    return traces;
  }, [params, mfType, label, existingTerms, uMin, uMax, paramCount, isEdit, term?.id]);

  const chartLayout = {
    xaxis: {
      title: { text: variable.name, font: { family: 'Inter, sans-serif', size: 12 } },
      range: [uMin, uMax],
      gridcolor: '#E5E7EB',
    },
    yaxis: {
      title: { text: 'μ', font: { family: 'Inter, sans-serif', size: 12 } },
      range: [0, 1.15],
      gridcolor: '#E5E7EB',
    },
    plot_bgcolor: '#FFFFFF',
    paper_bgcolor: '#FFFFFF',
    font: { family: 'Inter, sans-serif' },
    legend: { orientation: 'h' as const, x: 0, y: -0.25, font: { size: 10 } },
    margin: { l: 45, r: 20, t: 10, b: 65 },
    hovermode: 'x unified' as const,
  };

  // ─── Validation ──────────────────────────────────────────

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!label.trim()) {
      newErrors.label = 'La etiqueta es requerida';
    }

    const numericParams = params.slice(0, paramCount).map(Number);
    if (numericParams.some(isNaN)) {
      newErrors.params = 'Todos los parámetros deben ser numéricos';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // ─── Submit ──────────────────────────────────────────────

  const handleSubmit = async () => {
    if (!validate()) return;

    const membershipFunction: MembershipFunction = {
      functionType: mfType,
      parameters: params.slice(0, paramCount).map(Number),
      universeMin: uMin,
      universeMax: uMax,
    };

    if (isEdit) {
      const request: UpdateFuzzyTermRequest = {
        label: label.trim(),
        membershipFunction,
      };
      await onSubmit(request);
    } else {
      const request: CreateFuzzyTermRequest = {
        variableId: variable.id,
        label: label.trim(),
        membershipFunction,
      };
      await onSubmit(request);
    }
    handleClose();
  };

  // ─── Close ───────────────────────────────────────────────

  const handleClose = () => {
    setLabel('');
    setMfType('triangular');
    setParams(['0', '50', '100']);
    setErrors({});
    onClose();
  };

  // ─── Render ──────────────────────────────────────────────

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={isEdit ? 'Editar Término' : 'Crear Término'}
      maxWidth="xl"
    >
      <div className="space-y-5">
        {/* Label + MF type */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            type="text"
            label="Etiqueta"
            value={label}
            onChange={setLabel}
            placeholder="Ej: Alta, Media, Baja"
            required
            error={errors.label || ''}
          />
          <FormField
            type="select"
            label="Función de Membresía"
            value={mfType}
            onChange={handleMfTypeChange}
            options={mfTypeOptions}
            required
          />
        </div>

        {/* Parameters */}
        <div>
          <p className="text-sm font-medium text-gray-700 mb-2">
            Parámetros de la función
          </p>
          {errors.params && (
            <p className="text-xs text-red-500 mb-2">{errors.params}</p>
          )}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {paramLabels.slice(0, paramCount).map((pLabel, idx) => (
              <FormField
                key={`${mfType}-${idx}`}
                type="number"
                label={pLabel}
                value={params[idx] ?? '0'}
                onChange={(val) => handleParamChange(idx, val)}
                step="0.1"
              />
            ))}
          </div>
        </div>

        {/* Live preview chart */}
        <div className="border border-gray-200 rounded-lg overflow-hidden">
          <div className="bg-gray-50 px-4 py-2 border-b border-gray-200">
            <p className="text-sm font-medium text-gray-700">
              Vista previa en vivo
            </p>
          </div>
          <Plot
            data={chartData}
            layout={chartLayout}
            config={{ displayModeBar: false, responsive: true }}
            style={{ width: '100%', height: '280px' }}
          />
        </div>

        {/* Actions */}
        <div className="flex gap-3 pt-4 border-t border-gray-200">
          <Button variant="secondary" onClick={handleClose} className="flex-1">
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={handleSubmit}
            isLoading={isLoading}
            className="flex-1"
          >
            {isEdit ? 'Guardar Cambios' : 'Crear Término'}
          </Button>
        </div>
      </div>
    </Modal>
  );
});

export default TermForm;
