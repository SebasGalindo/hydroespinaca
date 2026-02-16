'use client';

import React, { useState } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';

import type { CreateCostConfigVersionRequest } from '@hydroespinaca/shared';

interface CostConfigFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateCostConfigVersionRequest) => Promise<void>;
  isLoading?: boolean;
}

const CostConfigForm: React.FC<CostConfigFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
}) => {
  const [form, setForm] = useState({
    currency: 'COP',
    electricityCostPerKwh: '',
    waterCostPerLiter: '',
    nutrientCostPerLiter: '',
    effectiveFrom: new Date().toISOString().split('T')[0] ?? '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

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

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    const request: CreateCostConfigVersionRequest = {
      currency: form.currency,
      electricityCostPerKwh: Number(form.electricityCostPerKwh),
      waterCostPerLiter: Number(form.waterCostPerLiter),
      nutrientCostPerLiter: Number(form.nutrientCostPerLiter),
      effectiveFrom: new Date(form.effectiveFrom!).toISOString(),
    };

    await onSubmit(request);
    handleClose();
  };

  const handleClose = () => {
    setForm({
      currency: 'COP',
      electricityCostPerKwh: '',
      waterCostPerLiter: '',
      nutrientCostPerLiter: '',
      effectiveFrom: new Date().toISOString().split('T')[0] ?? '',
    });
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Nueva Configuración de Costos" maxWidth="md">
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
          helperText="La configuración anterior se cerrará automáticamente"
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
            Crear Configuración
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default CostConfigForm;
