'use client';

import React from 'react';

interface FormFieldProps {
  label: string;
  type: 'text' | 'number' | 'email' | 'textarea' | 'select' | 'date' | 'time';
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string;
  helpText?: string;
  rows?: number;
  options?: { value: string; label: string }[];
  min?: string;
  max?: string;
  step?: string;
  className?: string;
}

export default function FormField({
  label,
  type,
  value,
  onChange,
  placeholder,
  required = false,
  disabled = false,
  error,
  helpText,
  rows = 4,
  options = [],
  min,
  max,
  step,
  className = ''
}: FormFieldProps) {
  const baseInputClasses = `
    w-full px-4 py-3 border border-gray-300 rounded-lg 
    focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent
    transition-all duration-200 font-inter
    ${disabled ? 'bg-gray-50 text-gray-500 cursor-not-allowed' : 'bg-white text-gray-900'}
    ${error ? 'border-red-500 focus:ring-red-500' : ''}
  `;

  const renderInput = () => {
    switch (type) {
      case 'textarea':
        return (
          <textarea
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            required={required}
            disabled={disabled}
            rows={rows}
            className={`${baseInputClasses} resize-none`}
          />
        );
      
      case 'select':
        return (
          <select
            value={value}
            onChange={(e) => onChange(e.target.value)}
            required={required}
            disabled={disabled}
            className={baseInputClasses}
          >
            <option value="">{placeholder || 'Selecciona una opción'}</option>
            {options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        );
      
      case 'date':
      case 'time':
        return (
          <input
            type={type}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            required={required}
            disabled={disabled}
            min={min}
            max={max}
            step={step}
            className={baseInputClasses}
          />
        );
      
      case 'number':
        return (
          <input
            type="number"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            required={required}
            disabled={disabled}
            min={min}
            max={max}
            step={step}
            className={baseInputClasses}
          />
        );
      
      default:
        return (
          <input
            type={type}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            required={required}
            disabled={disabled}
            className={baseInputClasses}
          />
        );
    }
  };

  return (
    <div className={`space-y-2 ${className}`}>
      <label className="block text-sm font-medium text-gray-700 font-inter">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      
      {renderInput()}
      
      {helpText && (
        <p className="text-sm text-gray-500 font-inter">
          {helpText}
        </p>
      )}
      
      {error && (
        <p className="text-sm text-red-600 font-inter">
          {error}
        </p>
      )}
    </div>
  );
}