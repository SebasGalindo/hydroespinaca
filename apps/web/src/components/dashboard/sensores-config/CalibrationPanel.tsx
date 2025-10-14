'use client';

import React, { useState, useEffect } from 'react';
import FormSection from '@/components/ui/FormSection';
import RangeSlider from './RangeSlider';

interface SensorData {
  id: string;
  name: string;
  type: 'temperature' | 'humidity' | 'ph' | 'conductivity' | 'light' | 'water_level';
  status: 'active' | 'inactive' | 'error' | 'calibrating';
  currentValue: string;
  unit: string;
  lastCalibration: string;
  calibrationDue: boolean;
  minRange: number;
  maxRange: number;
  optimalMin: number;
  optimalMax: number;
}

interface CalibrationPanelProps {
  sensor: SensorData;
  onRangeUpdate: (ranges: { minRange: number; maxRange: number; optimalMin: number; optimalMax: number }) => void;
  onCalibrate: () => void;
  isCalibrating: boolean;
}

export default function CalibrationPanel({ 
  sensor, 
  onRangeUpdate, 
  onCalibrate, 
  isCalibrating 
}: CalibrationPanelProps) {
  const [localRanges, setLocalRanges] = useState({
    minRange: sensor.minRange,
    maxRange: sensor.maxRange,
    optimalMin: sensor.optimalMin,
    optimalMax: sensor.optimalMax
  });

  const [calibrationStep, setCalibrationStep] = useState(0);
  const [calibrationValues, setCalibrationValues] = useState({
    point1: '',
    point2: '',
    point3: ''
  });

  useEffect(() => {
    setLocalRanges({
      minRange: sensor.minRange,
      maxRange: sensor.maxRange,
      optimalMin: sensor.optimalMin,
      optimalMax: sensor.optimalMax
    });
  }, [sensor]);

  const handleRangeChange = (field: string, value: number) => {
    const newRanges = { ...localRanges, [field]: value };
    setLocalRanges(newRanges);
    onRangeUpdate(newRanges);
  };

  const getCalibrationInstructions = () => {
    switch (sensor.type) {
      case 'ph':
        return [
          'Sumerge el sensor en solución buffer pH 4.0',
          'Sumerge el sensor en solución buffer pH 7.0',
          'Sumerge el sensor en solución buffer pH 10.0'
        ];
      case 'temperature':
        return [
          'Coloca el sensor en agua a temperatura ambiente (20°C)',
          'Coloca el sensor en agua tibia (30°C)',
          'Coloca el sensor en agua fría (10°C)'
        ];
      case 'conductivity':
        return [
          'Sumerge el sensor en agua destilada (0 mS/cm)',
          'Sumerge el sensor en solución estándar 1.41 mS/cm',
          'Sumerge el sensor en solución estándar 2.76 mS/cm'
        ];
      default:
        return [
          'Coloca el sensor en condiciones de referencia baja',
          'Coloca el sensor en condiciones de referencia media',
          'Coloca el sensor en condiciones de referencia alta'
        ];
    }
  };

  const calibrationInstructions = getCalibrationInstructions();

  return (
    <FormSection 
      title={`Configuración Detallada - ${sensor.name}`}
      subtitle="Ajusta los rangos de operación y calibra el sensor"
    >
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Panel de rangos */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Configuración de Rangos
          </h3>
          
          {/* Rango de medición */}
          <div className="space-y-4">
            <h4 className="text-sm font-medium text-gray-700">
              Rango de Medición ({sensor.unit})
            </h4>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Mínimo</label>
                <input
                  type="number"
                  value={localRanges.minRange}
                  onChange={(e) => handleRangeChange('minRange', parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  step="0.1"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Máximo</label>
                <input
                  type="number"
                  value={localRanges.maxRange}
                  onChange={(e) => handleRangeChange('maxRange', parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  step="0.1"
                />
              </div>
            </div>
          </div>

          {/* Rango óptimo */}
          <div className="space-y-4">
            <h4 className="text-sm font-medium text-gray-700">
              Rango Óptimo ({sensor.unit})
            </h4>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Mínimo Óptimo</label>
                <input
                  type="number"
                  value={localRanges.optimalMin}
                  onChange={(e) => handleRangeChange('optimalMin', parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  step="0.1"
                  min={localRanges.minRange}
                  max={localRanges.maxRange}
                />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Máximo Óptimo</label>
                <input
                  type="number"
                  value={localRanges.optimalMax}
                  onChange={(e) => handleRangeChange('optimalMax', parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  step="0.1"
                  min={localRanges.minRange}
                  max={localRanges.maxRange}
                />
              </div>
            </div>
          </div>

          {/* Visualización de rangos */}
          <RangeSlider
            minRange={localRanges.minRange}
            maxRange={localRanges.maxRange}
            optimalMin={localRanges.optimalMin}
            optimalMax={localRanges.optimalMax}
            currentValue={parseFloat(sensor.currentValue) || 0}
            unit={sensor.unit}
          />
        </div>

        {/* Panel de calibración */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Calibración del Sensor
          </h3>
          
          {/* Estado actual */}
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium text-gray-700">Valor Actual:</span>
              <span className="text-lg font-bold text-gray-900">
                {sensor.currentValue} {sensor.unit}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm font-medium text-gray-700">Última Calibración:</span>
              <span className="text-sm text-gray-600">{sensor.lastCalibration}</span>
            </div>
          </div>

          {/* Proceso de calibración */}
          {!isCalibrating ? (
            <div className="space-y-4">
              <h4 className="text-sm font-medium text-gray-700">
                Calibración de 3 Puntos
              </h4>
              <div className="space-y-3">
                {calibrationInstructions.map((instruction, index) => (
                  <div key={index} className="flex items-center space-x-3 p-3 bg-blue-50 rounded-lg">
                    <div className="w-6 h-6 bg-blue-500 text-white rounded-full flex items-center justify-center text-xs font-bold">
                      {index + 1}
                    </div>
                    <span className="text-sm text-blue-800">{instruction}</span>
                  </div>
                ))}
              </div>
              
              <button
                onClick={onCalibrate}
                className="w-full px-4 py-3 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 transition-colors duration-200"
                disabled={sensor.status === 'inactive'}
              >
                Iniciar Calibración
              </button>
            </div>
          ) : (
            <div className="space-y-4">
              <h4 className="text-sm font-medium text-gray-700">
                Calibrando... Paso {calibrationStep + 1} de 3
              </h4>
              <div className="space-y-3">
                <div className="flex items-center space-x-3 p-3 bg-yellow-50 rounded-lg">
                  <div className="w-6 h-6 bg-yellow-500 text-white rounded-full flex items-center justify-center text-xs font-bold animate-pulse">
                    {calibrationStep + 1}
                  </div>
                  <span className="text-sm text-yellow-800">
                    {calibrationInstructions[calibrationStep]}
                  </span>
                </div>
              </div>
              
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div 
                  className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                  style={{ width: `${((calibrationStep + 1) / 3) * 100}%` }}
                />
              </div>
              
              <div className="text-center text-sm text-gray-600">
                Calibración en progreso... Por favor espera.
              </div>
            </div>
          )}

          {/* Información adicional */}
          <div className="bg-green-50 rounded-lg p-4">
            <h5 className="text-sm font-medium text-green-800 mb-2">
              💡 Consejos de Calibración
            </h5>
            <ul className="text-xs text-green-700 space-y-1">
              <li>• Asegúrate de que el sensor esté limpio antes de calibrar</li>
              <li>• Espera a que la lectura se estabilice en cada punto</li>
              <li>• Calibra en un ambiente con temperatura estable</li>
              <li>• Realiza calibraciones cada 30 días para mayor precisión</li>
            </ul>
          </div>
        </div>
      </div>
    </FormSection>
  );
}