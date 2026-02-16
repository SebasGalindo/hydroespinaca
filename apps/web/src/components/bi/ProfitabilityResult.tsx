'use client';

import React from 'react';
import BaseCard from '@/components/ui/BaseCard';
import Badge from '@/components/ui/Badge';
import StatCard from '@/components/ui/StatCard';
import { formatCurrency, formatDate, CONSUMPTION_TYPE_LABELS } from '@hydroespinaca/shared';
import type { ProfitabilityResponse, ConsumptionType } from '@hydroespinaca/shared';
import {
  TrendingUpIcon,
  TrendingDownIcon,
  BoltIcon,
  CalculatorIcon,
  PlantIcon,
} from '@/components/ui/icons/Icons';

interface ProfitabilityResultProps {
  data: ProfitabilityResponse;
}

const ProfitabilityResult: React.FC<ProfitabilityResultProps> = ({ data }) => {
  const isProfit = data.netBenefit >= 0;

  return (
    <div className="space-y-6">
      {/* Net result banner */}
      <BaseCard
        padding="md"
        hover={false}
        className={isProfit ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'}
      >
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-gray-600 font-inter">Resultado Neto</p>
            <p className={`text-3xl font-bold font-inter ${isProfit ? 'text-green-700' : 'text-red-700'}`}>
              {formatCurrency(data.netBenefit, data.currency)}
            </p>
            <p className="text-sm text-gray-500 font-inter mt-1">
              Margen: {data.profitMarginPercent.toFixed(1)}%
            </p>
          </div>
          <div className={`p-3 rounded-full ${isProfit ? 'bg-green-100' : 'bg-red-100'}`}>
            {isProfit ? (
              <TrendingUpIcon size={32} className="text-green-600" />
            ) : (
              <TrendingDownIcon size={32} className="text-red-600" />
            )}
          </div>
        </div>
      </BaseCard>

      {/* Production info */}
      <div>
        <h4 className="text-sm font-semibold text-gray-700 mb-3 font-inter">Producción</h4>
        <BaseCard padding="sm" hover={false} className="bg-white">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-gray-500 font-inter">Cultivo</p>
              <p className="font-semibold text-gray-900 font-inter">{data.production.cropName}</p>
            </div>
            <div>
              <p className="text-gray-500 font-inter">Período</p>
              <p className="font-semibold text-gray-900 font-inter">
                {formatDate(data.production.startDate)} — {formatDate(data.production.harvestDate)}
              </p>
            </div>
            <div>
              <p className="text-gray-500 font-inter">Producción</p>
              <p className="font-semibold text-gray-900 font-inter">
                {data.production.kilosProduced.toFixed(1)} kg
              </p>
            </div>
            <div>
              <p className="text-gray-500 font-inter">Precio/kg</p>
              <p className="font-semibold text-gray-900 font-inter">
                {formatCurrency(data.production.pricePerKilo, data.currency)}
              </p>
            </div>
          </div>
        </BaseCard>
      </div>

      {/* Revenue */}
      <div>
        <h4 className="text-sm font-semibold text-gray-700 mb-3 font-inter">Ingresos</h4>
        <StatCard
          label="Ingreso Total"
          value={formatCurrency(data.revenue.totalRevenue, data.currency)}
          icon={<PlantIcon size={20} />}
          variant="success"
        />
      </div>

      {/* Expenses breakdown */}
      <div>
        <h4 className="text-sm font-semibold text-gray-700 mb-3 font-inter">Gastos</h4>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-4">
          <StatCard
            label="Costos Operacionales"
            value={formatCurrency(data.expenses.totalOperationalCost, data.currency)}
            icon={<BoltIcon size={20} />}
            variant="warning"
          />
          <StatCard
            label="Consumo Manual"
            value={formatCurrency(data.expenses.totalManualConsumptionCost, data.currency)}
            icon={<CalculatorIcon size={20} />}
            variant="info"
          />
          <StatCard
            label="Gastos Totales"
            value={formatCurrency(data.expenses.totalExpenses, data.currency)}
            icon={<CalculatorIcon size={20} />}
            variant="error"
          />
        </div>

        {/* Operational cost detail */}
        {data.expenses.operationalCosts.length > 0 && (
          <BaseCard padding="sm" hover={false} className="bg-white mb-3">
            <p className="text-xs font-semibold text-gray-600 mb-2 font-inter uppercase">
              Detalle Costos Operacionales (actuadores)
            </p>
            <div className="space-y-1">
              {data.expenses.operationalCosts.map((item) => (
                <div
                  key={item.actuatorCode}
                  className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100 last:border-b-0"
                >
                  <div className="flex items-center gap-2">
                    <Badge variant="warning" size="sm">{item.actuatorCode}</Badge>
                    <span className="text-gray-500">{item.estimatedKwh.toFixed(2)} kWh</span>
                  </div>
                  <span className="font-medium text-gray-700">
                    {formatCurrency(item.cost, data.currency)}
                  </span>
                </div>
              ))}
            </div>
          </BaseCard>
        )}

        {/* Manual consumption detail */}
        {data.expenses.manualConsumptionCosts.length > 0 && (
          <BaseCard padding="sm" hover={false} className="bg-white">
            <p className="text-xs font-semibold text-gray-600 mb-2 font-inter uppercase">
              Detalle Consumo Manual
            </p>
            <div className="space-y-1">
              {data.expenses.manualConsumptionCosts.map((item) => (
                <div
                  key={item.type}
                  className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100 last:border-b-0"
                >
                  <div className="flex items-center gap-2">
                    <Badge
                      variant={item.type === 1 ? 'warning' : item.type === 2 ? 'info' : 'success'}
                      size="sm"
                    >
                      {CONSUMPTION_TYPE_LABELS[item.type as ConsumptionType]}
                    </Badge>
                    <span className="text-gray-500">{item.totalAmount.toFixed(2)}</span>
                  </div>
                  <span className="font-medium text-gray-700">
                    {formatCurrency(item.totalCost, data.currency)}
                  </span>
                </div>
              ))}
            </div>
          </BaseCard>
        )}
      </div>
    </div>
  );
};

export default ProfitabilityResult;
