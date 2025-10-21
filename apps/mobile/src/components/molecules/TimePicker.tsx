import React, { useState, useCallback, useRef, useImperativeHandle, forwardRef } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Modal,
  Platform,
  Dimensions,
  ViewStyle,
  TextStyle,
  ScrollView,
} from 'react-native';
import { IconName } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

// Interfaces
export interface TimeValue {
  hours: number;
  minutes: number;
  seconds: number;
}

export interface TimePickerProps {
  value?: TimeValue;
  onTimeChange?: (time: TimeValue) => void;
  onConfirm?: (time: TimeValue) => void;
  onCancel?: () => void;
  mode?: 'time' | 'duration' | 'countdown';
  format?: '12h' | '24h';
  minuteInterval?: 1 | 5 | 10 | 15 | 20 | 30;
  showSeconds?: boolean;
  disabled?: boolean;
  placeholder?: string;
  label?: string;
  error?: string;
  variant?: 'default' | 'outlined' | 'minimal';
  size?: 'small' | 'medium' | 'large';
  style?: ViewStyle;
  textStyle?: TextStyle;
  theme?: 'light' | 'dark' | 'auto';
  modal?: boolean;
  visible?: boolean;
  title?: string;
  confirmText?: string;
  cancelText?: string;
  icon?: IconName;
  required?: boolean;
}

export interface TimePickerRef {
  open: () => void;
  close: () => void;
  getValue: () => TimeValue;
  setValue: (time: TimeValue) => void;
}

// Hook personalizado para TimePicker
export const useTimePicker = (initialTime?: TimeValue) => {
  const [time, setTime] = useState<TimeValue>(
    initialTime || { hours: 0, minutes: 0, seconds: 0 }
  );
  const [isVisible, setIsVisible] = useState(false);

  const openPicker = useCallback(() => {
    setIsVisible(true);
  }, []);

  const closePicker = useCallback(() => {
    setIsVisible(false);
  }, []);

  const handleTimeChange = useCallback((newTime: TimeValue) => {
    setTime(newTime);
  }, []);

  return {
    time,
    setTime: handleTimeChange,
    isVisible,
    openPicker,
    closePicker,
  };
};

