import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, StyleSheet, Platform } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { BottomSheetForm } from './BottomSheetForm';
import { Button } from '../atoms/Button';
import { Switch } from '../atoms/Switch';
import { Checkbox } from '../atoms/Checkbox';
import { Text } from '../atoms/Text';
import { ChannelToggleRow } from '../molecules/ChannelToggleRow';
import { spacing, semanticColors } from '@hydroespinaca/shared';
import type { DailySummaryConfig, NotificationChannel } from '@hydroespinaca/shared';

export interface DailySummaryConfigSheetProps {
  isOpen: boolean;
  onClose: () => void;
  config: DailySummaryConfig | null;
  availableChannels: NotificationChannel[];
  loading?: boolean;
  onSave: (config: DailySummaryConfig) => Promise<void>;
  testID?: string;
}

const CONTENT_OPTIONS: { key: keyof DailySummaryConfig; label: string }[] = [
  { key: 'includeFuzzyRules', label: 'Estado de rutinas fuzzy' },
  { key: 'includeSensorAverages', label: 'Promedios de sensores' },
  { key: 'includeActuatorRuntime', label: 'Tiempo de actuadores' },
  { key: 'includeWeatherForecast', label: 'Pronóstico del clima' },
];

export function DailySummaryConfigSheet({
  isOpen,
  onClose,
  config,
  availableChannels,
  loading = false,
  onSave,
  testID,
}: DailySummaryConfigSheetProps): React.ReactElement | null {
  const [enabled, setEnabled] = useState(false);
  const [hour, setHour] = useState(7);
  const [minute, setMinute] = useState(0);
  const [channels, setChannels] = useState<string[]>([]);
  const [content, setContent] = useState({
    includeFuzzyRules: true,
    includeSensorAverages: true,
    includeActuatorRuntime: false,
    includeWeatherForecast: true,
  });
  const [showTimePicker, setShowTimePicker] = useState(Platform.OS === 'ios');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (config) {
      setEnabled(config.enabled);
      setHour(config.hour);
      setMinute(config.minute);
      setChannels([...config.channels]);
      setContent({
        includeFuzzyRules: config.includeFuzzyRules,
        includeSensorAverages: config.includeSensorAverages,
        includeActuatorRuntime: config.includeActuatorRuntime,
        includeWeatherForecast: config.includeWeatherForecast,
      });
    }
  }, [config]);

  const handleTimeChange = useCallback((_: any, date?: Date) => {
    if (Platform.OS === 'android') setShowTimePicker(false);
    if (date) {
      setHour(date.getHours());
      setMinute(date.getMinutes());
    }
  }, []);

  const handleChannelToggle = useCallback((ch: NotificationChannel, on: boolean) => {
    setChannels(prev =>
      on ? [...prev, ch] : prev.filter(c => c !== ch)
    );
  }, []);

  const handleContentToggle = useCallback((key: keyof typeof content) => {
    setContent(prev => ({ ...prev, [key]: !prev[key] }));
  }, []);

  const handleSave = useCallback(async () => {
    setSaving(true);
    try {
      await onSave({
        enabled,
        hour,
        minute,
        channels,
        ...content,
      });
      onClose();
    } catch {
      // handled upstream
    } finally {
      setSaving(false);
    }
  }, [enabled, hour, minute, channels, content, onSave, onClose]);

  const timeDate = useMemo(() => {
    const d = new Date();
    d.setHours(hour, minute, 0, 0);
    return d;
  }, [hour, minute]);

  return (
    <BottomSheetForm
      title="Resumen Diario"
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['75%', '95%']}
      testID={testID}
    >
      <View style={styles.section}>
        <Switch
          value={enabled}
          onValueChange={setEnabled}
          label="Resumen diario habilitado"
          testID={`${testID}-enabled`}
        />
      </View>

      {enabled && (
        <>
          {/* Time picker */}
          <View style={styles.section}>
            <Text variant="h3" color={semanticColors.textPrimary} style={styles.sectionTitle}>
              Hora de envío
            </Text>
            {Platform.OS === 'android' && !showTimePicker && (
              <Button
                variant="outline"
                onPress={() => setShowTimePicker(true)}
                testID={`${testID}-time-btn`}
              >
                {`${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')} — Cambiar`}
              </Button>
            )}
            {showTimePicker && (
              <DateTimePicker
                value={timeDate}
                mode="time"
                is24Hour
                display={Platform.OS === 'ios' ? 'spinner' : 'default'}
                onChange={handleTimeChange}
                testID={`${testID}-time-picker`}
              />
            )}
          </View>

          {/* Channels */}
          <View style={styles.section}>
            <Text variant="h3" color={semanticColors.textPrimary} style={styles.sectionTitle}>
              Canales de envío
            </Text>
            {availableChannels.map(ch => (
              <ChannelToggleRow
                key={ch}
                channel={ch}
                enabled={channels.includes(ch)}
                onToggle={handleChannelToggle}
                testID={`${testID}-ch-${ch}`}
              />
            ))}
          </View>

          {/* Content checkboxes */}
          <View style={styles.section}>
            <Text variant="h3" color={semanticColors.textPrimary} style={styles.sectionTitle}>
              Contenido del resumen
            </Text>
            {CONTENT_OPTIONS.map(opt => (
              <Checkbox
                key={opt.key}
                label={opt.label}
                checked={!!content[opt.key as keyof typeof content]}
                onToggle={() => handleContentToggle(opt.key as keyof typeof content)}
                style={styles.checkbox}
                testID={`${testID}-${opt.key}`}
              />
            ))}
          </View>
        </>
      )}

      <Button
        onPress={handleSave}
        disabled={saving || loading}
        testID={`${testID}-save`}
      >
        {saving ? 'Guardando...' : 'Guardar'}
      </Button>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  section: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    marginBottom: spacing.sm,
  },
  checkbox: {
    marginBottom: spacing.xs,
  },
});
