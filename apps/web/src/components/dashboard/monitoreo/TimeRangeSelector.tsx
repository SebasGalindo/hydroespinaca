'use client';

import React from 'react';

interface TimeRangeSelectorProps {
  value: '1h' | '6h' | '24h' | '7d';
  onChange: (value: '1h' | '6h' | '24h' | '7d') => void;
}

const TimeRangeSelector: React.FC<TimeRangeSelectorProps> = ({ value, onChange }) => {
  const options = [
    { value: '1h' as const, label: '1 Hora' },
    { value: '6h' as const, label: '6 Horas' },
    { value: '24h' as const, label: '24 Horas' },
    { value: '7d' as const, label: '7 Días' }
  ];

  return (
    <fieldset className="flex items-center gap-1 bg-gray-100 p-1 rounded-lg">
      <legend className="sr-only">Seleccionar rango de tiempo para los gráficos</legend>
      {options.map((option) => (
        <label key={option.value} className="relative">
          <input
            type="radio"
            name="timeRange"
            value={option.value}
            checked={value === option.value}
            onChange={(e) => onChange(e.target.value as '1h' | '6h' | '24h' | '7d')}
            className="sr-only"
            aria-describedby={`time-range-${option.value}-desc`}
          />
          <span
            className={`
              block px-3 py-2 text-sm font-medium rounded-md transition-all duration-200 cursor-pointer font-inter
              ${
                value === option.value
                  ? 'bg-white text-green-700 shadow-sm border border-green-200'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
              }
            `}
          >
            {option.label}
          </span>
          <span id={`time-range-${option.value}-desc`} className="sr-only">
            Mostrar datos de los últimos {option.label.toLowerCase()}
          </span>
        </label>
      ))}
    </fieldset>
  );
};

export default TimeRangeSelector;