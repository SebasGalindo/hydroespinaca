'use client';

import React, { useEffect, useState } from 'react';
import { useBiStore, formatCurrency, formatDate } from '@hydroespinaca/shared';
import type { ProductionRecord } from '@hydroespinaca/shared';
import BaseCard from '@/components/ui/BaseCard';
import Button from '@/components/ui/Button';
import EmptyState from '@/components/ui/EmptyState';
import ProfitabilityResult from './ProfitabilityResult';
import { CalculatorIcon, TrendingUpIcon } from '@/components/ui/icons/Icons';

const ProfitabilitySection = React.memo(function ProfitabilitySection() {
  const {
    productionRecords,
    fetchProductionRecords,
    profitabilityResult,
    profitabilityLoading,
    profitabilityError,
    calculateProfitability,
  } = useBiStore();

  const [selectedProductionId, setSelectedProductionId] = useState<string | null>(null);
  const [initialInvestmentCost, setInitialInvestmentCost] = useState<string>('');
  const [includeAutoEnergy, setIncludeAutoEnergy] = useState(true);

  useEffect(() => {
    if (productionRecords.length === 0) {
      fetchProductionRecords();
    }
  }, [productionRecords.length, fetchProductionRecords]);

  const handleCalculate = async () => {
    if (!selectedProductionId) return;
    const investmentValue = initialInvestmentCost.trim() !== ''
      ? parseFloat(initialInvestmentCost.replace(/,/g, '.'))
      : undefined;
    await calculateProfitability({
      productionRecordId: selectedProductionId,
      initialInvestmentCost: !isNaN(investmentValue as number) ? investmentValue : undefined,
      includeAutomaticEnergyCalculation: includeAutoEnergy,
    });
  };

  const selectedRecord = productionRecords.find((r) => r.id === selectedProductionId);

  return (
    <div className="space-y-6">
      {/* Production selector */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 font-inter">
          Seleccionar Producción para Análisis
        </h3>

        {productionRecords.length === 0 ? (
          <EmptyState
            icon={<TrendingUpIcon size={48} />}
            title="Sin producciones registradas"
            description="Primero registra una producción en la pestaña 'Producción' para calcular rentabilidad."
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {productionRecords.map((record: ProductionRecord) => {
              const isSelected = record.id === selectedProductionId;
              const estimatedRevenue = record.kilosProduced * record.pricePerKilo;

              return (
                <BaseCard
                  key={record.id}
                  padding="sm"
                  hover={true}
                  onClick={() => setSelectedProductionId(record.id)}
                  className={`transition-all cursor-pointer ${
                    isSelected
                      ? 'ring-2 ring-green-500 border-green-300 bg-green-50'
                      : 'hover:border-green-300'
                  }`}
                >
                  <div className="space-y-2">
                    <div className="flex items-start justify-between">
                      <h4 className="text-sm font-semibold text-gray-900 font-inter">
                        {record.cropName}
                      </h4>
                      {isSelected && (
                        <span className="w-2 h-2 rounded-full bg-green-500 mt-1.5 flex-shrink-0" />
                      )}
                    </div>
                    <div className="text-xs text-gray-500 font-inter space-y-0.5">
                      <p>{formatDate(record.startDate)} → {formatDate(record.harvestDate)}</p>
                      <p>{record.kilosProduced.toFixed(1)} kg × {formatCurrency(record.pricePerKilo, record.currency)}/kg</p>
                    </div>
                    <p className="text-sm font-semibold text-green-700 font-inter">
                      Ingreso est.: {formatCurrency(estimatedRevenue, record.currency)}
                    </p>
                  </div>
                </BaseCard>
              );
            })}
          </div>
        )}
      </div>

      {/* Optional initial investment input + Calculate button */}
      {selectedProductionId && (
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-semibold text-gray-600 font-inter mb-1">
              Inversión inicial (opcional)
            </label>
            <div className="flex flex-col sm:flex-row gap-3 items-stretch sm:items-center">
              <input
                type="number"
                min="0"
                step="1000"
                value={initialInvestmentCost}
                onChange={(e) => setInitialInvestmentCost(e.target.value)}
                placeholder="Ej: 850000 (COP)"
                className="flex-1 max-w-xs px-3 py-2 text-sm border border-gray-300 rounded-lg font-inter focus:outline-none focus:ring-2 focus:ring-green-400"
              />
              <Button
                variant="primary"
                onClick={handleCalculate}
                isLoading={profitabilityLoading}
                disabled={!selectedProductionId}
              >
                <span className="flex items-center gap-2">
                  <CalculatorIcon size={18} />
                  Calcular Rentabilidad
                </span>
              </Button>
            </div>
            <p className="text-xs text-gray-400 mt-1 font-inter">
              Hardware, infraestructura, instalación. Permite calcular el ROI real del ciclo.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              role="switch"
              aria-checked={includeAutoEnergy}
              onClick={() => setIncludeAutoEnergy(!includeAutoEnergy)}
              className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 ${
                includeAutoEnergy ? 'bg-green-600' : 'bg-gray-300'
              }`}
            >
              <span
                className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow transition-transform ${
                  includeAutoEnergy ? 'translate-x-4' : 'translate-x-1'
                }`}
              />
            </button>
            <div>
              <p className="text-sm font-medium text-gray-700 font-inter">
                Incluir consumo automático de actuadores
              </p>
              <p className="text-xs text-gray-400 font-inter">
                Desactívalo si ya registraste ese consumo manualmente para evitar duplicados
              </p>
            </div>
          </div>
          {selectedRecord && (
            <span className="text-sm text-gray-500 font-inter">
              para: <strong>{selectedRecord.cropName}</strong>
            </span>
          )}
        </div>
      )}

      {/* Error */}
      {profitabilityError && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700 text-sm font-inter">
          {profitabilityError}
        </div>
      )}

      {/* Results */}
      {profitabilityResult && !profitabilityLoading && (
        <ProfitabilityResult data={profitabilityResult} />
      )}

      {/* Loading skeleton */}
      {profitabilityLoading && (
        <div className="space-y-4 animate-pulse">
          <div className="h-24 bg-gray-200 rounded-lg" />
          <div className="h-16 bg-gray-200 rounded-lg" />
          <div className="grid grid-cols-3 gap-3">
            <div className="h-20 bg-gray-200 rounded-lg" />
            <div className="h-20 bg-gray-200 rounded-lg" />
            <div className="h-20 bg-gray-200 rounded-lg" />
          </div>
        </div>
      )}
    </div>
  );
});

export default ProfitabilitySection;
