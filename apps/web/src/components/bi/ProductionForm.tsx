'use client';

import React, { useState } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';

import type { CreateProductionRecordRequest } from '@hydroespinaca/shared';

interface ProductionFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateProductionRecordRequest) => Promise<void>;
  isLoading?: boolean;
}

const ProductionForm = React.memo(function ProductionForm({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
}: ProductionFormProps) {
  const [form, setForm] = useState({
    cropName: '',
    startDate: '',
    harvestDate: '',
    kilosProduced: '',
    pricePerKilo: '',
    currency: 'COP',
    note: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.cropName.trim()) {
      newErrors.cropName = 'El nombre del cultivo es requerido';
    }
    if (!form.startDate) {
      newErrors.startDate = 'La fecha de inicio es requerida';
    }
    if (!form.harvestDate) {
      newErrors.harvestDate = 'La fecha de cosecha es requerida';
    }
    if (form.startDate && form.harvestDate && form.startDate >= form.harvestDate) {
      newErrors.harvestDate = 'La fecha de cosecha debe ser posterior al inicio';
    }
    if (!form.kilosProduced || Number(form.kilosProduced) <= 0) {
      newErrors.kilosProduced = 'Los kilos producidos deben ser mayores a 0';
    }
    if (!form.pricePerKilo || Number(form.pricePerKilo) <= 0) {
      newErrors.pricePerKilo = 'El precio por kilo debe ser mayor a 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    const trimmedNote = form.note.trim();
    const request: CreateProductionRecordRequest = {
      cropName: form.cropName.trim(),
      startDate: new Date(form.startDate).toISOString(),
      harvestDate: new Date(form.harvestDate).toISOString(),
      kilosProduced: Number(form.kilosProduced),
      pricePerKilo: Number(form.pricePerKilo),
      currency: form.currency,
      ...(trimmedNote ? { note: trimmedNote } : {}),
    };

    await onSubmit(request);
    handleClose();
  };

  const handleClose = () => {
    setForm({
      cropName: '',
      startDate: '',
      harvestDate: '',
      kilosProduced: '',
      pricePerKilo: '',
      currency: 'COP',
      note: '',
    });
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Registrar Producción" maxWidth="md">
      <div className="space-y-4">
        <FormField
          type="text"
          label="Nombre del Cultivo"
          value={form.cropName}
          onChange={(val) => setForm({ ...form, cropName: val })}
          placeholder="Ej: Espinaca hidropónica - Lote A"
          required
          error={errors.cropName ?? ''}
        />

        <div className="grid grid-cols-2 gap-3">
          <FormField
            type="date"
            label="Fecha de Inicio"
            value={form.startDate}
            onChange={(val) => setForm({ ...form, startDate: val })}
            required
            error={errors.startDate ?? ''}
          />
          <FormField
            type="date"
            label="Fecha de Cosecha"
            value={form.harvestDate}
            onChange={(val) => setForm({ ...form, harvestDate: val })}
            min={form.startDate}
            required
            error={errors.harvestDate ?? ''}
          />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <FormField
            type="number"
            label="Kilos Producidos"
            value={form.kilosProduced}
            onChange={(val) => setForm({ ...form, kilosProduced: val })}
            placeholder="Ej: 25.5"
            min="0"
            step="0.1"
            required
            error={errors.kilosProduced ?? ''}
          />
          <FormField
            type="number"
            label="Precio por Kilo"
            value={form.pricePerKilo}
            onChange={(val) => setForm({ ...form, pricePerKilo: val })}
            placeholder="Ej: 8000"
            min="0"
            step="1"
            required
            error={errors.pricePerKilo ?? ''}
          />
        </div>

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
          type="text"
          label="Nota (opcional)"
          value={form.note}
          onChange={(val) => setForm({ ...form, note: val })}
          placeholder="Ej: Primera cosecha del ciclo 2026"
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
            Registrar Producción
          </Button>
        </div>
      </div>
    </Modal>
  );
});

export default ProductionForm;
