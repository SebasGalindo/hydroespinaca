'use client';

import React from 'react';
import { 
  WaterIcon,
  CogIcon,
  FanIcon,
  SunIcon,
  LightBulbIcon,
  MotorIcon,
  ClockIcon,
  BoltIcon,
  SettingsIcon
} from '@/components/ui/icons/Icons';

import { ActuadorData } from '@hidroespinaca/shared';

interface ActuadorCardProps {
  actuador: ActuadorData;
  onEdit: (actuadorId: string) => void;
  onDelete: (actuadorId: string) => void;
  isSelected?: boolean;
  isMobile?: boolean;
}

const getActuadorIcon = (type: ActuadorData['type']) => {
  const iconProps = { size: 24, className: "text-green-600" };
  
  switch (type) {
    case 'pump':
      return <WaterIcon {...iconProps} />;
    case 'valve':
      return <CogIcon {...iconProps} />;
    case 'fan':
      return <FanIcon {...iconProps} />;
    case 'heater':
      return <SunIcon {...iconProps} />;
    case 'light':
      return <LightBulbIcon {...iconProps} />;
    case 'motor':
      return <MotorIcon {...iconProps} />;
    default:
      return <SettingsIcon {...iconProps} />;
  }
};

const getStatusColor = (status: ActuadorData['status']) => {
  switch (status) {
    case 'active':
      return 'bg-green-100 text-green-800 border-green-200';
    case 'inactive':
      return 'bg-gray-100 text-gray-800 border-gray-200';
    case 'error':
      return 'bg-red-100 text-red-800 border-red-200';
    case 'maintenance':
      return 'bg-yellow-100 text-yellow-800 border-yellow-200';
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200';
  }
};

const getStatusText = (status: ActuadorData['status']) => {
  switch (status) {
    case 'active':
      return 'Activo';
    case 'inactive':
      return 'Inactivo';
    case 'error':
      return 'Error';
    case 'maintenance':
      return 'Mantenimiento';
    default:
      return 'Desconocido';
  }
};

const getStateColor = (state: ActuadorData['status']) => {
  switch (state) {
    case 'active':
      return 'bg-green-100 text-green-800';
    case 'inactive':
      return 'bg-gray-100 text-gray-800';
    case 'error':
      return 'bg-red-100 text-red-800';
    case 'maintenance':
      return 'bg-yellow-100 text-yellow-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getStateText = (state: ActuadorData['status']) => {
  switch (state) {
    case 'active':
      return 'Activo';
    case 'inactive':
      return 'Inactivo';
    case 'error':
      return 'Error';
    case 'maintenance':
      return 'Mantenimiento';
    default:
      return 'Desconocido';
  }
};

const getTypeText = (type: ActuadorData['type']) => {
  switch (type) {
    case 'pump':
      return 'Bomba';
    case 'valve':
      return 'Válvula';
    case 'fan':
      return 'Ventilador';
    case 'heater':
      return 'Calefactor';
    case 'light':
      return 'Iluminación';
    case 'motor':
      return 'Motor';
    default:
      return 'Otro';
  }
};

export default function ActuadorCard({ 
  actuador, 
  onEdit, 
  onDelete, 
  isSelected = false,
  isMobile = false
}: ActuadorCardProps) {
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
          {getActuadorIcon(actuador.type)}
          <div>
            <h3 className="font-semibold text-gray-900 text-lg">
              {actuador.name}
            </h3>
            <p className="text-sm text-gray-600">
              {actuador.id}
            </p>
          </div>
        </div>
        <div>
          <span className={`px-3 py-1 rounded-full text-xs font-medium border ${getStatusColor(actuador.status)}`}>
            {getStatusText(actuador.status)}
          </span>
        </div>
      </div>

      {/* Información principal */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Tipo</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {getTypeText(actuador.type)}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Ubicación</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {actuador.location}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">ESP32 ID</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            {actuador.esp32Id}
          </p>
        </div>
        <div>
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Pin ESP32</span>
          <p className="text-sm font-medium text-gray-900 mt-1">
            GPIO {actuador.pin}
          </p>
        </div>
      </div>

      {/* Información adicional */}
      <div className="flex items-center justify-between text-xs text-gray-500 pt-4 border-t border-gray-100">
        <div className="flex items-center gap-1">
          <BoltIcon size={14} className="text-gray-400" />
          <span>Estado: {getStatusText(actuador.status)}</span>
        </div>
        <div className="flex items-center gap-1">
          <ClockIcon size={14} className="text-gray-400" />
          <span>Creado: {actuador.createdAt}</span>
        </div>
      </div>

      {/* Botones de acción */}
      <div className="flex gap-2 mt-4">
        <button
          onClick={() => onEdit(actuador.id)}
          className="flex-1 px-4 py-2 bg-green-50 text-green-700 rounded-lg text-sm font-medium hover:bg-green-100 transition-colors"
        >
          Editar
        </button>
        <button
          onClick={() => onDelete(actuador.id)}
          className="flex-1 px-4 py-2 bg-red-50 text-red-700 rounded-lg text-sm font-medium hover:bg-red-100 transition-colors"
        >
          Eliminar
        </button>
      </div>
    </div>
  );
}