import React from 'react';
import { theme } from './theme';

export interface InputProps {
  label?: string;
  placeholder?: string;
  value?: string;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  required?: boolean;
  error?: string;
  helperText?: string;
  fullWidth?: boolean;
  id?: string;
  name?: string;
  autoComplete?: string;
  className?: string;
  style?: React.CSSProperties;
}

const getInputStyles = (
  size: InputProps['size'] = 'md',
  disabled = false,
  hasError = false,
  fullWidth = false
) => {
  const baseStyles: React.CSSProperties = {
    fontFamily: theme.typography.fontFamily.primary,
    border: `1px solid ${hasError ? theme.colors.error[500] : theme.colors.gray[300]}`,
    borderRadius: theme.borderRadius.base,
    outline: 'none',
    transition: theme.transitions.normal,
    backgroundColor: disabled ? theme.colors.gray[50] : theme.colors.white,
    color: disabled ? theme.colors.gray[500] : theme.colors.gray[900],
    width: fullWidth ? '100%' : 'auto',
    boxSizing: 'border-box' as const,
  };

  const sizeStyles: Record<string, React.CSSProperties> = {
    sm: {
      fontSize: theme.typography.fontSize.sm,
      padding: `${theme.spacing[2]} ${theme.spacing[3]}`,
      minHeight: '2rem',
    },
    md: {
      fontSize: theme.typography.fontSize.base,
      padding: `${theme.spacing[3]} ${theme.spacing[4]}`,
      minHeight: '2.5rem',
    },
    lg: {
      fontSize: theme.typography.fontSize.lg,
      padding: `${theme.spacing[4]} ${theme.spacing[4]}`,
      minHeight: '3rem',
    },
  };

  return {
    ...baseStyles,
    ...sizeStyles[size],
  };
};

const getLabelStyles = (size: InputProps['size'] = 'md', hasError = false) => {
  return {
    display: 'block',
    marginBottom: theme.spacing[2],
    fontSize: size === 'sm' ? theme.typography.fontSize.sm : theme.typography.fontSize.base,
    fontWeight: theme.typography.fontWeight.medium,
    color: hasError ? theme.colors.error[600] : theme.colors.gray[700],
  };
};

const getHelperTextStyles = (hasError = false) => {
  return {
    marginTop: theme.spacing[1],
    fontSize: theme.typography.fontSize.sm,
    color: hasError ? theme.colors.error[600] : theme.colors.gray[600],
  };
};

export const Input: React.FC<InputProps> = ({
  label,
  placeholder,
  value,
  onChange,
  onFocus,
  onBlur,
  type = 'text',
  size = 'md',
  disabled = false,
  required = false,
  error,
  helperText,
  fullWidth = false,
  id,
  name,
  autoComplete,
  className,
  style,
  ...props
}) => {
  const inputId = id || name || `input-${Math.random().toString(36).substr(2, 9)}`;
  const hasError = Boolean(error);
  
  const inputStyles = getInputStyles(size, disabled, hasError, fullWidth);
  const labelStyles = getLabelStyles(size, hasError);
  const helperStyles = getHelperTextStyles(hasError);

  return (
    <div 
      className={className}
      style={{ 
        width: fullWidth ? '100%' : 'auto',
        ...style 
      }}
    >
      {label && (
        <label 
          htmlFor={inputId}
          style={labelStyles}
        >
          {label}
          {required && (
            <span style={{ color: theme.colors.error[500], marginLeft: theme.spacing[1] }}>
              *
            </span>
          )}
        </label>
      )}
      
      <input
        id={inputId}
        name={name}
        type={type}
        value={value}
        onChange={onChange}
        onFocus={(e) => {
          if (!disabled) {
            e.target.style.borderColor = hasError 
              ? theme.colors.error[500] 
              : theme.colors.secondary[500];
            e.target.style.boxShadow = `0 0 0 2px ${
              hasError ? theme.colors.error[500] : theme.colors.secondary[500]
            }20`;
          }
          onFocus?.(e);
        }}
        onBlur={(e) => {
          e.target.style.borderColor = hasError 
            ? theme.colors.error[500] 
            : theme.colors.gray[300];
          e.target.style.boxShadow = 'none';
          onBlur?.(e);
        }}
        placeholder={placeholder}
        disabled={disabled}
        required={required}
        autoComplete={autoComplete}
        style={inputStyles}
        {...props}
      />
      
      {(error || helperText) && (
        <div style={helperStyles}>
          {error || helperText}
        </div>
      )}
    </div>
  );
};