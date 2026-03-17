import React from 'react';
import { IconType } from '@hydroespinaca/shared/types/common';

import {
  CheckIcon,
  AlertTriangleIcon,
  XIcon,
  EditIcon,
  TemperatureIcon,
  HumidityIcon,
  PhIcon,
  SunIcon,
  ElectricIcon,
  RulerIcon,
  WaterIcon
} from '@/components/ui/icons/Icons';

export type TrendDirection = 'up' | 'down' | 'stable';

interface VariableCardProps {
  title: string;
  value: string;
  optimal?: string;
  subtitle?: string;
  iconType: IconType;
  status: 'optimal' | 'warning' | 'error' | 'manual';
  className?: string;
  trend?: TrendDirection;
  artificialLightActive?: boolean;
  showArtificialLightAlert?: boolean;
}

const VariableCard = React.memo(function VariableCard({
  title,
  value,
  optimal,
  subtitle,
  iconType,
  status,
  className = '',
  trend,
  artificialLightActive = false,
  showArtificialLightAlert = false
}: VariableCardProps) {
  const getStatusColor = () => {
    switch (status) {
      case 'optimal':
        return 'text-green-600';
      case 'warning':
        return 'text-yellow-600';
      case 'error':
        return 'text-red-600';
      case 'manual':
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  };

  const getBorderColor = () => {
    switch (status) {
      case 'optimal':
        return 'border-green-600';
      case 'warning':
        return 'border-yellow-600';
      case 'error':
        return 'border-red-600';
      case 'manual':
        return 'border-blue-600';
      default:
        return 'border-gray-600';
    }
  };

  const getIconColor = () => {
    switch (status) {
      case 'optimal':
        return '#16a34a';
      case 'warning':
        return '#f59e0b';
      case 'error':
        return '#ef4444';
      case 'manual':
        return '#3b82f6';
      default:
        return '#6b7280';
    }
  };

  const getStatusIcon = () => {
    switch (status) {
      case 'optimal':
        return <CheckIcon size={16} color="#16a34a" />;
      case 'warning':
        return <AlertTriangleIcon size={16} color="#f59e0b" />;
      case 'error':
        return <XIcon size={16} color="#ef4444" />;
      case 'manual':
        return <EditIcon size={16} color="#3b82f6" />;
      default:
        return null;
    }
  };

  const getVariableIcon = () => {
    const iconProps = { size: 20, color: getIconColor() };
    switch (iconType) {
      case 'temperature':
        return <TemperatureIcon {...iconProps} />;
      case 'humidity':
        return <HumidityIcon {...iconProps} />;
      case 'ph':
        return <PhIcon {...iconProps} />;
      case 'light':
      case 'sun':
        return <SunIcon {...iconProps} />;
      case 'electric':
        return <ElectricIcon {...iconProps} />;
      case 'ruler':
        return <RulerIcon {...iconProps} />;
      case 'water':
        return <WaterIcon {...iconProps} />;
      default:
        return null;
    }
  };

  const getTrendIcon = () => {
    if (!trend || trend === 'stable') return null;

    if (trend === 'up') {
      return (
        <span className="text-green-600 text-xs" title="Incrementó respecto a la medición anterior">
          ↑
        </span>
      );
    }

    return (
      <span className="text-red-600 text-xs" title="Disminuyó respecto a la medición anterior">
        ↓
      </span>
    );
  };

  return (
    <article className={`hidro-card p-4 flex flex-col h-full ${className} relative border-l-4 ${getBorderColor()}`}>
      <header className="flex items-start justify-between mb-3">
        <h3 className="text-sm font-medium text-gray-700 font-inter">{title}</h3>
        <div className="flex items-center space-x-1">
          {getVariableIcon()}
          {status !== 'manual' && (
            <span className={`${getStatusColor()}`}>
              {getStatusIcon()}
            </span>
          )}
        </div>
      </header>

      <div className="mb-2 flex-grow flex items-baseline gap-2">
        <p className={`text-2xl font-bold font-inter ${getStatusColor()}`}>{value}</p>
        {getTrendIcon()}
      </div>

      {optimal && (
        <p className={`text-xs ${getStatusColor()} font-inter`}>
          {optimal}
        </p>
      )}

      {subtitle && (
        <p className="text-xs text-gray-500 font-inter">
          {subtitle}
        </p>
      )}
    </article>
  );
});

export default VariableCard;