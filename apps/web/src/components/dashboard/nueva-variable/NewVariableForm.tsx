'use client';

import React, { useState } from 'react';
import FormSection from '@/components/ui/FormSection';
import FormField from './FormField';
import ActionButtons from '@/components/ui/ActionButtons';
import VariableTypeSelector from './VariableTypeSelector';
import RangeInputs from './RangeInputs';
import NotificationsConfiguration from './AlertsConfiguration';

export default function NewVariableForm() {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    unit: '',
    variableType: '',
    minValue: '',
    maxValue: '',
    optimalMin: '',
    optimalMax: '',
    notificationsEnabled: false,
    frequency: 'daily',
    startDate: '',
    endDate: '',
    startTime: '09:00'
  });

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Form submitted:', formData);
    // Aquí iría la lógica para enviar los datos
  };

  const handleCancel = () => {
    // Lógica para cancelar y volver
    console.log('Form cancelled');
  };

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
      <form onSubmit={handleSubmit} className="p-6 lg:p-8">
        <div className="space-y-8">
          {/* Información Básica */}
          <FormSection title="Información Básica">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <FormField
                label="Nombre de la Variable"
                type="text"
                value={formData.name}
                onChange={(value) => handleInputChange('name', value)}
                placeholder="Ej: Nivel de Nutrientes"
                required
              />
              <FormField
                label="Unidad de Medida"
                type="text"
                value={formData.unit}
                onChange={(value) => handleInputChange('unit', value)}
                placeholder="Ej: mg/L, cm, %"
                required
              />
            </div>
            <FormField
              label="Descripción"
              type="textarea"
              value={formData.description}
              onChange={(value) => handleInputChange('description', value)}
              placeholder="Describe brevemente qué mide esta variable y su importancia"
              rows={3}
            />
          </FormSection>

          {/* Tipo de Variable */}
          <FormSection title="Tipo de Variable">
            <VariableTypeSelector
              value={formData.variableType}
              onChange={(value) => handleInputChange('variableType', value)}
            />
          </FormSection>

          {/* Rangos de Valores */}
          <FormSection title="Rangos de Valores">
            <RangeInputs
              formData={formData}
              onChange={handleInputChange}
            />
          </FormSection>

          {/* Configuración de Alertas */}
          <FormSection title="Configuración de Notificaciones">
            <NotificationsConfiguration
              isEnabled={formData.notificationsEnabled}
              onToggle={(enabled) => handleInputChange('notificationsEnabled', enabled)}
              frequency={formData.frequency}
              onFrequencyChange={(value) => handleInputChange('frequency', value)}
              startDate={formData.startDate}
              onStartDateChange={(value) => handleInputChange('startDate', value)}
              endDate={formData.endDate}
              onEndDateChange={(value) => handleInputChange('endDate', value)}
              startTime={formData.startTime}
              onStartTimeChange={(value) => handleInputChange('startTime', value)}
            />
          </FormSection>
        </div>

        {/* Botones de Acción */}
        <ActionButtons
          onSecondary={handleCancel}
          onPrimary={() => console.log('Guardar variable')}
          secondaryText="Cancelar"
          primaryText="Guardar Variable"
          isLoading={false}
          layout="horizontal"
        />
      </form>
    </div>
  );
}