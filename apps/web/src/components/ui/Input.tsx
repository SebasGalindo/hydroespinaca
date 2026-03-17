import React, { InputHTMLAttributes } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  fullWidth?: boolean;
  className?: string;
  labelClassName?: string;
  inputClassName?: string;
  icon?: React.ReactNode;
}

const Input = React.memo(function Input({
  label,
  error,
  fullWidth = true,
  className = '',
  labelClassName = '',
  inputClassName = '',
  icon,
  ...props
}: InputProps) {
  // Base classes
  const containerClasses = `${fullWidth ? 'w-full' : ''} ${className}`;
  const labelClasses = `block text-sm font-medium text-gray-700 mb-1 ${labelClassName}`;
  const inputClasses = `px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 ${error ? 'border-red-500' : 'border-gray-300'} ${inputClassName}`;
  const inputWrapperClasses = 'relative';
   
  return (
    <div className={containerClasses}>
      {label && (
        <label htmlFor={props.id} className={labelClasses}>
          {label}
        </label>
      )}
      
      <div className={inputWrapperClasses}>
        <input
          className={`${inputClasses} ${icon ? 'pl-10' : ''} ${fullWidth ? 'w-full' : ''}`}
          {...props}
        />
        
        {icon && (
          <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-gray-500">
            {icon}
          </div>
        )}
      </div>
      
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  );
});

export default Input;