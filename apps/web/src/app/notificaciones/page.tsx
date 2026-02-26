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
                    <div className="mt-2 space-y-2">
                      <button
                        onClick={onWebPushRegister}
                        className="text-xs text-green-600 hover:underline font-medium block"
                      >
                        Habilitar notificaciones en este navegador
                      </button>
                      
                      {/* Brave Browser specific warning */}
                      <div className="p-3 mt-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800 leading-relaxed shadow-sm">
                        <p className="font-semibold mb-1 text-amber-900 flex items-center gap-1">
                          <span className="text-base">⚠️</span> Nota para usuarios de Brave:
                        </p>
                        <p className="mb-2">Por defecto, Brave bloquea las notificaciones push. Para habilitarlas:</p>
                        <ol className="list-decimal ml-5 space-y-1 text-amber-900/90">
                          <li>Ve a la URL <code className="bg-amber-100/80 px-1.5 py-0.5 rounded text-[11px] font-mono select-all">brave://settings/privacy</code></li>
                          <li>Activa la opción <strong>"Usar los servicios de Google para la mensajería de inserción (push)"</strong></li>
                          <li>Reinicia completamente el navegador</li>
                        </ol>
                      </div>
                    </div>
                  )}
                  {ch === 'whatsapp' && pref.enabled && (
                    <div className="mt-2 space-y-2">
                      <p className="text-xs text-gray-400">Requiere sandbox Twilio activo</p>
                      <input
                        type="text"
                        placeholder="+573001234567"
                        value={pref.target || ''}
                        onChange={(e) => {
                          const newChannels = channels.map(c => 
                            c.channel === ch ? { ...c, target: e.target.value } : c
                          );
                          onChange(newChannels);
                        }}
                        className="w-full px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-1 focus:ring-green-500 focus:border-green-500"
                      />
                    </div>
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
      <div className="p-5 border-b border-gray-100 flex items-center justify-between bg-gray-50/50">
        <div>
          <h3 className="text-base font-semibold text-gray-800">Resumen Diario</h3>
          <p className="text-sm text-gray-500 mt-1">Recibe un reporte automático con el estado de tu sistema cada día</p>
        </div>
        <Toggle enabled={config.enabled} onChange={() => update({ enabled: !config.enabled })} />
      </div>
      
      {config.enabled && (
        <div className="p-5 space-y-6">
          
          {/* Time Config */}
          <div className="bg-gray-50 rounded-lg p-4 border border-gray-100">
            <h4 className="text-sm font-medium text-gray-700 mb-3">Horario de entrega</h4>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 bg-white px-3 py-2 rounded-lg border border-gray-200 shadow-sm">
                <input
                  type="number" min={0} max={23} value={config.hour}
                  onChange={e => update({ hour: parseInt(e.target.value) || 0 })}
                  className="w-12 text-center text-base font-medium text-gray-800 focus:outline-none focus:ring-0 bg-transparent p-0 border-none"
                  aria-label="Hora"
                />
                <span className="text-gray-400 font-bold">:</span>
                <input
                  type="number" min={0} max={59} step={5} value={config.minute}
                  onChange={e => update({ minute: parseInt(e.target.value) || 0 })}
                  className="w-12 text-center text-base font-medium text-gray-800 focus:outline-none focus:ring-0 bg-transparent p-0 border-none"
                  aria-label="Minuto"
                />
              </div>
              <span className="text-sm text-gray-500">Hora local (Colombia)</span>
            </div>
          </div>

          {/* Content Config */}
          <div className="bg-gray-50 rounded-lg p-4 border border-gray-100">
            <h4 className="text-sm font-medium text-gray-700 mb-3">Contenido del resumen</h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {([
                ['includeSensorAverages', '📊 Promedios de sensores'],
                ['includeActuatorRuntime', '⚡ Tiempo de uso de actuadores'],
                ['includeFuzzyRules', '🧠 Decisiones del sistema inteligente'],
                ['includeWeatherForecast', '🌤️ Pronóstico meteorológico'],
              ] as const).map(([key, label]) => (
                <label key={key} className="flex items-start gap-3 cursor-pointer group hover:bg-white p-2 rounded-md transition-colors -ml-2">
                  <div className="flex items-center h-5 mt-0.5">
                    <input
                      type="checkbox"
                      checked={config[key as keyof DailySummaryConfig] as boolean}
                      onChange={() => update({ [key]: !config[key as keyof DailySummaryConfig] })}
                      className="w-4 h-4 rounded border-gray-300 text-green-600 focus:ring-green-500 transition-shadow"
                    />
                  </div>
                  <span className="text-sm text-gray-700 group-hover:text-gray-900 select-none">{label}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Channel Config */}
          <div className="bg-gray-50 rounded-lg p-4 border border-gray-100">
            <h4 className="text-sm font-medium text-gray-700 mb-3">Canales de entrega</h4>
            <div className="flex flex-wrap gap-3">
              {ALL_CHANNELS.map(ch => {
                const isSelected = config.channels.includes(ch);
                return (
                  <label 
                    key={ch} 
                    className={`flex items-center gap-2 cursor-pointer px-3 py-2 rounded-lg border transition-all ${
                      isSelected 
                        ? 'bg-green-50 border-green-200 text-green-800 shadow-sm' 
                        : 'bg-white border-gray-200 text-gray-600 hover:border-green-300 hover:bg-green-50/50'
                    }`}
                  >
                    <input
                      type="checkbox"
                      className="hidden"
                      checked={isSelected}
                      onChange={() => {
                        const channels = isSelected
                          ? config.channels.filter(c => c !== ch)
                          : [...config.channels, ch];
                        update({ channels });
                      }}
                    />
                    <span className="text-lg">{CHANNEL_ICONS[ch]}</span>
                    <span className="text-sm font-medium select-none">{CHANNEL_LABELS[ch]}</span>
                  </label>
                );
              })}
            </div>
            {config.channels.length === 0 && (
              <p className="text-xs text-amber-600 mt-2 flex items-center gap-1">
                <span>⚠️</span> Debes seleccionar al menos un canal para recibir el resumen.
              </p>
            )}
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

  const extractEmojiAndText = (entry: NotificationLogEntry) => {
    const rawTitle = entry.title || entry.templateKey || 'Notificación';
    // Match one or more emojis at the start of the string, followed by optional spaces
    const emojiMatch = rawTitle.match(/^([\p{Emoji_Presentation}\p{Emoji}\uFE0F]+)\s*/u);
    if (emojiMatch) {
      return {
        icon: emojiMatch[1],
        titleText: rawTitle.slice(emojiMatch[0].length).trim()
      };
    }
    return {
      icon: CHANNEL_ICONS[entry.channel as NotificationChannel] || '📨',
      titleText: rawTitle
    };
  };

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col">
      <div className="p-4 border-b border-gray-100 shrink-0">
        <h3 className="font-semibold text-gray-800">Historial de Envíos</h3>
      </div>
      {loading && !history.length ? (
        <div className="p-6 text-center shrink-0">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-green-600 mx-auto" />
        </div>
      ) : !history.length ? (
        <div className="p-6 text-center shrink-0">
          <span className="text-3xl block mb-2">📭</span>
          <p className="text-sm text-gray-500">No hay notificaciones enviadas</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100 max-h-[120vh] overflow-y-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
          {history.map(entry => {
            const { icon, titleText } = extractEmojiAndText(entry);
            return (
              <div key={entry.id} className="p-4 flex items-center gap-4 hover:bg-gray-50/50 transition-colors">
                <span className="text-2xl shrink-0">{icon}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm md:text-base font-medium text-gray-800 truncate">{titleText}</p>
                  <p className="text-xs md:text-sm text-gray-400 mt-0.5">
                    {entry.sentAt
                      ? new Date(entry.sentAt).toLocaleString('es-CO', { timeZone: 'America/Bogota' })
                      : 'Pendiente'}
                  </p>
                </div>
                <span className={`text-xs md:text-sm px-2.5 py-1 rounded-full shrink-0 ${getStatusStyle(entry.status)}`}>
                  {entry.status === 'sent' ? 'Enviado' : entry.status === 'failed' ? 'Fallido' : entry.status}
                </span>
              </div>
            );
          })}
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
      alert('Tu navegador no soporta notificaciones push o está en modo incógnito/privado.');
      return;
    }

    // Helper to convert VAPID key
    const urlBase64ToUint8Array = (base64String: string) => {
      try {
        const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
          outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
      } catch (e) {
        console.error('[WebPush] Error parsing VAPID key string:', e);
        throw new Error('La clave VAPID tiene un formato inválido.');
      }
    };

    try {
      const vapidKey = process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY?.trim();
      
      if (!vapidKey) {
        console.error('[WebPush] NEXT_PUBLIC_VAPID_PUBLIC_KEY is empty or undefined');
        alert('Error de configuración: No se encontró la clave pública VAPID.');
        return;
      }

      console.log(`[WebPush] Key detected. Length: ${vapidKey.length} chars. Content: ${vapidKey.substring(0, 10)}...`);

      const reg = await navigator.serviceWorker.ready;
      
      // Check for existing subscription first
      const existingSub = await reg.pushManager.getSubscription();
      if (existingSub) {
        console.log('[WebPush] Existing subscription found, unsubscribing first to refresh...');
        await existingSub.unsubscribe();
      }

      const subscription = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidKey),
      });

      console.log('[WebPush] Subscription successful:', subscription.endpoint);

      const p256dh = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('p256dh')!)));
      const auth = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('auth')!)));

      await useNotificationStore.getState().registerPush({
        userId: user.id,
        platform: 'web_push',
        token: JSON.stringify({ endpoint: subscription.endpoint, keys: { p256dh, auth } }),
        deviceName: navigator.userAgent.includes('Chrome') ? 'Chrome' : navigator.userAgent.includes('Firefox') ? 'Firefox' : 'Navegador',
      });
      
      // Auto-enable web_push channel in UI
      setLocalChannels(prev => {
        const hasWebPush = prev.some(c => c.channel === 'web_push');
        if (hasWebPush) {
          return prev.map(c => c.channel === 'web_push' ? { ...c, enabled: true } : c);
        }
        return [...prev, { channel: 'web_push', enabled: true }];
      });
      markDirty();
      
      alert('¡Notificaciones push habilitadas con éxito en este navegador! Recuerda guardar tus cambios.');
      fetchDevices(user.id);
    } catch (err: any) {
      console.error('[WebPush] Detailed registration error:', err);
      
      if (err.name === 'AbortError') {
        alert('Error: El servicio de push del navegador abortó la petición. Esto suele deberse a una clave VAPID inválida o problemas de red con los servidores de Google/Mozilla.');
      } else if (err.name === 'NotAllowedError') {
        alert('Permiso denegado: Has bloqueado las notificaciones en este sitio.');
      } else {
        alert(`Error al registrar notificaciones: ${err.message || 'Error desconocido'}`);
      }
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
