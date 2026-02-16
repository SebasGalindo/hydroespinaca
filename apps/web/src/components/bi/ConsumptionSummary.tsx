'use client';

import React from 'react';
import StatCard from '@/components/ui/StatCard';
import { BoltIcon, DropletIcon, PlantIcon, CalculatorIcon } from '@/components/ui/icons/Icons';
import { formatCurrency } from '@hydroespinaca/shared';
import type { BiSummary } from '@hydroespinaca/shared';

interface ConsumptionSummaryProps {
  summary: BiSummary;
}

const ConsumptionSummary: React.FC<ConsumptionSummaryProps> = ({ summary }) => {
  return (
    <div>
      <h3 className="text-sm font-semibold text-gray-700 mb-3 font-inter">
        Resumen del Período
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <StatCard
          label="Electricidad"
          value={summary.totalElectricityKwh.toFixed(1)}
          unit="kWh"
          icon={<BoltIcon size={20} />}
          variant="warning"
          trendLabel={formatCurrency(summary.costElectricity, summary.currency)}
          trend="neutral"
        />
        <StatCard
          label="Agua"
          value={summary.totalWaterLiters.toFixed(1)}
          unit="L"
          icon={<DropletIcon size={20} />}
          variant="info"
          trendLabel={formatCurrency(summary.costWater, summary.currency)}
          trend="neutral"
        />
        <StatCard
          label="Nutrientes"
          value={summary.totalNutrientLiters.toFixed(1)}
          unit="L"
          icon={<PlantIcon size={20} />}
          variant="success"
          trendLabel={formatCurrency(summary.costNutrients, summary.currency)}
          trend="neutral"
        />
        <StatCard
          label="Costo Total"
          value={formatCurrency(summary.costTotal, summary.currency)}
          icon={<CalculatorIcon size={20} />}
          variant="error"
        />
      </div>
    </div>
  );
};

export default ConsumptionSummary;
