'use client';

import React, { useState, useEffect } from 'react';
import Button from '@/components/ui/Button';
import { DatabaseIcon, CalculatorIcon, BoltIcon } from '@/components/ui/icons/Icons';

interface VariableData {
  id: string;
  name: string;
  description?: string;
  type: 'input' | 'output' | 'calculated';
  dataType: 'numeric' | 'boolean' | 'text';
  unit: string;
  minValue?: number;
  maxValue?: number;
  isRequired: boolean;
  category: 'environmental' | 'control' | 'system' | 'user';
  status: 'active' | 'inactive' | 'deprecated';
  createdAt: string;
  lastModified: string;
}

interface VariableFormProps {
  variable?: VariableData;
  onSubmit: (variable: Omit<VariableData, 'id' | 'createdAt' | 'lastModified'>) => void;
  onCancel: () => void;
  isEditing?: boolean;
}

const VariableForm: React.FC<VariableFormProps> = ({ 
  variable, 
  onSubmit, 
  onCancel, 
  isEditing = false 
}) => {
  const [formData, setFormData] = useState<{
    name: string;
    description: string;
    type: 'input' | 'output' | 'calculated';
    dataType: 'numeric' | 'boolean' | 'text';
    unit: string;
    minValue: number;
    maxValue: number;
    isRequired: boolean;
    category: 'environmental' | 'control' | 'system' | 'user';
    status: 'active' | 'inactive' | 'deprecated';
  }>({
    name: '',
    description: '',
    type: 'input',
    dataType: 'numeric',
    unit: '',
    minValue: 0,
    maxValue: 100,
    isRequired: true,
    category: 'environmental',
    status: 'active'
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (variable) {
      setFormData({
        name: variable.name,
        description: variable.description || '',
        type: variable.type,
        dataType: variable.dataType,
        unit: variable.unit,
        minValue: variable.minValue || 0,
        maxValue: variable.maxValue || 100,
        isRequired: variable.isRequired,
        category: variable.category,
        status: variable.status
      });
    }
  }, [variable]);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'El nombre es requerido';
    }

    if (!formData.description.trim()) {
      newErrors.description = 'La descripción es requerida';
    }

    if (!formData.unit.trim()) {
      newErrors.unit = 'La unidad es requerida';
    }

    if (formData.dataType === 'numeric' && formData.minValue !== undefined && formData.maxValue !== undefined && formData.minValue >= formData.maxValue) {
      newErrors.minValue = 'El rango mínimo debe ser menor al máximo';
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

  const handleInputChange = (field: string, value: any) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
    
    // Limpiar error del campo cuando el usuario empiece a escribir
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Nombre */}
      <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Nombre de la Variable
          </label>
          <input
            type="text"
            value={formData.name}
            onChange={(e) => handleInputChange('name', e.target.value)}
            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900 placeholder-gray-500 ${
              errors.name ? 'border-red-500' : 'border-gray-300'
            }`}
            placeholder="Ej: Temperatura del agua"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>

      {/* Descripción */}
      <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Descripción
          </label>
          <textarea
            value={formData.description}
            onChange={(e) => handleInputChange('description', e.target.value)}
            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900 placeholder-gray-500 ${
              errors.description ? 'border-red-500' : 'border-gray-300'
            }`}
            placeholder="Describe el propósito de esta variable"
            rows={3}
          />
          {errors.description && <p className="text-red-500 text-sm mt-1">{errors.description}</p>}
        </div>

      {/* Tipo y Tipo de Datos */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <DatabaseIcon className="w-4 h-4 inline mr-2" />
            Tipo de Variable
          </label>
          <select
            value={formData.type}
            onChange={(e) => handleInputChange('type', e.target.value as 'input' | 'output' | 'calculated')}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900"
          >
            <option value="input">Entrada</option>
            <option value="output">Salida</option>
            <option value="calculated">Calculada</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <CalculatorIcon className="w-4 h-4 inline mr-2" />
            Tipo de Datos
          </label>
          <select
            value={formData.dataType}
            onChange={(e) => handleInputChange('dataType', e.target.value as 'numeric' | 'boolean' | 'text')}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900"
          >
            <option value="numeric">Numérico</option>
            <option value="boolean">Booleano</option>
            <option value="text">Texto</option>
          </select>
        </div>
      </div>

      {/* Unidad y Categoría */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Unidad de Medida
          </label>
          <input
            type="text"
            value={formData.unit}
            onChange={(e) => handleInputChange('unit', e.target.value)}
            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900 placeholder-gray-500 ${
              errors.unit ? 'border-red-500' : 'border-gray-300'
            }`}
            placeholder="Ej: °C, pH, ppm"
          />
          {errors.unit && <p className="text-red-500 text-sm mt-1">{errors.unit}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Categoría
          </label>
          <select
            value={formData.category}
            onChange={(e) => handleInputChange('category', e.target.value as 'environmental' | 'control' | 'system' | 'user')}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900"
          >
            <option value="environmental">Ambiental</option>
            <option value="control">Control</option>
            <option value="system">Sistema</option>
            <option value="user">Usuario</option>
          </select>
        </div>
      </div>

      {/* Rango */}
      {formData.dataType === 'numeric' && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Rango de Valores
          </label>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <input
                type="number"
                value={formData.minValue !== undefined ? formData.minValue.toString() : ''}
                onChange={(e) => handleInputChange('minValue', parseFloat(e.target.value) || 0)}
                className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900 placeholder-gray-500 ${
                  errors.minValue ? 'border-red-500' : 'border-gray-300'
                }`}
                placeholder="Mínimo"
              />
            </div>
            <div>
              <input
                type="number"
                value={formData.maxValue !== undefined ? formData.maxValue.toString() : ''}
                onChange={(e) => handleInputChange('maxValue', parseFloat(e.target.value) || 100)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none text-gray-900 placeholder-gray-500"
                placeholder="Máximo"
              />
            </div>
          </div>
          {errors.minValue && <p className="text-red-500 text-sm mt-1">{errors.minValue}</p>}
        </div>
      )}

      {/* Botones */}
      <div className="flex justify-end space-x-3 pt-4">
        <Button
          type="button"
          variant="secondary"
          onClick={onCancel}
        >
          Cancelar
        </Button>
        <Button
          type="submit"
          variant="primary"
        >
          {isEditing ? 'Actualizar Variable' : 'Crear Variable'}
        </Button>
      </div>
    </form>
  );
};

export default VariableForm;