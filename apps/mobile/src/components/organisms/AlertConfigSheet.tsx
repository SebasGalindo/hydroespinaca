import React, { useState, useEffect, useCallback } from 'react';
import { View, StyleSheet, TextInput } from 'react-native';
import { BottomSheetForm } from './BottomSheetForm';
import { Button } from '../atoms/Button';
import { Switch } from '../atoms/Switch';
import { Text } from '../atoms/Text';
import { AlertThresholdRow } from '../molecules/AlertThresholdRow';
import { spacing, semanticColors, colors, typography, borderRadius } from '@hydroespinaca/shared';
import type { WeatherAlertConfig, AlertThreshold, AlertType } from '@hydroespinaca/shared';

export interface AlertConfigSheetProps {
  isOpen: boolean;
  onClose: () => void;
  config: WeatherAlertConfig | null;
  loading?: boolean;
  onSave: (isActive: boolean, alerts: AlertThreshold[], maxForecastDays: number, allowDuplicateAlerts: boolean) => Promise<void>;
  testID?: string;
}

export function AlertConfigSheet({
  isOpen,
  onClose,
  config,
  loading = false,
  onSave,
  testID,
}: AlertConfigSheetProps): React.ReactElement | null {
  const [isActive, setIsActive] = useState(false);
  const [alerts, setAlerts] = useState<AlertThreshold[]>([]);
  const [maxForecastDays, setMaxForecastDays] = useState(8);
  const [allowDuplicateAlerts, setAllowDuplicateAlerts] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (config) {
      setIsActive(config.isActive);
      setAlerts([...config.alerts]);
      setMaxForecastDays(config.maxForecastDays ?? 8);
      setAllowDuplicateAlerts(config.allowDuplicateAlerts ?? true);
    }
  }, [config]);

  const handleToggle = useCallback((type: AlertType, enabled: boolean) => {
    setAlerts(prev =>
      prev.map(a => (a.type === type ? { ...a, enabled } : a))
    );
  }, []);

  const handleValueChange = useCallback((type: AlertType, value: number) => {
    setAlerts(prev =>
      prev.map(a => (a.type === type ? { ...a, thresholdValue: value } : a))
    );
  }, []);

  const handleSave = useCallback(async () => {
    setSaving(true);
    try {
      await onSave(isActive, alerts, maxForecastDays, allowDuplicateAlerts);
      onClose();
    } catch {
      // Error handled upstream (store)
    } finally {
      setSaving(false);
    }
  }, [isActive, alerts, maxForecastDays, allowDuplicateAlerts, onSave, onClose]);

  return (
    <BottomSheetForm
      title="Configuración de Alertas"
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['70%', '95%']}
      testID={testID}
    >
      <View style={styles.section}>
        <Switch
          value={isActive}
          onValueChange={setIsActive}
          label="Alertas activas"
          testID={`${testID}-active-switch`}
        />

        <View style={styles.preferenceRow}>
          <View style={styles.preferenceLabel}>
            <Text variant="label" color={semanticColors.textPrimary}>Días de pronóstico</Text>
            <Text variant="caption" color={semanticColors.textSecondary}>Evaluar los primeros N días (1–8)</Text>
          </View>
          <TextInput
            style={styles.daysInput}
            value={String(maxForecastDays)}
            onChangeText={(v) => {
              const n = Math.max(1, Math.min(8, parseInt(v) || 1));
              setMaxForecastDays(n);
            }}
            keyboardType="number-pad"
            maxLength={1}
          />
        </View>

        <Switch
          value={allowDuplicateAlerts}
          onValueChange={setAllowDuplicateAlerts}
          label="Permitir alertas repetidas"
          testID={`${testID}-duplicate-switch`}
        />
      </View>

      {isActive && (
        <View style={styles.section}>
          <Text variant="h3" color={semanticColors.textPrimary} style={styles.sectionTitle}>
            Umbrales
          </Text>
          {alerts.map(threshold => (
            <AlertThresholdRow
              key={threshold.type}
              threshold={threshold}
              onToggle={handleToggle}
              onValueChange={handleValueChange}
              testID={`${testID}-threshold-${threshold.type}`}
            />
          ))}
        </View>
      )}

      <Button
        onPress={handleSave}
        disabled={saving || loading}
        testID={`${testID}-save`}
      >
        {saving ? 'Guardando...' : 'Guardar Configuración'}
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
  preferenceRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: spacing.md,
    marginBottom: spacing.sm,
  },
  preferenceLabel: {
    flex: 1,
    gap: 2,
  },
  daysInput: {
    width: 48,
    borderWidth: 1,
    borderColor: colors.gray[300],
    borderRadius: borderRadius.md,
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.sm,
    textAlign: 'center',
    fontSize: typography.fontSize.md,
    color: colors.gray[900],
  },
});
