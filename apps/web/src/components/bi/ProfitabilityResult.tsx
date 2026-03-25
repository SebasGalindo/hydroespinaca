'use client';

import React from 'react';
import BaseCard from '@/components/ui/BaseCard';
import Badge from '@/components/ui/Badge';
import StatCard from '@/components/ui/StatCard';
import { formatCurrency, formatDate, CONSUMPTION_TYPE_LABELS, CONSUMPTION_TYPE_UNITS } from '@hydroespinaca/shared';
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

const ProfitabilityResult = React.memo(function ProfitabilityResult({ data }: ProfitabilityResultProps) {
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
            <p className="text-base font-semibold text-gray-600 font-inter">Resultado Neto</p>
            <p className={`text-4xl font-bold font-inter ${isProfit ? 'text-green-700' : 'text-red-700'}`}>
              {formatCurrency(data.netBenefit, data.currency)}
            </p>
            <p className="text-base text-gray-500 font-inter mt-1">
              Margen: {data.profitMarginPercent.toFixed(1)}%
            </p>
          </div>
          <div className={`p-3 rounded-full ${isProfit ? 'bg-green-100' : 'bg-red-100'}`}>
            {isProfit ? (
              <TrendingUpIcon size={36} className="text-green-600" />
            ) : (
              <TrendingDownIcon size={36} className="text-red-600" />
            )}
          </div>
        </div>
      </BaseCard>

      {/* Production info */}
      <div>
        <h4 className="text-base font-semibold text-gray-700 mb-3 font-inter">Producción</h4>
        <BaseCard padding="sm" hover={false} className="bg-white">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
            <div className="text-center">
              <p className="text-sm text-gray-500 font-inter">Cultivo</p>
              <p className="text-base font-semibold text-gray-900 font-inter">{data.production.cropName}</p>
            </div>
            <div className="text-center">
              <p className="text-sm text-gray-500 font-inter">Período</p>
              <p className="text-base font-semibold text-gray-900 font-inter">
                {formatDate(data.production.startDate)} — {formatDate(data.production.harvestDate)}
              </p>
            </div>
            <div className="text-center">
              <p className="text-sm text-gray-500 font-inter">Producción</p>
              <p className="text-base font-semibold text-gray-900 font-inter">
                {data.production.kilosProduced.toFixed(1)} kg
              </p>
            </div>
            <div className="text-center">
              <p className="text-sm text-gray-500 font-inter">Precio/kg</p>
              <p className="text-base font-semibold text-gray-900 font-inter">
                {formatCurrency(data.production.pricePerKilo, data.currency)}
              </p>
            </div>
          </div>
        </BaseCard>
      </div>

      {/* Revenue */}
      <div>
        <h4 className="text-base font-semibold text-gray-700 mb-3 font-inter">Ingresos</h4>
        <StatCard
          label="Ingreso Total"
          value={formatCurrency(data.revenue.totalRevenue, data.currency)}
          icon={<PlantIcon size={24} />}
          variant="success"
        />
      </div>

      {/* Expenses breakdown */}
      <div>
        <h4 className="text-base font-semibold text-gray-700 mb-3 font-inter">Gastos</h4>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-4">
          <StatCard
            label="Costos Operacionales"
            value={formatCurrency(data.expenses.operationalCost?.totalOperationalCost ?? 0, data.currency)}
            icon={<BoltIcon size={24} />}
            variant="warning"
          />
          <StatCard
            label="Consumo Manual"
            value={formatCurrency(data.expenses.manualConsumptionCost?.totalManualCost ?? 0, data.currency)}
            icon={<CalculatorIcon size={24} />}
            variant="info"
          />
          <StatCard
            label="Gastos Totales"
            value={formatCurrency(data.expenses.totalExpenses, data.currency)}
            icon={<CalculatorIcon size={24} />}
            variant="error"
          />
        </div>

        {/* Operational cost detail — actuators */}
        {(data.expenses.operationalCost?.actuators?.length ?? 0) > 0 && (
          <BaseCard padding="sm" hover={false} className="bg-white mb-3">
            <p className="text-sm font-semibold text-gray-600 mb-2 font-inter uppercase">
              Detalle Costos Operacionales (actuadores)
            </p>
            <div className="space-y-1">
              {data.expenses.operationalCost.actuators.map((item) => (
                <div
                  key={item.actuatorCode}
                  className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100 last:border-b-0"
                >
                  <div className="flex items-center gap-2">
                    <Badge variant="warning" size="sm">{item.actuatorCode}</Badge>
                    <span className="text-gray-500">{item.totalHours.toFixed(1)} hrs</span>
                    <span className="text-gray-400">·</span>
                    <span className="text-gray-500">{item.estimatedKwh.toFixed(2)} kWh</span>
                  </div>
                  <span className="font-semibold text-gray-700">
                    {formatCurrency(item.estimatedCost, data.currency)}
                  </span>
                </div>
              ))}
            </div>
          </BaseCard>
        )}

        {/* Manual consumption cost detail */}
        {data.expenses.manualConsumptionCost && (
          <BaseCard padding="sm" hover={false} className="bg-white">
            <p className="text-sm font-semibold text-gray-600 mb-2 font-inter uppercase">
              Detalle Consumo Manual
            </p>
            <div className="space-y-1">
              {data.expenses.manualConsumptionCost.totalElectricityKwh > 0 && (
                <div className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100">
                  <div className="flex items-center gap-2">
                    <Badge variant="warning" size="sm">Electricidad</Badge>
                    <span className="text-gray-500">{data.expenses.manualConsumptionCost.totalElectricityKwh.toFixed(2)} kWh</span>
                  </div>
                  <span className="font-semibold text-gray-700">
                    {formatCurrency(data.expenses.manualConsumptionCost.costElectricity, data.currency)}
                  </span>
                </div>
              )}
              {data.expenses.manualConsumptionCost.totalWaterLiters > 0 && (
                <div className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100">
                  <div className="flex items-center gap-2">
                    <Badge variant="info" size="sm">Agua</Badge>
                    <span className="text-gray-500">{data.expenses.manualConsumptionCost.totalWaterLiters.toFixed(2)} L</span>
                  </div>
                  <span className="font-semibold text-gray-700">
                    {formatCurrency(data.expenses.manualConsumptionCost.costWater, data.currency)}
                  </span>
                </div>
              )}
              {data.expenses.manualConsumptionCost.totalNutrientLiters > 0 && (
                <div className="flex items-center justify-between py-1.5 text-sm border-b border-gray-100 last:border-b-0">
                  <div className="flex items-center gap-2">
                    <Badge variant="success" size="sm">Nutrientes</Badge>
                    <span className="text-gray-500">{data.expenses.manualConsumptionCost.totalNutrientLiters.toFixed(2)} L</span>
                  </div>
                  <span className="font-semibold text-gray-700">
                    {formatCurrency(data.expenses.manualConsumptionCost.costNutrients, data.currency)}
                  </span>
                </div>
              )}
            </div>

            {/* Individual entries with notes */}
            {(data.expenses.manualConsumptionCost.entries?.length ?? 0) > 0 && (
              <div className="mt-3 border-t border-gray-200 pt-3">
                <p className="text-xs font-semibold text-gray-500 mb-2 font-inter uppercase">Entradas individuales</p>
                <div className="space-y-1">
                  {data.expenses.manualConsumptionCost.entries.map((entry, idx) => {
                    const typeLabel = CONSUMPTION_TYPE_LABELS[entry.type as ConsumptionType] ?? 'Otro';
                    const unit = CONSUMPTION_TYPE_UNITS[entry.type as ConsumptionType] ?? '';
                    return (
                      <div
                        key={idx}
                        className="flex items-start justify-between py-1.5 text-xs border-b border-gray-50 last:border-b-0 gap-2"
                      >
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-1.5">
                            <Badge variant="default" size="sm">{typeLabel}</Badge>
                            <span className="text-gray-500">{entry.amount.toFixed(2)} {unit}</span>
                          </div>
                          {entry.note && (
                            <p className="text-gray-400 mt-0.5 truncate" title={entry.note}>
                              {entry.note.length > 60 ? `${entry.note.slice(0, 60)}…` : entry.note}
                            </p>
                          )}
                        </div>
                        <span className="font-semibold text-gray-600 whitespace-nowrap">
                          {formatCurrency(entry.costAmount, data.currency)}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </BaseCard>
        )}
      </div>
    </div>
  );
});

export default ProfitabilityResult;
