import React, { useState, useCallback } from 'react';
import { View, StyleSheet, TouchableOpacity, Platform, Text, ViewStyle, TextStyle } from 'react-native';
import DateTimePicker, { DateTimePickerAndroid, type AndroidNativeProps, type DateTimePickerEvent } from '@react-native-community/datetimepicker';
import { Icon } from '../atoms/Icon';
import { spacing, semanticColors, borderRadius, IconName } from '@hydroespinaca/shared';



// Interfaces para el componente DatePicker
export interface DatePickerProps {
  /** Valor de fecha seleccionada */
  value?: Date;
  /** Callback cuando cambia la fecha */
  onDateChange?: (date: Date) => void;
  /** Modo del picker: date, time, datetime */
  mode?: 'date' | 'time' | 'datetime';
  /** Fecha mínima seleccionable */
  minimumDate?: Date;
  /** Fecha máxima seleccionable */
  maximumDate?: Date;
  /** Placeholder cuando no hay fecha seleccionada */
  placeholder?: string;
  /** Si el componente está deshabilitado */
  disabled?: boolean;
  /** Formato de fecha personalizado */
  dateFormat?: 'short' | 'medium' | 'long' | 'full';
  /** Variante visual del componente */
  variant?: 'default' | 'outlined' | 'minimal';
  /** Tamaño del componente */
  size?: 'small' | 'medium' | 'large';
  /** Texto de error */
  error?: string;
  /** Etiqueta del campo */
  label?: React.ReactNode;
  /** Si es requerido */
  required?: boolean;
  /** Texto de ayuda */
  helperText?: string;
  /** Icono personalizado */
  icon?: IconName;
  /** Posición del icono */
  iconPosition?: 'left' | 'right';
  /** Estilo personalizado del contenedor */
  containerStyle?: import('react-native').StyleProp<ViewStyle>;
  /** Estilo personalizado del input */
  inputStyle?: import('react-native').StyleProp<ViewStyle>;
  /** Callback cuando se abre el picker */
  onOpen?: () => void;
  /** Callback cuando se cierra el picker */
  onClose?: () => void;
  /** Configuración de localización */
  locale?: string;
  /** Formato de 24 horas (solo Android) */
  is24Hour?: boolean;
  /** Intervalo de minutos */
  minuteInterval?: 1 | 2 | 3 | 4 | 5 | 6 | 10 | 12 | 15 | 20 | 30;
  /** Tema oscuro forzado */
  isDarkModeEnabled?: boolean;
  /** Texto del botón de confirmación (iOS) */
  confirmText?: string;
  /** Texto del botón de cancelación (iOS) */
  cancelText?: string;
  /** Color del texto de los botones (iOS) */
  buttonTextColor?: string;
}

// Hook personalizado para manejar el DatePicker
export const useDatePicker = () => {
  const [isVisible, setIsVisible] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date | undefined>();

  const show = useCallback(() => {
    setIsVisible(true);
  }, []);

  const hide = useCallback(() => {
    setIsVisible(false);
  }, []);

  const handleConfirm = useCallback((date: Date, onDateChange?: (date: Date) => void) => {
    setSelectedDate(date);
    onDateChange?.(date);
    hide();
  }, [hide]);

  const handleCancel = useCallback(() => {
    hide();
  }, [hide]);

  return {
    isVisible,
    selectedDate,
    show,
    hide,
    handleConfirm,
    handleCancel,
    setSelectedDate,
  };
};

// Función para formatear fechas
const formatDate = (date: Date, format: string = 'short', mode: string = 'date'): string => {
  if (!date) return '';

  const options: Intl.DateTimeFormatOptions = {};

  if (mode === 'date' || mode === 'datetime') {
    switch (format) {
      case 'short':
        options.day = '2-digit';
        options.month = '2-digit';
        options.year = 'numeric';
        break;
      case 'medium':
        options.day = 'numeric';
        options.month = 'short';
        options.year = 'numeric';
        break;
      case 'long':
        options.day = 'numeric';
        options.month = 'long';
        options.year = 'numeric';
        break;
      case 'full':
        options.weekday = 'long';
        options.day = 'numeric';
        options.month = 'long';
        options.year = 'numeric';
        break;
    }
  }

  if (mode === 'time' || mode === 'datetime') {
    options.hour = '2-digit';
    options.minute = '2-digit';
  }

  return String(date.toLocaleDateString('es-ES', options));
};

