'use client';

import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';

import type { CreateCostConfigVersionRequest, UpdateCostConfigVersionRequest, CostConfigVersion } from '@hydroespinaca/shared';

interface CostConfigFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateCostConfigVersionRequest | UpdateCostConfigVersionRequest) => Promise<void>;
  isLoading?: boolean;
  editData?: CostConfigVersion | null;
}

const getInitialForm = (editData?: CostConfigVersion | null) => ({
  currency: editData?.currency ?? 'COP',
  electricityCostPerKwh: editData ? String(editData.electricityCostPerKwh) : '',
  waterCostPerLiter: editData ? String(editData.waterCostPerLiter) : '',
  nutrientCostPerLiter: editData ? String(editData.nutrientCostPerLiter) : '',
  effectiveFrom: editData
    ? new Date(editData.effectiveFrom).toISOString().split('T')[0] ?? ''
    : new Date().toISOString().split('T')[0] ?? '',
  effectiveTo: editData?.effectiveTo
    ? new Date(editData.effectiveTo).toISOString().split('T')[0] ?? ''
    : '',
});

const CostConfigForm: React.FC<CostConfigFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
  editData = null,
}) => {
  const [form, setForm] = useState(getInitialForm(editData));
  const [errors, setErrors] = useState<Record<string, string>>({});

  const isEditMode = !!editData;

  useEffect(() => {
    if (isOpen) {
      setForm(getInitialForm(editData));
      setErrors({});
    }
  }, [isOpen, editData]);

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.electricityCostPerKwh || Number(form.electricityCostPerKwh) <= 0) {
      newErrors.electricityCostPerKwh = 'El costo de electricidad debe ser mayor a 0';
    }
    if (!form.waterCostPerLiter || Number(form.waterCostPerLiter) <= 0) {
      newErrors.waterCostPerLiter = 'El costo de agua debe ser mayor a 0';
    }
    if (!form.nutrientCostPerLiter || Number(form.nutrientCostPerLiter) <= 0) {
      newErrors.nutrientCostPerLiter = 'El costo de nutrientes debe ser mayor a 0';
    }
    if (!form.effectiveFrom) {
      newErrors.effectiveFrom = 'La fecha de inicio es requerida';
    }
    if (form.effectiveTo && form.effectiveFrom && new Date(form.effectiveTo) <= new Date(form.effectiveFrom)) {
      newErrors.effectiveTo = 'La fecha "hasta" debe ser posterior a la fecha "desde"';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    const payload = {
      currency: form.currency,
      electricityCostPerKwh: Number(form.electricityCostPerKwh),
      waterCostPerLiter: Number(form.waterCostPerLiter),
      nutrientCostPerLiter: Number(form.nutrientCostPerLiter),
      effectiveFrom: `${form.effectiveFrom}T12:00:00Z`,
      ...(form.effectiveTo ? { effectiveTo: `${form.effectiveTo}T12:00:00Z` } : {}),
    };

    await onSubmit(payload);
    handleClose();
  };

  const handleClose = () => {
    setForm(getInitialForm());
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={isEditMode ? 'Editar Configuración de Costos' : 'Nueva Configuración de Costos'} maxWidth="md">
      <div className="space-y-4">
        <FormField
          type="select"
          label="Moneda"
          value={form.currency}
          onChange={(val) => setForm({ ...form, currency: val })}
          options={[
            { value: 'COP', label: 'Peso Colombiano (COP)' },
            { value: 'USD', label: 'Dólar (USD)' },
          ]}
        />

        <FormField
          type="number"
          label="Costo Electricidad por kWh"
          value={form.electricityCostPerKwh}
          onChange={(val) => setForm({ ...form, electricityCostPerKwh: val })}
          placeholder="Ej: 950"
          min="0"
          step="0.01"
          required
          error={errors.electricityCostPerKwh ?? ''}
          helperText="Precio por kilowatt-hora en la moneda seleccionada"
        />

        <FormField
          type="number"
          label="Costo Agua por Litro"
          value={form.waterCostPerLiter}
          onChange={(val) => setForm({ ...form, waterCostPerLiter: val })}
          placeholder="Ej: 5.5"
          min="0"
          step="0.01"
          required
          error={errors.waterCostPerLiter ?? ''}
          helperText="Precio por litro de agua"
        />

        <FormField
          type="number"
          label="Costo Nutrientes por Litro"
          value={form.nutrientCostPerLiter}
          onChange={(val) => setForm({ ...form, nutrientCostPerLiter: val })}
          placeholder="Ej: 25"
          min="0"
          step="0.01"
          required
          error={errors.nutrientCostPerLiter ?? ''}
          helperText="Precio por litro de solución nutritiva"
        />

        <FormField
          type="date"
          label="Vigente desde"
          value={form.effectiveFrom}
          onChange={(val) => setForm({ ...form, effectiveFrom: val })}
          required
          error={errors.effectiveFrom ?? ''}
          helperText="La fecha a partir de la cual aplica esta configuración"
        />

        <FormField
          type="date"
          label="Vigente hasta"
          value={form.effectiveTo}
          onChange={(val) => setForm({ ...form, effectiveTo: val })}
          error={errors.effectiveTo ?? ''}
          helperText="Opcional — se calcula automáticamente si existe una versión posterior"
        />

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
            {isEditMode ? 'Guardar Cambios' : 'Crear Configuración'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default CostConfigForm;
