'use client';

import React, { useState, useEffect } from 'react';
import FormField from '@/components/ui/FormField';
import Button from '@/components/ui/Button';

export interface SensorData {
  id: string;
  physicalId: string;
  location: string;
  esp32Id: string;
  readFrequency: number;
  variables: string[];
  status: 'active' | 'inactive' | 'error' | 'calibrating';
  type: 'temperature' | 'humidity' | 'ph' | 'conductivity' | 'light' | 'water_level';
  currentValue: string;
  unit: string;
  lastCalibration: string;
  calibrationDue: boolean;
  minRange: number;
  maxRange: number;
  optimalMin: number;
  optimalMax: number;
  minValue?: number;
  maxValue?: number;
}

interface SensorFormProps {
  sensor?: SensorData;
  onSubmit: (sensorData: Omit<SensorData, 'id'>) => void;
  onCancel: () => void;
  isEditing?: boolean;
}

const SensorForm: React.FC<SensorFormProps> = ({ sensor, onSubmit, onCancel, isEditing = false }) => {
  const getCurrentDate = (): string => {
    return new Date().toISOString().split('T')[0] || new Date().toLocaleDateString('en-CA');
  };

  const [formData, setFormData] = useState<Omit<SensorData, 'id'>>(() => {
    if (sensor) {
      return {
        physicalId: sensor.physicalId,
        location: sensor.location,
        esp32Id: sensor.esp32Id,
        readFrequency: sensor.readFrequency,
        variables: sensor.variables,
        status: sensor.status,
        type: sensor.type,
        currentValue: sensor.currentValue,
        unit: sensor.unit,
        lastCalibration: sensor.lastCalibration,
        calibrationDue: sensor.calibrationDue,
        minRange: sensor.minRange,
        maxRange: sensor.maxRange,
        optimalMin: sensor.optimalMin,
        optimalMax: sensor.optimalMax,
        minValue: sensor.minValue ?? 0,
        maxValue: sensor.maxValue ?? 100
      };
    }
    return {
      physicalId: '',
      location: '',
      esp32Id: '',
      readFrequency: 5,
      variables: [],
      status: 'active',
      type: 'temperature',
      currentValue: '0',
      unit: '',
      lastCalibration: getCurrentDate(),
      calibrationDue: false,
      minRange: 0,
      maxRange: 100,
      optimalMin: 20,
      optimalMax: 80,
      minValue: 0,
      maxValue: 100
    };
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Auto-completar cuando se está editando
  useEffect(() => {
    if (isEditing && sensor) {
      setFormData({
        physicalId: sensor.physicalId,
        location: sensor.location,
        esp32Id: sensor.esp32Id,
        readFrequency: sensor.readFrequency,
        variables: sensor.variables,
        status: sensor.status,
        type: sensor.type,
        currentValue: sensor.currentValue,
        unit: sensor.unit,
        lastCalibration: sensor.lastCalibration,
        calibrationDue: sensor.calibrationDue,
        minRange: sensor.minRange,
        maxRange: sensor.maxRange,
        optimalMin: sensor.optimalMin,
        optimalMax: sensor.optimalMax,
        minValue: sensor.minValue ?? 0,
        maxValue: sensor.maxValue ?? 100
      });
    }
  }, [isEditing, sensor]);

  const handleInputChange = (field: keyof Omit<SensorData, 'id'>, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Limpiar error del campo cuando el usuario empiece a escribir
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.physicalId.trim()) {
      newErrors.physicalId = 'El ID físico es requerido';
    }

    if (!formData.location.trim()) {
      newErrors.location = 'La ubicación es requerida';
    }

    if (!formData.esp32Id.trim()) {
      newErrors.esp32Id = 'El ESP32 ID es requerido';
    }

    if (formData.readFrequency <= 0) {
      newErrors.readFrequency = 'La frecuencia debe ser mayor a 0';
    }

    if (formData.variables.length === 0) {
      newErrors.variables = 'Debe seleccionar al menos una variable';
    }

    if (formData.minValue !== undefined && formData.maxValue !== undefined && formData.minValue >= formData.maxValue) {
      newErrors.minValue = 'El valor mínimo debe ser menor al máximo';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  const variableOptions = [
    { value: 'temperatura', label: 'Temperatura' },
    { value: 'humedad', label: 'Humedad' },
    { value: 'luz', label: 'Luz' },
    { value: 'ph', label: 'pH' },
    { value: 'conductividad', label: 'Conductividad' },
    { value: 'nivel_agua', label: 'Nivel de Agua' }
  ];

  const statusOptions = [
    { value: 'active', label: 'Activo' },
    { value: 'inactive', label: 'Inactivo' },
    { value: 'error', label: 'Error' },
    { value: 'calibrating', label: 'Calibrando' }
  ];

  const typeOptions = [
    { value: 'temperature', label: 'Temperatura' },
    { value: 'humidity', label: 'Humedad' },
    { value: 'ph', label: 'pH' },
    { value: 'conductivity', label: 'Conductividad' },
    { value: 'light', label: 'Luz' },
    { value: 'water_level', label: 'Nivel de Agua' }
  ];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* ID Físico */}
      <FormField
        type="text"
        label="ID Físico del Sensor"
        value={formData.physicalId}
        onChange={(value) => handleInputChange('physicalId', value)}
        placeholder="Ej: SENS_001"
        required
        {...(errors.physicalId && { error: errors.physicalId })}
      />

      {/* Ubicación */}
      <FormField
        type="text"
        label="Ubicación"
        value={formData.location}
        onChange={(value) => handleInputChange('location', value)}
        placeholder="Ej: Invernadero A - Zona 1"
        required
        {...(errors.location && { error: errors.location })}
      />

      {/* ESP32 ID */}
      <FormField
        type="text"
        label="ESP32 ID"
        value={formData.esp32Id}
        onChange={(value) => handleInputChange('esp32Id', value)}
        placeholder="Ej: ESP32_001"
        required
        {...(errors.esp32Id && { error: errors.esp32Id })}
      />

      {/* Frecuencia de Lectura */}
      <FormField
        type="number"
        label="Frecuencia de Lectura (segundos)"
        value={formData.readFrequency.toString()}
        onChange={(value) => handleInputChange('readFrequency', parseInt(value) || 5)}
        placeholder="5"
        min="1"
        required
        {...(errors.readFrequency && { error: errors.readFrequency })}
      />

      {/* Variables */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Variables a Medir
        </label>
        <div className="grid grid-cols-2 gap-2">
          {variableOptions.map((option) => (
            <label key={option.value} className="flex items-center space-x-2">
              <input
                type="checkbox"
                checked={formData.variables.includes(option.value)}
                onChange={(e) => {
                  if (e.target.checked) {
                    handleInputChange('variables', [...formData.variables, option.value]);
                  } else {
                    handleInputChange('variables', formData.variables.filter(v => v !== option.value));
                  }
                }}
                className="rounded border-gray-300 text-green-600 focus:ring-green-500"
              />
              <span className="text-sm text-gray-700">{option.label}</span>
            </label>
          ))}
        </div>
        {errors.variables && <p className="text-red-500 text-sm mt-1">{errors.variables}</p>}
      </div>

      {/* Tipo de Sensor */}
      <FormField
        type="select"
        label="Tipo de Sensor"
        value={formData.type}
        onChange={(value) => handleInputChange('type', value)}
        options={typeOptions}
        required
      />

      {/* Valor Actual */}
      <FormField
        type="text"
        label="Valor Actual"
        value={formData.currentValue}
        onChange={(value) => handleInputChange('currentValue', value)}
        placeholder="0"
        required
      />

      {/* Unidad */}
      <FormField
        type="text"
        label="Unidad de Medida"
        value={formData.unit}
        onChange={(value) => handleInputChange('unit', value)}
        placeholder="Ej: °C, %, pH"
        required
      />

      {/* Última Calibración */}
      <FormField
        type="date"
        label="Última Calibración"
        value={formData.lastCalibration}
        onChange={(value) => handleInputChange('lastCalibration', value)}
        required
      />

      {/* Calibración Pendiente */}
      <div>
        <label className="flex items-center space-x-2">
          <input
            type="checkbox"
            checked={formData.calibrationDue}
            onChange={(e) => handleInputChange('calibrationDue', e.target.checked)}
            className="rounded border-gray-300 text-green-600 focus:ring-green-500"
          />
          <span className="text-sm font-medium text-gray-700">Calibración Pendiente</span>
        </label>
      </div>

      {/* Rangos del Sensor */}
      <div className="grid grid-cols-2 gap-4">
        <FormField
          type="number"
          label="Rango Mínimo"
          value={formData.minRange.toString()}
          onChange={(value) => handleInputChange('minRange', parseFloat(value) || 0)}
          placeholder="0"
          required
        />
        <FormField
          type="number"
          label="Rango Máximo"
          value={formData.maxRange.toString()}
          onChange={(value) => handleInputChange('maxRange', parseFloat(value) || 100)}
          placeholder="100"
          required
        />
      </div>

      {/* Rangos Óptimos */}
      <div className="grid grid-cols-2 gap-4">
        <FormField
          type="number"
          label="Óptimo Mínimo"
          value={formData.optimalMin.toString()}
          onChange={(value) => handleInputChange('optimalMin', parseFloat(value) || 0)}
          placeholder="20"
          required
        />
        <FormField
          type="number"
          label="Óptimo Máximo"
          value={formData.optimalMax.toString()}
          onChange={(value) => handleInputChange('optimalMax', parseFloat(value) || 80)}
          placeholder="80"
          required
        />
      </div>

      {/* Estado */}
      <FormField
        type="select"
        label="Estado"
        value={formData.status}
        onChange={(value) => handleInputChange('status', value)}
        options={statusOptions}
        required
      />

      {/* Rangos de Valores */}
      <div className="grid grid-cols-2 gap-4">
        <FormField
          type="number"
          label="Valor Mínimo"
          value={formData.minValue?.toString() || '0'}
          onChange={(value) => handleInputChange('minValue', parseFloat(value) || 0)}
          placeholder="0"
          {...(errors.minValue && { error: errors.minValue })}
        />
        <FormField
          type="number"
          label="Valor Máximo"
          value={formData.maxValue?.toString() || '100'}
          onChange={(value) => handleInputChange('maxValue', parseFloat(value) || 100)}
          placeholder="100"
        />
      </div>

      {/* Botones */}
      <div className="flex gap-3 pt-6">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          className="flex-1"
        >
          Cancelar
        </Button>
        <Button
          type="submit"
          variant="primary"
          className="flex-1"
        >
          {isEditing ? 'Actualizar' : 'Crear'} Sensor
        </Button>
      </div>
    </form>
  );
};

export default SensorForm;