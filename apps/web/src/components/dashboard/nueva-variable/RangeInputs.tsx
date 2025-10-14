'use client';

import React from 'react';
import FormField from './FormField';

interface RangeInputsProps {
  formData: {
    minValue: string;
    maxValue: string;
    optimalMin: string;
    optimalMax: string;
    unit: string;
  };
  onChange: (field: string, value: string) => void;
}

export default function RangeInputs({ formData, onChange }: RangeInputsProps) {
  const unitDisplay = formData.unit ? ` (${formData.unit})` : '';

  return (
    <div className="space-y-6">
      {/* Rango General */}
      <div className="bg-gray-50 rounded-lg p-4">
        <h4 className="text-sm font-medium text-gray-700 mb-4 font-inter">
          Rango General de Medición
        </h4>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            label={`Valor Mínimo${unitDisplay}`}
            type="number"
            value={formData.minValue}
            onChange={(value) => onChange('minValue', value)}
            placeholder="0"
            step="0.1"
            helpText="Valor mínimo que puede medir el sensor"
          />
          <FormField
            label={`Valor Máximo${unitDisplay}`}
            type="number"
            value={formData.maxValue}
            onChange={(value) => onChange('maxValue', value)}
            placeholder="100"
            step="0.1"
            helpText="Valor máximo que puede medir el sensor"
          />
        </div>
      </div>

      {/* Rango Óptimo */}
      <div className="bg-green-50 rounded-lg p-4">
        <h4 className="text-sm font-medium text-green-700 mb-4 font-inter">
          Rango Óptimo para el Cultivo
        </h4>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            label={`Óptimo Mínimo${unitDisplay}`}
            type="number"
            value={formData.optimalMin}
            onChange={(value) => onChange('optimalMin', value)}
            placeholder="20"
            step="0.1"
            helpText="Valor mínimo del rango óptimo"
            {...(formData.minValue && { min: String(formData.minValue) })}
            {...(formData.maxValue && { max: String(formData.maxValue) })}
          />
          <FormField
            label={`Óptimo Máximo${unitDisplay}`}
            type="number"
            value={formData.optimalMax}
            onChange={(value) => onChange('optimalMax', value)}
            placeholder="80"
            step="0.1"
            helpText="Valor máximo del rango óptimo"
            {...(formData.optimalMin ? { min: String(formData.optimalMin) } : formData.minValue ? { min: String(formData.minValue) } : {})}
            {...(formData.maxValue && { max: String(formData.maxValue) })}
          />
        </div>
        
        {/* Visualización del rango */}
        {formData.minValue && formData.maxValue && (
          <div className="mt-4">
            <div className="text-xs text-gray-600 mb-2 font-inter">Visualización del rango:</div>
            <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
              {/* Rango total */}
              <div className="absolute inset-0 bg-gray-300"></div>
              
              {/* Rango óptimo */}
              {formData.optimalMin && formData.optimalMax && (
                <div 
                  className="absolute h-full bg-green-400 transition-all duration-300"
                  style={{
                    left: `${((parseFloat(formData.optimalMin) - parseFloat(formData.minValue)) / (parseFloat(formData.maxValue) - parseFloat(formData.minValue))) * 100}%`,
                    width: `${((parseFloat(formData.optimalMax) - parseFloat(formData.optimalMin)) / (parseFloat(formData.maxValue) - parseFloat(formData.minValue))) * 100}%`
                  }}
                ></div>
              )}
            </div>
            <div className="flex justify-between text-xs text-gray-500 mt-1 font-inter">
              <span>{formData.minValue}{formData.unit}</span>
              <span className="text-green-600 font-medium">
                Óptimo: {formData.optimalMin || '?'} - {formData.optimalMax || '?'}{formData.unit}
              </span>
              <span>{formData.maxValue}{formData.unit}</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}