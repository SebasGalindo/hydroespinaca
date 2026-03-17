'use client';

import React from 'react';
import Badge from '@/components/ui/Badge';
import SystemStatusBadge from './SystemStatusBadge';
import type { FuzzySystemDetail } from '@hydroespinaca/shared';

interface SystemInfoHeaderProps {
  detail: FuzzySystemDetail;
}

const SystemInfoHeader = React.memo(function SystemInfoHeader({ detail }: SystemInfoHeaderProps) {
  const { system, variables, rules } = detail;
  const inputVars = variables.filter((v) => v.variableType === 'input');
  const outputVars = variables.filter((v) => v.variableType === 'output');

  const formatDate = (iso: string | null): string => {
    if (!iso) return '—';
    try {
      return new Intl.DateTimeFormat('es-CO', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: true,
        timeZone: 'America/Bogota',
      }).format(new Date(iso));
    } catch {
      return iso;
    }
  };

  return (
    <div className="space-y-4">
      {/* Title + Status */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl lg:text-3xl font-bold text-green-800 font-inter">
            {system.name}
          </h1>
          <p className="text-sm text-gray-500 font-inter mt-1">
            Creado {formatDate(system.createdAt)}
            {system.updatedAt && system.updatedAt !== system.createdAt && (
              <> · Actualizado {formatDate(system.updatedAt)}</>
            )}
          </p>
        </div>
        <SystemStatusBadge status={system.status} size="lg" />
      </div>

      {/* Stats row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <InfoStat
          label="Variables de entrada"
          value={String(inputVars.length)}
          variant="info"
        />
        <InfoStat
          label="Variables de salida"
          value={String(outputVars.length)}
          variant="warning"
        />
        <InfoStat
          label="Reglas"
          value={String(rules.length)}
          variant="success"
        />
        <InfoStat
          label="Defuzzificación"
          value={system.defuzzificationMethod}
          variant="default"
        />
      </div>

      {/* Operators */}
      <div className="flex flex-wrap gap-2 text-sm">
        <Badge variant="default" size="sm">AND: {system.operators.andMethod}</Badge>
        <Badge variant="default" size="sm">OR: {system.operators.orMethod}</Badge>
        <Badge variant="default" size="sm">NOT: {system.operators.notMethod}</Badge>
      </div>
    </div>
  );
});

// ─── Small helper sub-component ───────────────────────────────

interface InfoStatProps {
  label: string;
  value: string;
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
}

const InfoStat: React.FC<InfoStatProps> = ({ label, value, variant = 'default' }) => {
  const bgMap: Record<string, string> = {
    default: 'bg-gray-50',
    success: 'bg-green-50',
    warning: 'bg-yellow-50',
    error: 'bg-red-50',
    info: 'bg-blue-50',
  };

  const textMap: Record<string, string> = {
    default: 'text-gray-800',
    success: 'text-green-800',
    warning: 'text-yellow-800',
    error: 'text-red-800',
    info: 'text-blue-800',
  };

  return (
    <div className={`rounded-lg p-3 ${bgMap[variant]}`}>
      <p className="text-xs text-gray-500 font-inter">{label}</p>
      <p className={`text-lg font-semibold font-inter mt-0.5 ${textMap[variant]}`}>
        {value}
      </p>
    </div>
  );
};

export default SystemInfoHeader;
