/**
 * VariableForm — Formulario para crear/editar variables fuzzy.
 * BottomSheet: nombre, tipo (input/output), actuador, universo min/max, código referencia.
 */
import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import { FormField } from '../molecules/FormField';
import { Input } from '../atoms/Input';
import { Select } from '../molecules/Select';
import { Button } from '../atoms/Button';
import { Alert } from '../molecules/Alert';
import type {
  FuzzyVariable,
  CreateFuzzyVariableRequest,
  UpdateFuzzyVariableRequest,
  VariableType,
  ActuatorType,
} from '@hydroespinaca/shared';
import { spacing, useFuzzyStore } from '@hydroespinaca/shared';

export interface VariableFormProps {
  isOpen: boolean;
  onClose: () => void;
  systemId: string;
  variable?: FuzzyVariable | null;
  onSuccess?: (variable: FuzzyVariable) => void;
}

const variableTypeOptions = [
  { label: 'Entrada', value: 'input' },
  { label: 'Salida', value: 'output' },
];

const actuatorTypeOptions = [
  { label: 'Ninguno', value: '' },
  { label: 'PWM', value: 'PWM' },
  { label: 'Digital', value: 'DIGITAL' },
];

export function VariableForm({
  isOpen,
  onClose,
  systemId,
  variable,
  onSuccess,
}: VariableFormProps): React.ReactElement {
  const isEditing = !!variable;
  const { createVariable, updateVariable, crudLoading, crudError, clearErrors } = useFuzzyStore();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [variableType, setVariableType] = useState<VariableType>('input');
  const [actuatorType, setActuatorType] = useState<string>('');
  const [universeMin, setUniverseMin] = useState('0');
  const [universeMax, setUniverseMax] = useState('100');
  const [referenceCode, setReferenceCode] = useState('');
  const [threshold, setThreshold] = useState('0.5');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      clearErrors();
      if (variable) {
        setName(variable.name);
        setDescription(variable.description || '');
        setVariableType(variable.variableType);
        setActuatorType(variable.actuatorType || '');
        setUniverseMin(String(variable.universeMin ?? 0));
        setUniverseMax(String(variable.universeMax ?? 100));
        setReferenceCode(variable.referenceCode || '');
        setThreshold(String(variable.defuzzificationThreshold ?? 0.5));
      } else {
        setName('');
        setDescription('');
        setVariableType('input');
        setActuatorType('');
        setUniverseMin('0');
        setUniverseMax('100');
        setReferenceCode('');
        setThreshold('0.5');
      }
      setError(null);
    }
  }, [isOpen, variable, clearErrors]);

  const handleSubmit = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('El nombre es obligatorio');
      return;
    }

    const minVal = parseFloat(universeMin);
    const maxVal = parseFloat(universeMax);
    if (isNaN(minVal) || isNaN(maxVal) || minVal >= maxVal) {
      setError('El rango del universo debe ser válido (min < max)');
      return;
    }

    try {
      if (isEditing && variable) {
        const request: UpdateFuzzyVariableRequest = {
          name: trimmedName,
          description: description.trim() || undefined,
          variableType,
          actuatorType: (variableType === 'output' && actuatorType ? actuatorType as ActuatorType : undefined),
          universeMin: minVal,
          universeMax: maxVal,
          referenceCode: referenceCode.trim() || undefined,
          defuzzificationThreshold: parseFloat(threshold) || 0.5,
        };
        const updated = await updateVariable(variable.id, request);
        onSuccess?.(updated);
      } else {
        const request: CreateFuzzyVariableRequest = {
          systemId,
          name: trimmedName,
          variableType,
          description: description.trim() || undefined,
          actuatorType: (variableType === 'output' && actuatorType ? actuatorType as ActuatorType : undefined),
          universeMin: minVal,
          universeMax: maxVal,
          referenceCode: referenceCode.trim() || undefined,
          defuzzificationThreshold: parseFloat(threshold) || 0.5,
        };
        const created = await createVariable(request);
        onSuccess?.(created);
      }
      onClose();
    } catch {
      // Handled by store
    }
  };

  return (
    <BottomSheetForm
      title={isEditing ? 'Editar Variable' : 'Nueva Variable'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['75%', '95%']}
    >
      <View style={styles.form}>
        {(error || crudError) && (
          <Alert
            type="error"
            message={error || crudError || ''}
            onDismiss={() => { setError(null); clearErrors(); }}
          />
        )}

        <FormField label="Nombre" required>
          <Input value={name} onChangeText={setName} placeholder="Ej: Temperatura" autoFocus />
        </FormField>

        <FormField label="Descripción">
          <Input value={description} onChangeText={setDescription} placeholder="Descripción opcional" />
        </FormField>

        <FormField label="Tipo de variable" required>
          <Select
            options={variableTypeOptions}
            value={variableType}
            onValueChange={(v) => setVariableType(v as VariableType)}
            accessibilityLabel="Tipo de variable"
          />
        </FormField>

        {variableType === 'output' && (
          <FormField label="Tipo de actuador">
            <Select
              options={actuatorTypeOptions}
              value={actuatorType}
              onValueChange={setActuatorType}
              accessibilityLabel="Tipo de actuador"
            />
          </FormField>
        )}

        <View style={styles.row}>
          <FormField label="Universo mín" required style={styles.halfField}>
            <Input
              value={universeMin}
              onChangeText={setUniverseMin}
              keyboardType="numeric"
              placeholder="0"
            />
          </FormField>
          <FormField label="Universo máx" required style={styles.halfField}>
            <Input
              value={universeMax}
              onChangeText={setUniverseMax}
              keyboardType="numeric"
              placeholder="100"
            />
          </FormField>
        </View>

        <FormField label="Código de referencia" helperText="Identificador único para simulación">
          <Input
            value={referenceCode}
            onChangeText={setReferenceCode}
            placeholder="Ej: TEMP_01"
            autoCapitalize="characters"
          />
        </FormField>

        <FormField label="Umbral de defuzzificación">
          <Input
            value={threshold}
            onChangeText={setThreshold}
            keyboardType="numeric"
            placeholder="0.5"
          />
        </FormField>

        <Button
          variant="primary"
          onPress={handleSubmit}
          loading={crudLoading}
          fullWidth
          style={styles.submitButton}
        >
          {isEditing ? 'Guardar Cambios' : 'Crear Variable'}
        </Button>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  halfField: {
    flex: 1,
  },
  submitButton: {
    marginTop: spacing.md,
  },
});
