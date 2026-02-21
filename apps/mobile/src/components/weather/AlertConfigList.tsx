import React, { useCallback } from 'react';
import { View, StyleSheet, TouchableOpacity } from 'react-native';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Spinner } from '../atoms/Spinner';
import { AlertThresholdRow } from '../molecules/AlertThresholdRow';
import { spacing, semanticColors, typography, colors } from '@hydroespinaca/shared';
import type { WeatherAlertConfig, AlertType } from '@hydroespinaca/shared';

export interface AlertConfigListProps {
  config: WeatherAlertConfig | null;
  loading?: boolean;
  error?: string | null;
  onConfigure: () => void;
}

export function AlertConfigList({
  config,
  loading,
  error,
  onConfigure,
}: AlertConfigListProps): React.ReactElement {
  if (loading) {
    return (
      <View style={styles.center}>
        <Spinner size="sm" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.section}>
        <Text variant="caption" color={semanticColors.error}>{error}</Text>
      </View>
    );
  }

  if (!config) {
    return (
      <View style={styles.section}>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          No hay configuración de alertas.
        </Text>
        <Button
          variant="outline"
          onPress={onConfigure}
          testID="alert-config-setup-btn"
        >
          Configurar Alertas
        </Button>
      </View>
    );
  }

  const enabledAlerts = config.alerts.filter(a => a.enabled);

  return (
    <View style={styles.section}>
      <View style={styles.header}>
        <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
          Alertas Configuradas
        </Text>
        <TouchableOpacity onPress={onConfigure} hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}>
          <Text variant="body" color={semanticColors.primary}>Editar</Text>
        </TouchableOpacity>
      </View>

      {!config.isActive && (
        <Text variant="caption" color={colors.warning[600]} style={styles.inactive}>
          ⚠️ Las alertas están desactivadas
        </Text>
      )}

      {enabledAlerts.length === 0 ? (
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          No hay umbrales habilitados.
        </Text>
      ) : (
        enabledAlerts.map(threshold => (
          <View key={threshold.type} style={styles.readonlyRow}>
            <Text variant="body" color={semanticColors.textPrimary}>
              {threshold.type === 'government' ? '🏛️' : ''} {threshold.recommendation || threshold.type}
            </Text>
            {threshold.thresholdValue != null && (
              <Text variant="caption" color={semanticColors.textSecondary}>
                Umbral: {threshold.thresholdValue}
              </Text>
            )}
          </View>
        ))
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
  },
  center: {
    alignItems: 'center',
    paddingVertical: spacing.xl,
  },
  empty: {
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  inactive: {
    marginBottom: spacing.sm,
  },
  readonlyRow: {
    paddingVertical: spacing.xs,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
  },
});
