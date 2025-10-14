'use client';

import React from 'react';
import { 
  CheckCircleIcon, 
  ExclamationTriangleIcon, 
  XCircleIcon,
  WifiIcon,
  BatteryIcon,
  CpuChipIcon
} from '@/components/ui/icons/Icons';
import { useSensorStore } from '@hidroespinaca/shared';

// Re-export type for backward compatibility
export type { SystemComponent } from '@hidroespinaca/shared';

const SystemStatusCard: React.FC = () => {
  const { systemComponents } = useSensorStore();

  const getStatusIcon = (status: string) => {
    const iconProps = { className: "w-5 h-5" };
    
    switch (status) {
      case 'online':
        return <CheckCircleIcon {...iconProps} className="w-5 h-5 text-green-500" />;
      case 'warning':
        return <ExclamationTriangleIcon {...iconProps} className="w-5 h-5 text-yellow-500" />;
      case 'offline':
        return <XCircleIcon {...iconProps} className="w-5 h-5 text-red-500" />;
      default:
        return <XCircleIcon {...iconProps} className="w-5 h-5 text-gray-400" />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'online':
        return 'En línea';
      case 'warning':
        return 'Advertencia';
      case 'offline':
        return 'Desconectado';
      default:
        return 'Desconocido';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'online':
        return 'text-green-700 bg-green-50';
      case 'warning':
        return 'text-yellow-700 bg-yellow-50';
      case 'offline':
        return 'text-red-700 bg-red-50';
      default:
        return 'text-gray-700 bg-gray-50';
    }
  };

  const onlineCount = systemComponents.filter(c => c.status === 'online').length;
  const warningCount = systemComponents.filter(c => c.status === 'warning').length;
  const offlineCount = systemComponents.filter(c => c.status === 'offline').length;

  return (
    <article className="hidro-card" role="region" aria-labelledby="system-status-title">
      <header className="flex items-center justify-between mb-6">
        <div>
          <h2 id="system-status-title" className="text-lg font-semibold text-gray-900 font-inter">
            Estado del Sistema
          </h2>
          <p className="text-sm text-gray-600 font-inter">
            Monitoreo en tiempo real de componentes
          </p>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <CpuChipIcon className="w-5 h-5 text-blue-500" />
            <span className="text-sm font-medium text-gray-700 font-inter">
              {systemComponents.length} Componentes
            </span>
          </div>
        </div>
      </header>

      {/* Resumen de estado */}
      <section className="grid grid-cols-3 gap-4 mb-6" aria-labelledby="status-summary">
        <h3 id="status-summary" className="sr-only">Resumen de estado de componentes</h3>
        
        <div className="text-center p-3 bg-green-50 rounded-lg border border-green-200">
          <div className="text-2xl font-bold text-green-700 font-inter">{onlineCount}</div>
          <div className="text-sm text-green-600 font-inter">En línea</div>
        </div>
        
        <div className="text-center p-3 bg-yellow-50 rounded-lg border border-yellow-200">
          <div className="text-2xl font-bold text-yellow-700 font-inter">{warningCount}</div>
          <div className="text-sm text-yellow-600 font-inter">Advertencias</div>
        </div>
        
        <div className="text-center p-3 bg-red-50 rounded-lg border border-red-200">
          <div className="text-2xl font-bold text-red-700 font-inter">{offlineCount}</div>
          <div className="text-sm text-red-600 font-inter">Desconectados</div>
        </div>
      </section>

      {/* Lista de componentes */}
      <section aria-labelledby="components-list">
        <h3 id="components-list" className="text-sm font-medium text-gray-700 mb-3 font-inter">
          Componentes del Sistema
        </h3>
        
        <div className="space-y-3">
          {systemComponents.map((component, index) => (
            <div 
              key={index}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200"
            >
              <div className="flex items-center gap-3">
                {getStatusIcon(component.status)}
                <div>
                  <h4 className="text-sm font-medium text-gray-900 font-inter">
                    {component.name}
                  </h4>
                  {component.details && (
                    <p className="text-xs text-gray-600 font-inter">
                      {component.details}
                    </p>
                  )}
                </div>
              </div>
              
              <div className="text-right">
                <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(component.status)} font-inter`}>
                  {getStatusText(component.status)}
                </span>
                <p className="text-xs text-gray-500 mt-1 font-inter">
                  {component.lastUpdate}
                </p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Información adicional */}
      <footer className="mt-6 pt-4 border-t border-gray-200">
        <div className="flex items-center justify-between text-sm text-gray-600">
          <div className="flex items-center gap-2">
            <WifiIcon className="w-4 h-4" />
            <span className="font-inter">Conectividad estable</span>
          </div>
          <div className="flex items-center gap-2">
            <BatteryIcon className="w-4 h-4" />
            <span className="font-inter">Energía: Normal</span>
          </div>
        </div>
      </footer>
    </article>
  );
};

export default SystemStatusCard;