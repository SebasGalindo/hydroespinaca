'use client';

import React from 'react';
import { 
  ExclamationTriangleIcon, 
  CheckCircleIcon, 
  XCircleIcon, 
  ClockIcon, 
  FunnelIcon, 
  XMarkIcon,
  InformationCircleIcon,
  BellIcon 
} from '@heroicons/react/24/outline';
import { useAlertStore } from '@hidroespinaca/shared';

// Re-export type for backward compatibility
export type { Alert } from '@hidroespinaca/shared';

const AlertsPanel: React.FC = () => {
  const {
    alerts,
    filter,
    filteredAlerts,
    unreadCount,
    setFilter,
    markAsRead,
    removeAlert,
    clearAllAlerts
  } = useAlertStore();

  const getAlertIcon = (type: string) => {
    const iconProps = { className: "w-5 h-5" };
    
    switch (type) {
      case 'warning':
        return <ExclamationTriangleIcon {...iconProps} className="w-5 h-5 text-yellow-500" />;
      case 'error':
        return <XCircleIcon {...iconProps} className="w-5 h-5 text-red-500" />;
      case 'info':
        return <InformationCircleIcon {...iconProps} className="w-5 h-5 text-blue-500" />;
      case 'success':
        return <CheckCircleIcon {...iconProps} className="w-5 h-5 text-green-500" />;
      default:
        return <InformationCircleIcon {...iconProps} className="w-5 h-5 text-gray-500" />;
    }
  };

  const getAlertColor = (type: string) => {
    switch (type) {
      case 'warning':
        return 'border-l-yellow-400 bg-yellow-50';
      case 'error':
        return 'border-l-red-400 bg-red-50';
      case 'info':
        return 'border-l-blue-400 bg-blue-50';
      case 'success':
        return 'border-l-green-400 bg-green-50';
      default:
        return 'border-l-gray-400 bg-gray-50';
    }
  };

  // Functions are now managed by the store

  return (
    <article className="hidro-card" role="region" aria-labelledby="alerts-title">
      <header className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <BellIcon className="w-5 h-5 text-gray-600" />
          <h2 id="alerts-title" className="text-lg font-semibold text-gray-900 font-inter">
            Alertas
          </h2>
          {unreadCount > 0 && (
            <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800 font-inter">
              {unreadCount} nuevas
            </span>
          )}
        </div>
      </header>

      {/* Filtros */}
      <nav className="flex gap-2 mb-4" aria-label="Filtros de alertas">
        {[
          { key: 'all', label: 'Todas' },
          { key: 'unread', label: 'No leídas' },
          { key: 'warning', label: 'Advertencias' },
          { key: 'error', label: 'Errores' }
        ].map((filterOption) => (
          <button
            key={filterOption.key}
            onClick={() => setFilter(filterOption.key as any)}
            className={`px-3 py-1 text-xs font-medium rounded-full transition-colors font-inter ${
              filter === filterOption.key
                ? 'bg-green-100 text-green-700 border border-green-200'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
            aria-pressed={filter === filterOption.key}
          >
            {filterOption.label}
          </button>
        ))}
      </nav>

      {/* Lista de alertas */}
      <section className="space-y-3 max-h-96 overflow-y-auto" aria-live="polite">
        {filteredAlerts.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <BellIcon className="w-8 h-8 mx-auto mb-2 text-gray-300" />
            <p className="text-sm font-inter">
              {filter === 'all' ? 'No hay alertas' : `No hay alertas de tipo "${filter}"`}
            </p>
          </div>
        ) : (
          filteredAlerts.map((alert) => (
            <div
              key={alert.id}
              className={`border-l-4 p-3 rounded-r-lg ${getAlertColor(alert.type)} ${
                !alert.isRead ? 'ring-1 ring-gray-200' : ''
              }`}
              role="alert"
              aria-labelledby={`alert-${alert.id}-title`}
            >
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-3 flex-1">
                  {getAlertIcon(alert.type)}
                  <div className="flex-1 min-w-0">
                    <h3 
                      id={`alert-${alert.id}-title`}
                      className={`text-sm font-medium font-inter ${
                        !alert.isRead ? 'text-gray-900' : 'text-gray-700'
                      }`}
                    >
                      {alert.title}
                      {!alert.isRead && (
                        <span className="ml-2 w-2 h-2 bg-blue-500 rounded-full inline-block"></span>
                      )}
                    </h3>
                    <p className="text-sm text-gray-600 mt-1 font-inter">
                      {alert.message}
                    </p>
                    <div className="flex items-center gap-4 mt-2 text-xs text-gray-500">
                      <time className="font-inter">{alert.timestamp}</time>
                      {alert.sensor && (
                        <span className="font-inter">Sensor: {alert.sensor}</span>
                      )}
                    </div>
                  </div>
                </div>
                
                <div className="flex items-center gap-1 ml-2">
                  {!alert.isRead && (
                    <button
                      onClick={() => markAsRead(alert.id)}
                      className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
                      aria-label="Marcar como leída"
                      title="Marcar como leída"
                    >
                      <CheckCircleIcon className="w-4 h-4" />
                    </button>
                  )}
                  <button
                    onClick={() => removeAlert(alert.id)}
                    className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
                    aria-label="Descartar alerta"
                    title="Descartar"
                  >
                    <XMarkIcon className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </section>

      {/* Acciones */}
      {alerts.length > 0 && (
        <footer className="mt-4 pt-4 border-t border-gray-200">
          <div className="flex justify-between items-center">
            <button
              onClick={() => {
                alerts.forEach(alert => {
                  if (!alert.isRead) markAsRead(alert.id);
                });
              }}
              className="text-sm text-green-600 hover:text-green-700 font-medium font-inter"
              disabled={unreadCount === 0}
            >
              Marcar todas como leídas
            </button>
            <button
              onClick={clearAllAlerts}
              className="text-sm text-gray-600 hover:text-gray-700 font-medium font-inter"
            >
              Limpiar todas
            </button>
          </div>
        </footer>
      )}
    </article>
  );
};

export default AlertsPanel;