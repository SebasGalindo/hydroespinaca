'use client';

import React, { useState, useEffect } from 'react';
import { useAuthStore, useWeatherStore } from '@hydroespinaca/shared';
import type { AlertThreshold, UpdateAlertConfigRequest, AlertType } from '@hydroespinaca/shared';
import { ALERT_TYPE_LABELS, ALERT_TYPE_ICONS } from '@hydroespinaca/shared';

interface AlertConfigSectionProps {
  fuzzySystemId: string;
  fuzzySystemName: string;
}

const AlertConfigSection: React.FC<AlertConfigSectionProps> = ({ fuzzySystemId, fuzzySystemName }) => {
  const user = useAuthStore(s => s.user);
  const {
    alertConfig, alertConfigLoading, alertConfigError,
    fetchAlertConfig, updateAlertConfig, seedAlertConfig,
  } = useWeatherStore();

  const [editingAlerts, setEditingAlerts] = useState<AlertThreshold[]>([]);
  const [isDirty, setIsDirty] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (fuzzySystemId) {
      fetchAlertConfig(fuzzySystemId);
    }
  }, [fuzzySystemId, fetchAlertConfig]);

  useEffect(() => {
    if (alertConfig?.alerts) {
      setEditingAlerts(alertConfig.alerts);
      setIsDirty(false);
    }
  }, [alertConfig]);

  const handleSeed = async () => {
    if (!user?.id) return;
    try {
      await seedAlertConfig(fuzzySystemId, { fuzzySystemName, userId: user.id });
    } catch {
      // Error handled by store
    }
  };

  const handleToggle = (idx: number) => {
    const updated = [...editingAlerts];
    const current = updated[idx]!;
    updated[idx] = { ...current, enabled: !current.enabled };
    setEditingAlerts(updated);
    setIsDirty(true);
  };

  const handleThresholdChange = (idx: number, value: string) => {
    const updated = [...editingAlerts];
    const current = updated[idx]!;
    updated[idx] = { ...current, thresholdValue: value === '' ? null : parseFloat(value) };
    setEditingAlerts(updated);
    setIsDirty(true);
  };

  const handleSave = async () => {
    if (!user?.id || !alertConfig) return;
    setSaving(true);
    try {
      const req: UpdateAlertConfigRequest = {
        userId: user.id,
        isActive: alertConfig.isActive,
        alerts: editingAlerts,
      };
      await updateAlertConfig(fuzzySystemId, req);
      setIsDirty(false);
    } catch {
      // Error handled by store
    } finally {
      setSaving(false);
    }
  };

  if (alertConfigLoading && !alertConfig) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 p-6 animate-pulse">
        <div className="h-6 bg-gray-200 rounded w-48 mb-4" />
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-12 bg-gray-100 rounded" />
          ))}
        </div>
      </div>
    );
  }

  if (alertConfigError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-xl p-6">
        <p className="text-sm text-red-700 mb-3">{alertConfigError}</p>
        <button
          onClick={() => fetchAlertConfig(fuzzySystemId)}
          className="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700"
        >
          Reintentar
        </button>
      </div>
    );
  }

  if (!alertConfig) {
    return (
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-6 text-center">
        <p className="text-sm text-amber-700 mb-3">
          No hay configuración de alertas para este sistema fuzzy.
        </p>
        <button
          onClick={handleSeed}
          className="px-4 py-2 text-sm bg-amber-600 text-white rounded-lg hover:bg-amber-700"
          disabled={alertConfigLoading}
        >
          {alertConfigLoading ? 'Creando...' : 'Crear Configuración por Defecto'}
        </button>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100 flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-gray-800">Umbrales de Alerta</h3>
          <p className="text-xs text-gray-500 mt-0.5">Sistema: {alertConfig.fuzzySystemName}</p>
        </div>
        {isDirty && (
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Guardando...' : '💾 Guardar Cambios'}
          </button>
        )}
      </div>

      <div className="divide-y divide-gray-100">
        {editingAlerts.map((alert, idx) => (
          <div
            key={alert.type}
            className={`p-4 flex flex-col sm:flex-row sm:items-center gap-3 transition-colors ${
              alert.enabled ? 'bg-white' : 'bg-gray-50'
            }`}
          >
            {/* Toggle + Label */}
            <div className="flex items-center gap-3 min-w-[200px]">
              <button
                onClick={() => handleToggle(idx)}
                className={`relative w-11 h-6 rounded-full transition-colors ${
                  alert.enabled ? 'bg-green-500' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
                    alert.enabled ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
              <span className="text-lg">{ALERT_TYPE_ICONS[alert.type as AlertType] || '⚠️'}</span>
              <span className={`text-sm font-medium ${alert.enabled ? 'text-gray-800' : 'text-gray-400'}`}>
                {ALERT_TYPE_LABELS[alert.type as AlertType] || alert.type}
              </span>
            </div>

            {/* Threshold input or placeholder */}
            {alert.enabled && (
              <div className="flex items-center gap-2 w-32 shrink-0">
                {alert.type !== 'thunderstorm' && alert.type !== 'government' && (
                  <>
                    <span className="text-sm font-bold text-gray-500 whitespace-nowrap px-2">
                      {alert.comparison === 'gt' ? '≥' : alert.comparison === 'lt' ? '≤' : ''}
                    </span>
                    <input
                      type="number"
                      value={alert.thresholdValue ?? ''}
                      onChange={e => handleThresholdChange(idx, e.target.value)}
                      className="w-24 px-2 py-1.5 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                      placeholder="Valor"
                      step="0.1"
                    />
                  </>
                )}
              </div>
            )}

            {/* Recommendation (Read-only) */}
            {alert.enabled && alert.recommendation && (
              <div className="flex-1 text-xs text-gray-500 italic mt-2 sm:mt-0 sm:ml-4 flex items-center">
                💡 {alert.recommendation}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default AlertConfigSection;
