'use client';

import React from 'react';
import { 
  SunIcon, 
  WaterIcon, 
  PhIcon, 
  SensorIcon,
  LightBulbIcon,
  RulerIcon 
} from '@/components/ui/icons/Icons';

import { SensorData } from './SensorsConfigForm'; // Assuming SensorData is exported from here

interface SensorCardProps {
  sensor: SensorData;
  onSelect: () => void;
  onToggle: () => void;
  onCalibrate: () => void;
  isSelected: boolean;
  isCalibrating: boolean;
  isMobile?: boolean;
}

const getSensorIcon = (type: SensorData['type']) => {
  const iconProps = { size: 24, className: "text-green-600" };
  
  switch (type) {
    case 'temperature':
      return <SunIcon {...iconProps} />;
    case 'humidity':
      return <WaterIcon {...iconProps} />;
    case 'ph':
      return <PhIcon {...iconProps} />;
    case 'conductivity':
      return <SensorIcon {...iconProps} />;
    case 'light':
      return <LightBulbIcon {...iconProps} />;
    case 'water_level':
      return <RulerIcon {...iconProps} />;
    default:
      return <SensorIcon {...iconProps} />;
  }
};

const getStatusColor = (status: SensorData['status']) => {
  switch (status) {
    case 'active':
      return 'bg-green-100 text-green-800 border-green-200';
    case 'inactive':
      return 'bg-gray-100 text-gray-800 border-gray-200';
    case 'error':
      return 'bg-red-100 text-red-800 border-red-200';
    case 'calibrating':
      return 'bg-yellow-100 text-yellow-800 border-yellow-200';
    default:
      return 'bg-gray-100 text-gray-800 border-gray-200';
  }
};

const getStatusText = (status: SensorData['status']) => {
  switch (status) {
    case 'active':
      return 'Activo';
    case 'inactive':
      return 'Inactivo';
    case 'error':
      return 'Error';
    case 'calibrating':
      return 'Calibrando';
    default:
      return 'Desconocido';
  }
};

export default function SensorCard({ 
  sensor, 
  onSelect, 
  onToggle, 
  onCalibrate, 
  isSelected, 
  isCalibrating,
  isMobile = false
}: SensorCardProps) {
  return (
    <div 
      className={`
        relative bg-white rounded-lg border-2 p-6 cursor-pointer transition-all duration-200 hover:shadow-md
        ${
          isSelected 
            ? 'border-green-500 shadow-lg ring-2 ring-green-200' 
            : 'border-gray-200 hover:border-green-300'
        }
      `}
      onClick={onSelect}
    >
      {/* Indicador de calibración pendiente */}
      {sensor.calibrationDue && (
        <div className="absolute -top-2 -right-2 w-4 h-4 bg-orange-500 rounded-full animate-pulse" />
      )}

      {/* Header con icono y estado */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-green-50 rounded-lg">
            {getSensorIcon(sensor.type)}
          </div>
          <div>
            <h3 className="font-semibold text-gray-900 text-base">
              {sensor.physicalId}
            </h3>
          </div>
        </div>
        <span className={`inline-block px-3 py-1 text-sm font-medium rounded-full border ${getStatusColor(sensor.status)}`}>
          {getStatusText(sensor.status)}
        </span>
      </div>



      {/* Información del sensor */}
      <div className="mb-6 space-y-3">
        <div className="text-sm">
          <span className="text-gray-600 font-semibold">ID:</span> 
          <span className="text-gray-800 font-medium ml-2">{sensor.id}</span>
        </div>
        <div className="text-sm">
          <span className="text-gray-600 font-semibold">Ubicación:</span> 
          <span className="text-gray-800 font-medium ml-2">{sensor.location}</span>
        </div>
        <div className="text-sm">
          <span className="text-gray-600 font-semibold">Frecuencia:</span> 
          <span className="text-gray-800 font-medium ml-2">{sensor.readFrequency}s</span>
        </div>
        <div className="text-sm">
          <span className="text-gray-600 font-semibold">ESP32 ID:</span> 
          <span className="text-gray-800 font-medium ml-2">{sensor.esp32Id}</span>
        </div>
      </div>

      {/* Botones de acción */}
      <div className="flex space-x-3">
        <button
          onClick={(e) => {
            e.stopPropagation();
            onCalibrate();
          }}
          className="flex-1 px-4 py-3 text-sm font-semibold bg-green-50 text-green-700 rounded-md hover:bg-green-100 transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          disabled={sensor.status === 'inactive' || isCalibrating}
        >
          {isCalibrating ? (isMobile ? 'Editando...' : 'Calibrando...') : (isMobile ? 'Editar' : 'Calibrar')}
        </button>
        
        <button
          onClick={(e) => {
            e.stopPropagation();
            onToggle();
          }}
          className="px-4 py-3 text-sm font-semibold bg-red-100 text-red-700 rounded-md hover:bg-red-200 transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          disabled={sensor.status === 'error' || isCalibrating}
        >
          Eliminar
        </button>
      </div>
    </div>
  );
}