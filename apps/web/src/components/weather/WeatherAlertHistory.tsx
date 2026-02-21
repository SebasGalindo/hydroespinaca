'use client';

import React, { useEffect, useState } from 'react';
import { useAuthStore, useWeatherStore } from '@hydroespinaca/shared';
import type { WeatherAlert, AlertType, AlertSeverity, AlertFilterParams } from '@hydroespinaca/shared';
import { ALERT_TYPE_LABELS, ALERT_TYPE_ICONS, ALERT_SEVERITY_COLORS } from '@hydroespinaca/shared';

interface WeatherAlertHistoryProps {
  fuzzySystemId?: string;
}

const SEVERITY_STYLES: Record<AlertSeverity, string> = {
  info: 'bg-blue-100 text-blue-800 border-blue-200',
  warning: 'bg-amber-100 text-amber-800 border-amber-200',
  critical: 'bg-red-100 text-red-800 border-red-200',
};

const WeatherAlertHistory: React.FC<WeatherAlertHistoryProps> = ({ fuzzySystemId }) => {
  const user = useAuthStore(s => s.user);
  const { alerts, alertsLoading, alertsError, fetchAlerts, markAlertRead } = useWeatherStore();
  const [filterType, setFilterType] = useState<string>('');

  useEffect(() => {
    if (user?.id) {
      const params: AlertFilterParams = { userId: user.id };
      if (fuzzySystemId) params.fuzzySystemId = fuzzySystemId;
      fetchAlerts(params);
    }
  }, [user?.id, fuzzySystemId, fetchAlerts]);

  const formatDate = (iso: string) => {
    return new Date(iso).toLocaleString('es-CO', {
      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
      hour12: true, timeZone: 'America/Bogota',
    });
  };

  const isUnread = (alert: WeatherAlert) => {
    if (!user?.id) return true;
    const nu = alert.notifiedUsers.find(u => u.userId === user.id);
    return !nu?.isRead;
  };

  const handleMarkRead = async (alertId: string) => {
    if (!user?.id) return;
    await markAlertRead(alertId, user.id);
  };

  const filteredAlerts = filterType
    ? alerts.filter((a: WeatherAlert) => a.alertType === filterType)
    : alerts;

  if (alertsError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-xl p-6">
        <p className="text-sm text-red-700">{alertsError}</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="p-4 border-b border-gray-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h3 className="font-semibold text-gray-800">Historial de Alertas</h3>
        <select
          value={filterType}
          onChange={e => setFilterType(e.target.value)}
          className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500"
        >
          <option value="">Todos los tipos</option>
          {Object.entries(ALERT_TYPE_LABELS).map(([val, label]) => (
            <option key={val} value={val}>{label}</option>
          ))}
        </select>
      </div>

      {alertsLoading && !alerts.length ? (
        <div className="p-8 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto mb-2" />
          <p className="text-sm text-gray-500">Cargando alertas...</p>
        </div>
      ) : !filteredAlerts.length ? (
        <div className="p-8 text-center">
          <span className="text-4xl block mb-2">✅</span>
          <p className="text-sm text-gray-500">No hay alertas registradas.</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100 max-h-[500px] overflow-y-auto">
          {filteredAlerts.map((alert: WeatherAlert) => {
            const unread = isUnread(alert);
            const severity = (alert.severity || 'warning') as AlertSeverity;
            return (
              <div
                key={alert.id}
                className={`p-4 flex items-start gap-3 transition-colors cursor-pointer hover:bg-gray-50 ${
                  unread ? 'bg-amber-50/40' : ''
                }`}
                onClick={() => unread && handleMarkRead(alert.id)}
              >
                <span className="text-2xl flex-shrink-0">
                  {ALERT_TYPE_ICONS[alert.alertType as AlertType] || '⚠️'}
                </span>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-semibold text-gray-800">{alert.title}</span>
                    <span className={`text-xs px-2 py-0.5 rounded-full border ${SEVERITY_STYLES[severity]}`}>
                      {severity === 'critical' ? 'Crítica' : severity === 'warning' ? 'Advertencia' : 'Info'}
                    </span>
                    {unread && (
                      <span className="w-2 h-2 bg-green-500 rounded-full flex-shrink-0" />
                    )}
                  </div>
                  <p className="text-xs text-gray-600 mt-1 line-clamp-2">{alert.message}</p>
                  {alert.recommendation && (
                    <p className="text-xs text-blue-600 mt-1">💡 {alert.recommendation}</p>
                  )}
                  <div className="flex items-center gap-3 mt-2 text-xs text-gray-400">
                    <span>📅 Pronóstico: {formatDate(alert.forecastDatetime)}</span>
                    <span>🕐 Creada: {formatDate(alert.createdAt)}</span>
                    {alert.forecastValue != null && (
                      <span>📊 Valor: {alert.forecastValue.toFixed(1)}</span>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default WeatherAlertHistory;
