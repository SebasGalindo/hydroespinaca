'use client';

import React from 'react';
import {
  FUZZY_STATUS_LABELS,
  FUZZY_STATUS_COLORS,
  type FuzzySystemStatus,
} from '@hydroespinaca/shared';

interface SystemStatusBadgeProps {
  status: FuzzySystemStatus;
  size?: 'sm' | 'md' | 'lg';
  showIcon?: boolean;
}

const statusDotColors: Record<string, string> = {
  green: 'bg-green-500',
  red: 'bg-red-500',
  yellow: 'bg-yellow-500',
  gray: 'bg-gray-400',
};

const statusBgColors: Record<string, string> = {
  green: 'bg-green-50 text-green-700 border-green-200',
  red: 'bg-red-50 text-red-700 border-red-200',
  yellow: 'bg-yellow-50 text-yellow-700 border-yellow-200',
  gray: 'bg-gray-50 text-gray-700 border-gray-200',
};

const sizeMap = {
  sm: { container: 'px-2 py-0.5 text-xs', dot: 'w-1.5 h-1.5', gap: 'gap-1' },
  md: { container: 'px-3 py-1 text-sm', dot: 'w-2 h-2', gap: 'gap-1.5' },
  lg: { container: 'px-4 py-1.5 text-base', dot: 'w-2.5 h-2.5', gap: 'gap-2' },
};

const SystemStatusBadge = React.memo(function SystemStatusBadge({
  status,
  size = 'md',
  showIcon = true,
}: SystemStatusBadgeProps) {
  const colorKey = FUZZY_STATUS_COLORS[status] ?? 'gray';
  const label = FUZZY_STATUS_LABELS[status] ?? status;
  const sz = sizeMap[size];

  return (
    <span
      className={`
        inline-flex items-center font-medium rounded-full border font-inter
        ${statusBgColors[colorKey] ?? statusBgColors.gray}
        ${sz.container}
        ${showIcon ? sz.gap : ''}
      `}
      role="status"
      aria-label={`Estado: ${label}`}
    >
      {showIcon && (
        <span
          className={`rounded-full flex-shrink-0 ${statusDotColors[colorKey] ?? statusDotColors.gray} ${sz.dot}`}
          aria-hidden="true"
        />
      )}
      {label}
    </span>
  );
});

export default SystemStatusBadge;
