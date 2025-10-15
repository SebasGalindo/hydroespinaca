import React from 'react';
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

interface VariableCardProps {
  title: string;
  value: string;
  optimal?: string;
  subtitle?: string;
  iconType: 'temperature' | 'humidity' | 'ph' | 'light' | 'sun' | 'electric' | 'ruler' | 'water';
  status: 'optimal' | 'warning' | 'error' | 'manual';
  className?: string;
}

const VariableCard: React.FC<VariableCardProps> = ({
  title,
  value,
  optimal,
  subtitle,
  iconType,
  status,
  className = ''
}) => {
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
    const iconProps = { size: 20, color: '#6b7280' };
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

  return (
    <article className={`hidro-card p-4 flex flex-col h-full ${className}`}>
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

      <div className="mb-2 flex-grow">
        <p className="text-2xl font-bold text-gray-900 font-inter">{value}</p>
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
};

export default VariableCard;