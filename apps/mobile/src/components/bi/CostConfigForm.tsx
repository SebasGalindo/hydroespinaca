import React, { useState, useCallback, useEffect } from 'react';
import { View, StyleSheet, Alert as RNAlert } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import {
  spacing,
  useBiStore,
  type CreateCostConfigVersionRequest,
  type UpdateCostConfigVersionRequest,
  type CostConfigVersion,
} from '@hydroespinaca/shared';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { FormField } from '../molecules/FormField';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { CURRENCY_OPTIONS, getTodayISO, toNoonUTC } from '../../utils/biHelpers';

interface CostConfigFormProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
  editData?: CostConfigVersion | null;
}

interface FormState {
  currency: string;
  electricityCostPerKwh: string;
  waterCostPerLiter: string;
  nutrientCostPerLiter: string;
  effectiveFrom: Date;
}

interface FormErrors {
  electricityCostPerKwh?: string;
  waterCostPerLiter?: string;
  nutrientCostPerLiter?: string;
}

const buildInitialForm = (editData?: CostConfigVersion | null): FormState => ({
  currency: editData?.currency ?? 'COP',
  electricityCostPerKwh: editData ? String(editData.electricityCostPerKwh) : '',
  waterCostPerLiter: editData ? String(editData.waterCostPerLiter) : '',
  nutrientCostPerLiter: editData ? String(editData.nutrientCostPerLiter) : '',
  effectiveFrom: editData ? new Date(editData.effectiveFrom) : new Date(),
});

export function CostConfigForm({
  isOpen,
  onClose,
  onCreated,
  editData = null,
}: CostConfigFormProps): React.ReactElement {
  const isEditMode = !!editData;

  const [form, setForm] = useState<FormState>(buildInitialForm(editData));
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [showDatePicker, setShowDatePicker] = useState(false);

  const { createCostConfigVersion, updateCostConfigVersion } = useBiStore();

  useEffect(() => {
    if (isOpen) {
      setForm(buildInitialForm(editData));
      setErrors({});
    }
  }, [isOpen, editData]);

  const validate = (): boolean => {
    const newErrors: FormErrors = {};
    const elec = parseFloat(form.electricityCostPerKwh);
    const water = parseFloat(form.waterCostPerLiter);
    const nutrient = parseFloat(form.nutrientCostPerLiter);

    if (!form.electricityCostPerKwh || isNaN(elec) || elec <= 0) {
      newErrors.electricityCostPerKwh = 'Debe ser un número mayor a 0';
    }
    if (!form.waterCostPerLiter || isNaN(water) || water <= 0) {
      newErrors.waterCostPerLiter = 'Debe ser un número mayor a 0';
    }
    if (!form.nutrientCostPerLiter || isNaN(nutrient) || nutrient <= 0) {
      newErrors.nutrientCostPerLiter = 'Debe ser un número mayor a 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = useCallback(async () => {
    if (!validate()) return;

    setSubmitting(true);
    try {
      const payload = {
        currency: form.currency,
        electricityCostPerKwh: parseFloat(form.electricityCostPerKwh),
        waterCostPerLiter: parseFloat(form.waterCostPerLiter),
        nutrientCostPerLiter: parseFloat(form.nutrientCostPerLiter),
        effectiveFrom: toNoonUTC(form.effectiveFrom),
      };

      if (isEditMode && editData) {
        await updateCostConfigVersion(editData.id, payload);
      } else {
        await createCostConfigVersion(payload);
      }
      setForm(buildInitialForm());
      setErrors({});
      onCreated();
    } catch (error) {
      RNAlert.alert('Error', isEditMode
        ? 'No se pudo actualizar la versión de costos. Intenta de nuevo.'
        : 'No se pudo crear la versión de costos. Intenta de nuevo.');
    } finally {
      setSubmitting(false);
    }
  }, [form, createCostConfigVersion, updateCostConfigVersion, editData, isEditMode, onCreated]);

  const handleClose = useCallback(() => {
    setForm(buildInitialForm());
    setErrors({});
    onClose();
  }, [onClose]);

  return (
    <BottomSheetForm
      title={isEditMode ? 'Editar Versión de Costos' : 'Nueva Versión de Costos'}
      isOpen={isOpen}
      onClose={handleClose}
      snapPoints={['70%', '90%']}
    >
      <View style={styles.form}>
        <FormField label="Moneda">
          <Select
            options={CURRENCY_OPTIONS}
            value={form.currency}
            onValueChange={(val) => setForm((s) => ({ ...s, currency: val }))}
            accessibilityLabel="Moneda"
          />
        </FormField>

        <FormField
          label="Costo Electricidad por kWh"
          required
          error={errors.electricityCostPerKwh}
        >
          <Input
            value={form.electricityCostPerKwh}
            onChangeText={(val) => setForm((s) => ({ ...s, electricityCostPerKwh: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.electricityCostPerKwh}
          />
        </FormField>

        <FormField
          label="Costo Agua por Litro"
          required
          error={errors.waterCostPerLiter}
        >
          <Input
            value={form.waterCostPerLiter}
            onChangeText={(val) => setForm((s) => ({ ...s, waterCostPerLiter: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.waterCostPerLiter}
          />
        </FormField>

        <FormField
          label="Costo Nutrientes por Litro"
          required
          error={errors.nutrientCostPerLiter}
        >
          <Input
            value={form.nutrientCostPerLiter}
            onChangeText={(val) => setForm((s) => ({ ...s, nutrientCostPerLiter: val }))}
            keyboardType="decimal-pad"
            placeholder="0.00"
            error={!!errors.nutrientCostPerLiter}
          />
        </FormField>

        <FormField label="Vigente desde">
          <Button
            variant="outline"
            size="md"
            onPress={() => setShowDatePicker(true)}
          >
            {form.effectiveFrom.toLocaleDateString('es-CO')}
          </Button>
        </FormField>

        {showDatePicker && (
          <DateTimePicker
            value={form.effectiveFrom}
            mode="date"
            display="default"
            onChange={(_, date) => {
              setShowDatePicker(false);
              if (date) setForm((s) => ({ ...s, effectiveFrom: date }));
            }}
          />
        )}

        <Button
          variant="primary"
          size="lg"
          onPress={handleSubmit}
          loading={submitting}
          disabled={submitting}
          style={styles.submitButton}
        >
          {isEditMode ? 'Guardar Cambios' : 'Crear Versión'}
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
