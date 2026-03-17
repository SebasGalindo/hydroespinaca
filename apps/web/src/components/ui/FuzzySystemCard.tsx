import React from 'react';
import BaseCard from './BaseCard';
import Badge from './Badge';
import SystemStatusBadge from '@/components/fuzzy/SystemStatusBadge';
import type { FuzzySystem, FuzzySystemExport } from '@hydroespinaca/shared';

export interface FuzzySystemCardProps {
  system: FuzzySystem;
  onClick?: () => void;
  /** Action handlers — if provided, action buttons are shown */
  onActivate?: (id: string) => Promise<void>;
  onClone?: (id: string) => Promise<void>;
  onDelete?: (id: string) => Promise<void>;
  onExport?: (id: string) => Promise<FuzzySystemExport>;
  actionsDisabled?: boolean;
}

const FuzzySystemCard = React.memo(function FuzzySystemCard({
  system,
  onClick,
  onActivate,
  onClone,
  onDelete,
  onExport,
  actionsDisabled = false,
}: FuzzySystemCardProps) {
  // Debug: log system data to identify structure issues
  if (!system?.inputVariableIds || !system?.outputVariableIds || !system?.ruleIds) {
    console.warn('[FuzzySystemCard] Missing array properties:', {
      id: system?.id,
      name: system?.name,
      inputVariableIds: system?.inputVariableIds,
      outputVariableIds: system?.outputVariableIds,
      ruleIds: system?.ruleIds,
      fullSystem: system
    });
  }

  const handleCardClick = (e: React.MouseEvent) => {
    // Don't navigate if a button was clicked
    if ((e.target as HTMLElement).closest('button')) return;
    onClick?.();
  };

  const formatLastUpdated = (dateString?: string | null) => {
    if (!dateString) return 'Sin actualizar';
    try {
      const date = new Date(dateString);
      return new Intl.RelativeTimeFormat('es', { numeric: 'auto' }).format(
        Math.ceil((date.getTime() - Date.now()) / (1000 * 60 * 60 * 24)),
        'day'
      );
    } catch {
      return 'Fecha inválida';
    }
  };

  const inputCount = system.inputVariableIds?.length ?? 0;
  const outputCount = system.outputVariableIds?.length ?? 0;
  const ruleCount = system.ruleIds?.length ?? 0;
  const isActive = system.status === 'ACTIVE';
  const canDelete = system.status === 'DRAFT' || system.status === 'INACTIVE';

  return (
    <BaseCard
      className="group transition-all duration-200 hover:shadow-md hover:border-green-300"
      onClick={handleCardClick}
      hover={true}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold text-gray-900 font-inter group-hover:text-green-700 transition-colors truncate">
            {system.name}
          </h3>
          <p className="text-sm text-gray-600 font-inter mt-1">
            Defuzzificación: {system.defuzzificationMethod}
          </p>
          <p className="text-xs text-gray-500 font-inter mt-1">
            AND: {system.operators.andMethod} | OR: {system.operators.orMethod}
          </p>
        </div>
        <SystemStatusBadge status={system.status} size="sm" />
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-2 mb-4">
        <div className="bg-blue-50 rounded-lg p-2.5 text-center">
          <p className="text-lg font-bold text-blue-700">{inputCount}</p>
          <p className="text-xs text-blue-600">Entradas</p>
        </div>
        <div className="bg-amber-50 rounded-lg p-2.5 text-center">
          <p className="text-lg font-bold text-amber-700">{outputCount}</p>
          <p className="text-xs text-amber-600">Salidas</p>
        </div>
        <div className="bg-green-50 rounded-lg p-2.5 text-center">
          <p className="text-lg font-bold text-green-700">{ruleCount}</p>
          <p className="text-xs text-green-600">Reglas</p>
        </div>
      </div>

      {/* Action buttons row */}
      {(onActivate || onClone || onDelete || onExport) && (
        <div className="flex flex-wrap gap-1.5 mb-3">
          {onActivate && !isActive && (
            <ActionBtn
              label="⚡ Activar"
              variant="green"
              onClick={() => onActivate(system.id)}
              disabled={actionsDisabled}
            />
          )}
          {onClone && (
            <ActionBtn
              label="📋 Duplicar"
              variant="gray"
              onClick={() => onClone(system.id)}
              disabled={actionsDisabled}
            />
          )}
          {onExport && (
            <ActionBtn
              label="📤 Exportar"
              variant="gray"
              onClick={() => onExport(system.id)}
              disabled={actionsDisabled}
            />
          )}
          {onDelete && canDelete && (
            <ActionBtn
              label="🗑️"
              variant="red"
              onClick={() => onDelete(system.id)}
              disabled={actionsDisabled}
            />
          )}
        </div>
      )}

      {/* Footer */}
      <div className="pt-3 border-t border-gray-100">
        <div className="text-xs text-gray-500 font-inter">
          Actualizado {formatLastUpdated(system.updatedAt)}
        </div>
      </div>
    </BaseCard>
  );
});

// ─── Small inline action button ─────────────────────────────

interface ActionBtnProps {
  label: string;
  variant: 'green' | 'gray' | 'red';
  onClick: () => void;
  disabled?: boolean;
}

const variantStyles: Record<string, string> = {
  green: 'text-green-700 hover:bg-green-100',
  gray: 'text-gray-600 hover:bg-gray-100',
  red: 'text-red-600 hover:bg-red-100',
};

const ActionBtn: React.FC<ActionBtnProps> = ({ label, variant, onClick, disabled }) => (
  <button
    onClick={(e) => { e.stopPropagation(); onClick(); }}
    disabled={disabled}
    className={`px-2 py-1 text-xs font-medium rounded transition-colors disabled:opacity-50 ${variantStyles[variant]}`}
  >
    {label}
  </button>
);

export default FuzzySystemCard;