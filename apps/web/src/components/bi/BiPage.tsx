'use client';

import React, { useState } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import PageHeader from '@/components/ui/PageHeader';
import BiTabs, { type BiTabId } from './BiTabs';
import CostConfigSection from './CostConfigSection';
import ConsumptionSection from './ConsumptionSection';
import ProductionSection from './ProductionSection';
import ProfitabilitySection from './ProfitabilitySection';

const BiPage = React.memo(function BiPage() {
  const [activeTab, setActiveTab] = useState<BiTabId>('cost-config');

  return (
    <PageLayout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <PageHeader
          title="Consumo y Costos"
          subtitle="Gestión de costos, consumo, producción y análisis de rentabilidad"
          alignment="left"
        />

        <BiTabs activeTab={activeTab} onTabChange={setActiveTab} />

        <div className="mt-2">
          {activeTab === 'cost-config' && <CostConfigSection />}
          {activeTab === 'consumption' && <ConsumptionSection />}
          {activeTab === 'production' && <ProductionSection />}
          {activeTab === 'profitability' && <ProfitabilitySection />}
        </div>
      </div>
    </PageLayout>
  );
});

export default BiPage;
