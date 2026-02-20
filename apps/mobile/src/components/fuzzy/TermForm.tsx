/**
 * TermForm — Formulario para crear/editar términos fuzzy.
 * BottomSheet: etiqueta, tipo MF, parámetros dinámicos, preview chart.
 */
import React, { useState, useEffect, useMemo } from 'react';
import { View, StyleSheet } from 'react-native';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { FormField } from '../molecules/FormField';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { Button } from '../atoms/Button';
import { Alert } from '../molecules/Alert';
import { MembershipChart } from './MembershipChart';
import type {
  FuzzyTerm,
  FuzzyVariable,
  MembershipFunctionType,
  CreateFuzzyTermRequest,
  UpdateFuzzyTermRequest,
} from '@hydroespinaca/shared';
import {
  spacing,
  useFuzzyStore,
  MEMBERSHIP_FUNCTION_LABELS,
  MF_PARAM_COUNTS,
  MF_PARAM_LABELS,
} from '@hydroespinaca/shared';
import { getDefaultMFParams } from '../../utils/fuzzyHelpers';

export interface TermFormProps {
  isOpen: boolean;
  onClose: () => void;
  variable: FuzzyVariable;
  existingTerms: FuzzyTerm[];
  term?: FuzzyTerm | null;
  onSuccess?: (term: FuzzyTerm) => void;
}

const mfTypeOptions = Object.entries(MEMBERSHIP_FUNCTION_LABELS).map(([value, label]) => ({
  label: label as string,
  value,
}));

export function TermForm({
  isOpen,
  onClose,
  variable,
  existingTerms,
  term,
  onSuccess,
}: TermFormProps): React.ReactElement {
  const isEditing = !!term;
  const { createTerm, updateTerm, crudLoading, crudError, clearErrors } = useFuzzyStore();

  const uMin = variable.universeMin ?? 0;
  const uMax = variable.universeMax ?? 100;

  const [label, setLabel] = useState('');
  const [mfType, setMfType] = useState<MembershipFunctionType>('triangular');
  const [params, setParams] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      clearErrors();
      if (term) {
        setLabel(term.label);
        setMfType(term.membershipFunction.functionType);
        setParams(term.membershipFunction.parameters.map(String));
      } else {
        setLabel('');
        setMfType('triangular');
        const defaults = getDefaultMFParams('triangular', uMin, uMax);
        setParams(defaults.map(String));
      }
      setError(null);
    }
  }, [isOpen, term, uMin, uMax, clearErrors]);

  const handleMfTypeChange = (type: string) => {
    const newType = type as MembershipFunctionType;
    setMfType(newType);
    const defaults = getDefaultMFParams(newType, uMin, uMax);
    setParams(defaults.map(String));
  };

  const handleParamChange = (index: number, value: string) => {
    setParams((prev) => {
      const next = [...prev];
      next[index] = value;
      return next;
    });
  };

  const paramLabels = MF_PARAM_LABELS[mfType] ?? [];
  const paramCount = MF_PARAM_COUNTS[mfType] ?? 0;

  // Preview term (the one being created/edited)
  const previewTerm = useMemo((): FuzzyTerm | null => {
    const parsedParams = params.map((p) => parseFloat(p)).filter((p) => !isNaN(p));
    if (parsedParams.length !== paramCount) return null;
    return {
      id: '__preview__',
      variableId: variable.id,
      label: label || 'Nuevo',
      membershipFunction: {
        functionType: mfType,
        parameters: parsedParams,
        universeMin: uMin,
        universeMax: uMax,
      },
      createdAt: null,
      updatedAt: null,
    };
  }, [params, paramCount, mfType, label, variable.id, uMin, uMax]);

  // All terms for chart (existing + preview)
  const chartTerms = useMemo(() => {
    const others = isEditing
      ? existingTerms.filter((t) => t.id !== term?.id)
      : existingTerms;
    return previewTerm ? [...others, previewTerm] : others;
  }, [existingTerms, previewTerm, isEditing, term?.id]);

  const handleSubmit = async () => {
    const trimmedLabel = label.trim();
    if (!trimmedLabel) {
      setError('La etiqueta es obligatoria');
      return;
    }

    const parsedParams = params.map((p) => parseFloat(p));
    if (parsedParams.some(isNaN)) {
      setError('Todos los parámetros deben ser numéricos');
      return;
    }
    if (parsedParams.length !== paramCount) {
      setError(`Se requieren ${paramCount} parámetros para ${MEMBERSHIP_FUNCTION_LABELS[mfType]}`);
      return;
    }

    try {
      if (isEditing && term) {
        const request: UpdateFuzzyTermRequest = {
          label: trimmedLabel,
          membershipFunction: {
            functionType: mfType,
            parameters: parsedParams,
            universeMin: uMin,
            universeMax: uMax,
          },
        };
        const updated = await updateTerm(term.id, request);
        onSuccess?.(updated);
      } else {
        const request: CreateFuzzyTermRequest = {
          variableId: variable.id,
          label: trimmedLabel,
          membershipFunction: {
            functionType: mfType,
            parameters: parsedParams,
            universeMin: uMin,
            universeMax: uMax,
          },
        };
        const created = await createTerm(request);
        onSuccess?.(created);
      }
      onClose();
    } catch {
      // Handled by store
    }
  };

  return (
    <BottomSheetForm
      title={isEditing ? 'Editar Término' : 'Nuevo Término'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['80%', '95%']}
    >
      <View style={styles.form}>
        {(error || crudError) && (
          <Alert
            type="error"
            message={error || crudError || ''}
            onDismiss={() => { setError(null); clearErrors(); }}
          />
        )}

        <FormField label="Etiqueta" required>
          <Input value={label} onChangeText={setLabel} placeholder="Ej: Alto, Bajo, Medio" autoFocus />
        </FormField>

        <FormField label="Tipo de función de membresía">
          <Select
            options={mfTypeOptions}
            value={mfType}
            onValueChange={handleMfTypeChange}
            accessibilityLabel="Tipo de función de membresía"
          />
        </FormField>

        {/* Dynamic Parameters */}
        {paramLabels.slice(0, paramCount).map((paramLabel, index) => (
          <FormField key={`param-${index}`} label={paramLabel} required>
            <Input
              value={params[index] ?? ''}
              onChangeText={(v) => handleParamChange(index, v)}
              keyboardType="numeric"
              placeholder="0"
            />
          </FormField>
        ))}

        {/* Chart Preview */}
        {chartTerms.length > 0 && (
          <View style={styles.chartContainer}>
            <MembershipChart
              terms={chartTerms}
              universeMin={uMin}
              universeMax={uMax}
              height={140}
              highlightTermId="__preview__"
            />
          </View>
        )}

        <Button
          variant="primary"
          onPress={handleSubmit}
          loading={crudLoading}
          fullWidth
          style={styles.submitButton}
        >
          {isEditing ? 'Guardar Cambios' : 'Crear Término'}
        </Button>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.sm,
  },
  chartContainer: {
    marginVertical: spacing.sm,
  },
  submitButton: {
    marginTop: spacing.md,
  },
});