// Componente principal DatePicker
export const DatePicker: React.FC<DatePickerProps> = ({
  value,
  onDateChange,
  mode = 'date',
  minimumDate,
  maximumDate,
  placeholder,
  disabled = false,
  dateFormat = 'short',
  variant = 'default',
  size = 'medium',
  error,
  label,
  required = false,
  helperText,
  icon,
  iconPosition = 'right',
  containerStyle,
  inputStyle,
  onOpen,
  onClose,
  locale = 'es-ES',
  is24Hour = true,
  minuteInterval = 1,
  isDarkModeEnabled,
  confirmText = 'Confirmar',
  cancelText = 'Cancelar',
  buttonTextColor,
}) => {
  const { isVisible, show, hide, handleConfirm, handleCancel } = useDatePicker();
  const [iosTempDate, setIosTempDate] = useState<Date>(value || new Date());

  // Estilos dinámicos basados en props
  const getContainerStyles = (): ViewStyle[] => {
    const baseStyles: ViewStyle[] = [styles.container];

    if (variant === 'outlined') {
      baseStyles.push({
        borderWidth: 1,
        borderColor: error ? semanticColors.errorText : semanticColors.border,
        backgroundColor: 'transparent',
      } as ViewStyle);
    } else if (variant === 'minimal') {
      baseStyles.push({
        borderBottomWidth: 1,
        borderBottomColor: error ? semanticColors.errorText : semanticColors.border,
        backgroundColor: 'transparent',
        borderRadius: 0,
      } as ViewStyle);
    } else {
      baseStyles.push({
        backgroundColor: semanticColors.background,
        borderWidth: 1,
        borderColor: error ? semanticColors.errorText : semanticColors.border,
      } as ViewStyle);
    }

    if (size === 'small') {
      baseStyles.push({ paddingVertical: spacing.xs, paddingHorizontal: spacing.sm } as ViewStyle);
    } else if (size === 'large') {
      baseStyles.push({ paddingVertical: spacing.md, paddingHorizontal: spacing.lg } as ViewStyle);
    }

    if (disabled) {
      baseStyles.push({
        backgroundColor: semanticColors.backgroundMuted,
        opacity: 0.6,
      } as ViewStyle);
    }

    return baseStyles;
  };

  const getTextStyles = (): TextStyle[] => {
    const baseStyles: TextStyle[] = [styles.text];

    if (size === 'small') {
      baseStyles.push({ fontSize: 14 } as TextStyle);
    } else if (size === 'large') {
      baseStyles.push({ fontSize: 18 } as TextStyle);
    }

    if (!value) {
      baseStyles.push({ color: semanticColors.textMuted } as TextStyle);
    } else {
      baseStyles.push({ color: semanticColors.textPrimary } as TextStyle);
    }

    return baseStyles;
  };

  const handlePress = () => {
    if (disabled) return;
    onOpen?.();

    if (Platform.OS === 'android') {
      const androidProps: AndroidNativeProps = {
        value: value || new Date(),
        onChange: (event: DateTimePickerEvent, selectedDate?: Date) => {
          if (event.type === 'set' && selectedDate) {
            handleConfirm(selectedDate, onDateChange);
            onClose?.();
          } else if (event.type === 'dismissed') {
            handleCancel();
            onClose?.();
          }
        },
        mode: mode === 'datetime' ? 'date' : mode, // se abrirá date y luego time si es datetime
        is24Hour,
        ...(minimumDate ? { minimumDate } : {}),
        ...(maximumDate ? { maximumDate } : {}),
      } as AndroidNativeProps;

      // Si el modo es datetime, primero abrimos fecha y luego hora
      if (mode === 'datetime') {
        DateTimePickerAndroid.open({
          value: value || new Date(),
          onChange: (event: DateTimePickerEvent, selectedDate?: Date) => {
            if (event.type === 'set' && selectedDate) {
              onAndroidDateThenTime(selectedDate);
            } else if (event.type === 'dismissed') {
              handleCancel();
              onClose?.();
            }
          },
          mode: 'date',
          is24Hour,
          ...(minimumDate ? { minimumDate } : {}),
          ...(maximumDate ? { maximumDate } : {}),
        } as AndroidNativeProps);
      } else {
        DateTimePickerAndroid.open(androidProps);
      }
    } else {
      setIosTempDate(value || new Date());
      show();
    }
  };

  // Secuencia Android para datetime: tras elegir fecha, abrir hora
  const onAndroidDateThenTime = (selectedDate: Date) => {
    DateTimePickerAndroid.open({
      value: selectedDate,
      onChange: (event: DateTimePickerEvent, timeDate?: Date) => {
        if (event.type === 'set' && timeDate) {
          handleConfirm(timeDate, onDateChange);
          onClose?.();
        } else if (event.type === 'dismissed') {
          handleCancel();
          onClose?.();
        }
      },
      mode: 'time',
      is24Hour,
    });
  };

  const getDefaultPlaceholder = () => {
    switch (mode) {
      case 'date': return 'Seleccionar fecha';
      case 'time': return 'Seleccionar hora';
      default: return 'Seleccionar fecha y hora';
    }
  };

  const displayText = value 
    ? formatDate(value, dateFormat, mode)
    : (placeholder || getDefaultPlaceholder());

  const rightIconName: IconName = icon ?? (mode === 'time' ? 'clock' : 'calendar');

  return (
    <View style={[containerStyle]}> {/* containerStyle ya es StyleProp<ViewStyle> */}
      {label && (
        <View style={styles.labelContainer}>
          {typeof label === 'string' ? (
            <Text style={[styles.label, { color: semanticColors.textPrimary }]}>
              {label}{required && <Text style={{ color: semanticColors.errorText }}>*</Text>}
            </Text>
          ) : (
            <View style={{ flexDirection: 'row', alignItems: 'center' }}>
              {label}
              {required && <Text style={{ color: semanticColors.errorText }}>*</Text>}
            </View>
          )}
        </View>
      )}
      
      <TouchableOpacity
        style={[...getContainerStyles(), inputStyle]}
        onPress={handlePress}
        disabled={disabled}
        activeOpacity={0.7}
      >
        {icon && iconPosition === 'left' && (
          <Icon 
            name={icon}
            size={size === 'small' ? 16 : size === 'large' ? 24 : 20}
            color={disabled ? semanticColors.textMuted : semanticColors.textPrimary}
            style={styles.iconLeft}
          />
        )}
        
        <Text style={getTextStyles()}>
          {displayText}
        </Text>
        
        {iconPosition === 'right' && (
          <Icon 
            name={rightIconName}
            size={size === 'small' ? 16 : size === 'large' ? 24 : 20}
            color={disabled ? semanticColors.textMuted : semanticColors.textPrimary}
            style={styles.iconRight}
          />
        )}
      </TouchableOpacity>

      {error && (
        <Text style={[styles.errorText, { color: semanticColors.errorText }]}>
          {error}
        </Text>
      )}

      {helperText && !error && (
        <Text style={[styles.helperText, { color: semanticColors.textMuted }]}>
          {helperText}
        </Text>
      )}

      {/* iOS: Modal simple con confirmación */}
      {Platform.OS === 'ios' && isVisible && (
        <View style={styles.pickerBackdrop}>
          <View style={styles.pickerModal}>
            <View style={styles.pickerToolbar}>
              <TouchableOpacity onPress={() => { handleCancel(); onClose?.(); }}>
                <Text style={[styles.pickerButton, { color: buttonTextColor || semanticColors.primary }]}>{cancelText}</Text>
              </TouchableOpacity>
              <TouchableOpacity onPress={() => { handleConfirm(iosTempDate, onDateChange); onClose?.(); }}>
                <Text style={[styles.pickerButton, { color: buttonTextColor || semanticColors.primary }]}>{confirmText}</Text>
              </TouchableOpacity>
            </View>
            <DateTimePicker
              value={iosTempDate}
              mode={mode === 'datetime' ? 'date' : mode}
              display={mode === 'time' ? 'spinner' : 'inline'}
              onChange={(event: DateTimePickerEvent, selectedDate?: Date) => {
                if (selectedDate) {
                  setIosTempDate(selectedDate);
                  if (mode !== 'datetime') {
                    // para date o time simples, aplicamos en el cambio si quieres comportamiento instantáneo
                  }
                }
              }}
              locale={locale}
              {...(minimumDate ? { minimumDate } : {})}
              {...(maximumDate ? { maximumDate } : {})}
              minuteInterval={minuteInterval}
            />
            {mode === 'datetime' && (
              <DateTimePicker
                value={iosTempDate}
                mode={'time'}
                display={'spinner'}
                onChange={(event: DateTimePickerEvent, selectedDate?: Date) => {
                  if (selectedDate) setIosTempDate(selectedDate);
                }}
                locale={locale}
                minuteInterval={minuteInterval}
              />
            )}
          </View>
        </View>
      )}
    </View>
  );
};

