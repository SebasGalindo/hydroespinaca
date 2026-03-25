import React from 'react';
import type { PushSubscriptionInfo } from '@hydroespinaca/shared';

interface DeviceListSectionProps {
  devices: PushSubscriptionInfo[];
  loading: boolean;
  onUnregister: (id: string) => void;
}

const DeviceListSection = React.memo(function DeviceListSection({
  devices,
  loading,
  onUnregister,
}: DeviceListSectionProps) {
  const activeDevices = devices.filter((d) => d.isActive);

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
          {activeDevices.map((device) => (
            <div key={device.id} className="p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="text-xl">
                  {device.platform === 'expo' ? '📱' : device.platform === 'web_push' ? '🖥️' : '📲'}
                </span>
                <div>
                  <p className="text-sm font-medium text-gray-700">{device.deviceName || 'Dispositivo'}</p>
                  <p className="text-xs text-gray-400">
                    {device.platform} ·{' '}
                    {device.createdAt
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
});

export default DeviceListSection;
