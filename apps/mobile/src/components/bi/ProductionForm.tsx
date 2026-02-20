import React, { useState, useCallback } from 'react';
import { View, StyleSheet, Alert as RNAlert } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import {
  spacing,
  useBiStore,
  type CreateProductionRecordRequest,
} from '@hydroespinaca/shared';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { TextArea } from '../atoms/TextArea';
import { FormField } from '../molecules/FormField';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { CURRENCY_OPTIONS } from '../../utils/biHelpers';

interface ProductionFormProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

interface FormState {
  cropName: string;
  startDate: Date;
  harvestDate: Date;
  kilosProduced: string;
  pricePerKilo: string;
  currency: string;
  note: string;
}

interface FormErrors {
  cropName?: string;
  dates?: string;
  kilosProduced?: string;
  pricePerKilo?: string;
}

const initialForm: FormState = {
  cropName: '',
  startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
  harvestDate: new Date(),
  kilosProduced: '',
  pricePerKilo: '',
  currency: 'COP',
  note: '',
};

export function ProductionForm({
  isOpen,
  onClose,
  onCreated,
}: ProductionFormProps): React.ReactElement {
  const [form, setForm] = useState<FormState>({ ...initialForm });
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [showStartDate, setShowStartDate] = useState(false);
  const [showHarvestDate, setShowHarvestDate] = useState(false);

  const { createProductionRecord } = useBiStore();

  const validate = (): boolean => {
    const newErrors: FormErrors = {};
    const kilos = parseFloat(form.kilosProduced);
    const price = parseFloat(form.pricePerKilo);

    if (!form.cropName.trim()) {
      newErrors.cropName = 'El nombre del cultivo es requerido';
    }
    if (form.harvestDate <= form.startDate) {
      newErrors.dates = 'La fecha de cosecha debe ser posterior a la de inicio';
    }
    if (!form.kilosProduced || isNaN(kilos) || kilos <= 0) {
      newErrors.kilosProduced = 'Debe ser un número mayor a 0';
    }
    if (!form.pricePerKilo || isNaN(price) || price <= 0) {
      newErrors.pricePerKilo = 'Debe ser un número mayor a 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = useCallback(async () => {
    if (!validate()) return;

    setSubmitting(true);
    try {
      const request: CreateProductionRecordRequest = {
        cropName: form.cropName.trim(),
        startDate: form.startDate.toISOString(),
        harvestDate: form.harvestDate.toISOString(),
        kilosProduced: parseFloat(form.kilosProduced),
        pricePerKilo: parseFloat(form.pricePerKilo),
        currency: form.currency,
        note: form.note.trim() || undefined,
      };
      await createProductionRecord(request);
      setForm({ ...initialForm });
      setErrors({});
      onCreated();
    } catch (error) {
      RNAlert.alert('Error', 'No se pudo registrar la producción. Intenta de nuevo.');
    } finally {
      setSubmitting(false);
    }
  }, [form, createProductionRecord, onCreated]);

  const handleClose = useCallback(() => {
    setForm({ ...initialForm });
    setErrors({});
    onClose();
  }, [onClose]);

  return (
    <BottomSheetForm
      title="Nueva Producción"
      isOpen={isOpen}
      onClose={handleClose}
      snapPoints={['75%', '95%']}
    >
      <View style={styles.form}>
        <FormField label="Nombre del cultivo" required error={errors.cropName}>
          <Input
            value={form.cropName}
            onChangeText={(val) => setForm((s) => ({ ...s, cropName: val }))}
            placeholder="Ej: Espinaca Hidropónica"
            error={!!errors.cropName}
          />
        </FormField>

        <FormField label="Fecha de inicio" error={errors.dates}>
          <Button
            variant="outline"
            size="md"
            onPress={() => setShowStartDate(true)}
          >
            {form.startDate.toLocaleDateString('es-CO')}
          </Button>
        </FormField>

        {showStartDate && (
          <DateTimePicker
            value={form.startDate}
            mode="date"
            display="default"
            maximumDate={form.harvestDate}
            onChange={(_, date) => {
              setShowStartDate(false);
              if (date) setForm((s) => ({ ...s, startDate: date }));
            }}
          />
        )}

        <FormField label="Fecha de cosecha" error={errors.dates}>
          <Button
            variant="outline"
            size="md"
            onPress={() => setShowHarvestDate(true)}
          >
            {form.harvestDate.toLocaleDateString('es-CO')}
          </Button>
        </FormField>

        {showHarvestDate && (
          <DateTimePicker
            value={form.harvestDate}
            mode="date"
            display="default"
            minimumDate={form.startDate}
            maximumDate={new Date()}
            onChange={(_, date) => {
              setShowHarvestDate(false);
              if (date) setForm((s) => ({ ...s, harvestDate: date }));
            }}
          />
        )}

        <FormField label="Kilos producidos" required error={errors.kilosProduced}>
          <Input
            value={form.kilosProduced}
            onChangeText={(val) => setForm((s) => ({ ...s, kilosProduced: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.kilosProduced}
          />
        </FormField>

        <FormField label="Precio por kilo" required error={errors.pricePerKilo}>
          <Input
            value={form.pricePerKilo}
            onChangeText={(val) => setForm((s) => ({ ...s, pricePerKilo: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.pricePerKilo}
          />
        </FormField>

        <FormField label="Moneda">
          <Select
            options={CURRENCY_OPTIONS}
            value={form.currency}
            onValueChange={(val) => setForm((s) => ({ ...s, currency: val }))}
            accessibilityLabel="Moneda"
          />
        </FormField>

        <FormField label="Nota (opcional)">
          <TextArea
            value={form.note}
            onChangeText={(val) => setForm((s) => ({ ...s, note: val }))}
            placeholder="Observaciones sobre la cosecha..."
            minHeight={80}
          />
        </FormField>

        <Button
          variant="primary"
          size="lg"
          onPress={handleSubmit}
          loading={submitting}
          disabled={submitting}
          style={styles.submitButton}
        >
          Registrar Producción
        </Button>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.xs,
  },
  submitButton: {
    marginTop: spacing.md,
  },
});