// Componentes predefinidos para casos comunes
export const DatePickerBirthday: React.FC<Omit<DatePickerProps, 'mode' | 'maximumDate'>> = (props) => (
  <DatePicker
    {...props}
    mode="date"
    maximumDate={new Date()}
    placeholder="Seleccionar fecha de nacimiento"
    label="Fecha de nacimiento"
  />
);

export const DatePickerAppointment: React.FC<Omit<DatePickerProps, 'mode' | 'minimumDate'>> = (props) => (
  <DatePicker
    {...props}
    mode="datetime"
    minimumDate={new Date()}
    placeholder="Seleccionar fecha y hora"
    label="Fecha de cita"
  />
);

export const DatePickerDeadline: React.FC<Omit<DatePickerProps, 'mode' | 'minimumDate'>> = (props) => (
  <DatePicker
    {...props}
    mode="date"
    minimumDate={new Date()}
    placeholder="Seleccionar fecha límite"
    label="Fecha límite"
    variant="outlined"
  />
);

export const DatePickerRange: React.FC<{
  startDate?: Date;
  endDate?: Date;
  onStartDateChange?: (date: Date) => void;
  onEndDateChange?: (date: Date) => void;
  minimumDate?: Date;
  maximumDate?: Date;
  disabled?: boolean;
  variant?: 'default' | 'outlined' | 'minimal';
  size?: 'small' | 'medium' | 'large';
}> = ({
  startDate,
  endDate,
  onStartDateChange,
  onEndDateChange,
  minimumDate,
  maximumDate,
  disabled = false,
  variant = 'default',
  size = 'medium',
}) => {
  return (
    <View style={styles.rangeContainer}>
      <View style={styles.rangeItem}>
        <DatePicker
          {...(startDate && { value: startDate })}
          onDateChange={onStartDateChange || (() => {})}
          mode="date"
          {...(minimumDate && { minimumDate })}
          {...((endDate || maximumDate) && { maximumDate: endDate || maximumDate })}
          placeholder="Fecha inicio"
          label="Desde"
          disabled={disabled}
          variant={variant}
          size={size}
        />
      </View>
      <View style={styles.rangeItem}>
        <DatePicker
          {...(endDate && { value: endDate })}
          onDateChange={onEndDateChange || (() => {})}
          mode="date"
          {...((startDate || minimumDate) && { minimumDate: startDate || minimumDate })}
          {...(maximumDate && { maximumDate })}
          placeholder="Fecha fin"
          label="Hasta"
          disabled={disabled}
          variant={variant}
          size={size}
        />
      </View>
    </View>
  );
}

