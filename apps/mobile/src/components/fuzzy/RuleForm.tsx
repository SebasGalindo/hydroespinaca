/**
 * RuleForm — Formulario multi-step para crear/editar reglas fuzzy.
 * Step 1: Nombre/Descripción.
 * Step 2: Condiciones (IF) con pickers de variable/operador/término.
 * Step 3: Consecuentes (THEN) con pickers de variable/términos/agregación.
 * Preview de la regla al final.
 */
import React, { useState, useEffect, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet, ScrollView } from 'react-native';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { FormField } from '../molecules/FormField';
import { Input } from '../atoms/Input';
import { Select } from '../molecules/Select';
import { Button } from '../atoms/Button';
import { Alert } from '../molecules/Alert';
import { Icon } from '../atoms/Icon';
import { Text } from '../atoms/Text';
import { Card } from '../molecules/Card';
import type {
  FuzzyRule,
  FuzzyVariable,
  FuzzyTerm,
  RuleCondition,
  RuleConsequent,
  RuleConnector,
  LogicalOperator,
  AggregationMethod,
  CreateFuzzyRuleRequest,
  UpdateFuzzyRuleRequest,
} from '@hydroespinaca/shared';
import {
  spacing,
  colors,
  semanticColors,
  borderRadius,
  typography,
  useFuzzyStore,
  AGGREGATION_METHODS,
} from '@hydroespinaca/shared';
import { generateRuleText } from '../../utils/fuzzyHelpers';

export interface RuleFormProps {
  isOpen: boolean;
  onClose: () => void;
  systemId: string;
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
  rule?: FuzzyRule | null;
  onSuccess?: (rule: FuzzyRule) => void;
}

const STEPS = ['Info', 'Condiciones', 'Consecuentes'];

