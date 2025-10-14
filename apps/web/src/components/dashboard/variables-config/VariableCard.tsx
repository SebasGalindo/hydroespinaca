'use client';

import React from 'react';
import { 
  DatabaseIcon,
  CogIcon,
  CalculatorIcon,
  BoltIcon,
  ClockIcon,
  ChartBarIcon
} from '@/components/ui/icons/Icons';

import { VariableData } from '@hidroespinaca/shared';

interface VariableCardProps {
  variable: VariableData;
  onEdit: (variableId: string) => void;
  onDelete: (variableId: string) => void;
  isSelected?: boolean;
  isMobile?: boolean;
}

const getVariableIcon = (type: VariableData['type']) => {
  const iconProps = { size: 24, className: "text-green-600" };
  
  switch (type) {
    case 'input':
      return <DatabaseIcon {...iconProps} />;
    case 'output':
      return <CogIcon {...iconProps} />;
    case 'calculated':
      return <CalculatorIcon {...iconProps} />;
    default:
      return <BoltIcon {...iconProps} />;
  }
};

const getStatusColor = (status: VariableData['status']) => {
  switch (status) {
    case 'active':
      return 'bg-green-100 text-green-800 border-green-200';
    case 'inactive':
      return 'bg-gray-100 text-gray-800 border-gray-200';
    case 'deprecated':
      return 'bg-red-100 text-red-800 border-red-200';
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200';
  }
};

const getStatusText = (status: VariableData['status']) => {
  switch (status) {
    case 'active':
      return 'Activa';
    case 'inactive':
      return 'Inactiva';
    case 'deprecated':
      return 'Obsoleta';
    default:
      return 'Desconocido';
  }
};

const getTypeText = (type: VariableData['type']) => {
  switch (type) {
    case 'input':
      return 'Entrada';
    case 'output':
      return 'Salida';
    case 'calculated':
      return 'Calculada';
    default:
      return 'Desconocido';
  }
};

const getCategoryText = (category: VariableData['category']) => {
  switch (category) {
    case 'environmental':
      return 'Ambiental';
    case 'control':
      return 'Control';
    case 'system':
      return 'Sistema';
    case 'user':
      return 'Usuario';
    default:
      return 'Otro';
  }
};

export default function VariableCard({ 
  variable, 
  onEdit, 
  onDelete, 
  isSelected = false,
  isMobile = false
}: VariableCardProps) {
  return (
    <div 
      className={`
        bg-white rounded-lg border-2 p-6 transition-all duration-200
        ${isSelected ? 'border-green-500 shadow-lg' : 'border-gray-200 hover:border-green-300'}
        ${isMobile ? 'shadow-sm' : 'shadow-md hover:shadow-lg'}
      `}
    >
      {/* Header con icono y estado */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          {getVariableIcon(variable.type)}
          <div>
            <h3 className="font-semibold text-gray-900 text-lg">
              {variable.name}
            </h3>
            <p className="text-sm text-gray-600">
              {variable.id}
            </p>
          </div>
        </div>
        <span className={`px-3 py-1 rounded-full text-xs font-medium border ${getStatusColor(variable.status)}`}>
          {getStatusText(variable.status)}
        </span>
      </div>

      {/* Descripción */}
      <div className="mb-4">
        <p className="text-gray-700 text-sm leading-relaxed">
          {variable.description}
        </p>
      </div>

      {/* Información principal */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Tipo</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {getTypeText(variable.type)}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Categoría</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {getCategoryText(variable.category)}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Unidad</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {variable.unit}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Tipo de Dato</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {variable.dataType === 'numeric' ? 'Numérico' : variable.dataType === 'boolean' ? 'Booleano' : 'Texto'}
          </p>
        </div>
      </div>

      {/* Rango de valores (si aplica) */}
      {variable.minValue !== undefined && variable.maxValue !== undefined && (
        <div className="mb-4">
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Rango</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {variable.minValue} - {variable.maxValue} {variable.unit}
          </p>
        </div>
      )}

      {/* Información adicional */}
      <div className="flex items-center justify-end text-xs text-gray-500 pt-4 border-t border-gray-100">
        <div className="flex items-center gap-1">
          <ClockIcon size={14} className="text-gray-400" />
          <span>Modificado: {variable.lastModified}</span>
        </div>
      </div>

      {/* Botones de acción */}
      <div className="flex gap-2 mt-4">
        <button
          onClick={(e) => {
            e.stopPropagation();
            onEdit(variable.id);
          }}
          className="flex-1 px-4 py-2 bg-green-50 text-green-700 rounded-lg text-sm font-medium hover:bg-green-100 transition-colors"
        >
          Editar
        </button>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(variable.id);
          }}
          className="flex-1 px-4 py-2 bg-red-50 text-red-700 rounded-lg text-sm font-medium hover:bg-red-100 transition-colors"
        >
          Eliminar
        </button>
      </div>
    </div>
  );
}