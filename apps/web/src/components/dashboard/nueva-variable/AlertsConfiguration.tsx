'use client';

import React from 'react';
import FormField from './FormField';

interface NotificationsConfigurationProps {
  isEnabled: boolean;
  onToggle: (enabled: boolean) => void;
  frequency: string;
  onFrequencyChange: (value: string) => void;
  startDate: string;
  onStartDateChange: (value: string) => void;
  endDate: string;
  onEndDateChange: (value: string) => void;
  startTime: string;
  onStartTimeChange: (value: string) => void;
}

export default function NotificationsConfiguration({
  isEnabled,
  onToggle,
  frequency,
  onFrequencyChange,
  startDate,
  onStartDateChange,
  endDate,
  onEndDateChange,
  startTime,
  onStartTimeChange
}: NotificationsConfigurationProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <label className="text-sm font-medium text-gray-900 font-inter">
          Activar Recordatorios de Lectura
        </label>
        <button
          type="button"
          onClick={() => onToggle(!isEnabled)}
          className={`
            relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2
            ${isEnabled ? 'bg-green-600' : 'bg-gray-200'}
          `}
        >
          <span
            className={`
              inline-block h-4 w-4 transform rounded-full bg-white transition-transform duration-200 ease-in-out
              ${isEnabled ? 'translate-x-6' : 'translate-x-1'}
            `}
          />
        </button>
      </div>

      {isEnabled && (
        <div className="space-y-4 pt-4 border-t border-gray-200">
          <FormField
            label="Frecuencia de Recordatorio"
            type="select"
            value={frequency}
            onChange={onFrequencyChange}
            options={[
              { value: 'daily', label: 'Diario' },
              { value: 'every2days', label: 'Cada 2 días' },
              { value: 'every3days', label: 'Cada 3 días' },
              { value: 'weekly', label: 'Semanal' },
              { value: 'biweekly', label: 'Quincenal' },
              { value: 'monthly', label: 'Mensual' }
            ]}
            helpText="Con qué frecuencia deseas recibir recordatorios para ingresar la lectura"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              label="Fecha de Inicio"
              type="date"
              value={startDate}
              onChange={onStartDateChange}
              helpText="Cuándo comenzar los recordatorios"
            />
            <FormField
              label="Fecha de Fin"
              type="date"
              value={endDate}
              onChange={onEndDateChange}
              helpText="Cuándo terminar los recordatorios (opcional)"
            />
          </div>

          <FormField
            label="Hora de Recordatorio"
            type="time"
            value={startTime}
            onChange={onStartTimeChange}
            helpText="A qué hora del día enviar el recordatorio"
          />
        </div>
      )}
    </div>
  );
}