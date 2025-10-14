'use client';

import React from 'react';

interface RangeSliderProps {
  minRange: number;
  maxRange: number;
  optimalMin: number;
  optimalMax: number;
  currentValue: number;
  unit: string;
}

export default function RangeSlider({ 
  minRange, 
  maxRange, 
  optimalMin, 
  optimalMax, 
  currentValue, 
  unit 
}: RangeSliderProps) {
  // Calcular posiciones como porcentajes
  const range = maxRange - minRange;
  const optimalMinPercent = ((optimalMin - minRange) / range) * 100;
  const optimalMaxPercent = ((optimalMax - minRange) / range) * 100;
  const currentPercent = ((currentValue - minRange) / range) * 100;

  // Determinar el estado del valor actual
  const getValueStatus = () => {
    if (currentValue < optimalMin || currentValue > optimalMax) {
      return 'warning';
    }
    return 'optimal';
  };

  const valueStatus = getValueStatus();

  return (
    <div className="space-y-4">
      <h4 className="text-sm font-medium text-gray-700">
        Visualización de Rangos
      </h4>
      
      {/* Barra de rango */}
      <div className="relative">
        {/* Barra base */}
        <div className="w-full h-6 bg-gray-200 rounded-lg relative overflow-hidden">
          {/* Zona óptima */}
          <div 
            className="absolute h-full bg-green-300 opacity-70"
            style={{
              left: `${optimalMinPercent}%`,
              width: `${optimalMaxPercent - optimalMinPercent}%`
            }}
          />
          
          {/* Indicador de valor actual */}
          <div 
            className={`absolute top-0 w-1 h-full transition-all duration-300 ${
              valueStatus === 'optimal' ? 'bg-green-600' : 'bg-orange-500'
            }`}
            style={{ left: `${Math.max(0, Math.min(100, currentPercent))}%` }}
          />
          
          {/* Marcador de valor actual (círculo) */}
          <div 
            className={`absolute top-1/2 transform -translate-y-1/2 w-4 h-4 rounded-full border-2 border-white shadow-md transition-all duration-300 ${
              valueStatus === 'optimal' ? 'bg-green-600' : 'bg-orange-500'
            }`}
            style={{ left: `calc(${Math.max(0, Math.min(100, currentPercent))}% - 8px)` }}
          />
        </div>
        
        {/* Etiquetas de rango */}
        <div className="flex justify-between mt-2 text-xs text-gray-600">
          <span>{minRange} {unit}</span>
          <span className="text-green-600 font-medium">
            Óptimo: {optimalMin} - {optimalMax} {unit}
          </span>
          <span>{maxRange} {unit}</span>
        </div>
      </div>
      
      {/* Información del valor actual */}
      <div className={`p-3 rounded-lg ${
        valueStatus === 'optimal' 
          ? 'bg-green-50 border border-green-200' 
          : 'bg-orange-50 border border-orange-200'
      }`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <div className={`w-3 h-3 rounded-full ${
              valueStatus === 'optimal' ? 'bg-green-500' : 'bg-orange-500'
            }`} />
            <span className={`text-sm font-medium ${
              valueStatus === 'optimal' ? 'text-green-800' : 'text-orange-800'
            }`}>
              Valor Actual: {currentValue} {unit}
            </span>
          </div>
          <span className={`text-xs px-2 py-1 rounded-full font-medium ${
            valueStatus === 'optimal' 
              ? 'bg-green-100 text-green-700' 
              : 'bg-orange-100 text-orange-700'
          }`}>
            {valueStatus === 'optimal' ? '✓ En rango óptimo' : '⚠ Fuera del rango óptimo'}
          </span>
        </div>
      </div>
      
      {/* Leyenda */}
      <div className="flex items-center justify-center space-x-6 text-xs text-gray-600">
        <div className="flex items-center space-x-2">
          <div className="w-3 h-3 bg-gray-200 rounded" />
          <span>Rango total</span>
        </div>
        <div className="flex items-center space-x-2">
          <div className="w-3 h-3 bg-green-300 rounded" />
          <span>Zona óptima</span>
        </div>
        <div className="flex items-center space-x-2">
          <div className="w-3 h-3 bg-green-600 rounded" />
          <span>Valor actual</span>
        </div>
      </div>
    </div>
  );
}