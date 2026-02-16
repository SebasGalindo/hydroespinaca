'use client';

import React, { useState } from 'react';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import BaseCard from '@/components/ui/BaseCard';
import Badge from '@/components/ui/Badge';
import type {
  FuzzyVariable,
  SimulateFuzzySystemRequest,
  SimulateFuzzySystemResponse,
} from '@hydroespinaca/shared';

interface SimulationPanelProps {
  systemId: string;
  systemName: string;
  inputVariables: FuzzyVariable[];
  onSimulate: (id: string, request: SimulateFuzzySystemRequest) => Promise<void>;
  simulationResult: SimulateFuzzySystemResponse | null;
  isLoading: boolean;
  error: string | null;
  onClearSimulation: () => void;
}

const SimulationPanel: React.FC<SimulationPanelProps> = ({
  systemId,
  systemName,
  inputVariables,
  onSimulate,
  simulationResult,
  isLoading,
  error,
  onClearSimulation,
}) => {
  const [inputValues, setInputValues] = useState<Record<string, string>>(() => {
    const initial: Record<string, string> = {};
    inputVariables.forEach((v) => {
      const mid =
        v.universeMin != null && v.universeMax != null
          ? ((v.universeMin + v.universeMax) / 2).toFixed(1)
          : '0';
      initial[v.referenceCode ?? v.id] = mid;
    });
    return initial;
  });

  const handleInputChange = (code: string, value: string) => {
    setInputValues((prev) => ({ ...prev, [code]: value }));
  };

  const handleSimulate = async () => {
    const request: SimulateFuzzySystemRequest = {
      inputs: inputVariables.map((v) => ({
        referenceCode: v.referenceCode ?? v.id,
        value: parseFloat(inputValues[v.referenceCode ?? v.id] ?? '0'),
      })),
    };
    await onSimulate(systemId, request);
  };

  const handleReset = () => {
    onClearSimulation();
    const initial: Record<string, string> = {};
    inputVariables.forEach((v) => {
      const mid =
        v.universeMin != null && v.universeMax != null
          ? ((v.universeMin + v.universeMax) / 2).toFixed(1)
          : '0';
      initial[v.referenceCode ?? v.id] = mid;
    });
    setInputValues(initial);
  };

  if (inputVariables.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <p className="text-gray-500 font-inter">
          No hay variables de entrada configuradas para simular.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="border-b border-gray-200 pb-4">
        <h2 className="text-xl font-semibold text-gray-900 font-inter">
          Simulación
        </h2>
        <p className="text-sm text-gray-600 font-inter mt-1">
          Evalúa el sistema con valores arbitrarios sin afectar el estado real
        </p>
      </div>

      {/* Inputs */}
      <BaseCard hover={false} className="border border-gray-200">
        <h3 className="text-base font-medium text-gray-800 font-inter mb-4">
          Variables de entrada
        </h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {inputVariables.map((v) => {
            const code = v.referenceCode ?? v.id;
            const rangeText =
              v.universeMin != null && v.universeMax != null
                ? `Rango: ${v.universeMin} – ${v.universeMax}`
                : undefined;

            return (
              <Input
                key={v.id}
                id={`sim-${v.id}`}
                label={`${v.name} (${code})`}
                type="number"
                step="any"
                value={inputValues[code] ?? ''}
                onChange={(e) => handleInputChange(code, e.target.value)}
                placeholder={rangeText ?? 'Valor'}
              />
            );
          })}
        </div>

        <div className="flex gap-3 mt-6">
          <Button
            variant="primary"
            onClick={handleSimulate}
            isLoading={isLoading}
            disabled={isLoading}
          >
            🧪 Simular
          </Button>
          <Button variant="ghost" onClick={handleReset} disabled={isLoading}>
            Limpiar
          </Button>
        </div>
      </BaseCard>

      {/* Error */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-700 font-inter">{error}</p>
        </div>
      )}

      {/* Results */}
      {simulationResult && (
        <BaseCard hover={false} className="border border-green-200 bg-green-50/30">
          <h3 className="text-base font-medium text-green-800 font-inter mb-4">
            Resultado de la simulación
          </h3>

          {/* Final outputs */}
          <div className="mb-6">
            <h4 className="text-sm font-semibold text-gray-700 uppercase mb-3">
              Salidas Finales
            </h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {simulationResult.finalOutputs.map((output, i) => (
                <div
                  key={i}
                  className="bg-white rounded-lg p-4 border border-gray-200 shadow-sm"
                >
                  <p className="text-xs text-gray-500 font-inter">
                    {output.variableName}
                    {output.referenceCode && (
                      <span className="ml-1 text-gray-400">({output.referenceCode})</span>
                    )}
                  </p>
                  <p className="text-2xl font-bold text-green-700 font-inter mt-1">
                    {output.crispValue.toFixed(2)}
                  </p>
                  {output.actuatorType && (
                    <Badge variant="info" size="sm" className="mt-1">
                      {output.actuatorType}
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Activated rules */}
          {simulationResult.activatedRules.length > 0 && (
            <div>
              <h4 className="text-sm font-semibold text-gray-700 uppercase mb-3">
                Reglas Activadas ({simulationResult.activatedRules.length})
              </h4>
              <div className="space-y-2">
                {simulationResult.activatedRules.map((ar, i) => (
                  <div
                    key={i}
                    className="bg-white rounded-lg p-3 border border-gray-200 flex items-center justify-between gap-3"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-medium text-gray-900 truncate">
                        {ar.ruleName}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <Badge
                        variant={ar.firingStrength > 0.5 ? 'success' : 'warning'}
                        size="sm"
                      >
                        {(ar.firingStrength * 100).toFixed(1)}%
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Timestamp */}
          <p className="text-xs text-gray-400 mt-4 font-inter">
            Simulado: {new Date(simulationResult.simulatedAt).toLocaleString('es-CO', { timeZone: 'America/Bogota' })}
          </p>
        </BaseCard>
      )}
    </div>
  );
};

export default SimulationPanel;
