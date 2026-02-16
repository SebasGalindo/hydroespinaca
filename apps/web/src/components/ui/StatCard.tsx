'use client';

import React from 'react';
import BaseCard from './BaseCard';

interface StatCardProps {
  label: string;
  value: string | number;
  unit?: string;
  icon?: React.ReactNode;
  trend?: 'up' | 'down' | 'neutral';
  trendLabel?: string;
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
  className?: string;
}

const variantStyles = {
  default: { bg: 'bg-gray-50', text: 'text-gray-700', icon: 'text-gray-500' },
  success: { bg: 'bg-green-50', text: 'text-green-700', icon: 'text-green-500' },
  warning: { bg: 'bg-yellow-50', text: 'text-yellow-700', icon: 'text-yellow-500' },
  error: { bg: 'bg-red-50', text: 'text-red-700', icon: 'text-red-500' },
  info: { bg: 'bg-blue-50', text: 'text-blue-700', icon: 'text-blue-500' },
};

const trendIcons = {
  up: '↑',
  down: '↓',
  neutral: '→',
};

const trendColors = {
  up: 'text-green-600',
  down: 'text-red-600',
  neutral: 'text-gray-500',
};

const StatCard: React.FC<StatCardProps> = ({
  label,
  value,
  unit,
  icon,
  trend,
  trendLabel,
  variant = 'default',
  className = '',
}) => {
  const styles = variantStyles[variant];

  return (
    <BaseCard padding="sm" hover={false} className={`${styles.bg} ${className}`}>
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <p className="text-xs font-medium text-gray-500 font-inter uppercase tracking-wider truncate">
            {label}
          </p>
          <div className="mt-1 flex items-baseline gap-1">
            <p className={`text-2xl font-bold ${styles.text} font-inter`}>
              {value}
            </p>
            {unit && (
              <span className="text-sm text-gray-500 font-inter">{unit}</span>
            )}
          </div>
          {trend && trendLabel && (
            <p className={`mt-1 text-xs font-medium ${trendColors[trend]} font-inter`}>
              {trendIcons[trend]} {trendLabel}
            </p>
          )}
        </div>
        {icon && (
          <div className={`flex-shrink-0 p-2 rounded-lg ${styles.icon}`}>
            {icon}
          </div>
        )}
      </div>
    </BaseCard>
  );
};

export default StatCard;
