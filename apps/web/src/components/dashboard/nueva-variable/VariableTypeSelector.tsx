'use client';

import React from 'react';
import { SunIcon, PhIcon, WaterIcon, PlantIcon } from '@/components/ui/icons/Icons';

interface VariableTypeSelectorProps {
  value: string;
  onChange: (value: string) => void;
}

const variableTypes = [
  {
    id: 'environmental',
    label: 'Ambiental',
    description: 'Variables del entorno como temperatura, humedad, luz',
    IconComponent: SunIcon
  },
  {
    id: 'nutritional',
    label: 'Nutricional',
    description: 'Niveles de nutrientes, pH, conductividad eléctrica',
    IconComponent: PhIcon
  },
  {
    id: 'physical',
    label: 'Física',
    description: 'Medidas físicas como nivel de agua, flujo, presión',
    IconComponent: WaterIcon
  },
  {
    id: 'biological',
    label: 'Biológica',
    description: 'Variables relacionadas con el crecimiento de las plantas',
    IconComponent: PlantIcon
  }
];

export default function VariableTypeSelector({ value, onChange }: VariableTypeSelectorProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {variableTypes.map((type) => (
        <div
          key={type.id}
          className={`
            relative cursor-pointer rounded-lg border-2 p-4 transition-all duration-200
            ${value === type.id 
              ? 'border-green-500 bg-green-50 ring-2 ring-green-200' 
              : 'border-gray-200 bg-white hover:border-gray-300 hover:bg-gray-50'
            }
          `}
          onClick={() => onChange(type.id)}
        >
          <div className="flex items-start space-x-3">
            <div className="text-green-600">
              <type.IconComponent size={24} />
            </div>
            <div className="flex-1">
              <div className="flex items-center">
                <input
                  type="radio"
                  name="variableType"
                  value={type.id}
                  checked={value === type.id}
                  onChange={() => onChange(type.id)}
                  className="h-4 w-4 text-green-600 focus:ring-green-500 border-gray-300"
                />
                <label className="ml-3 text-sm font-medium text-gray-900 font-inter">
                  {type.label}
                </label>
              </div>
              <p className="mt-1 text-sm text-gray-500 font-inter">
                {type.description}
              </p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}