export function RuleForm({
  isOpen,
  onClose,
  systemId,
  variables,
  terms,
  rule,
  onSuccess,
}: RuleFormProps): React.ReactElement {
  const isEditing = !!rule;
  const { createRule, updateRule, crudLoading, crudError, clearErrors } = useFuzzyStore();

  const [step, setStep] = useState(0);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [conditions, setConditions] = useState<RuleCondition[]>([]);
  const [connectors, setConnectors] = useState<RuleConnector[]>([]);
  const [consequents, setConsequents] = useState<RuleConsequent[]>([]);
  const [error, setError] = useState<string | null>(null);

  const inputVars = useMemo(() => variables.filter((v) => v.variableType === 'input'), [variables]);
  const outputVars = useMemo(() => variables.filter((v) => v.variableType === 'output'), [variables]);

  const getTermsForVariable = (varId: string) =>
    terms.filter((t) => t.variableId === varId);

  useEffect(() => {
    if (isOpen) {
      clearErrors();
      setStep(0);
      if (rule) {
        setName(rule.name);
        setDescription(rule.description || '');
        setConditions(rule.conditions.length > 0 ? [...rule.conditions] : [{ variableId: '', operator: 'IS', value: '' }]);
        setConnectors([...rule.connectors]);
        setConsequents(rule.consequents.length > 0 ? [...rule.consequents] : [{ variableId: '', terms: [], aggregationMethod: 'max' }]);
      } else {
        setName('');
        setDescription('');
        setConditions([{ variableId: '', operator: 'IS', value: '' }]);
        setConnectors([]);
        setConsequents([{ variableId: '', terms: [], aggregationMethod: 'max' }]);
      }
      setError(null);
    }
  }, [isOpen, rule, clearErrors]);

  // ─── Conditions management ──────────────────

  const addCondition = () => {
    setConnectors((prev) => [...prev, 'AND']);
    setConditions((prev) => [...prev, { variableId: '', operator: 'IS', value: '' }]);
  };

  const removeCondition = (index: number) => {
    if (conditions.length <= 1) return;
    setConditions((prev) => prev.filter((_, i) => i !== index));
    if (index > 0) {
      setConnectors((prev) => prev.filter((_, i) => i !== index - 1));
    } else if (connectors.length > 0) {
      setConnectors((prev) => prev.filter((_, i) => i !== 0));
    }
  };

  const updateCondition = (index: number, field: keyof RuleCondition, value: string) => {
    setConditions((prev) => prev.map((c, i) => (i === index ? { ...c, [field]: value } : c)));
  };

  const updateConnector = (index: number, value: RuleConnector) => {
    setConnectors((prev) => prev.map((c, i) => (i === index ? value : c)));
  };

  // ─── Consequents management ─────────────────

  const addConsequent = () => {
    setConsequents((prev) => [...prev, { variableId: '', terms: [], aggregationMethod: 'max' }]);
  };

  const removeConsequent = (index: number) => {
    if (consequents.length <= 1) return;
    setConsequents((prev) => prev.filter((_, i) => i !== index));
  };

  const updateConsequent = (index: number, field: string, value: unknown) => {
    setConsequents((prev) =>
      prev.map((c, i) => (i === index ? { ...c, [field]: value } : c)),
    );
  };

  const toggleConsequentTerm = (consIndex: number, termId: string) => {
    setConsequents((prev) =>
      prev.map((c, i) => {
        if (i !== consIndex) return c;
        const has = c.terms.includes(termId);
        return { ...c, terms: has ? c.terms.filter((t) => t !== termId) : [...c.terms, termId] };
      }),
    );
  };

  // ─── Preview ────────────────────────────────

  const previewRule = useMemo((): FuzzyRule => ({
    id: '__preview__',
    name,
    systemId,
    description,
    conditions,
    connectors,
    consequents,
    createdAt: null,
    ruleText: null,
  }), [name, systemId, description, conditions, connectors, consequents]);

  const previewText = useMemo(
    () => generateRuleText(previewRule, variables, terms),
    [previewRule, variables, terms],
  );

  // ─── Validation ─────────────────────────────

  const validateStep = (): boolean => {
    if (step === 0) {
      if (!name.trim()) { setError('El nombre es obligatorio'); return false; }
    } else if (step === 1) {
      const invalid = conditions.some((c) => !c.variableId || !c.value);
      if (invalid) { setError('Todas las condiciones deben tener variable y término'); return false; }
    } else if (step === 2) {
      const invalid = consequents.some((c) => !c.variableId || c.terms.length === 0);
      if (invalid) { setError('Todos los consecuentes deben tener variable y al menos un término'); return false; }
    }
    setError(null);
    return true;
  };

  const handleNext = () => {
    if (validateStep()) {
      setStep((prev) => Math.min(prev + 1, STEPS.length - 1));
    }
  };

  const handleBack = () => {
    setError(null);
    setStep((prev) => Math.max(prev - 1, 0));
  };

  const handleSubmit = async () => {
    if (!validateStep()) return;

    try {
      if (isEditing && rule) {
        const request: UpdateFuzzyRuleRequest = {
          name: name.trim(),
          description: description.trim() || undefined,
          conditions,
          connectors,
          consequents,
        };
        const updated = await updateRule(rule.id, request);
        onSuccess?.(updated);
      } else {
        const request: CreateFuzzyRuleRequest = {
          systemId,
          name: name.trim(),
          description: description.trim() || undefined,
          conditions,
          connectors,
          consequents,
        };
        const created = await createRule(request);
        onSuccess?.(created);
      }
      onClose();
    } catch {
      // Handled by store
    }
  };

  const aggOptions = AGGREGATION_METHODS.map((m) => ({ label: m.label, value: m.value }));

  const operatorOptions = [
    { label: 'ES', value: 'IS' },
    { label: 'NO ES', value: 'IS_NOT' },
  ];

  const connectorOptions = [
    { label: 'AND', value: 'AND' },
    { label: 'OR', value: 'OR' },
  ];

  return (
    <BottomSheetForm
      title={isEditing ? 'Editar Regla' : 'Nueva Regla'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['80%', '95%']}
    >
      <View style={styles.form}>
        {/* Step Indicator */}
        <View style={styles.stepIndicator}>
          {STEPS.map((s, i) => (
            <View key={s} style={styles.stepItem}>
              <View style={[styles.stepDot, i <= step && styles.stepDotActive]}>
                <Text variant="caption" color={i <= step ? semanticColors.textInverse : semanticColors.textTertiary}>
                  {i + 1}
                </Text>
              </View>
              <Text
                variant="caption"
                color={i === step ? semanticColors.primary : semanticColors.textTertiary}
              >
                {s}
              </Text>
            </View>
          ))}
        </View>

        {(error || crudError) && (
          <Alert
            type="error"
            message={error || crudError || ''}
            onDismiss={() => { setError(null); clearErrors(); }}
          />
        )}

        {/* Step 0: Info */}
        {step === 0 && (
          <>
            <FormField label="Nombre de la regla" required>
              <Input value={name} onChangeText={setName} placeholder="Ej: Riego alto" autoFocus />
            </FormField>
            <FormField label="Descripción">
              <Input value={description} onChangeText={setDescription} placeholder="Descripción opcional" />
            </FormField>
          </>
        )}

        {/* Step 1: Conditions (IF) */}
        {step === 1 && (
          <>
            <Text variant="label" color={semanticColors.textPrimary}>
              SI...
            </Text>
            {conditions.map((cond, i) => {
              const varOptions = inputVars.map((v) => ({ label: v.name, value: v.id }));
              const termOptions = cond.variableId
                ? getTermsForVariable(cond.variableId).map((t) => ({ label: t.label, value: t.id }))
                : [];

              return (
                <View key={`cond-${i}`}>
                  {i > 0 && (
                    <View style={styles.connectorRow}>
                      <Select
                        options={connectorOptions}
                        value={connectors[i - 1] || 'AND'}
                        onValueChange={(v) => updateConnector(i - 1, v as RuleConnector)}
                        style={styles.connectorSelect}
                        accessibilityLabel="Conector"
                      />
                    </View>
                  )}
                  <Card variant="outlined" padding="sm">
                    <View style={styles.conditionHeader}>
                      <Text variant="caption" color={semanticColors.textTertiary}>
                        Condición {i + 1}
                      </Text>
                      {conditions.length > 1 && (
                        <TouchableOpacity onPress={() => removeCondition(i)}>
                          <Icon name="close" size={16} color={colors.error[500]} />
                        </TouchableOpacity>
                      )}
                    </View>
                    <FormField label="Variable">
                      <Select
                        options={varOptions}
                        value={cond.variableId}
                        onValueChange={(v) => updateCondition(i, 'variableId', v)}
                        placeholder="Seleccionar variable"
                        accessibilityLabel="Variable de condición"
                      />
                    </FormField>
                    <FormField label="Operador">
                      <Select
                        options={operatorOptions}
                        value={cond.operator}
                        onValueChange={(v) => updateCondition(i, 'operator', v)}
                        accessibilityLabel="Operador"
                      />
                    </FormField>
                    <FormField label="Término">
                      <Select
                        options={termOptions}
                        value={cond.value}
                        onValueChange={(v) => updateCondition(i, 'value', v)}
                        placeholder="Seleccionar término"
                        disabled={!cond.variableId}
                        accessibilityLabel="Término de condición"
                      />
                    </FormField>
                  </Card>
                </View>
              );
            })}
            <Button
              variant="ghost"
              size="sm"
              onPress={addCondition}
              leftIcon={<Icon name="add" size={16} color={semanticColors.primary} />}
            >
              Añadir condición
            </Button>
          </>
        )}

        {/* Step 2: Consequents (THEN) */}
        {step === 2 && (
          <>
            <Text variant="label" color={semanticColors.textPrimary}>
              ENTONCES...
            </Text>
            {consequents.map((cons, i) => {
              const varOptions = outputVars.map((v) => ({ label: v.name, value: v.id }));
              const consTerms = cons.variableId
                ? getTermsForVariable(cons.variableId)
                : [];

              return (
                <Card key={`cons-${i}`} variant="outlined" padding="sm">
                  <View style={styles.conditionHeader}>
                    <Text variant="caption" color={semanticColors.textTertiary}>
                      Consecuente {i + 1}
                    </Text>
                    {consequents.length > 1 && (
                      <TouchableOpacity onPress={() => removeConsequent(i)}>
                        <Icon name="close" size={16} color={colors.error[500]} />
                      </TouchableOpacity>
                    )}
                  </View>
                  <FormField label="Variable de salida">
                    <Select
                      options={varOptions}
                      value={cons.variableId}
                      onValueChange={(v) => updateConsequent(i, 'variableId', v)}
                      placeholder="Seleccionar variable"
                      accessibilityLabel="Variable de consecuente"
                    />
                  </FormField>

                  {consTerms.length > 0 && (
                    <FormField label="Términos (seleccionar uno o más)">
                      <View style={styles.termChips}>
                        {consTerms.map((t) => {
                          const selected = cons.terms.includes(t.id);
                          return (
                            <TouchableOpacity
                              key={t.id}
                              style={[
                                styles.termChip,
                                selected && styles.termChipSelected,
                              ]}
                              onPress={() => toggleConsequentTerm(i, t.id)}
                            >
                              <Text
                                variant="caption"
                                color={selected ? semanticColors.textInverse : semanticColors.textSecondary}
                              >
                                {t.label}
                              </Text>
                            </TouchableOpacity>
                          );
                        })}
                      </View>
                    </FormField>
                  )}

                  <FormField label="Agregación">
                    <Select
                      options={aggOptions}
                      value={cons.aggregationMethod}
                      onValueChange={(v) => updateConsequent(i, 'aggregationMethod', v)}
                      accessibilityLabel="Método de agregación"
                    />
                  </FormField>
                </Card>
              );
            })}
            <Button
              variant="ghost"
              size="sm"
              onPress={addConsequent}
              leftIcon={<Icon name="add" size={16} color={semanticColors.primary} />}
            >
              Añadir consecuente
            </Button>

            {/* Rule Preview */}
            <Card variant="filled" padding="md" style={styles.previewCard}>
              <Text variant="label" color={semanticColors.textPrimary} style={styles.previewTitle}>
                Vista previa:
              </Text>
              <Text variant="caption" color={semanticColors.textSecondary}>
                {previewText}
              </Text>
            </Card>
          </>
        )}

        {/* Navigation Buttons */}
        <View style={styles.navButtons}>
          {step > 0 && (
            <Button variant="outline" onPress={handleBack} style={styles.navButton}>
              Atrás
            </Button>
          )}
          {step < STEPS.length - 1 ? (
            <Button variant="primary" onPress={handleNext} style={styles.navButton}>
              Siguiente
            </Button>
          ) : (
            <Button
              variant="primary"
              onPress={handleSubmit}
              loading={crudLoading}
              style={styles.navButton}
            >
              {isEditing ? 'Guardar' : 'Crear Regla'}
            </Button>
          )}
        </View>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.sm,
  },
  stepIndicator: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: spacing.lg,
    marginBottom: spacing.sm,
  },
  stepItem: {
    alignItems: 'center',
    gap: 4,
  },
  stepDot: {
    width: 24,
    height: 24,
    borderRadius: borderRadius.xl,
    backgroundColor: colors.gray[200],
    justifyContent: 'center',
    alignItems: 'center',
  },
  stepDotActive: {
    backgroundColor: colors.hidro[500],
  },
  connectorRow: {
    alignItems: 'center',
    marginVertical: spacing.xs,
  },
  connectorSelect: {
    width: 100,
  },
  conditionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  termChips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  termChip: {
    paddingVertical: 6,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius['2xl'],
    backgroundColor: colors.gray[100],
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  termChipSelected: {
    backgroundColor: colors.hidro[500],
    borderColor: colors.hidro[500],
  },
  previewCard: {
    marginTop: spacing.sm,
  },
  previewTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  navButtons: {
    flexDirection: 'row',
    gap: spacing.md,
    marginTop: spacing.md,
  },
  navButton: {
    flex: 1,
  },
});
