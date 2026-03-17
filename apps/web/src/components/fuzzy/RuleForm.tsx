'use client';

import React, { useState, useEffect, useMemo } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import FormSection from '@/components/ui/FormSection';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';
import type {
  FuzzyRule,
  FuzzyVariable,
  FuzzyTerm,
  CreateFuzzyRuleRequest,
  UpdateFuzzyRuleRequest,
  RuleCondition,
  RuleConsequent,
  RuleConnector,
  LogicalOperator,
  AggregationMethod,
} from '@hydroespinaca/shared';
import { AGGREGATION_METHODS } from '@hydroespinaca/shared';

// ─── Props ───────────────────────────────────────────────────

interface RuleFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateFuzzyRuleRequest | UpdateFuzzyRuleRequest) => Promise<void>;
  isLoading?: boolean;
  systemId: string;
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
  /** If provided, the form is in edit mode */
  rule?: FuzzyRule | null;
}

// ─── Empty condition/consequent ──────────────────────────────

const emptyCondition: RuleCondition = { variableId: '', operator: 'IS', value: '' };
const emptyConsequent: RuleConsequent = { variableId: '', terms: [], aggregationMethod: 'max' };

// ─── Component ───────────────────────────────────────────────

const RuleForm = React.memo(function RuleForm({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
  systemId,
  variables,
  terms,
  rule = null,
}: RuleFormProps) {
  const isEdit = !!rule;

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [conditions, setConditions] = useState<RuleCondition[]>([{ ...emptyCondition }]);
  const [connectors, setConnectors] = useState<RuleConnector[]>([]);
  const [consequents, setConsequents] = useState<RuleConsequent[]>([{ ...emptyConsequent }]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Derived
  const inputVars = useMemo(() => variables.filter((v) => v.variableType === 'input'), [variables]);
  const outputVars = useMemo(() => variables.filter((v) => v.variableType === 'output'), [variables]);
  const termsByVariable = useMemo(() => {
    const map = new Map<string, FuzzyTerm[]>();
    terms.forEach((t) => {
      const arr = map.get(t.variableId) ?? [];
      arr.push(t);
      map.set(t.variableId, arr);
    });
    return map;
  }, [terms]);

  // Populate form when editing
  useEffect(() => {
    if (rule) {
      setName(rule.name);
      setDescription(rule.description ?? '');
      setConditions(rule.conditions.length > 0 ? [...rule.conditions] : [{ ...emptyCondition }]);
      setConnectors([...rule.connectors]);
      setConsequents(rule.consequents.length > 0 ? [...rule.consequents] : [{ ...emptyConsequent }]);
    } else {
      setName('');
      setDescription('');
      setConditions([{ ...emptyCondition }]);
      setConnectors([]);
      setConsequents([{ ...emptyConsequent }]);
    }
    setErrors({});
  }, [rule, isOpen]);

  // ─── Condition handlers ──────────────────────────────────

  const addCondition = () => {
    setConditions([...conditions, { ...emptyCondition }]);
    setConnectors([...connectors, 'AND']);
  };

  const removeCondition = (index: number) => {
    if (conditions.length <= 1) return;
    const newConds = conditions.filter((_, i) => i !== index);
    // Remove the connector: if removing first condition, remove connector at 0; else at index-1
    const connIdx = index > 0 ? index - 1 : 0;
    const newConns = connectors.filter((_, i) => i !== connIdx);
    setConditions(newConds);
    setConnectors(newConns);
  };

  const updateCondition = (index: number, field: keyof RuleCondition, value: string) => {
    const updated = [...conditions];
    updated[index] = { ...updated[index]!, [field]: value };
    // Reset value when variable changes
    if (field === 'variableId') {
      updated[index] = { ...updated[index]!, value: '' };
    }
    setConditions(updated);
  };

  const updateConnector = (index: number, value: RuleConnector) => {
    const updated = [...connectors];
    updated[index] = value;
    setConnectors(updated);
  };

  // ─── Consequent handlers ─────────────────────────────────

  const addConsequent = () => {
    setConsequents([...consequents, { ...emptyConsequent }]);
  };

  const removeConsequent = (index: number) => {
    if (consequents.length <= 1) return;
    setConsequents(consequents.filter((_, i) => i !== index));
  };

  const updateConsequent = (index: number, field: string, value: unknown) => {
    const updated = [...consequents];
    updated[index] = { ...updated[index]!, [field]: value };
    // Reset terms when variable changes
    if (field === 'variableId') {
      updated[index] = { ...updated[index]!, terms: [] };
    }
    setConsequents(updated);
  };

  const toggleConsequentTerm = (cIndex: number, termId: string) => {
    const updated = [...consequents];
    const currentTerms = updated[cIndex]!.terms;
    if (currentTerms.includes(termId)) {
      updated[cIndex] = { ...updated[cIndex]!, terms: currentTerms.filter((t) => t !== termId) };
    } else {
      updated[cIndex] = { ...updated[cIndex]!, terms: [...currentTerms, termId] };
    }
    setConsequents(updated);
  };

  // ─── Validation ──────────────────────────────────────────

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'El nombre es requerido';
    }

    const hasEmptyCondition = conditions.some((c) => !c.variableId || !c.value);
    if (hasEmptyCondition) {
      newErrors.conditions = 'Todas las condiciones deben tener variable y valor seleccionados';
    }

    const hasEmptyConsequent = consequents.some((c) => !c.variableId || c.terms.length === 0);
    if (hasEmptyConsequent) {
      newErrors.consequents = 'Todos los consecuentes deben tener variable y al menos un término';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // ─── Submit ──────────────────────────────────────────────

  const handleSubmit = async () => {
    if (!validate()) return;

    if (isEdit) {
      const request: UpdateFuzzyRuleRequest = {
        name: name.trim(),
        ...(description.trim() ? { description: description.trim() } : {}),
        conditions,
        connectors,
        consequents,
      };
      await onSubmit(request);
    } else {
      const request: CreateFuzzyRuleRequest = {
        systemId,
        name: name.trim(),
        ...(description.trim() ? { description: description.trim() } : {}),
        conditions,
        connectors,
        consequents,
      };
      await onSubmit(request);
    }
    handleClose();
  };

  // ─── Close ───────────────────────────────────────────────

  const handleClose = () => {
    setName('');
    setDescription('');
    setConditions([{ ...emptyCondition }]);
    setConnectors([]);
    setConsequents([{ ...emptyConsequent }]);
    setErrors({});
    onClose();
  };

  // ─── Build rule preview text ─────────────────────────────

  const getVarName = (id: string) => variables.find((v) => v.id === id)?.name ?? '?';
  const getTermLabel = (id: string) => terms.find((t) => t.id === id)?.label ?? '?';

  const previewText = useMemo(() => {
    const condParts = conditions
      .map((c, i) => {
        const varName = c.variableId ? getVarName(c.variableId) : '___';
        const op = c.operator === 'IS_NOT' ? 'NO ES' : 'ES';
        const termLabel = c.value ? getTermLabel(c.value) : '___';
        const connector = i < connectors.length ? (connectors[i] === 'AND' ? ' Y ' : ' O ') : '';
        return `${varName} ${op} ${termLabel}${connector}`;
      })
      .join('');

    const consqParts = consequents
      .map((cq) => {
        const varName = cq.variableId ? getVarName(cq.variableId) : '___';
        const termLabels = cq.terms.length > 0
          ? cq.terms.map((t) => getTermLabel(t)).join(', ')
          : '___';
        return `${varName} = {${termLabels}}`;
      })
      .join('; ');

    return `SI ${condParts} ENTONCES ${consqParts}`;
  }, [conditions, connectors, consequents, variables, terms]);

  // ─── Render ──────────────────────────────────────────────

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={isEdit ? 'Editar Regla' : 'Crear Regla'}
      maxWidth="2xl"
    >
      <div className="space-y-5">
        {/* Name + description */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            type="text"
            label="Nombre"
            value={name}
            onChange={setName}
            placeholder="Ej: Regla de temperatura alta"
            required
            error={errors.name || ''}
          />
          <FormField
            type="text"
            label="Descripción (opcional)"
            value={description}
            onChange={setDescription}
            placeholder="Ej: Activa ventilación cuando hace calor"
          />
        </div>

        {/* Conditions (IF part) */}
        <FormSection title="Condiciones (SI...)" subtitle="Define cuándo se activa la regla">
          {errors.conditions && (
            <p className="text-xs text-red-500 mb-3">{errors.conditions}</p>
          )}
          <div className="space-y-3">
            {conditions.map((cond, idx) => (
              <React.Fragment key={idx}>
                {/* Connector between conditions */}
                {idx > 0 && (
                  <div className="flex items-center justify-center gap-2 py-1">
                    <div className="flex-1 border-t border-gray-200" />
                    <select
                      value={connectors[idx - 1] ?? 'AND'}
                      onChange={(e) => updateConnector(idx - 1, e.target.value as RuleConnector)}
                      className="px-3 py-1 text-xs font-semibold border border-gray-300 rounded-md bg-white focus:ring-2 focus:ring-green-500"
                    >
                      <option value="AND">Y (AND)</option>
                      <option value="OR">O (OR)</option>
                    </select>
                    <div className="flex-1 border-t border-gray-200" />
                  </div>
                )}

                <div className="flex items-end gap-2 bg-gray-50 rounded-lg p-3">
                  {/* Variable select */}
                  <div className="flex-1">
                    <FormField
                      type="select"
                      label="Variable"
                      value={cond.variableId}
                      onChange={(val) => updateCondition(idx, 'variableId', val)}
                      options={inputVars.map((v) => ({ value: v.id, label: v.name }))}
                    />
                  </div>

                  {/* Operator */}
                  <div className="w-28">
                    <FormField
                      type="select"
                      label="Operador"
                      value={cond.operator}
                      onChange={(val) => updateCondition(idx, 'operator', val)}
                      options={[
                        { value: 'IS', label: 'ES' },
                        { value: 'IS_NOT', label: 'NO ES' },
                      ]}
                    />
                  </div>

                  {/* Term value */}
                  <div className="flex-1">
                    <FormField
                      type="select"
                      label="Término"
                      value={cond.value}
                      onChange={(val) => updateCondition(idx, 'value', val)}
                      options={
                        cond.variableId
                          ? (termsByVariable.get(cond.variableId) ?? []).map((t) => ({
                              value: t.id,
                              label: t.label,
                            }))
                          : []
                      }
                    />
                  </div>

                  {/* Remove */}
                  {conditions.length > 1 && (
                    <button
                      onClick={() => removeCondition(idx)}
                      className="p-2 text-red-500 hover:bg-red-50 rounded-md transition-colors mb-1"
                      title="Eliminar condición"
                    >
                      ✕
                    </button>
                  )}
                </div>
              </React.Fragment>
            ))}
          </div>
          <Button variant="ghost" size="sm" onClick={addCondition} className="mt-3">
            + Agregar condición
          </Button>
        </FormSection>

        {/* Consequents (THEN part) */}
        <FormSection title="Consecuentes (ENTONCES...)" subtitle="Define qué variables de salida se activan">
          {errors.consequents && (
            <p className="text-xs text-red-500 mb-3">{errors.consequents}</p>
          )}
          <div className="space-y-4">
            {consequents.map((cq, idx) => (
              <div key={idx} className="bg-gray-50 rounded-lg p-4 space-y-3">
                <div className="flex items-end gap-2">
                  {/* Output variable */}
                  <div className="flex-1">
                    <FormField
                      type="select"
                      label="Variable de Salida"
                      value={cq.variableId}
                      onChange={(val) => updateConsequent(idx, 'variableId', val)}
                      options={outputVars.map((v) => ({ value: v.id, label: v.name }))}
                    />
                  </div>

                  {/* Aggregation method */}
                  <div className="w-44">
                    <FormField
                      type="select"
                      label="Agregación"
                      value={cq.aggregationMethod}
                      onChange={(val) =>
                        updateConsequent(idx, 'aggregationMethod', val as AggregationMethod)
                      }
                      options={AGGREGATION_METHODS.map((m) => ({
                        value: m.value,
                        label: m.label,
                      }))}
                    />
                  </div>

                  {/* Remove */}
                  {consequents.length > 1 && (
                    <button
                      onClick={() => removeConsequent(idx)}
                      className="p-2 text-red-500 hover:bg-red-50 rounded-md transition-colors mb-1"
                      title="Eliminar consecuente"
                    >
                      ✕
                    </button>
                  )}
                </div>

                {/* Term picker (toggle buttons) */}
                {cq.variableId && (
                  <div>
                    <p className="text-xs font-medium text-gray-600 mb-2">
                      Selecciona los términos de salida:
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {(termsByVariable.get(cq.variableId) ?? []).map((t) => {
                        const isSelected = cq.terms.includes(t.id);
                        return (
                          <button
                            key={t.id}
                            onClick={() => toggleConsequentTerm(idx, t.id)}
                            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
                              isSelected
                                ? 'bg-green-100 border-green-400 text-green-800 font-medium'
                                : 'bg-white border-gray-300 text-gray-600 hover:border-green-300'
                            }`}
                          >
                            {t.label}
                          </button>
                        );
                      })}
                      {(termsByVariable.get(cq.variableId) ?? []).length === 0 && (
                        <p className="text-xs text-gray-400 italic">No hay términos definidos para esta variable</p>
                      )}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
          <Button variant="ghost" size="sm" onClick={addConsequent} className="mt-3">
            + Agregar consecuente
          </Button>
        </FormSection>

        {/* Rule preview */}
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
          <p className="text-xs font-medium text-amber-700 uppercase mb-1">Vista previa</p>
          <p className="text-sm text-amber-900 font-inter">{previewText}</p>
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
            {isEdit ? 'Guardar Cambios' : 'Crear Regla'}
          </Button>
        </div>
      </div>
    </Modal>
  );
});

export default RuleForm;
