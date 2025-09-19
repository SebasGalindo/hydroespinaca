import React from 'react';
import { theme } from './theme';

export interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  onClick?: (event: React.MouseEvent<HTMLButtonElement>) => void;
  type?: 'button' | 'submit' | 'reset';
  className?: string;
  style?: React.CSSProperties;
}

const getButtonStyles = (
  variant: ButtonProps['variant'] = 'primary',
  size: ButtonProps['size'] = 'md',
  disabled = false,
  loading = false,
  fullWidth = false
) => {
  const baseStyles: React.CSSProperties = {
    fontFamily: theme.typography.fontFamily.primary,
    border: 'none',
    borderRadius: theme.borderRadius.base,
    cursor: disabled || loading ? 'not-allowed' : 'pointer',
    transition: theme.transitions.normal,
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontWeight: theme.typography.fontWeight.medium,
    outline: 'none',
    textDecoration: 'none',
    width: fullWidth ? '100%' : 'auto',
    opacity: disabled || loading ? 0.6 : 1,
    position: 'relative',
  };

  // Size styles
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
      padding: `${theme.spacing[4]} ${theme.spacing[6]}`,
      minHeight: '3rem',
    },
  };

  // Variant styles
  const variantStyles: Record<string, React.CSSProperties> = {
    primary: {
      backgroundColor: theme.colors.secondary[500],
      color: theme.colors.white,
      border: `1px solid ${theme.colors.secondary[500]}`,
    },
    secondary: {
      backgroundColor: theme.colors.gray[100],
      color: theme.colors.gray[900],
      border: `1px solid ${theme.colors.gray[300]}`,
    },
    outline: {
      backgroundColor: 'transparent',
      color: theme.colors.secondary[600],
      border: `1px solid ${theme.colors.secondary[500]}`,
    },
    ghost: {
      backgroundColor: 'transparent',
      color: theme.colors.secondary[600],
      border: '1px solid transparent',
    },
  };

  return {
    ...baseStyles,
    ...sizeStyles[size],
    ...variantStyles[variant],
  };
};

const getHoverStyles = (variant: ButtonProps['variant'] = 'primary') => {
  const hoverStyles: Record<string, React.CSSProperties> = {
    primary: {
      backgroundColor: theme.colors.secondary[600],
    },
    secondary: {
      backgroundColor: theme.colors.gray[200],
    },
    outline: {
      backgroundColor: theme.colors.secondary[50],
    },
    ghost: {
      backgroundColor: theme.colors.secondary[50],
    },
  };

  return hoverStyles[variant];
};

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  disabled = false,
  loading = false,
  fullWidth = false,
  onClick,
  type = 'button',
  className,
  style,
  ...props
}) => {
  const buttonStyles = getButtonStyles(variant, size, disabled, loading, fullWidth);
  const hoverStyles = getHoverStyles(variant);

  return (
    <button
      type={type}
      disabled={disabled || loading}
      onClick={onClick}
      className={className}
      style={{
        ...buttonStyles,
        ...style,
      }}
      onMouseOver={(e) => {
        if (!disabled && !loading) {
          Object.assign(e.currentTarget.style, hoverStyles);
        }
      }}
      onMouseOut={(e) => {
        if (!disabled && !loading) {
          Object.assign(e.currentTarget.style, {
            backgroundColor: buttonStyles.backgroundColor,
          });
        }
      }}
      onFocus={(e) => {
        if (!disabled && !loading) {
          e.currentTarget.style.boxShadow = `0 0 0 2px ${theme.colors.secondary[500]}40`;
        }
      }}
      onBlur={(e) => {
        e.currentTarget.style.boxShadow = 'none';
      }}
      {...props}
    >
      {loading && (
        <span
          style={{
            marginRight: theme.spacing[2],
            width: '1rem',
            height: '1rem',
            border: '2px solid transparent',
            borderTop: '2px solid currentColor',
            borderRadius: '50%',
            animation: 'spin 1s linear infinite',
          }}
        />
      )}
      {children}
      
      <style>
        {`
          @keyframes spin {
            to {
              transform: rotate(360deg);
            }
          }
        `}
      </style>
    </button>
  );
};