'use client';

import React from 'react';
import { MetricData } from './DashboardMonitoreo';
import { 
  ThermometerIcon, 
  DropletIcon, 
  SunIcon, 
  BoltIcon,
  TrendingUpIcon,
  TrendingDownIcon,
  MinusIcon
} from '@/components/ui/icons/Icons';

interface MetricsGridProps {
  metrics: MetricData[];
}

const MetricsGrid: React.FC<MetricsGridProps> = ({ metrics }) => {
  const getIcon = (iconType: string) => {
    const iconProps = { className: "w-6 h-6" };
    
    switch (iconType) {
      case 'temperature':
        return <ThermometerIcon {...iconProps} />;
      case 'humidity':
        return <DropletIcon {...iconProps} />;
      case 'ph':
        return <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center text-xs font-bold text-blue-600">pH</div>;
      case 'sun':
        return <SunIcon {...iconProps} />;
      case 'electric':
        return <BoltIcon {...iconProps} />;
      default:
        return <div className="w-6 h-6 rounded-full bg-gray-100"></div>;
    }
  };

  const getTrendIcon = (trend: string) => {
    const iconProps = { className: "w-4 h-4" };
    
    switch (trend) {
      case 'up':
        return <TrendingUpIcon {...iconProps} />;
      case 'down':
        return <TrendingDownIcon {...iconProps} />;
      case 'stable':
        return <MinusIcon {...iconProps} />;
      default:
        return <MinusIcon {...iconProps} />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'optimal':
        return 'text-green-600 bg-green-50 border-green-200';
      case 'warning':
        return 'text-yellow-600 bg-yellow-50 border-yellow-200';
      case 'critical':
        return 'text-red-600 bg-red-50 border-red-200';
      default:
        return 'text-gray-600 bg-gray-50 border-gray-200';
    }
  };

  const getTrendColor = (trend: string) => {
    switch (trend) {
      case 'up':
        return 'text-green-600';
      case 'down':
        return 'text-red-600';
      case 'stable':
        return 'text-gray-600';
      default:
        return 'text-gray-600';
    }
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
      {metrics.map((metric, index) => (
        <article 
          key={index}
          className={`hidro-card border-l-4 ${getStatusColor(metric.status)}`}
          role="region"
          aria-labelledby={`metric-${index}-title`}
        >
          <header className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <div className={`p-2 rounded-lg ${getStatusColor(metric.status)}`}>
                {getIcon(metric.iconType)}
              </div>
              <h3 
                id={`metric-${index}-title`}
                className="text-sm font-medium text-gray-700 font-inter"
              >
                {metric.title}
              </h3>
            </div>
          </header>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-1">
              <span 
                className="text-2xl font-bold text-gray-900 font-inter"
                aria-label={`Valor actual: ${metric.value} ${metric.unit}`}
              >
                {metric.value}
              </span>
              {metric.unit && (
                <span className="text-sm text-gray-500 font-inter">
                  {metric.unit}
                </span>
              )}
            </div>
            
            <div className="flex items-center gap-1">
              <div className={`flex items-center gap-1 ${getTrendColor(metric.trend)}`}>
                {getTrendIcon(metric.trend)}
                <span className="text-xs font-medium font-inter">
                  {metric.change}
                </span>
              </div>
              <span className="text-xs text-gray-500 font-inter">
                vs. anterior
              </span>
            </div>
          </div>
          
          {/* Indicador de estado */}
          <div className="mt-3 pt-3 border-t border-gray-100">
            <div className="flex items-center gap-2">
              <div className={`w-2 h-2 rounded-full ${
                metric.status === 'optimal' ? 'bg-green-500' :
                metric.status === 'warning' ? 'bg-yellow-500' :
                'bg-red-500'
              }`}></div>
              <span className="text-xs font-medium text-gray-600 font-inter capitalize">
                {metric.status === 'optimal' ? 'Óptimo' :
                 metric.status === 'warning' ? 'Advertencia' :
                 'Crítico'}
              </span>
            </div>
          </div>
        </article>
      ))}
    </div>
  );
};

export default MetricsGrid;