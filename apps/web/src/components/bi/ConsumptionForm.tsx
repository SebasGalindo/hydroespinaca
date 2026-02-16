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

const ConsumptionForm: React.FC<ConsumptionFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isLoading = false,
}) => {
  const [form, setForm] = useState({
    date: new Date().toISOString().split('T')[0] ?? '',
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

    if (!form.date) {
      newErrors.date = 'La fecha es requerida';
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
      date: new Date(form.date!).toISOString(),
      type: Number(form.type) as ConsumptionType,
      amount: Number(form.amount),
      ...(trimmedNote ? { note: trimmedNote } : {}),
    };

    await onSubmit(request);
    handleClose();
  };

  const handleClose = () => {
    setForm({ date: new Date().toISOString().split('T')[0] ?? '', type: '1', amount: '', note: '' });
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Registrar Consumo Manual" maxWidth="md">
      <div className="space-y-4">
        <FormField
          type="date"
          label="Fecha"
          value={form.date}
          onChange={(val) => setForm({ ...form, date: val })}
          required
          error={errors.date ?? ''}
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
};

export default ConsumptionForm;
