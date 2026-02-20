'use client';

import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import FormSection from '@/components/ui/FormSection';
import Button from '@/components/ui/Button';
import type {
  FuzzySystem,
  CreateFuzzySystemRequest,
  UpdateFuzzySystemRequest,
} from '@hydroespinaca/shared';
import {
  DEFUZZIFICATION_METHODS,
  AND_METHODS,
  OR_METHODS,
  NOT_METHODS,
} from '@hydroespinaca/shared';

// ─── Props ───────────────────────────────────────────────────

interface SystemFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateFuzzySystemRequest | UpdateFuzzySystemRequest) => Promise<void>;
  isLoading?: boolean;
  /** If provided, the form is in edit mode */
  system?: FuzzySystem | null;
}

// ─── Default form values ─────────────────────────────────────

const defaultForm = {
  name: '',
  defuzzificationMethod: 'centroid',
  andMethod: 'min',
  orMethod: 'max',
  notMethod: 'complement',
};

// ─── Component ───────────────────────────────────────────────

const SystemForm: React.FC<SystemFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
  system = null,
}) => {
  const isEdit = !!system;

  const [form, setForm] = useState(defaultForm);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Populate form when editing
  useEffect(() => {
    if (system) {
      setForm({
        name: system.name,
        defuzzificationMethod: system.defuzzificationMethod,
        andMethod: system.operators.andMethod,
        orMethod: system.operators.orMethod,
        notMethod: system.operators.notMethod,
      });
    } else {
      setForm(defaultForm);
    }
    setErrors({});
  }, [system, isOpen]);

  // ─── Validation ──────────────────────────────────────────

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.name.trim()) {
      newErrors.name = 'El nombre es requerido';
    } else if (form.name.trim().length < 3) {
      newErrors.name = 'El nombre debe tener al menos 3 caracteres';
    }

    if (!form.defuzzificationMethod) {
      newErrors.defuzzificationMethod = 'El método de defuzzificación es requerido';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // ─── Submit ──────────────────────────────────────────────

  const handleSubmit = async () => {
    if (!validate()) return;

    const operators = {
      andMethod: form.andMethod,
      orMethod: form.orMethod,
      notMethod: form.notMethod,
    };

    if (isEdit) {
      const request: UpdateFuzzySystemRequest = {
        name: form.name.trim(),
        defuzzificationMethod: form.defuzzificationMethod,
        operators,
      };
      await onSubmit(request);
    } else {
      const request: CreateFuzzySystemRequest = {
        name: form.name.trim(),
        defuzzificationMethod: form.defuzzificationMethod,
        operators,
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
      title={isEdit ? 'Editar Sistema Fuzzy' : 'Crear Sistema Fuzzy'}
      maxWidth="lg"
    >
      <div className="space-y-6">
        {/* Name */}
        <FormField
          type="text"
          label="Nombre del Sistema"
          value={form.name}
          onChange={(val) => setForm({ ...form, name: val })}
          placeholder="Ej: Control de Riego Automático"
          required
          error={errors.name || ''}
        />

        {/* Defuzzification */}
        <FormField
          type="select"
          label="Método de Defuzzificación"
          value={form.defuzzificationMethod}
          onChange={(val) => setForm({ ...form, defuzzificationMethod: val })}
          options={DEFUZZIFICATION_METHODS.map((m) => ({ value: m.value, label: m.label }))}
          required
          error={errors.defuzzificationMethod || ''}
          helperText="Define cómo se calcula el valor nítido a partir de la salida difusa"
        />

        {/* Operators section */}
        <FormSection title="Operadores" subtitle="Configuración de operadores lógicos del sistema">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <FormField
              type="select"
              label="Método AND"
              value={form.andMethod}
              onChange={(val) => setForm({ ...form, andMethod: val })}
              options={AND_METHODS.map((m) => ({ value: m.value, label: m.label }))}
            />
            <FormField
              type="select"
              label="Método OR"
              value={form.orMethod}
              onChange={(val) => setForm({ ...form, orMethod: val })}
              options={OR_METHODS.map((m) => ({ value: m.value, label: m.label }))}
            />
            <FormField
              type="select"
              label="Método NOT"
              value={form.notMethod}
              onChange={(val) => setForm({ ...form, notMethod: val })}
              options={NOT_METHODS.map((m) => ({ value: m.value, label: m.label }))}
            />
          </div>
        </FormSection>

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
            {isEdit ? 'Guardar Cambios' : 'Crear Sistema'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default SystemForm;
