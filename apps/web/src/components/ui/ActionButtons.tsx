import React from 'react';
import { SaveIcon, RefreshIcon } from '@/components/ui/icons/Icons';

interface ActionButtonsProps {
  onPrimary: () => void;
  onSecondary: () => void;
  primaryText?: string;
  secondaryText?: string;
  isLoading?: boolean;
  primaryIcon?: 'save' | 'submit' | 'add';
  secondaryIcon?: 'cancel' | 'reset' | 'back';
  layout?: 'horizontal' | 'vertical';
  className?: string;
}

const ActionButtons = React.memo(function ActionButtons({
  onPrimary,
  onSecondary,
  primaryText = 'Guardar',
  secondaryText = 'Cancelar',
  isLoading = false,
  primaryIcon = 'save',
  secondaryIcon = 'cancel',
  layout = 'horizontal',
  className = ''
}: ActionButtonsProps) {
  const getPrimaryIcon = () => {
    // All cases return the same icon, so we can simplify
    return <SaveIcon size={16} />;
  };

  const getSecondaryIcon = () => {
    // All cases return the same icon, so we can simplify
    return <RefreshIcon size={16} />;
  };

  const containerClasses = layout === 'vertical' 
    ? 'flex flex-col space-y-3'
    : 'flex flex-col sm:flex-row sm:space-x-4 sm:space-y-0 space-y-3';

  return (
    <div className={`${containerClasses} ${className}`}>
      {/* Botón secundario */}
      <button
        type="button"
        onClick={onSecondary}
        disabled={isLoading}
        className="hidro-button-secondary flex items-center justify-center gap-2 font-inter disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {getSecondaryIcon()}
        {secondaryText}
      </button>
      
      {/* Botón primario */}
      <button
        type="button"
        onClick={onPrimary}
        disabled={isLoading}
        className="hidro-button-primary flex items-center justify-center gap-2 font-inter disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {isLoading ? (
          <>
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            Guardando...
          </>
        ) : (
          <>
            {getPrimaryIcon()}
            {primaryText}
          </>
        )}
      </button>
    </div>
  );
});

export default ActionButtons;