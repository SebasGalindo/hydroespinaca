'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { useWeatherStore, useAuthStore } from '@hydroespinaca/shared';
import type { WeatherAlert } from '@hydroespinaca/shared';
import { BellIcon } from '@/components/ui/icons/Icons';

const SEVERITY_STYLES: Record<string, string> = {
  critical: 'bg-red-100 text-red-800 border-red-300',
  high: 'bg-orange-100 text-orange-800 border-orange-300',
  medium: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  low: 'bg-blue-100 text-blue-800 border-blue-300',
  info: 'bg-gray-100 text-gray-700 border-gray-300',
};

const SEVERITY_DOT: Record<string, string> = {
  critical: 'bg-red-500',
  high: 'bg-orange-500',
  medium: 'bg-yellow-500',
  low: 'bg-blue-500',
  info: 'bg-gray-400',
};

export default function WeatherAlertWidget() {
  const user = useAuthStore((s) => s.user);
  const { alerts, unreadAlertCount, alertsLoading, fetchAlerts } = useWeatherStore();

  useEffect(() => {
    if (user?.id) {
      fetchAlerts({ userId: user.id, unreadOnly: true, pageSize: 5 });
    }
  }, [user?.id, fetchAlerts]);

  const recentAlerts = (alerts ?? []).filter((a: WeatherAlert) => {
    if (!user?.id) return true;
    const nu = a.notifiedUsers.find((u) => u.userId === user.id);
    return !nu?.isRead;
  }).slice(0, 3);

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
        <div className="flex items-center gap-2">
          <BellIcon size={18} className="text-green-700" />
          <h3 className="text-sm font-semibold text-gray-800">Alertas Meteorológicas</h3>
        </div>
        {unreadAlertCount > 0 && (
          <span className="inline-flex items-center justify-center min-w-[22px] h-[22px] px-1.5 text-xs font-bold text-white bg-red-500 rounded-full">
            {unreadAlertCount > 99 ? '99+' : unreadAlertCount}
          </span>
        )}
      </div>

      {/* Content */}
      <div className="px-5 py-3">
        {alertsLoading ? (
          <div className="flex items-center justify-center py-6">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-green-600" />
          </div>
        ) : recentAlerts.length === 0 ? (
          <div className="text-center py-6">
            <p className="text-sm text-gray-500">Sin alertas pendientes</p>
            <p className="text-xs text-gray-400 mt-1">Todo está en orden ✓</p>
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {recentAlerts.map((alert: WeatherAlert) => (
              <li key={alert.id} className="py-2.5 flex items-start gap-2.5">
                <span className={`mt-1.5 flex-shrink-0 w-2 h-2 rounded-full ${SEVERITY_DOT[alert.severity] ?? SEVERITY_DOT.info}`} />
                <div className="min-w-0 flex-1">
                  <p className="text-sm text-gray-800 font-medium truncate">
                    {alert.message}
                  </p>
                  <div className="flex items-center gap-2 mt-0.5">
                    <span className={`inline-block text-[10px] font-semibold px-1.5 py-0.5 rounded border ${SEVERITY_STYLES[alert.severity] ?? SEVERITY_STYLES.info}`}>
                      {alert.severity.toUpperCase()}
                    </span>
                    <span className="text-[10px] text-gray-400">
                      {new Date(alert.createdAt).toLocaleDateString('es-CO', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Footer */}
      <div className="px-5 py-3 border-t border-gray-100 bg-gray-50/50">
        <Link
          href="/clima"
          className="text-xs font-medium text-green-700 hover:text-green-800 hover:underline transition-colors"
        >
          Ver pronóstico y todas las alertas →
        </Link>
      </div>
    </div>
  );
}
