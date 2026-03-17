'use client';

import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';
import type {
  FuzzyVariable,
  CreateFuzzyVariableRequest,
  UpdateFuzzyVariableRequest,
  VariableType,
  ActuatorType,
} from '@hydroespinaca/shared';

// ─── Props ───────────────────────────────────────────────────

interface VariableFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateFuzzyVariableRequest | UpdateFuzzyVariableRequest) => Promise<void>;
  isLoading?: boolean;
  systemId: string;
  /** If provided, the form is in edit mode */
  variable?: FuzzyVariable | null;
}

// ─── Default form values ─────────────────────────────────────

const defaultForm = {
  name: '',
  description: '',
  variableType: 'input' as VariableType,
  actuatorType: '' as string,
  defuzzificationThreshold: '0.5',
  universeMin: '0',
  universeMax: '100',
  referenceCode: '',
};

// ─── Component ───────────────────────────────────────────────

const VariableForm = React.memo(function VariableForm({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
  systemId,
  variable = null,
}: VariableFormProps) {
  const isEdit = !!variable;

  const [form, setForm] = useState(defaultForm);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Populate form when editing
  useEffect(() => {
    if (variable) {
      setForm({
        name: variable.name,
        description: variable.description ?? '',
        variableType: variable.variableType,
        actuatorType: variable.actuatorType ?? '',
        defuzzificationThreshold: String(variable.defuzzificationThreshold),
        universeMin: variable.universeMin != null ? String(variable.universeMin) : '0',
        universeMax: variable.universeMax != null ? String(variable.universeMax) : '100',
        referenceCode: variable.referenceCode ?? '',
      });
    } else {
      setForm(defaultForm);
    }
    setErrors({});
  }, [variable, isOpen]);

  const isOutput = form.variableType === 'output';

  // ─── Validation ──────────────────────────────────────────

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.name.trim()) {
      newErrors.name = 'El nombre es requerido';
    }

    if (form.universeMin !== '' && form.universeMax !== '' &&
        Number(form.universeMin) >= Number(form.universeMax)) {
      newErrors.universeMax = 'El máximo debe ser mayor al mínimo';
    }

    if (isOutput && !form.actuatorType) {
      newErrors.actuatorType = 'El tipo de actuador es requerido para variables de salida';
    }

    if (isOutput && !form.referenceCode.trim()) {
      newErrors.referenceCode = 'El código de referencia es requerido para variables de salida';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // ─── Submit ──────────────────────────────────────────────

  const handleSubmit = async () => {
    if (!validate()) return;

    if (isEdit) {
      const request: UpdateFuzzyVariableRequest = {
        name: form.name.trim(),
        ...(form.description.trim() ? { description: form.description.trim() } : {}),
        variableType: form.variableType,
        ...(isOutput && form.actuatorType ? { actuatorType: form.actuatorType as ActuatorType } : {}),
        defuzzificationThreshold: Number(form.defuzzificationThreshold),
        ...(form.universeMin !== '' ? { universeMin: Number(form.universeMin) } : {}),
        ...(form.universeMax !== '' ? { universeMax: Number(form.universeMax) } : {}),
        ...(form.referenceCode.trim() ? { referenceCode: form.referenceCode.trim() } : {}),
      };
      await onSubmit(request);
    } else {
      const request: CreateFuzzyVariableRequest = {
        systemId,
        name: form.name.trim(),
        variableType: form.variableType,
        ...(form.description.trim() ? { description: form.description.trim() } : {}),
        ...(isOutput && form.actuatorType ? { actuatorType: form.actuatorType as ActuatorType } : {}),
        defuzzificationThreshold: Number(form.defuzzificationThreshold),
        ...(form.universeMin !== '' ? { universeMin: Number(form.universeMin) } : {}),
        ...(form.universeMax !== '' ? { universeMax: Number(form.universeMax) } : {}),
        ...(form.referenceCode.trim() ? { referenceCode: form.referenceCode.trim() } : {}),
      };
      await onSubmit(request);
    }
    handleClose();
  };

  // ─── Close ───────────────────────────────────────────────

  const handleClose = () => {
    setForm(defaultForm);
    setErrors({});
    onClose();
  };

  // ─── Render ──────────────────────────────────────────────

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={isEdit ? 'Editar Variable' : 'Crear Variable'}
      maxWidth="lg"
    >
      <div className="space-y-4">
        {/* Name */}
        <FormField
          type="text"
          label="Nombre"
          value={form.name}
          onChange={(val) => setForm({ ...form, name: val })}
          placeholder="Ej: Temperatura Ambiente"
          required
          error={errors.name || ''}
        />

        {/* Description */}
        <FormField
          type="text"
          label="Descripción (opcional)"
          value={form.description}
          onChange={(val) => setForm({ ...form, description: val })}
          placeholder="Ej: Sensor de temperatura del invernadero"
        />

        {/* Type + Actuator */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            type="select"
            label="Tipo de Variable"
            value={form.variableType}
            onChange={(val) => setForm({ ...form, variableType: val as VariableType })}
            options={[
              { value: 'input', label: 'Entrada' },
              { value: 'output', label: 'Salida' },
            ]}
            required
          />

          {isOutput && (
            <FormField
              type="select"
              label="Tipo de Actuador"
              value={form.actuatorType}
              onChange={(val) => setForm({ ...form, actuatorType: val })}
              options={[
                { value: 'PWM', label: 'PWM (Analógico)' },
                { value: 'DIGITAL', label: 'Digital (ON/OFF)' },
              ]}
              required
              error={errors.actuatorType || ''}
            />
          )}
        </div>

        {/* Universe range */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            type="number"
            label="Universo Mínimo"
            value={form.universeMin}
            onChange={(val) => setForm({ ...form, universeMin: val })}
            placeholder="0"
            step="0.1"
            helperText="Valor mínimo del rango"
          />
          <FormField
            type="number"
            label="Universo Máximo"
            value={form.universeMax}
            onChange={(val) => setForm({ ...form, universeMax: val })}
            placeholder="100"
            step="0.1"
            error={errors.universeMax || ''}
            helperText="Valor máximo del rango"
          />
        </div>

        {/* Reference code + threshold */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            type="text"
            label="Código de Referencia"
            value={form.referenceCode}
            onChange={(val) => setForm({ ...form, referenceCode: val })}
            placeholder="Ej: TEMP_AMB"
            error={errors.referenceCode || ''}
            helperText="Código único para identificar la variable en el ESP32"
          />

          {isOutput && (
            <FormField
              type="number"
              label="Umbral de Defuzzificación"
              value={form.defuzzificationThreshold}
              onChange={(val) => setForm({ ...form, defuzzificationThreshold: val })}
              min="0"
              max="1"
              step="0.05"
              helperText="Umbral para activar el actuador (0-1)"
            />
          )}
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
            {isEdit ? 'Guardar Cambios' : 'Crear Variable'}
          </Button>
        </div>
      </div>
    </Modal>
  );
});

export default VariableForm;
