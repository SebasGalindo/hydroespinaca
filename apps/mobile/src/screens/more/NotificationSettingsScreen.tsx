/**
 * NotificationSettingsScreen — Preferencias de notificación:
 * canales, resumen diario, horas de silencio, dispositivos push.
 */
import React, { useState, useCallback } from 'react';
import { View, StyleSheet } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { ScreenLayout } from '../../components/organisms/ScreenLayout';
import { DailySummaryConfigSheet } from '../../components/organisms/DailySummaryConfigSheet';
import { QuietHoursSheet } from '../../components/organisms/QuietHoursSheet';
import { TermsModal } from '../../components/organisms/TermsModal';
import { ChannelPreferences } from '../../components/notifications/ChannelPreferences';
import { DeviceList } from '../../components/notifications/DeviceList';
import { Button } from '../../components/atoms/Button';
import { Text } from '../../components/atoms/Text';
import {
  useNotificationStore,
  useAuthStore,
  spacing,
  semanticColors,
  colors,
} from '@hydroespinaca/shared';
import type {
  NotificationChannel,
  DailySummaryConfig,
  QuietHoursConfig,
  UpdatePreferencesRequest,
} from '@hydroespinaca/shared';

export function NotificationSettingsScreen(): React.ReactElement {
  const userId = useAuthStore(s => s.user?.id) ?? '';

  const preferences = useNotificationStore(s => s.preferences);
  const preferencesLoading = useNotificationStore(s => s.preferencesLoading);
  const preferencesError = useNotificationStore(s => s.preferencesError);
  const fetchPreferences = useNotificationStore(s => s.fetchPreferences);
  const updatePreferences = useNotificationStore(s => s.updatePreferences);

  const devices = useNotificationStore(s => s.devices);
  const devicesLoading = useNotificationStore(s => s.devicesLoading);
  const devicesError = useNotificationStore(s => s.devicesError);
  const fetchDevices = useNotificationStore(s => s.fetchDevices);
  const unregisterPush = useNotificationStore(s => s.unregisterPush);

  const [dailySummaryOpen, setDailySummaryOpen] = useState(false);
  const [quietHoursOpen, setQuietHoursOpen] = useState(false);
  const [termsVisible, setTermsVisible] = useState(false);

  useFocusEffect(
    useCallback(() => {
      if (userId) {
        fetchPreferences(userId);
        fetchDevices(userId);
      }
    }, [userId])
  );

  const handleChannelToggle = useCallback(
    async (channel: NotificationChannel, enabled: boolean) => {
      if (!preferences) return;
      const updatedChannels = preferences.channels.map(ch =>
        ch.channel === channel ? { ...ch, enabled } : ch
      );
      const req: UpdatePreferencesRequest = {
        channels: updatedChannels,
        dailySummary: preferences.dailySummary ?? {
          enabled: false,
          hour: 7,
          minute: 0,
          channels: [],
          includeFuzzyRules: true,
          includeSensorAverages: true,
          includeActuatorRuntime: false,
          includeWeatherForecast: true,
        },
        weatherAlertsSubscription: preferences.weatherAlertsSubscription ?? {
          enabled: false,
          alertTypes: [],
        },
        quietHours: preferences.quietHours,
      };
      await updatePreferences(userId, req);
    },
    [preferences, userId, updatePreferences]
  );

  const handleSaveDailySummary = useCallback(
    async (config: DailySummaryConfig) => {
      if (!preferences) return;
      await updatePreferences(userId, {
        channels: preferences.channels,
        dailySummary: config,
        weatherAlertsSubscription: preferences.weatherAlertsSubscription ?? {
          enabled: false,
          alertTypes: [],
        },
        quietHours: preferences.quietHours,
      });
    },
    [preferences, userId, updatePreferences]
  );

  const handleSaveQuietHours = useCallback(
    async (config: QuietHoursConfig) => {
      if (!preferences) return;
      await updatePreferences(userId, {
        channels: preferences.channels,
        dailySummary: preferences.dailySummary ?? {
          enabled: false,
          hour: 7,
          minute: 0,
          channels: [],
          includeFuzzyRules: true,
          includeSensorAverages: true,
          includeActuatorRuntime: false,
          includeWeatherForecast: true,
        },
        weatherAlertsSubscription: preferences.weatherAlertsSubscription ?? {
          enabled: false,
          alertTypes: [],
        },
        quietHours: config,
      });
    },
    [preferences, userId, updatePreferences]
  );

  const handleRemoveDevice = useCallback(
    (subscriptionId: string) => {
      unregisterPush(subscriptionId, userId);
    },
    [userId, unregisterPush]
  );

  const handleRefresh = useCallback(() => {
    if (userId) {
      fetchPreferences(userId);
      fetchDevices(userId);
    }
  }, [userId]);

  const enabledChannels: NotificationChannel[] = (preferences?.channels ?? [])
    .filter(ch => ch.enabled)
    .map(ch => ch.channel);

  return (
    <>
      <ScreenLayout
        title="Notificaciones"
        subtitle="Configura cómo y cuándo recibir notificaciones"
        scrollable
        refreshing={preferencesLoading}
        onRefresh={handleRefresh}
        testID="notification-settings-screen"
      >
        {/* Channel preferences */}
        <ChannelPreferences
          channels={preferences?.channels ?? []}
          loading={preferencesLoading}
          error={preferencesError}
          onToggle={handleChannelToggle}
        />

        {/* Quick config buttons */}
        <View style={styles.configButtons}>
          <Button
            variant="outline"
            onPress={() => setDailySummaryOpen(true)}
            style={styles.configButton}
            testID="daily-summary-btn"
          >
            📊 Resumen Diario
          </Button>
          <Button
            variant="outline"
            onPress={() => setQuietHoursOpen(true)}
            style={styles.configButton}
            testID="quiet-hours-btn"
          >
            🌙 Horas de Silencio
          </Button>
        </View>

        {/* Quiet hours status */}
        {preferences?.quietHours?.enabled && (
          <View style={styles.statusRow}>
            <Text variant="caption" color={colors.warning[600]}>
              🌙 Silencio activo: {preferences.quietHours.startHour}:00 — {preferences.quietHours.endHour}:00
            </Text>
          </View>
        )}

        {/* Push devices */}
        <DeviceList
          devices={devices}
          loading={devicesLoading}
          error={devicesError}
          onRemove={handleRemoveDevice}
        />

        {/* Tratamiento de datos */}
        <Button
          variant="ghost"
          onPress={() => setTermsVisible(true)}
          style={styles.termsButton}
        >
          <Text variant="caption" color={semanticColors.primary} style={styles.termsButtonText}>
            Tratamiento de datos personales
          </Text>
        </Button>
      </ScreenLayout>

      {/* Bottom sheets */}
      <DailySummaryConfigSheet
        isOpen={dailySummaryOpen}
        onClose={() => setDailySummaryOpen(false)}
        config={preferences?.dailySummary ?? null}
        availableChannels={enabledChannels}
        loading={preferencesLoading}
        onSave={handleSaveDailySummary}
        testID="daily-summary-sheet"
      />

      <QuietHoursSheet
        isOpen={quietHoursOpen}
        onClose={() => setQuietHoursOpen(false)}
        config={preferences?.quietHours ?? null}
        loading={preferencesLoading}
        onSave={handleSaveQuietHours}
        testID="quiet-hours-sheet"
      />

      <TermsModal
        visible={termsVisible}
        readOnly
        onClose={() => setTermsVisible(false)}
      />
    </>
  );
}

const styles = StyleSheet.create({
  configButtons: {
    flexDirection: 'row',
    paddingHorizontal: spacing.md,
    marginBottom: spacing.lg,
    gap: spacing.sm,
  },
  configButton: {
    flex: 1,
  },
  statusRow: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  termsButton: {
    marginHorizontal: spacing.md,
    marginBottom: spacing.md,
  },
  termsButtonText: {
    textDecorationLine: 'underline',
  },
});
