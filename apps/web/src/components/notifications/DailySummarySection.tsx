import React from 'react';
import type { NotificationChannel, DailySummaryConfig } from '@hydroespinaca/shared';
import { CHANNEL_LABELS, CHANNEL_ICONS } from '@hydroespinaca/shared';
import Toggle from '@/components/ui/Toggle';

const ALL_CHANNELS: NotificationChannel[] = ['email', 'push', 'web_push', 'whatsapp'];

const SUMMARY_CONTENT_OPTIONS = [
  ['includeSensorAverages', '📊 Promedios de sensores'],
  ['includeActuatorRuntime', '⚡ Tiempo de uso de actuadores'],
  ['includeFuzzyRules', '🧠 Decisiones del sistema inteligente'],
  ['includeWeatherForecast', '🌤️ Pronóstico meteorológico'],
] as const;

interface DailySummarySectionProps {
  config: DailySummaryConfig;
  onChange: (config: DailySummaryConfig) => void;
}

const DailySummarySection = React.memo(function DailySummarySection({
  config,
  onChange,
}: DailySummarySectionProps) {
  const update = (partial: Partial<DailySummaryConfig>) => onChange({ ...config, ...partial });

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-5 border-b border-gray-100 flex items-center justify-between bg-gray-50/50">
        <div>
          <h3 className="text-base font-semibold text-gray-800">Resumen Diario</h3>
          <p className="text-sm text-gray-500 mt-1">
            Recibe un reporte automático con el estado de tu sistema cada día
          </p>
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
                  type="number"
                  min={0}
                  max={23}
                  value={config.hour}
                  onChange={(e) => update({ hour: parseInt(e.target.value) || 0 })}
                  className="w-12 text-center text-base font-medium text-gray-800 focus:outline-none focus:ring-0 bg-transparent p-0 border-none"
                  aria-label="Hora"
                />
                <span className="text-gray-400 font-bold">:</span>
                <input
                  type="number"
                  min={0}
                  max={59}
                  step={5}
                  value={config.minute}
                  onChange={(e) => update({ minute: parseInt(e.target.value) || 0 })}
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
              {SUMMARY_CONTENT_OPTIONS.map(([key, label]) => (
                <label
                  key={key}
                  className="flex items-start gap-3 cursor-pointer group hover:bg-white p-2 rounded-md transition-colors -ml-2"
                >
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
              {ALL_CHANNELS.map((ch) => {
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
                          ? config.channels.filter((c) => c !== ch)
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
});

export default DailySummarySection;
