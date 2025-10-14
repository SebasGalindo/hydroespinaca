import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { Text, Switch, Select } from '../atoms';
import { FormField, DatePicker, TimePickerComponent as TimePicker } from '../molecules';
import { colors, semanticColors, spacing, typography } from '@hidroespinaca/shared';
import type { TimeValue } from './TimePicker';

export interface NotificationConfigProps {
  /** Si los recordatorios están activados */
  enabled?: boolean;
  /** Frecuencia de recordatorio */
  frequency?: string;
  /** Fecha de inicio */
  startDate?: string;
  /** Fecha de fin */
  endDate?: string;
  /** Hora de recordatorio */
  reminderTime?: string;
  /** Callback cuando cambia el estado de activación */
  onEnabledChange?: (enabled: boolean) => void;
  /** Callback cuando cambia la frecuencia */
  onFrequencyChange?: (frequency: string) => void;
  /** Callback cuando cambia la fecha de inicio */
  onStartDateChange?: (date: string) => void;
  /** Callback cuando cambia la fecha de fin */
  onEndDateChange?: (date: string) => void;
  /** Callback cuando cambia la hora */
  onReminderTimeChange?: (time: string) => void;
  /** Si está deshabilitado */
  disabled?: boolean;
  /** Errores de validación */
  errors?: {
    frequency?: string | undefined;
    startDate?: string | undefined;
    endDate?: string | undefined;
    reminderTime?: string | undefined;
  };
}

const frequencyOptions = [
  { label: 'Diario', value: 'daily' },
  { label: 'Semanal', value: 'weekly' },
  { label: 'Quincenal', value: 'biweekly' },
  { label: 'Mensual', value: 'monthly' },
];

export const NotificationConfig: React.FC<NotificationConfigProps> = ({
  enabled = false,
  frequency = 'daily',
  startDate = '',
  endDate = '',
  reminderTime = '09:00',
  onEnabledChange,
  onFrequencyChange,
  onStartDateChange,
  onEndDateChange,
  onReminderTimeChange,
  disabled = false,
  errors = {},
}) => {
  const [isEnabled, setIsEnabled] = useState(enabled);
  const [selectedFrequency, setSelectedFrequency] = useState(frequency);
  const [selectedStartDate, setSelectedStartDate] = useState(startDate);
  const [selectedEndDate, setSelectedEndDate] = useState(endDate);
  const [selectedTime, setSelectedTime] = useState(reminderTime);

  // Handlers
  const handleEnabledChange = (value: boolean) => {
    setIsEnabled(value);
    onEnabledChange?.(value);
  };

  const handleFrequencyChange = (value: string) => {
    setSelectedFrequency(value);
    onFrequencyChange?.(value);
  };

  const handleStartDateChange = (date: Date) => {
    const dateString = date?.toISOString().split('T')[0] || '';
    setSelectedStartDate(dateString);
    onStartDateChange?.(dateString);
  };

  const handleEndDateChange = (date: Date) => {
    const dateString = date?.toISOString().split('T')[0] || '';
    setSelectedEndDate(dateString);
    onEndDateChange?.(dateString);
  };

  const handleTimeChange = (time: TimeValue) => {
    const timeString = `${time.hours.toString().padStart(2, '0')}:${time.minutes.toString().padStart(2, '0')}`;
    setSelectedTime(timeString);
    onReminderTimeChange?.(timeString);
  };

  // Funciones de conversión
  const stringToDate = (dateString: string): Date | undefined => {
    if (!dateString) return undefined;
    return new Date(dateString);
  };

  const stringToTimeValue = (timeString: string): TimeValue | undefined => {
    if (!timeString) return undefined;
    const [hours, minutes] = timeString.split(':').map(Number);
    return { hours: hours || 0, minutes: minutes || 0, seconds: 0 };
  };

  // Actualizar valores cuando cambien las props
  useEffect(() => {
    setIsEnabled(enabled);
    setSelectedFrequency(frequency);
    setSelectedStartDate(startDate);
    setSelectedEndDate(endDate);
    setSelectedTime(reminderTime);
  }, [enabled, frequency, startDate, endDate, reminderTime]);

  return (
    <View style={styles.container}>
      <Text style={styles.sectionTitle}>Configuración de Notificaciones</Text>
      
      {/* Switch para activar recordatorios */}
      <View style={styles.switchContainer}>
        <View style={styles.switchLabelContainer}>
          <Text style={styles.switchLabel}>Activar Recordatorios de Lectura</Text>
        </View>
        <Switch
          value={isEnabled}
          onValueChange={handleEnabledChange}
          disabled={disabled}
        />
      </View>

      {/* Configuraciones adicionales (solo si está activado) */}
      {isEnabled && (
        <View style={styles.configContainer}>
          {/* Frecuencia de Recordatorio */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Frecuencia de Recordatorio</Text>
            <Text style={styles.fieldDescription}>
              Con qué frecuencia deseas recibir recordatorios para ingresar la lectura
            </Text>
            <Select
              value={selectedFrequency}
              onSelect={(option) => handleFrequencyChange(option.value as string)}
              options={frequencyOptions}
              disabled={disabled}
              error={!!errors.frequency}
              placeholder="Selecciona la frecuencia"
            />
          </View>

          {/* Fecha de Inicio */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Fecha de Inicio</Text>
            <Text style={styles.fieldDescription}>
              Cuándo comenzar los recordatorios
            </Text>
            <DatePicker
              value={stringToDate(selectedStartDate) || new Date()}
              onDateChange={handleStartDateChange}
              disabled={disabled}
              placeholder="mm/dd/yyyy"
              {...(errors.startDate && { error: errors.startDate })}
            />
          </View>

          {/* Fecha de Fin */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Fecha de Fin</Text>
            <Text style={styles.fieldDescription}>
              Cuándo terminar los recordatorios (opcional)
            </Text>
            <DatePicker
              value={stringToDate(selectedEndDate) || new Date()}
              onDateChange={handleEndDateChange}
              disabled={disabled}
              placeholder="mm/dd/yyyy (opcional)"
              {...(errors.endDate && { error: errors.endDate })}
            />
          </View>

          {/* Hora de Recordatorio */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Hora de Recordatorio</Text>
            <Text style={styles.fieldDescription}>
              A qué hora del día enviar el recordatorio
            </Text>
            <TimePicker
              value={stringToTimeValue(selectedTime) || { hours: 9, minutes: 0, seconds: 0 }}
              onTimeChange={handleTimeChange}
              disabled={disabled}
              placeholder="09:00"
              {...(errors.reminderTime && { error: errors.reminderTime })}
            />
          </View>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.semibold as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  switchContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    marginBottom: spacing.md,
  },
  switchLabelContainer: {
    flex: 1,
    marginRight: spacing.md,
  },
  switchLabel: {
    fontSize: typography.fontSize.md,
    fontWeight: typography.fontWeight.medium as any,
    color: semanticColors.textPrimary,
  },
  configContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    padding: spacing.md,
    gap: spacing.md,
  },
  fieldContainer: {
    marginBottom: spacing.md,
  },
  fieldLabel: {
    fontSize: typography.fontSize.md,
    fontWeight: typography.fontWeight.medium as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },
  fieldDescription: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
    marginBottom: spacing.sm,
    lineHeight: 18,
  },
});

export default NotificationConfig;