export const TimePicker: React.FC<Omit<DatePickerProps, 'mode'>> = (props) => (
  <DatePicker
    {...props}
    mode="time"
    placeholder="Seleccionar hora"
  />
);

// Estilos del componente
const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius.lg,
    minHeight: 48,
  },
  labelContainer: {
    marginBottom: spacing.xs,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
  },
  text: {
    flex: 1,
    fontSize: 16,
  },
  iconLeft: {
    marginRight: spacing.sm,
  },
  iconRight: {
    marginLeft: spacing.sm,
  },
  errorText: {
    fontSize: 12,
    marginTop: spacing.xs,
  },
  helperText: {
    fontSize: 12,
    marginTop: spacing.xs,
  },
  rangeContainer: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  rangeItem: {
    flex: 1,
  },
  pickerBackdrop: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    top: 0,
    backgroundColor: 'rgba(0,0,0,0.3)',
    justifyContent: 'flex-end',
  },
  pickerModal: {
    backgroundColor: semanticColors.surface,
    borderTopLeftRadius: 12,
    borderTopRightRadius: 12,
    paddingBottom: 16,
  },
  pickerToolbar: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomColor: semanticColors.border,
    borderBottomWidth: 1,
  },
  pickerButton: {
    fontSize: 16,
    fontWeight: '600',
  },
});

// Manager para controlar DatePickers programáticamente
export class DatePickerManager {
  private static instances: Map<string, any> = new Map();

  static register(id: string, instance: any) {
    this.instances.set(id, instance);
  }

  static unregister(id: string) {
    this.instances.delete(id);
  }

  static show(id: string) {
    const instance = this.instances.get(id);
    if (instance) {
      instance.show();
    }
  }

  static hide(id: string) {
    const instance = this.instances.get(id);
    if (instance) {
      instance.hide();
    }
  }

  static setDate(id: string, date: Date) {
    const instance = this.instances.get(id);
    if (instance) {
      instance.setSelectedDate(date);
    }
  }
}

export default DatePicker;