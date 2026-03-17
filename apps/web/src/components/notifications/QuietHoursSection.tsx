import React from 'react';
import type { QuietHoursConfig } from '@hydroespinaca/shared';
import Toggle from '@/components/ui/Toggle';

interface QuietHoursSectionProps {
  config: QuietHoursConfig;
  onChange: (config: QuietHoursConfig) => void;
}

const QuietHoursSection = React.memo(function QuietHoursSection({
  config,
  onChange,
}: QuietHoursSectionProps) {
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
            type="number"
            min={0}
            max={23}
            value={config.startHour}
            onChange={(e) => update({ startHour: parseInt(e.target.value) || 0 })}
            className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
          />
          <label className="text-sm text-gray-600">a</label>
          <input
            type="number"
            min={0}
            max={23}
            value={config.endHour}
            onChange={(e) => update({ endHour: parseInt(e.target.value) || 0 })}
            className="w-16 px-2 py-1.5 text-sm text-center border border-gray-300 rounded-lg"
          />
          <span className="text-xs text-gray-400">(hora Colombia)</span>
        </div>
      )}
    </div>
  );
});

export default QuietHoursSection;
