/**
 * SystemForm — Formulario para crear/editar sistemas fuzzy.
 * BottomSheet con nombre, método de defuzzificación, operadores.
 */
import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { FormField } from '../molecules/FormField';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { Button } from '../atoms/Button';
import { Alert } from '../molecules/Alert';
import type {
  FuzzySystem,
  CreateFuzzySystemRequest,
  UpdateFuzzySystemRequest,
} from '@hydroespinaca/shared';
import {
  spacing,
  DEFUZZIFICATION_METHODS,
  AND_METHODS,
  OR_METHODS,
  NOT_METHODS,
  useFuzzyStore,
} from '@hydroespinaca/shared';

export interface SystemFormProps {
  isOpen: boolean;
  onClose: () => void;
  system?: FuzzySystem | null;
  onSuccess?: (system: FuzzySystem) => void;
}

export function SystemForm({
  isOpen,
  onClose,
  system,
  onSuccess,
}: SystemFormProps): React.ReactElement {
  const isEditing = !!system;
  const { createSystem, updateSystem, crudLoading, crudError, clearErrors } = useFuzzyStore();

  const [name, setName] = useState('');
  const [defuzzMethod, setDefuzzMethod] = useState('centroid');
  const [andMethod, setAndMethod] = useState('min');
  const [orMethod, setOrMethod] = useState('max');
  const [notMethod, setNotMethod] = useState('complement');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      clearErrors();
      if (system) {
        setName(system.name);
        setDefuzzMethod(system.defuzzificationMethod || 'centroid');
        setAndMethod(system.operators?.andMethod || 'min');
        setOrMethod(system.operators?.orMethod || 'max');
        setNotMethod(system.operators?.notMethod || 'complement');
      } else {
        setName('');
        setDefuzzMethod('centroid');
        setAndMethod('min');
        setOrMethod('max');
        setNotMethod('complement');
      }
      setError(null);
    }
  }, [isOpen, system, clearErrors]);

  const defuzzOptions = DEFUZZIFICATION_METHODS.map((m) => ({
    label: m.label,
    value: m.value,
  }));

  const andOptions = AND_METHODS.map((m) => ({
    label: m.label,
    value: m.value,
  }));

  const orOptions = OR_METHODS.map((m) => ({
    label: m.label,
    value: m.value,
  }));

  const notOptions = NOT_METHODS.map((m) => ({
    label: m.label,
    value: m.value,
  }));

  const handleSubmit = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('El nombre es obligatorio');
      return;
    }

    try {
      if (isEditing && system) {
        const request: UpdateFuzzySystemRequest = {
          name: trimmedName,
          defuzzificationMethod: defuzzMethod,
          operators: {
            andMethod,
            orMethod,
            notMethod,
          },
        };
        const updated = await updateSystem(system.id, request);
        onSuccess?.(updated);
      } else {
        const request: CreateFuzzySystemRequest = {
          name: trimmedName,
          defuzzificationMethod: defuzzMethod,
          operators: {
            andMethod,
            orMethod,
            notMethod,
          },
        };
        const created = await createSystem(request);
        onSuccess?.(created);
      }
      onClose();
    } catch {
      // Error handled by store
    }
  };

  return (
    <BottomSheetForm
      title={isEditing ? 'Editar Sistema' : 'Nuevo Sistema Fuzzy'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['70%', '90%']}
    >
      <View style={styles.form}>
        {(error || crudError) && (
          <Alert
            type="error"
            message={error || crudError || ''}
            onDismiss={() => { setError(null); clearErrors(); }}
            style={styles.alert}
          />
        )}

        <FormField label="Nombre del sistema" required>
          <Input
            value={name}
            onChangeText={setName}
            placeholder="Ej: Control de riego"
            autoFocus
          />
        </FormField>

        <FormField label="Método de defuzzificación">
          <Select
            options={defuzzOptions}
            value={defuzzMethod}
            onValueChange={setDefuzzMethod}
            accessibilityLabel="Método de defuzzificación"
          />
        </FormField>

        <FormField label="Operador AND">
          <Select
            options={andOptions}
            value={andMethod}
            onValueChange={setAndMethod}
            accessibilityLabel="Operador AND"
          />
        </FormField>

        <FormField label="Operador OR">
          <Select
            options={orOptions}
            value={orMethod}
            onValueChange={setOrMethod}
            accessibilityLabel="Operador OR"
          />
        </FormField>

        <FormField label="Método NOT">
          <Select
            options={notOptions}
            value={notMethod}
            onValueChange={setNotMethod}
            accessibilityLabel="Método NOT"
          />
        </FormField>

        <Button
          variant="primary"
          onPress={handleSubmit}
          loading={crudLoading}
          fullWidth
          style={styles.submitButton}
        >
          {isEditing ? 'Guardar Cambios' : 'Crear Sistema'}
        </Button>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.sm,
  },
  alert: {
    marginBottom: spacing.sm,
  },
  submitButton: {
    marginTop: spacing.md,
  },
});
