import React from 'react';
import type { NotificationChannel, ChannelPreference } from '@hydroespinaca/shared';
import { CHANNEL_LABELS, CHANNEL_ICONS } from '@hydroespinaca/shared';
import Toggle from '@/components/ui/Toggle';

const ALL_CHANNELS: NotificationChannel[] = ['email', 'push', 'web_push', 'whatsapp'];

interface ChannelPreferencesSectionProps {
  channels: ChannelPreference[];
  onChange: (channels: ChannelPreference[]) => void;
  onWebPushRegister: () => void;
}

const ChannelPreferencesSection = React.memo(function ChannelPreferencesSection({
  channels,
  onChange,
  onWebPushRegister,
}: ChannelPreferencesSectionProps) {
  const getChannel = (ch: NotificationChannel): ChannelPreference =>
    channels.find((c) => c.channel === ch) || { channel: ch, enabled: false };

  const toggle = (ch: NotificationChannel) => {
    const existing = channels.find((c) => c.channel === ch);
    if (existing) {
      onChange(channels.map((c) => (c.channel === ch ? { ...c, enabled: !c.enabled } : c)));
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
        {ALL_CHANNELS.map((ch) => {
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
                      <div className="p-3 mt-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800 leading-relaxed shadow-sm">
                        <p className="font-semibold mb-1 text-amber-900 flex items-center gap-1">
                          <span className="text-base">⚠️</span> Nota para usuarios de Brave:
                        </p>
                        <p className="mb-2">Por defecto, Brave bloquea las notificaciones push. Para habilitarlas:</p>
                        <ol className="list-decimal ml-5 space-y-1 text-amber-900/90">
                          <li>
                            Ve a la URL{' '}
                            <code className="bg-amber-100/80 px-1.5 py-0.5 rounded text-[11px] font-mono select-all">
                              brave://settings/privacy
                            </code>
                          </li>
                          <li>
                            Activa la opción{' '}
                            <strong>&quot;Usar los servicios de Google para la mensajería de inserción (push)&quot;</strong>
                          </li>
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
                          const newChannels = channels.map((c) =>
                            c.channel === ch ? { ...c, target: e.target.value } : c,
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
});

export default ChannelPreferencesSection;
