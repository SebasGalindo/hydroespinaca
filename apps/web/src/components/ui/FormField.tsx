import React from 'react';

export interface FormFieldProps {
  type: 'text' | 'number' | 'date' | 'select' | 'email' | 'password';
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  error?: string;
  min?: string;
  max?: string;
  maxLength?: number;
  step?: string;
  options?: { value: string; label: string }[];
  disabled?: boolean;
  helperText?: string;
}

const FormField: React.FC<FormFieldProps> = ({
  type,
  label,
  value,
  onChange,
  placeholder = '',
  required = false,
  error,
  min,
  max,
  maxLength,
  step,
  options = [],
  disabled = false,
  helperText
}) => {
  const baseInputClasses = `
    w-full px-4 py-2 border rounded-lg transition-colors
    focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent
    disabled:bg-gray-100 disabled:cursor-not-allowed
    ${error ? 'border-red-500' : 'border-gray-300'}
  `;

  const renderInput = () => {
    if (type === 'select') {
      return (
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          required={required}
          disabled={disabled}
          className={baseInputClasses}
        >
          <option value="">Seleccione una opción</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      );
    }

    return (
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        required={required}
        min={min}
        max={max}
        maxLength={maxLength}
        step={step}
        disabled={disabled}
        className={baseInputClasses}
      />
    );
  };

  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-gray-700">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>

      {renderInput()}

      {helperText && !error && (
        <p className="text-xs text-gray-500">{helperText}</p>
      )}

      {error && (
        <p className="text-xs text-red-500">{error}</p>
      )}
    </div>
  );
};

export default FormField;
