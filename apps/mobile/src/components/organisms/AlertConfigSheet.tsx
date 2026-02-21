import React, { useState, useEffect, useCallback } from 'react';
import { View, StyleSheet } from 'react-native';
import { BottomSheetForm } from './BottomSheetForm';
import { Button } from '../atoms/Button';
import { Switch } from '../atoms/Switch';
import { Text } from '../atoms/Text';
import { AlertThresholdRow } from '../molecules/AlertThresholdRow';
import { spacing, semanticColors, colors } from '@hydroespinaca/shared';
import type { WeatherAlertConfig, AlertThreshold, AlertType } from '@hydroespinaca/shared';

export interface AlertConfigSheetProps {
  isOpen: boolean;
  onClose: () => void;
  config: WeatherAlertConfig | null;
  loading?: boolean;
  onSave: (isActive: boolean, alerts: AlertThreshold[]) => Promise<void>;
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
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (config) {
      setIsActive(config.isActive);
      setAlerts([...config.alerts]);
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
      await onSave(isActive, alerts);
      onClose();
    } catch {
      // Error handled upstream (store)
    } finally {
      setSaving(false);
    }
  }, [isActive, alerts, onSave, onClose]);

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
});
