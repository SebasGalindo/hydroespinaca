'use client';

import React, { useState } from 'react';
import Modal from '@/components/ui/Modal';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';

import type { CreateManualConsumptionEntryRequest, ConsumptionType } from '@hydroespinaca/shared';

interface ConsumptionFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateManualConsumptionEntryRequest) => Promise<void>;
  isLoading?: boolean;
}

const ConsumptionForm = React.memo(function ConsumptionForm({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
}: ConsumptionFormProps) {
  const [form, setForm] = useState({
    dateFrom: new Date().toISOString().split('T')[0] ?? '',
    dateTo: '',
    type: '1',
    amount: '',
    note: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const typeOptions = [
    { value: '1', label: 'Electricidad (kWh)' },
    { value: '2', label: 'Agua (Litros)' },
    { value: '3', label: 'Nutrientes (Litros)' },
  ];

  const getUnitLabel = (): string => {
    switch (form.type) {
      case '1': return 'kWh';
      case '2': return 'Litros';
      case '3': return 'Litros';
      default: return '';
    }
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.dateFrom) {
      newErrors.dateFrom = 'La fecha de inicio es requerida';
    }
    if (form.dateTo && form.dateFrom && form.dateTo < form.dateFrom) {
      newErrors.dateTo = 'La fecha de fin no puede ser anterior a la de inicio';
    }
    if (!form.amount || Number(form.amount) <= 0) {
      newErrors.amount = 'La cantidad debe ser mayor a 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    const trimmedNote = form.note.trim();
    const request: CreateManualConsumptionEntryRequest = {
      dateFrom: `${form.dateFrom}T12:00:00Z`,
      ...(form.dateTo ? { dateTo: `${form.dateTo}T12:00:00Z` } : {}),
      type: Number(form.type) as ConsumptionType,
      amount: Number(form.amount),
      ...(trimmedNote ? { note: trimmedNote } : {}),
    };

    await onSubmit(request);
    handleClose();
  };

  const handleClose = () => {
    setForm({ dateFrom: new Date().toISOString().split('T')[0] ?? '', dateTo: '', type: '1', amount: '', note: '' });
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Registrar Consumo Manual" maxWidth="md">
      <div className="space-y-4">
        <FormField
          type="date"
          label="Fecha desde"
          value={form.dateFrom}
          onChange={(val) => setForm({ ...form, dateFrom: val })}
          required
          error={errors.dateFrom ?? ''}
        />

        <FormField
          type="date"
          label="Fecha hasta (opcional)"
          value={form.dateTo}
          onChange={(val) => setForm({ ...form, dateTo: val })}
          error={errors.dateTo ?? ''}
        />

        <FormField
          type="select"
          label="Tipo de Consumo"
          value={form.type}
          onChange={(val) => setForm({ ...form, type: val })}
          options={typeOptions}
          required
        />

        <FormField
          type="number"
          label={`Cantidad (${getUnitLabel()})`}
          value={form.amount}
          onChange={(val) => setForm({ ...form, amount: val })}
          placeholder={`Ej: ${form.type === '1' ? '15.5' : '100'}`}
          min="0"
          step="0.01"
          required
          error={errors.amount ?? ''}
        />

        <FormField
          type="text"
          label="Nota (opcional)"
          value={form.note}
          onChange={(val) => setForm({ ...form, note: val })}
          placeholder="Ej: Lectura del medidor semanal"
          maxLength={120}
          helperText={`${form.note.length}/120 caracteres`}
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
            Registrar Consumo
          </Button>
        </div>
      </div>
    </Modal>
  );
});

export default ConsumptionForm;
