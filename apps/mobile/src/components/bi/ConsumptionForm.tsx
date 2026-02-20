import React, { useState, useCallback } from 'react';
import { View, StyleSheet, Alert as RNAlert } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import {
  spacing,
  useBiStore,
  CONSUMPTION_TYPE_LABELS,
  type ConsumptionType,
  type CreateManualConsumptionEntryRequest,
} from '@hydroespinaca/shared';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { TextArea } from '../atoms/TextArea';
import { FormField } from '../molecules/FormField';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { toNoonUTC } from '../../utils/biHelpers';

interface ConsumptionFormProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

interface FormState {
  dateFrom: Date;
  dateTo: Date | null;
  type: string;
  amount: string;
  note: string;
}

interface FormErrors {
  type?: string;
  amount?: string;
  dateTo?: string;
}

const TYPE_OPTIONS = [
  { label: CONSUMPTION_TYPE_LABELS[1], value: '1' },
  { label: CONSUMPTION_TYPE_LABELS[2], value: '2' },
  { label: CONSUMPTION_TYPE_LABELS[3], value: '3' },
];

const UNIT_LABELS: Record<string, string> = {
  '1': 'kWh',
  '2': 'Litros',
  '3': 'Litros',
};

const initialForm: FormState = {
  dateFrom: new Date(),
  dateTo: null,
  type: '1',
  amount: '',
  note: '',
};

export function ConsumptionForm({
  isOpen,
  onClose,
  onCreated,
}: ConsumptionFormProps): React.ReactElement {
  const [form, setForm] = useState<FormState>({ ...initialForm });
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [showDateFromPicker, setShowDateFromPicker] = useState(false);
  const [showDateToPicker, setShowDateToPicker] = useState(false);

  const { createConsumptionEntry } = useBiStore();

  const validate = (): boolean => {
    const newErrors: FormErrors = {};
    const amount = parseFloat(form.amount);

    if (!form.type) {
      newErrors.type = 'Selecciona un tipo';
    }
    if (!form.amount || isNaN(amount) || amount <= 0) {
      newErrors.amount = 'Debe ser un número mayor a 0';
    }
    if (form.dateTo && form.dateTo < form.dateFrom) {
      newErrors.dateTo = 'La fecha fin no puede ser anterior a la de inicio';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = useCallback(async () => {
    if (!validate()) return;

    setSubmitting(true);
    try {
      const request: CreateManualConsumptionEntryRequest = {
        dateFrom: toNoonUTC(form.dateFrom),
        ...(form.dateTo ? { dateTo: toNoonUTC(form.dateTo) } : {}),
        type: parseInt(form.type, 10) as ConsumptionType,
        amount: parseFloat(form.amount),
        note: form.note.trim() || undefined,
      };
      await createConsumptionEntry(request);
      setForm({ ...initialForm });
      setErrors({});
      onCreated();
    } catch (error) {
      RNAlert.alert('Error', 'No se pudo registrar el consumo. Intenta de nuevo.');
    } finally {
      setSubmitting(false);
    }
  }, [form, createConsumptionEntry, onCreated]);

  const handleClose = useCallback(() => {
    setForm({ ...initialForm });
    setErrors({});
    onClose();
  }, [onClose]);

  const unitLabel = UNIT_LABELS[form.type] ?? '';

  return (
    <BottomSheetForm
      title="Nuevo Consumo"
      isOpen={isOpen}
      onClose={handleClose}
      snapPoints={['65%', '90%']}
    >
      <View style={styles.form}>
        <FormField label="Fecha desde">
          <Button
            variant="outline"
            size="md"
            onPress={() => setShowDateFromPicker(true)}
          >
            {form.dateFrom.toLocaleDateString('es-CO')}
          </Button>
        </FormField>

        {showDateFromPicker && (
          <DateTimePicker
            value={form.dateFrom}
            mode="date"
            display="default"
            maximumDate={new Date()}
            onChange={(_, date) => {
              setShowDateFromPicker(false);
              if (date) setForm((s) => ({ ...s, dateFrom: date }));
            }}
          />
        )}

        <FormField label="Fecha hasta (opcional)" error={errors.dateTo}>
          <Button
            variant="outline"
            size="md"
            onPress={() => setShowDateToPicker(true)}
          >
            {form.dateTo ? form.dateTo.toLocaleDateString('es-CO') : 'Sin definir (un solo día)'}
          </Button>
        </FormField>

        {showDateToPicker && (
          <DateTimePicker
            value={form.dateTo ?? form.dateFrom}
            mode="date"
            display="default"
            minimumDate={form.dateFrom}
            maximumDate={new Date()}
            onChange={(_, date) => {
              setShowDateToPicker(false);
              if (date) setForm((s) => ({ ...s, dateTo: date }));
            }}
          />
        )}

        <FormField label="Tipo de consumo" required error={errors.type}>
          <Select
            options={TYPE_OPTIONS}
            value={form.type}
            onValueChange={(val) => setForm((s) => ({ ...s, type: val }))}
            accessibilityLabel="Tipo de consumo"
            error={!!errors.type}
          />
        </FormField>

        <FormField
          label={`Cantidad (${unitLabel})`}
          required
          error={errors.amount}
        >
          <Input
            value={form.amount}
            onChangeText={(val) => setForm((s) => ({ ...s, amount: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.amount}
          />
        </FormField>

        <FormField label="Nota (opcional)">
          <TextArea
            value={form.note}
            onChangeText={(val) => setForm((s) => ({ ...s, note: val }))}
            placeholder="Observaciones adicionales..."
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
          Registrar Consumo
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
