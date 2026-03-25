'use client';

import React from 'react';

export type BiTabId = 'cost-config' | 'consumption' | 'production' | 'profitability';

interface BiTab {
  id: BiTabId;
  label: string;
  description: string;
}

interface BiTabsProps {
  activeTab: BiTabId;
  onTabChange: (tab: BiTabId) => void;
}

const tabs: BiTab[] = [
  { id: 'cost-config', label: 'Costos', description: 'Configuración de precios' },
  { id: 'consumption', label: 'Consumo', description: 'Registro manual' },
  { id: 'production', label: 'Producción', description: 'Cosechas' },
  { id: 'profitability', label: 'Rentabilidad', description: 'Análisis financiero' },
];

const BiTabs = React.memo(function BiTabs({ activeTab, onTabChange }: BiTabsProps) {
  return (
    <div className="border-b border-gray-200 mb-6">
      <nav className="flex gap-0 overflow-x-auto scrollbar-hidden" aria-label="Tabs de BI">
        {tabs.map((tab) => {
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => onTabChange(tab.id)}
              className={`
                flex-shrink-0 px-4 py-3 text-sm font-medium font-inter border-b-2 transition-colors
                ${isActive
                  ? 'border-hidro-green-primary text-hidro-green-primary'
                  : 'border-transparent text-gray-500 hover:text-green-600 hover:border-green-300'
                }
              `}
              role="tab"
              aria-selected={isActive}
            >
              <span className="block">{tab.label}</span>
              <span className={`block text-xs mt-0.5 ${isActive ? 'text-green-600' : 'text-gray-400'}`}>
                {tab.description}
              </span>
            </button>
          );
        })}
      </nav>
    </div>
  );
});

export default BiTabs;
