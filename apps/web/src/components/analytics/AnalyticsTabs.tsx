'use client';

import React from 'react';

export type AnalyticsLevel = 'environmental' | 'actuators';

interface Tab {
  id: AnalyticsLevel;
  label: string;
  description: string;
}

const tabs: Tab[] = [
  {
    id: 'environmental',
    label: 'Nivel 1',
    description: 'Condiciones Ambientales',
  },
  {
    id: 'actuators',
    label: 'Nivel 2',
    description: 'Actividad de Actuadores',
  },
];

interface AnalyticsTabsProps {
  activeTab: AnalyticsLevel;
  onTabChange: (tab: AnalyticsLevel) => void;
}

export default function AnalyticsTabs({ activeTab, onTabChange }: AnalyticsTabsProps) {
  return (
    <div className="mb-6">
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-4 overflow-x-auto" aria-label="Tabs">
          {tabs.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                onClick={() => onTabChange(tab.id)}
                className={`
                  whitespace-nowrap py-4 px-6 border-b-2 font-medium text-sm transition-colors
                  ${
                    isActive
                      ? 'border-hidro-green-primary text-hidro-green-primary hover:border-hidro-green-dark hover:text-hidro-green-dark'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }
                `}
                aria-current={isActive ? 'page' : undefined}
              >
                <div className="flex flex-col items-start">
                  <span className="font-semibold">{tab.label}</span>
                  <span className="text-xs mt-0.5 opacity-80">{tab.description}</span>
                </div>
              </button>
            );
          })}
        </nav>
      </div>
    </div>
  );
}
