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
    fontWeight: theme.typography.fontWeight.semibold,
    borderRadius: theme.borderRadius.base,
    border: 'none',
    cursor: disabled || loading ? 'not-allowed' : 'pointer',
    transition: theme.transitions.normal,
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: theme.spacing[2],
    textDecoration: 'none',
    outline: 'none',
    width: fullWidth ? '100%' : 'auto',
    opacity: disabled ? 0.6 : 1,
  };

  const variantStyles: Record<string, React.CSSProperties> = {
    primary: {
      backgroundColor: theme.colors.secondary[600],
      color: theme.colors.white,
      boxShadow: theme.shadows.sm,
    },
    secondary: {
      backgroundColor: theme.colors.gray[600],
      color: theme.colors.white,
      boxShadow: theme.shadows.sm,
    },
    outline: {
      backgroundColor: 'transparent',
      color: theme.colors.secondary[700],
      border: `1px solid ${theme.colors.secondary[300]}`,
    },
    ghost: {
      backgroundColor: 'transparent',
      color: theme.colors.gray[700],
    },
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
      padding: `${theme.spacing[4]} ${theme.spacing[6]}`,
      minHeight: '3rem',
    },
  };

  const hoverStyles: Record<string, React.CSSProperties> = {
    primary: {
      backgroundColor: theme.colors.secondary[700],
    },
    secondary: {
      backgroundColor: theme.colors.gray[700],
    },
    outline: {
      backgroundColor: theme.colors.secondary[50],
      borderColor: theme.colors.secondary[400],
    },
    ghost: {
      backgroundColor: theme.colors.gray[100],
    },
  };

  return {
    base: {
      ...baseStyles,
      ...variantStyles[variant],
      ...sizeStyles[size],
    },
    hover: hoverStyles[variant],
  };
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
  const styles = getButtonStyles(variant, size, disabled, loading, fullWidth);
  
  return (
    <button
      type={type}
      disabled={disabled || loading}
      onClick={disabled || loading ? undefined : onClick}
      className={className}
      style={{
        ...styles.base,
        ...style,
      }}
      onMouseEnter={(e) => {
        if (!disabled && !loading) {
          Object.assign(e.currentTarget.style, styles.hover);
        }
      }}
      onMouseLeave={(e) => {
        if (!disabled && !loading) {
          Object.assign(e.currentTarget.style, styles.base);
        }
      }}
      {...props}
    >
      {loading && (
        <span
          style={{
            display: 'inline-block',
            width: '1rem',
            height: '1rem',
            border: '2px solid currentColor',
            borderTop: '2px solid transparent',
            borderRadius: '50%',
            animation: 'spin 1s linear infinite',
          }}
        />
      )}
      {children}
    </button>
  );
};