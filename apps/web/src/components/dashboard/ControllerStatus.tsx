'use client';

import React from 'react';
import { CheckCircleIcon, ClockIcon, XCircleIcon } from '@/components/ui/icons/Icons';
import type { JobStatus, Stats, InternalRoutine } from '@hydroespinaca/shared/types';

interface ControllerStatusProps {
  timeSinceUpdate: number;
  jobStatus: JobStatus;
  stats: Stats;
  internalRoutines: InternalRoutine[];
}

export default function ControllerStatus({
  timeSinceUpdate,
  jobStatus,
  stats,
  internalRoutines
}: ControllerStatusProps) {

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running':
        return <span className="text-green-600">🟢</span>;
      case 'scheduled':
        return <span className="text-yellow-600">🟡</span>;
      case 'queued':
        return <span className="text-orange-600">🟠</span>;
      case 'failed':
        return <span className="text-red-600">🔴</span>;
      default:
        return null;
    }
  };

  const getStatusLabel = (status: string) => {
    const labels: { [key: string]: string } = {
      running: 'En ejecución',
      scheduled: 'Programado',
      queued: 'En cola',
      failed: 'Fallido'
    };
    return labels[status] || status;
  };

  const formatNextExecution = (isoDate: string) => {
    const date = new Date(isoDate);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 60) return `en ${diffMins}m`;
    if (diffMins < 1440) return `en ${Math.floor(diffMins / 60)}h`;
    return date.toLocaleDateString('es-ES', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  return (
    <section className="space-y-6" aria-labelledby="controller-status-heading">
      <h2 id="controller-status-heading" className="text-xl font-bold text-green-800 font-inter">
        Estado actual del controlador
      </h2>

      {/* Resumen General */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="hidro-card p-4">
          <p className="text-xs text-gray-600 font-inter mb-1">Activos</p>
          <p className="text-2xl font-bold text-green-600 font-inter">{stats.activeCount}</p>
        </div>
        <div className="hidro-card p-4">
          <p className="text-xs text-gray-600 font-inter mb-1">Pendientes</p>
          <p className="text-2xl font-bold text-yellow-600 font-inter">{stats.pendingCount}</p>
        </div>
        <div className="hidro-card p-4">
          <p className="text-xs text-gray-600 font-inter mb-1">Pines Bloqueados</p>
          <p className="text-2xl font-bold text-gray-700 font-inter">{stats.totalLockedPins}</p>
        </div>
        <div className="hidro-card p-4">
          <p className="text-xs text-gray-600 font-inter mb-1">Controladores</p>
          <p className="text-2xl font-bold text-blue-600 font-inter">{stats.esp32Ids.length}</p>
        </div>
      </div>

      {/* Cola de Comandos */}
      <div className="hidro-card p-6">
        <h3 className="text-lg font-semibold text-gray-900 font-inter mb-4">
          Cola de Comandos - ESP32
        </h3>
        {jobStatus.queue.length > 0 ? (
          <div className="space-y-3">
            {jobStatus.queue.map((command, index) => (
              <div
                key={command.commandId}
                className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200"
              >
                <div className="flex items-center gap-3">
                  <div className="text-xl">
                    {getStatusIcon(command.status)}
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900 font-inter">
                      {command.commandId.split('_')[0]?.replace(/-/g, ' ') || command.commandId}
                    </p>
                    <p className="text-xs text-gray-500 font-inter">
                      ID: {command.commandId.split('_')[1] || command.commandId}
                    </p>
                  </div>
                </div>
                <span className={`text-xs font-medium px-3 py-1 rounded-full ${
                  command.status === 'running' ? 'bg-green-100 text-green-700' :
                  command.status === 'scheduled' ? 'bg-yellow-100 text-yellow-700' :
                  command.status === 'queued' ? 'bg-orange-100 text-orange-700' :
                  'bg-red-100 text-red-700'
                }`}>
                  {getStatusLabel(command.status)}
                </span>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500 font-inter">No hay comandos en la cola</p>
        )}
      </div>

      {/* Rutinas Internas */}
      <div className="hidro-card p-6">
        <h3 className="text-lg font-semibold text-gray-900 font-inter mb-4">
          Rutinas Internas
        </h3>
        {internalRoutines.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-3 px-2 text-xs font-semibold text-gray-700 font-inter">
                    Estado
                  </th>
                  <th className="text-left py-3 px-2 text-xs font-semibold text-gray-700 font-inter">
                    Rutina
                  </th>
                  <th className="text-left py-3 px-2 text-xs font-semibold text-gray-700 font-inter hidden md:table-cell">
                    Descripción
                  </th>
                  <th className="text-left py-3 px-2 text-xs font-semibold text-gray-700 font-inter">
                    Intervalo
                  </th>
                  <th className="text-left py-3 px-2 text-xs font-semibold text-gray-700 font-inter">
                    Próxima ejecución
                  </th>
                </tr>
              </thead>
              <tbody>
                {internalRoutines.map((routine, index) => (
                  <tr key={routine.name} className="border-b border-gray-100 hover:bg-gray-50">
                    <td className="py-3 px-2">
                      {routine.isActive ? (
                        <CheckCircleIcon size={18} className="text-green-600" />
                      ) : (
                        <XCircleIcon size={18} className="text-gray-400" />
                      )}
                    </td>
                    <td className="py-3 px-2">
                      <p className="text-sm font-medium text-gray-900 font-inter">
                        {routine.name}
                      </p>
                    </td>
                    <td className="py-3 px-2 hidden md:table-cell">
                      <p className="text-sm text-gray-600 font-inter">
                        {routine.description}
                      </p>
                    </td>
                    <td className="py-3 px-2">
                      <div className="flex items-center gap-1">
                        <ClockIcon size={14} className="text-gray-400" />
                        <span className="text-sm text-gray-700 font-inter">
                          {routine.interval}
                        </span>
                      </div>
                    </td>
                    <td className="py-3 px-2">
                      <span className="text-sm text-gray-700 font-inter">
                        {formatNextExecution(routine.nextExecutionEstimate)}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-sm text-gray-500 font-inter">No hay rutinas configuradas</p>
        )}
      </div>
    </section>
  );
}
