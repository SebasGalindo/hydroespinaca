import React from 'react';

export type StatusType = 'active' | 'inactive' | 'warning' | 'error';

interface StatusBadgeProps {
  status: StatusType;
  text?: string;
  size?: 'sm' | 'md' | 'lg';
  showIcon?: boolean;
}

const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  text,
  size = 'md',
  showIcon = true
}) => {
  const getStatusConfig = () => {
    switch (status) {
      case 'active':
        return {
          bgColor: 'bg-green-50',
          textColor: 'text-green-700',
          borderColor: 'border-green-200',
          dotColor: 'bg-green-500',
          defaultText: 'Activo'
        };
      case 'inactive':
        return {
          bgColor: 'bg-gray-50',
          textColor: 'text-gray-700',
          borderColor: 'border-gray-200',
          dotColor: 'bg-gray-400',
          defaultText: 'Inactivo'
        };
      case 'warning':
        return {
          bgColor: 'bg-yellow-50',
          textColor: 'text-yellow-700',
          borderColor: 'border-yellow-200',
          dotColor: 'bg-yellow-500',
          defaultText: 'Advertencia'
        };
      case 'error':
        return {
          bgColor: 'bg-red-50',
          textColor: 'text-red-700',
          borderColor: 'border-red-200',
          dotColor: 'bg-red-500',
          defaultText: 'Error'
        };
      default:
        return {
          bgColor: 'bg-gray-50',
          textColor: 'text-gray-700',
          borderColor: 'border-gray-200',
          dotColor: 'bg-gray-400',
          defaultText: 'Desconocido'
        };
    }
  };

  const getSizeClasses = () => {
    switch (size) {
      case 'sm':
        return {
          container: 'px-2 py-1 text-xs',
          dot: 'w-1.5 h-1.5',
          spacing: 'gap-1'
        };
      case 'md':
        return {
          container: 'px-3 py-1.5 text-sm',
          dot: 'w-2 h-2',
          spacing: 'gap-1.5'
        };
      case 'lg':
        return {
          container: 'px-4 py-2 text-base',
          dot: 'w-2.5 h-2.5',
          spacing: 'gap-2'
        };
      default:
        return {
          container: 'px-3 py-1.5 text-sm',
          dot: 'w-2 h-2',
          spacing: 'gap-1.5'
        };
    }
  };

  const statusConfig = getStatusConfig();
  const sizeClasses = getSizeClasses();
  const displayText = text || statusConfig.defaultText;

  return (
    <span
      className={`
        inline-flex items-center font-medium rounded-full border font-inter
        ${statusConfig.bgColor}
        ${statusConfig.textColor}
        ${statusConfig.borderColor}
        ${sizeClasses.container}
        ${showIcon ? sizeClasses.spacing : ''}
      `}
      role="status"
      aria-label={`Estado: ${displayText}`}
    >
      {showIcon && (
        <span
          className={`
            rounded-full flex-shrink-0
            ${statusConfig.dotColor}
            ${sizeClasses.dot}
          `}
          aria-hidden="true"
        />
      )}
      {displayText}
    </span>
  );
};

export default StatusBadge;