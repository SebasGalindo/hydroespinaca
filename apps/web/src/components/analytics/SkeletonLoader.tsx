import React from 'react';
export const ChartSkeleton = React.memo(function ChartSkeleton() {
  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 animate-pulse">
      <div className="h-6 bg-gray-200 rounded w-1/3 mb-4"></div>
      <div className="h-80 bg-gray-100 rounded"></div>
      <div className="mt-4 h-12 bg-gray-100 rounded"></div>
    </div>
  );
});

export function LevelSkeleton() {
  return (
    <div className="space-y-6">
      <div className="h-16 bg-blue-50 rounded animate-pulse"></div>
      <ChartSkeleton />
      <ChartSkeleton />
    </div>
  );
}
