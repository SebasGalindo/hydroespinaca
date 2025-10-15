'use client';

import React, { useState, useEffect } from 'react';
import Button from '@/components/ui/Button';
import { FanIcon, MotorIcon, BoltIcon, LightBulbIcon } from '@/components/ui/icons/Icons';
import type { ActuadorData } from '@hydroespinaca/shared'; // Importando desde la única fuente de verdad

interface ActuadorFormProps {
  actuador?: ActuadorData;
  onSubmit: (actuador: Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'>) => void;
  onCancel: () => void;
  isEditing?: boolean;
}

// ⭐ PASO 3: El estado inicial ahora coincide con el nuevo modelo simplificado.
const initialState: Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'> = {
  name: '',
  type: 'pump',
  location: '',
  pin: 0,
  esp32Id: '',
  status: 'inactive',
};

const ActuadorForm: React.FC<ActuadorFormProps> = ({ 
  actuador, 
  onSubmit, 
  onCancel, 
  isEditing = false 
}) => {
  const [formData, setFormData] = useState(initialState);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isEditing && actuador) {
      // Ahora solo llenamos los campos que existen
      setFormData({
        name: actuador.name,
        type: actuador.type,
        location: actuador.location,
        pin: actuador.pin,
        esp32Id: actuador.esp32Id,
        status: actuador.status,
      });
    } else {
      setFormData(initialState);
    }
  }, [actuador, isEditing]);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.name.trim()) newErrors.name = 'El nombre es requerido';
    if (!formData.location.trim()) newErrors.location = 'La ubicación es requerida';
    if (!formData.esp32Id.trim()) newErrors.esp32Id = 'El ESP32 ID es requerido';
    if (formData.pin < 0 || formData.pin > 39) newErrors.pin = 'El pin debe estar entre 0 y 39';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      // Esto ahora funcionará perfectamente.
      onSubmit(formData);
    }
  };

  const handleInputChange = (field: keyof typeof initialState, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const getActuadorTypeIcon = (type: ActuadorData['type']) => {
    const iconMap: Record<ActuadorData['type'], React.ReactNode> = {
      fan: <FanIcon className="w-4 h-4" />,
      motor: <MotorIcon className="w-4 h-4" />,
      light: <LightBulbIcon className="w-4 h-4" />,
      pump: <BoltIcon className="w-4 h-4" />,
      valve: <BoltIcon className="w-4 h-4" />,
      heater: <BoltIcon className="w-4 h-4" />,
    };
    return iconMap[type];
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* El JSX de tu formulario ya estaba perfecto para este modelo de datos,
          así que no necesita cambios. */}
      
      {/* Nombre */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Nombre del Actuador</label>
        <input type="text" value={formData.name} onChange={(e) => handleInputChange('name', e.target.value)} className={`w-full px-3 py-2 border rounded-lg focus:ring-2 ${errors.name ? 'border-red-500' : 'border-gray-300'}`} placeholder="Ej: Bomba de Riego Principal" />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Tipo */}
      <div>
        <label className="flex items-center text-sm font-medium text-gray-700 mb-2">{getActuadorTypeIcon(formData.type)}<span className="ml-2">Tipo de Actuador</span></label>
        <select value={formData.type} onChange={(e) => handleInputChange('type', e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg">
          <option value="pump">Bomba</option>
          <option value="valve">Válvula</option>
          <option value="fan">Ventilador</option>
          <option value="heater">Calefactor</option>
          <option value="light">Iluminación</option>
          <option value="motor">Motor</option>
        </select>
      </div>
      
      {/* Ubicación */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Ubicación</label>
        <input type="text" value={formData.location} onChange={(e) => handleInputChange('location', e.target.value)} className={`w-full px-3 py-2 border rounded-lg focus:ring-2 ${errors.location ? 'border-red-500' : 'border-gray-300'}`} placeholder="Ej: Zona A - Sector 1" />
        {errors.location && <p className="text-red-500 text-sm mt-1">{errors.location}</p>}
      </div>

      {/* Pin y ESP32 ID */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Pin GPIO</label>
          <input type="number" value={formData.pin} onChange={(e) => handleInputChange('pin', parseInt(e.target.value, 10))} className={`w-full px-3 py-2 border rounded-lg focus:ring-2 ${errors.pin ? 'border-red-500' : 'border-gray-300'}`} placeholder="Ej: 12" />
          {errors.pin && <p className="text-red-500 text-sm mt-1">{errors.pin}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">ID del ESP32</label>
          <input type="text" value={formData.esp32Id} onChange={(e) => handleInputChange('esp32Id', e.target.value)} className={`w-full px-3 py-2 border rounded-lg focus:ring-2 ${errors.esp32Id ? 'border-red-500' : 'border-gray-300'}`} placeholder="Ej: ESP32-001" />
          {errors.esp32Id && <p className="text-red-500 text-sm mt-1">{errors.esp32Id}</p>}
        </div>
      </div>

      {/* Estado */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Estado</label>
        <select value={formData.status} onChange={(e) => handleInputChange('status', e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg">
          <option value="active">Activo</option>
          <option value="inactive">Inactivo</option>
          <option value="error">Error</option>
          <option value="maintenance">Mantenimiento</option>
        </select>
      </div>

      {/* Botones */}
      <div className="flex gap-3 pt-6">
        <Button type="button" variant="outline" onClick={onCancel} className="flex-1">Cancelar</Button>
        <Button type="submit" variant="primary" className="flex-1">{isEditing ? 'Actualizar' : 'Crear'} Actuador</Button>
      </div>
    </form>
  );
};

export default ActuadorForm;