// Función para formatear tiempo
export const formatTime = (
  time: TimeValue,
  format: '12h' | '24h' = '24h',
  showSeconds: boolean = false
): string => {
  const { hours, minutes, seconds = 0 } = time;
  
  if (format === '12h') {
    const period = hours >= 12 ? 'PM' : 'AM';
    const displayHours = hours === 0 ? 12 : hours > 12 ? hours - 12 : hours;
    const timeString = showSeconds 
      ? `${displayHours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
      : `${displayHours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
    return `${timeString} ${period}`;
  }
  
  return showSeconds 
    ? `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
    : `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
};

// Componente Picker Wheel
const PickerWheel: React.FC<{
  values: number[];
  selectedValue: number;
  onValueChange: (value: number) => void;
  label: React.ReactNode;
}> = ({ values, selectedValue, onValueChange, label }) => {
  return (
    <View style={styles.wheelContainer}>
      <Text style={styles.wheelLabel}>{label}</Text>
      <ScrollView
        style={styles.wheel}
        contentContainerStyle={styles.wheelScroll}
        showsVerticalScrollIndicator={false}
      >
        {values.map((value) => (
          <TouchableOpacity
            key={value}
            style={[
              styles.wheelItem,
              selectedValue === value && styles.wheelItemSelected,
            ]}
            onPress={() => onValueChange(value)}
          >
            <Text
              style={[
                styles.wheelItemText,
                selectedValue === value && styles.wheelItemTextSelected,
              ]}
            >
              {value.toString().padStart(2, '0')}
            </Text>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </View>
  );
};

// Componente principal TimePicker
export const TimePicker = forwardRef<TimePickerRef, TimePickerProps>(
  (
    {
      value = { hours: 0, minutes: 0, seconds: 0 },
      onTimeChange,
      onConfirm,
      onCancel,
      mode = 'time',
      format = '24h',
      minuteInterval = 1,
      showSeconds = false,
      disabled = false,
      placeholder = 'Select time',
      label,
      error,
      variant = 'default',
      size = 'medium',
      style,
      textStyle,
      theme = 'auto',
      modal = true,
      visible = false,
      title = 'Select Time',
      confirmText = 'Confirm',
      cancelText = 'Cancel',
      icon = 'clock',
      required = false,
    },
    ref
  ) => {
    const [internalTime, setInternalTime] = useState<TimeValue>(value);
    const [isModalVisible, setIsModalVisible] = useState(visible);

    useImperativeHandle(ref, () => ({
      open: () => setIsModalVisible(true),
      close: () => setIsModalVisible(false),
      getValue: () => internalTime,
      setValue: (time: TimeValue) => setInternalTime(time),
    }));

    const handleTimeChange = useCallback((newTime: TimeValue) => {
      setInternalTime(newTime);
      onTimeChange?.(newTime);
    }, [onTimeChange]);

    const handleConfirm = useCallback(() => {
      onConfirm?.(internalTime);
      setIsModalVisible(false);
    }, [internalTime, onConfirm]);

    const handleCancel = useCallback(() => {
      onCancel?.();
      setIsModalVisible(false);
    }, [onCancel]);

    const openPicker = useCallback(() => {
      if (!disabled) {
        setIsModalVisible(true);
      }
    }, [disabled]);

    // Generar valores para los wheels
    const hours = Array.from({ length: format === '12h' ? 12 : 24 }, (_, i) => 
      format === '12h' ? (i === 0 ? 12 : i) : i
    );
    const minutes = Array.from({ length: 60 / minuteInterval }, (_, i) => i * minuteInterval);
    const seconds = Array.from({ length: 60 }, (_, i) => i);

    const getContainerStyle = (): ViewStyle[] => {
      const baseStyle: ViewStyle[] = [styles.container];
      
      if (variant === 'outlined') baseStyle.push(styles.outlined);
      if (variant === 'minimal') baseStyle.push(styles.minimal);
      if (size === 'small') baseStyle.push(styles.small);
      if (size === 'large') baseStyle.push(styles.large);
      if (disabled) baseStyle.push(styles.disabled);
      if (error) baseStyle.push(styles.error);
      
      return baseStyle;
    };

    const getTextStyle = (): TextStyle[] => {
      const baseStyle: TextStyle[] = [styles.text];
      
      if (size === 'small') baseStyle.push(styles.textSmall);
      if (size === 'large') baseStyle.push(styles.textLarge);
      if (disabled) baseStyle.push(styles.textDisabled);
      
      return baseStyle;
    };

    const renderPicker = () => (
      <View style={styles.pickerContainer}>
        <PickerWheel
          values={hours}
          selectedValue={internalTime.hours}
          onValueChange={(hours) => handleTimeChange({ ...internalTime, hours })}
          label="Hours"
        />
        <PickerWheel
          values={minutes}
          selectedValue={internalTime.minutes}
          onValueChange={(minutes) => handleTimeChange({ ...internalTime, minutes })}
          label="Minutes"
        />
        {showSeconds && (
          <PickerWheel
            values={seconds}
            selectedValue={internalTime.seconds || 0}
            onValueChange={(seconds) => handleTimeChange({ ...internalTime, seconds })}
            label="Seconds"
          />
        )}
      </View>
    );

    if (modal) {
      return (
        <>
          <TouchableOpacity
            style={[getContainerStyle(), style]}
            onPress={openPicker}
            disabled={disabled}
          >
            {label && (
              <Text style={styles.label}>
                {label}
                {required && <Text style={styles.required}>*</Text>}
              </Text>
            )}
            <View style={styles.inputContainer}>
              <Icon name={icon} size={20} color="#666" style={styles.icon} />
              <Text style={[getTextStyle(), textStyle]}>
                {formatTime(internalTime, format, showSeconds) || placeholder}
              </Text>
            </View>
            {error && <Text style={styles.errorText}>{error}</Text>}
          </TouchableOpacity>

          <Modal
            visible={isModalVisible}
            transparent
            animationType="slide"
            onRequestClose={handleCancel}
          >
            <View style={styles.modalOverlay}>
              <View style={styles.modalContent}>
                <View style={styles.modalHeader}>
                  <TouchableOpacity onPress={handleCancel}>
                    <Text style={styles.cancelButton}>{cancelText}</Text>
                  </TouchableOpacity>
                  <Text style={styles.modalTitle}>{title}</Text>
                  <TouchableOpacity onPress={handleConfirm}>
                    <Text style={styles.confirmButton}>{confirmText}</Text>
                  </TouchableOpacity>
                </View>
                {renderPicker()}
              </View>
            </View>
          </Modal>
        </>
      );
    }

    return (
      <View style={[getContainerStyle(), style]}>
        {label && (
          <Text style={styles.label}>
            {label}
            {required && <Text style={styles.required}>*</Text>}
          </Text>
        )}
        {renderPicker()}
        {error && <Text style={styles.errorText}>{error}</Text>}
      </View>
    );
  }
);

// Componentes predefinidos para casos comunes
export const TimePickerAlarm: React.FC<Omit<TimePickerProps, 'mode' | 'title' | 'icon'>> = (props) => (
  <TimePicker
    {...props}
    mode="time"
    title="Set Alarm"
    icon="bell"
    showSeconds={false}
  />
);

export const TimePickerDuration: React.FC<Omit<TimePickerProps, 'mode' | 'title' | 'icon'>> = (props) => (
  <TimePicker
    {...props}
    mode="duration"
    title="Set Duration"
    icon="clock"
    showSeconds={true}
  />
);

export const TimePickerCountdown: React.FC<Omit<TimePickerProps, 'mode' | 'title' | 'icon'>> = (props) => (
  <TimePicker
    {...props}
    mode="countdown"
    title="Set Countdown"
    icon="clock"
    showSeconds={true}
  />
);

export const TimePickerMeeting: React.FC<Omit<TimePickerProps, 'mode' | 'title' | 'icon'>> = (props) => (
  <TimePicker
    {...props}
    mode="time"
    title="Meeting Time"
    icon="calendar"
    minuteInterval={15}
  />
);

export const TimePickerWorkout: React.FC<Omit<TimePickerProps, 'mode' | 'title' | 'icon'>> = (props) => (
  <TimePicker
    {...props}
    mode="duration"
    title="Workout Duration"
    icon="heart"
    showSeconds={true}
  />
);

// Manager para TimePicker global
class TimePickerManagerClass {
  private static instance: TimePickerManagerClass;
  private currentPicker: React.RefObject<TimePickerRef> | null = null;

  static getInstance(): TimePickerManagerClass {
    if (!TimePickerManagerClass.instance) {
      TimePickerManagerClass.instance = new TimePickerManagerClass();
    }
    return TimePickerManagerClass.instance;
  }

  setCurrentPicker(picker: React.RefObject<TimePickerRef>) {
    this.currentPicker = picker;
  }

  openPicker() {
    this.currentPicker?.current?.open();
  }

  closePicker() {
    this.currentPicker?.current?.close();
  }

  getCurrentTime(): TimeValue | null {
    return this.currentPicker?.current?.getValue() || null;
  }

  setCurrentTime(time: TimeValue) {
    this.currentPicker?.current?.setValue(time);
  }
}

export const TimePickerManager = TimePickerManagerClass.getInstance();

// Estilos
const { width, height } = Dimensions.get('window');

const styles = StyleSheet.create({
  container: {
    marginVertical: 8,
  },
  outlined: {
    borderWidth: 1,
    borderColor: '#E0E0E0',
    borderRadius: 8,
    padding: 12,
  },
  minimal: {
    borderBottomWidth: 1,
    borderBottomColor: '#E0E0E0',
    paddingVertical: 8,
  },
  small: {
    padding: 8,
  },
  large: {
    padding: 16,
  },
  disabled: {
    opacity: 0.5,
  },
  error: {
    borderColor: '#F44336',
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    color: '#333',
    marginBottom: 4,
  },
  required: {
    color: '#F44336',
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    minHeight: 40,
  },
  icon: {
    marginRight: 8,
  },
  text: {
    fontSize: 16,
    color: '#333',
    flex: 1,
  },
  textSmall: {
    fontSize: 14,
  },
  textLarge: {
    fontSize: 18,
  },
  textDisabled: {
    color: '#999',
  },
  errorText: {
    fontSize: 12,
    color: '#F44336',
    marginTop: 4,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#FFF',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingBottom: Platform.OS === 'ios' ? 34 : 20,
    maxHeight: Math.min(360, height * 0.5),
    overflow: 'hidden',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E0E0E0',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  cancelButton: {
    fontSize: 16,
    color: '#666',
  },
  confirmButton: {
    fontSize: 16,
    color: '#007AFF',
    fontWeight: '600',
  },
  pickerContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 20,
  },
  wheelContainer: {
    alignItems: 'center',
    marginHorizontal: 10,
  },
  wheelLabel: {
    fontSize: 12,
    color: '#666',
    marginBottom: 8,
    textTransform: 'uppercase',
    fontWeight: '500',
  },
  wheel: {
    height: 200,
    overflow: 'hidden',
  },
  wheelScroll: {
    paddingVertical: 4,
    flexGrow: 1,
  },
  wheelItem: {
    paddingVertical: 8,
    paddingHorizontal: 12,
    marginVertical: 2,
    borderRadius: 6,
    minWidth: 50,
    alignItems: 'center',
  },
  wheelItemSelected: {
    backgroundColor: '#007AFF',
  },
  wheelItemText: {
    fontSize: 18,
    color: '#333',
    fontWeight: '500',
  },
  wheelItemTextSelected: {
    color: '#FFF',
    fontWeight: '600',
  },
});

export default TimePicker;