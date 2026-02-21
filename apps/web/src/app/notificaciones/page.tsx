'use client';

import React, { useEffect, useState, useCallback } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import {
  useAuthStore,
  useNotificationStore,
} from '@hydroespinaca/shared';
import type {
  NotificationChannel,
  ChannelPreference,
  DailySummaryConfig,
  QuietHoursConfig,
  WeatherAlertSubscription,
  UpdatePreferencesRequest,
  PushSubscriptionInfo,
  NotificationLogEntry,
} from '@hydroespinaca/shared';
import { CHANNEL_LABELS, CHANNEL_ICONS, ALERT_TYPE_LABELS } from '@hydroespinaca/shared';
import type { AlertType } from '@hydroespinaca/shared';

// ────────────── Toggle Switch ──────────────

const Toggle: React.FC<{ enabled: boolean; onChange: () => void; disabled?: boolean }> = ({ enabled, onChange, disabled }) => (
  <button
    onClick={onChange}
    disabled={disabled}
    className={`relative w-11 h-6 rounded-full transition-colors ${
      enabled ? 'bg-green-500' : 'bg-gray-300'
    } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
  >
    <span
      className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
        enabled ? 'translate-x-5' : 'translate-x-0'
      }`}
    />
  </button>
);

// ────────────── Channel Preferences ──────────────

const ALL_CHANNELS: NotificationChannel[] = ['email', 'push', 'web_push', 'whatsapp'];

