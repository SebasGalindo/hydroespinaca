import React, { useState, useEffect, useCallback } from 'react';
import { View, StyleSheet, Platform } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { BottomSheetForm } from './BottomSheetForm';
import { Button } from '../atoms/Button';
import { Switch } from '../atoms/Switch';
import { Text } from '../atoms/Text';
import { spacing, semanticColors } from '@hydroespinaca/shared';
import type { QuietHoursConfig } from '@hydroespinaca/shared';

function makeDate(h: number): Date {
  const d = new Date();
  d.setHours(h, 0, 0, 0);
  return d;
}

function formatHour(h: number): string {
  return `${h.toString().padStart(2, '0')}:00`;
}

export interface QuietHoursSheetProps {
  isOpen: boolean;
  onClose: () => void;
  config: QuietHoursConfig | null;
  loading?: boolean;
  onSave: (config: QuietHoursConfig) => Promise<void>;
  testID?: string;
}

type PickerTarget = 'start' | 'end' | null;

export function QuietHoursSheet({
  isOpen,
  onClose,
  config,
  loading = false,
  onSave,
  testID,
}: QuietHoursSheetProps): React.ReactElement | null {
  const [enabled, setEnabled] = useState(false);
  const [startHour, setStartHour] = useState(22);
  const [endHour, setEndHour] = useState(7);
  const [pickerTarget, setPickerTarget] = useState<PickerTarget>(
    Platform.OS === 'ios' ? null : null
  );
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (config) {
      setEnabled(config.enabled);
      setStartHour(config.startHour);
      setEndHour(config.endHour);
    }
  }, [config]);

  const handleTimeChange = useCallback(
    (_: any, date?: Date) => {
      if (Platform.OS === 'android') setPickerTarget(null);
      if (!date || !pickerTarget) return;
      const h = date.getHours();
      if (pickerTarget === 'start') setStartHour(h);
      else setEndHour(h);
    },
    [pickerTarget]
  );

  const handleSave = useCallback(async () => {
    setSaving(true);
    try {
      await onSave({ enabled, startHour, endHour });
      onClose();
    } catch {
      // handled upstream
    } finally {
      setSaving(false);
    }
  }, [enabled, startHour, endHour, onSave, onClose]);



  return (
    <BottomSheetForm
      title="Horas de Silencio"
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['50%', '70%']}
      testID={testID}
    >
      <View style={styles.section}>
        <Switch
          value={enabled}
          onValueChange={setEnabled}
          label="Horas de silencio activas"
          testID={`${testID}-enabled`}
        />
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.hint}>
          Las notificaciones se pausarán durante este período.
        </Text>
      </View>

      {enabled && (
        <>
          {/* Start hour */}
          <View style={styles.section}>
            <Text variant="body" weight="medium" color={semanticColors.textPrimary}>
              Desde
            </Text>
            {Platform.OS === 'android' ? (
              <Button
                variant="outline"
                onPress={() => setPickerTarget('start')}
                testID={`${testID}-start-btn`}
              >
                {formatHour(startHour)}
              </Button>
            ) : (
              <DateTimePicker
                value={makeDate(startHour)}
                mode="time"
                is24Hour
                display="spinner"
                onChange={(_, d) => d && setStartHour(d.getHours())}
                testID={`${testID}-start-picker`}
              />
            )}
          </View>

          {/* End hour */}
          <View style={styles.section}>
            <Text variant="body" weight="medium" color={semanticColors.textPrimary}>
              Hasta
            </Text>
            {Platform.OS === 'android' ? (
              <Button
                variant="outline"
                onPress={() => setPickerTarget('end')}
                testID={`${testID}-end-btn`}
              >
                {formatHour(endHour)}
              </Button>
            ) : (
              <DateTimePicker
                value={makeDate(endHour)}
                mode="time"
                is24Hour
                display="spinner"
                onChange={(_, d) => d && setEndHour(d.getHours())}
                testID={`${testID}-end-picker`}
              />
            )}
          </View>

          {/* Android picker dialog */}
          {Platform.OS === 'android' && pickerTarget && (
            <DateTimePicker
              value={makeDate(pickerTarget === 'start' ? startHour : endHour)}
              mode="time"
              is24Hour
              display="default"
              onChange={handleTimeChange}
            />
          )}
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
  hint: {
    marginTop: spacing.xs,
  },
});
