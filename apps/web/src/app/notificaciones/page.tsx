'use client';

import React, { useEffect, useState, useCallback } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import {
  useAuthStore,
  useNotificationStore,
} from '@hydroespinaca/shared';
import { TermsModal } from '@/components/auth/TermsModal';
import type {
  ChannelPreference,
  DailySummaryConfig,
  QuietHoursConfig,
  UpdatePreferencesRequest,
} from '@hydroespinaca/shared';
import {
  ChannelPreferencesSection,
  DailySummarySection,
  QuietHoursSection,
  DeviceListSection,
  HistorySection,
} from '@/components/notifications';
import { useWebPushRegistration } from '@/hooks/useWebPushRegistration';

// ────────────── Defaults ──────────────

const DEFAULT_DAILY_SUMMARY: DailySummaryConfig = {
  enabled: false, hour: 7, minute: 0,
  channels: ['email'],
  includeFuzzyRules: true, includeSensorAverages: true,
  includeActuatorRuntime: true, includeWeatherForecast: true,
};

const DEFAULT_QUIET_HOURS: QuietHoursConfig = {
  enabled: false, startHour: 22, endHour: 7,
};

export default function NotificacionesPage() {
  const user = useAuthStore((s) => s.user);
  const [showTerms, setShowTerms] = useState(false);
  const {
    preferences, preferencesLoading, preferencesError,
    fetchPreferences, updatePreferences,
    devices, devicesLoading, fetchDevices, unregisterPush,
    history, historyLoading, fetchHistory,
  } = useNotificationStore();

  const [localChannels, setLocalChannels] = useState<ChannelPreference[]>([]);
  const [localDailySummary, setLocalDailySummary] = useState<DailySummaryConfig>(DEFAULT_DAILY_SUMMARY);
  const [localQuietHours, setLocalQuietHours] = useState<QuietHoursConfig>(DEFAULT_QUIET_HOURS);
  const [isDirty, setIsDirty] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const markDirty = useCallback(() => {
    setIsDirty(true);
    setSaveSuccess(false);
  }, []);

  const handleWebPushRegister = useWebPushRegistration({
    userId: user?.id,
    setLocalChannels,
    markDirty,
  });

  // Load data
  useEffect(() => {
    if (user?.id) {
      fetchPreferences(user.id);
      fetchDevices(user.id);
      fetchHistory(user.id, { limit: 50 });
    }
  }, [user?.id, fetchPreferences, fetchDevices, fetchHistory]);

  // Sync local state from fetched preferences
  useEffect(() => {
    if (preferences) {
      setLocalChannels(preferences.channels || []);
      setLocalDailySummary(preferences.dailySummary || DEFAULT_DAILY_SUMMARY);
      setLocalQuietHours(preferences.quietHours || DEFAULT_QUIET_HOURS);
      setIsDirty(false);
    }
  }, [preferences]);

  const handleSave = async () => {
    if (!user?.id) return;
    setSaving(true);
    try {
      const req: UpdatePreferencesRequest = {
        channels: localChannels,
        dailySummary: localDailySummary,
        weatherAlertsSubscription: preferences?.weatherAlertsSubscription || { enabled: true, alertTypes: [] },
        quietHours: localQuietHours,
      };
      await updatePreferences(user.id, req);
      setIsDirty(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch {
      // Error handled by store
    } finally {
      setSaving(false);
    }
  };

  const handleUnregister = async (id: string) => {
    if (!user?.id) return;
    if (confirm('¿Deseas desregistrar este dispositivo?')) {
      await unregisterPush(id, user.id);
    }
  };

  return (
    <PageLayout
      title="Notificaciones"
      subtitle="Configura cómo y cuándo quieres recibir alertas y resúmenes"
      maxWidth="lg"
    >
      {preferencesError && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl">
          <p className="text-sm text-red-700">{preferencesError}</p>
        </div>
      )}

      {(isDirty || saveSuccess) && (
        <div className={`mb-6 p-4 rounded-xl flex items-center justify-between ${
          saveSuccess ? 'bg-green-50 border border-green-200' : 'bg-amber-50 border border-amber-200'
        }`}>
          <p className={`text-sm font-medium ${saveSuccess ? 'text-green-700' : 'text-amber-700'}`}>
            {saveSuccess ? '✅ Preferencias guardadas correctamente' : 'Tienes cambios sin guardar'}
          </p>
          {isDirty && (
            <button
              onClick={handleSave}
              disabled={saving}
              className="px-4 py-2 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
            >
              {saving ? 'Guardando...' : '💾 Guardar'}
            </button>
          )}
        </div>
      )}

      {preferencesLoading && !preferences ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-green-600" />
        </div>
      ) : (
        <div className="space-y-6">
          <ChannelPreferencesSection
            channels={localChannels}
            onChange={(channels) => { setLocalChannels(channels); markDirty(); }}
            onWebPushRegister={handleWebPushRegister}
          />
          <DailySummarySection
            config={localDailySummary}
            onChange={(config) => { setLocalDailySummary(config); markDirty(); }}
          />
          <QuietHoursSection
            config={localQuietHours}
            onChange={(config) => { setLocalQuietHours(config); markDirty(); }}
          />
          <DeviceListSection
            devices={devices}
            loading={devicesLoading}
            onUnregister={handleUnregister}
          />
          <HistorySection
            history={history}
            loading={historyLoading}
          />
          <div className="pt-2">
            <button
              onClick={() => setShowTerms(true)}
              className="text-sm text-green-600 hover:underline font-medium"
            >
              Tratamiento de datos personales
            </button>
          </div>
        </div>
      )}

      {showTerms && (
        <TermsModal readOnly onClose={() => setShowTerms(false)} />
      )}
    </PageLayout>
  );
}