const ChannelPreferencesSection: React.FC<{
  channels: ChannelPreference[];
  onChange: (channels: ChannelPreference[]) => void;
  onWebPushRegister: () => void;
}> = ({ channels, onChange, onWebPushRegister }) => {

  const getChannel = (ch: NotificationChannel): ChannelPreference =>
    channels.find(c => c.channel === ch) || { channel: ch, enabled: false };

  const toggle = (ch: NotificationChannel) => {
    const existing = channels.find(c => c.channel === ch);
    if (existing) {
      onChange(channels.map(c => c.channel === ch ? { ...c, enabled: !c.enabled } : c));
    } else {
      onChange([...channels, { channel: ch, enabled: true }]);
    }
  };

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100">
        <h3 className="font-semibold text-gray-800">Canales de Notificación</h3>
        <p className="text-xs text-gray-500 mt-0.5">Elige cómo quieres recibir tus notificaciones</p>
      </div>
      <div className="divide-y divide-gray-100">
        {ALL_CHANNELS.map(ch => {
          const pref = getChannel(ch);
          return (
            <div key={ch} className="p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="text-xl">{CHANNEL_ICONS[ch]}</span>
                <div>
                  <p className="text-sm font-medium text-gray-700">{CHANNEL_LABELS[ch]}</p>
                  {ch === 'web_push' && pref.enabled && (
                    <button
                      onClick={onWebPushRegister}
                      className="text-xs text-green-600 hover:underline mt-0.5"
                    >
                      Habilitar notificaciones en este navegador
                    </button>
                  )}
                  {ch === 'whatsapp' && pref.enabled && (
                    <p className="text-xs text-gray-400 mt-0.5">Requiere sandbox Twilio activo</p>
                  )}
                </div>
              </div>
              <Toggle enabled={pref.enabled} onChange={() => toggle(ch)} />
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ────────────── Daily Summary Config ──────────────

const DailySummarySection: React.FC<{
  config: DailySummaryConfig;
  onChange: (config: DailySummaryConfig) => void;
}> = ({ config, onChange }) => {

  const update = (partial: Partial<DailySummaryConfig>) => onChange({ ...config, ...partial });

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100 flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-gray-800">Resumen Diario</h3>
          <p className="text-xs text-gray-500 mt-0.5">Recibe un resumen automático en tu correo cada día</p>
        </div>
        <Toggle enabled={config.enabled} onChange={() => update({ enabled: !config.enabled })} />
      </div>
      {config.enabled && (
        <div className="p-4 space-y-4">
          {/* Time picker */}
          <div className="flex items-center gap-4">
            <label className="text-sm text-gray-600 w-24">Hora de envío</label>
            <div className="flex items-center gap-1">
              <input
                type="number" min={0} max={23} value={config.hour}
                onChange={e => update({ hour: parseInt(e.target.value) || 0 })}
                className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
              />
              <span className="text-gray-500">:</span>
              <input
                type="number" min={0} max={59} step={5} value={config.minute}
                onChange={e => update({ minute: parseInt(e.target.value) || 0 })}
                className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
              />
              <span className="text-xs text-gray-400 ml-2">(hora Colombia)</span>
            </div>
          </div>

          {/* Include checkboxes */}
          <div>
            <p className="text-sm text-gray-600 mb-2">Incluir en el resumen:</p>
            <div className="grid grid-cols-2 gap-2">
              {([
                ['includeSensorAverages', '📊 Promedios de sensores'],
                ['includeActuatorRuntime', '⚡ Tiempo de actuadores'],
                ['includeFuzzyRules', '🧠 Evaluaciones fuzzy'],
                ['includeWeatherForecast', '🌤️ Pronóstico del clima'],
              ] as const).map(([key, label]) => (
                <label key={key} className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config[key]}
                    onChange={() => update({ [key]: !config[key] })}
                    className="w-4 h-4 rounded border-gray-300 text-green-600 focus:ring-green-500"
                  />
                  {label}
                </label>
              ))}
            </div>
          </div>

          {/* Channel for summary */}
          <div>
            <p className="text-sm text-gray-600 mb-2">Enviar resumen por:</p>
            <div className="flex flex-wrap gap-2">
              {ALL_CHANNELS.map(ch => (
                <label key={ch} className="flex items-center gap-1.5 text-sm text-gray-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.channels.includes(ch)}
                    onChange={() => {
                      const channels = config.channels.includes(ch)
                        ? config.channels.filter(c => c !== ch)
                        : [...config.channels, ch];
                      update({ channels });
                    }}
                    className="w-4 h-4 rounded border-gray-300 text-green-600 focus:ring-green-500"
                  />
                  {CHANNEL_ICONS[ch]} {CHANNEL_LABELS[ch]}
                </label>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// ────────────── Quiet Hours Config ──────────────

const QuietHoursSection: React.FC<{
  config: QuietHoursConfig;
  onChange: (config: QuietHoursConfig) => void;
}> = ({ config, onChange }) => {
  const update = (partial: Partial<QuietHoursConfig>) => onChange({ ...config, ...partial });

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100 flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-gray-800">Horario de Silencio</h3>
          <p className="text-xs text-gray-500 mt-0.5">No enviar notificaciones durante estas horas</p>
        </div>
        <Toggle enabled={config.enabled} onChange={() => update({ enabled: !config.enabled })} />
      </div>
      {config.enabled && (
        <div className="p-4 flex items-center gap-4">
          <label className="text-sm text-gray-600">De</label>
          <input
            type="number" min={0} max={23} value={config.startHour}
            onChange={e => update({ startHour: parseInt(e.target.value) || 0 })}
            className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
          />
          <label className="text-sm text-gray-600">a</label>
          <input
            type="number" min={0} max={23} value={config.endHour}
            onChange={e => update({ endHour: parseInt(e.target.value) || 0 })}
            className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
          />
          <span className="text-xs text-gray-400">(hora Colombia)</span>
        </div>
      )}
    </div>
  );
};

// ────────────── Device List ──────────────

const DeviceListSection: React.FC<{
  devices: PushSubscriptionInfo[];
  loading: boolean;
  onUnregister: (id: string) => void;
}> = ({ devices, loading, onUnregister }) => {
  const activeDevices = devices.filter(d => d.isActive);

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100">
        <h3 className="font-semibold text-gray-800">Dispositivos Push Registrados</h3>
        <p className="text-xs text-gray-500 mt-0.5">{activeDevices.length} dispositivo(s) activo(s)</p>
      </div>
      {loading && !devices.length ? (
        <div className="p-6 text-center">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-green-600 mx-auto" />
        </div>
      ) : !activeDevices.length ? (
        <div className="p-6 text-center">
          <span className="text-3xl block mb-2">📵</span>
          <p className="text-sm text-gray-500">No hay dispositivos registrados</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100">
          {activeDevices.map(device => (
            <div key={device.id} className="p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="text-xl">
                  {device.platform === 'expo' ? '📱' : device.platform === 'web_push' ? '🖥️' : '📲'}
                </span>
                <div>
                  <p className="text-sm font-medium text-gray-700">{device.deviceName || 'Dispositivo'}</p>
                  <p className="text-xs text-gray-400">
                    {device.platform} · {device.createdAt
                      ? new Date(device.createdAt).toLocaleDateString('es-CO')
                      : 'Fecha desconocida'}
                  </p>
                </div>
              </div>
              <button
                onClick={() => device.id && onUnregister(device.id)}
                className="px-3 py-1.5 text-xs text-red-600 hover:bg-red-50 rounded-lg transition-colors"
              >
                Desregistrar
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// ────────────── Notification History ──────────────

const HistorySection: React.FC<{
  history: NotificationLogEntry[];
  loading: boolean;
}> = ({ history, loading }) => {
  const getStatusStyle = (status: string) => {
    if (status === 'sent' || status === 'delivered') return 'bg-green-100 text-green-700';
    if (status === 'failed') return 'bg-red-100 text-red-700';
    return 'bg-gray-100 text-gray-600';
  };

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100">
        <h3 className="font-semibold text-gray-800">Historial de Envíos</h3>
      </div>
      {loading && !history.length ? (
        <div className="p-6 text-center">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-green-600 mx-auto" />
        </div>
      ) : !history.length ? (
        <div className="p-6 text-center">
          <span className="text-3xl block mb-2">📭</span>
          <p className="text-sm text-gray-500">No hay notificaciones enviadas</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100 max-h-[400px] overflow-y-auto">
          {history.map(entry => (
            <div key={entry.id} className="p-3 flex items-center gap-3">
              <span className="text-lg">
                {CHANNEL_ICONS[entry.channel as NotificationChannel] || '📨'}
              </span>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-700 truncate">{entry.title || entry.templateKey || 'Notificación'}</p>
                <p className="text-xs text-gray-400">
                  {entry.sentAt
                    ? new Date(entry.sentAt).toLocaleString('es-CO', { timeZone: 'America/Bogota' })
                    : 'Pendiente'}
                </p>
              </div>
              <span className={`text-xs px-2 py-0.5 rounded-full ${getStatusStyle(entry.status)}`}>
                {entry.status === 'sent' ? 'Enviado' : entry.status === 'failed' ? 'Fallido' : entry.status}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// ────────────── Main Page ──────────────

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
  const user = useAuthStore(s => s.user);
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

  const markDirty = useCallback(() => {
    setIsDirty(true);
    setSaveSuccess(false);
  }, []);

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

  const handleWebPushRegister = async () => {
    if (!user?.id) return;
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      alert('Tu navegador no soporta notificaciones push.');
      return;
    }

    try {
      const reg = await navigator.serviceWorker.ready;
      const subscription = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY ?? null,
      });

      const p256dh = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('p256dh')!)));
      const auth = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('auth')!)));

      await useNotificationStore.getState().registerPush({
        userId: user.id,
        platform: 'web_push',
        token: JSON.stringify({ endpoint: subscription.endpoint, keys: { p256dh, auth } }),
        deviceName: navigator.userAgent.includes('Chrome') ? 'Chrome' : navigator.userAgent.includes('Firefox') ? 'Firefox' : 'Navegador',
      });
    } catch (err) {
      console.error('Error registering web push:', err);
      alert('No se pudo registrar las notificaciones push. Verifica que hayas otorgado permisos.');
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
      {/* Error banner */}
      {preferencesError && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl">
          <p className="text-sm text-red-700">{preferencesError}</p>
        </div>
      )}

      {/* Save bar */}
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
          {/* Channel preferences */}
          <ChannelPreferencesSection
            channels={localChannels}
            onChange={channels => { setLocalChannels(channels); markDirty(); }}
            onWebPushRegister={handleWebPushRegister}
          />

          {/* Daily summary */}
          <DailySummarySection
            config={localDailySummary}
            onChange={config => { setLocalDailySummary(config); markDirty(); }}
          />

          {/* Quiet hours */}
          <QuietHoursSection
            config={localQuietHours}
            onChange={config => { setLocalQuietHours(config); markDirty(); }}
          />

          {/* Devices */}
          <DeviceListSection
            devices={devices}
            loading={devicesLoading}
            onUnregister={handleUnregister}
          />

          {/* History */}
          <HistorySection
            history={history}
            loading={historyLoading}
          />
        </div>
      )}
    </PageLayout>
  );
}
