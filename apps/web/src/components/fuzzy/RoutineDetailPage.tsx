'use client';

import React, { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import PageLayout from '@/components/layout/PageLayout';
import Button from '@/components/ui/Button';
import BaseCard from '@/components/ui/BaseCard';
import {
  SystemInfoHeader,
  SystemActionButtons,
  VariableSection,
  RulesSection,
  SimulationPanel,
} from '@/components/fuzzy';
import {
  useFuzzyStore,
  type FuzzySystemExport,
} from '@hydroespinaca/shared';

// ─── Tab definitions ─────────────────────────────────────────

type DetailTab = 'info' | 'variables' | 'rules' | 'simulate';

interface TabDef {
  id: DetailTab;
  label: string;
  description: string;
}

const tabs: TabDef[] = [
  { id: 'info', label: 'Información', description: 'Datos generales' },
  { id: 'variables', label: 'Variables', description: 'Entradas y salidas' },
  { id: 'rules', label: 'Reglas', description: 'Inferencia Mamdani' },
  { id: 'simulate', label: 'Simular', description: 'Evaluación en vivo' },
];

// ─── Props ───────────────────────────────────────────────────

interface RoutineDetailPageProps {
  systemId: string;
}

// ─── Component ───────────────────────────────────────────────

const RoutineDetailPage: React.FC<RoutineDetailPageProps> = ({ systemId }) => {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<DetailTab>('info');

  const {
    selectedDetail,
    detailLoading,
    detailError,
    simulationResult,
    simulationLoading,
    simulationError,
    operationLoading,
    fetchSystemDetail,
    clearSelectedDetail,
    activateSystem,
    cloneSystem,
    deleteSystem,
    exportSystem,
    simulateSystem,
    clearSimulation,
  } = useFuzzyStore();

  useEffect(() => {
    fetchSystemDetail(systemId);
    return () => {
      clearSelectedDetail();
      clearSimulation();
    };
  }, [systemId]);

  // ─── Action callbacks ──────────────────────────────────────

  const handleActivate = useCallback(async (id: string) => {
    await activateSystem(id);
    // Refresh detail to get updated status
    await fetchSystemDetail(id);
  }, [activateSystem, fetchSystemDetail]);

  const handleClone = useCallback(async (id: string) => {
    const cloned = await cloneSystem(id);
    router.push(`/rutinas/${cloned.id}`);
  }, [cloneSystem, router]);

  const handleDelete = useCallback(async (id: string) => {
    await deleteSystem(id);
    router.push('/rutinas');
  }, [deleteSystem, router]);

  const handleExport = useCallback(async (id: string): Promise<FuzzySystemExport> => {
    return await exportSystem(id);
  }, [exportSystem]);

  const handleSimulate = useCallback(async (
    id: string,
    request: { inputs: { referenceCode: string; value: number }[] }
  ) => {
    await simulateSystem(id, request);
  }, [simulateSystem]);

  // ─── Loading state ─────────────────────────────────────────

  if (detailLoading) {
    return (
      <PageLayout>
        <div className="flex items-center justify-center py-24">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-green-600 mx-auto mb-4" />
            <p className="text-gray-600 font-inter">Cargando detalle del sistema…</p>
          </div>
        </div>
      </PageLayout>
    );
  }

  // ─── Error state ───────────────────────────────────────────

  if (detailError || !selectedDetail) {
    return (
      <PageLayout>
        <div className="max-w-2xl mx-auto px-4 py-16 text-center">
          <div className="w-20 h-20 mx-auto bg-red-50 rounded-full flex items-center justify-center mb-4">
            <span className="text-3xl">❌</span>
          </div>
          <h2 className="text-xl font-bold text-gray-800 mb-2 font-inter">
            {detailError ?? 'Sistema no encontrado'}
          </h2>
          <p className="text-gray-500 mb-6 font-inter">
            No se pudo cargar el detalle del sistema fuzzy.
          </p>
          <div className="flex justify-center gap-3">
            <Button variant="primary" onClick={() => fetchSystemDetail(systemId)}>
              Reintentar
            </Button>
            <Button variant="outline" onClick={() => router.push('/rutinas')}>
              Volver a la lista
            </Button>
          </div>
        </div>
      </PageLayout>
    );
  }

  // ─── Detail loaded ─────────────────────────────────────────

  const { system, variables, terms, rules } = selectedDetail;
  const inputVars = variables.filter((v) => v.variableType === 'input');

  return (
    <PageLayout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Back button */}
        <div className="mb-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push('/rutinas')}
          >
            ← Volver a rutinas
          </Button>
        </div>

        {/* Header */}
        <BaseCard hover={false} className="border border-gray-200 mb-6">
          <SystemInfoHeader detail={selectedDetail} />

          <div className="mt-4 pt-4 border-t border-gray-100">
            <SystemActionButtons
              system={system}
              onActivate={handleActivate}
              onClone={handleClone}
              onDelete={handleDelete}
              onExport={handleExport}
              disabled={operationLoading}
            />
          </div>
        </BaseCard>

        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="flex gap-0 overflow-x-auto scrollbar-hidden" aria-label="Tabs de detalle">
            {tabs.map((tab) => {
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
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

        {/* Tab content */}
        <div className="mt-2">
          {activeTab === 'info' && (
            <div className="space-y-6">
              {/* Summary cards */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Input variables summary */}
                <BaseCard hover={false} className="border border-gray-200">
                  <div className="flex items-center gap-3 mb-4">
                    <div className="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center">
                      <span className="text-lg">📥</span>
                    </div>
                    <div>
                      <h3 className="text-lg font-bold text-gray-900 font-inter">
                        Variables de Entrada
                      </h3>
                      <p className="text-xs text-gray-500 font-inter">{inputVars.length} variable{inputVars.length !== 1 ? 's' : ''} configurada{inputVars.length !== 1 ? 's' : ''}</p>
                    </div>
                  </div>
                  {inputVars.length === 0 ? (
                    <p className="text-sm text-gray-400 italic py-4 text-center">Sin variables de entrada</p>
                  ) : (
                    <div className="space-y-2">
                      {inputVars.map((v) => (
                        <div key={v.id} className="flex items-center justify-between bg-gray-50 rounded-lg px-4 py-3">
                          <div className="flex items-center gap-3 min-w-0">
                            <div className="w-2 h-2 rounded-full bg-emerald-400 flex-shrink-0" />
                            <span className="text-sm font-medium text-gray-800 truncate">{v.name}</span>
                          </div>
                          {v.referenceCode && (
                            <span className="text-xs font-mono bg-emerald-50 text-emerald-700 px-2 py-1 rounded flex-shrink-0 ml-2">
                              {v.referenceCode}
                            </span>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </BaseCard>

                {/* Output variables summary */}
                <BaseCard hover={false} className="border border-gray-200">
                  <div className="flex items-center gap-3 mb-4">
                    <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
                      <span className="text-lg">📤</span>
                    </div>
                    <div>
                      <h3 className="text-lg font-bold text-gray-900 font-inter">
                        Variables de Salida
                      </h3>
                      <p className="text-xs text-gray-500 font-inter">
                        {variables.filter((v) => v.variableType === 'output').length} variable{variables.filter((v) => v.variableType === 'output').length !== 1 ? 's' : ''} configurada{variables.filter((v) => v.variableType === 'output').length !== 1 ? 's' : ''}
                      </p>
                    </div>
                  </div>
                  {variables.filter((v) => v.variableType === 'output').length === 0 ? (
                    <p className="text-sm text-gray-400 italic py-4 text-center">Sin variables de salida</p>
                  ) : (
                    <div className="space-y-2">
                      {variables
                        .filter((v) => v.variableType === 'output')
                        .map((v) => (
                          <div key={v.id} className="flex items-center justify-between bg-gray-50 rounded-lg px-4 py-3">
                            <div className="flex items-center gap-3 min-w-0">
                              <div className="w-2 h-2 rounded-full bg-blue-400 flex-shrink-0" />
                              <span className="text-sm font-medium text-gray-800 truncate">{v.name}</span>
                            </div>
                            <div className="flex items-center gap-2 flex-shrink-0 ml-2">
                              {v.actuatorType && (
                                <span className="text-xs bg-blue-50 text-blue-700 px-2 py-1 rounded">
                                  {v.actuatorType}
                                </span>
                              )}
                              {v.referenceCode && (
                                <span className="text-xs font-mono bg-gray-100 text-gray-600 px-2 py-1 rounded">
                                  {v.referenceCode}
                                </span>
                              )}
                            </div>
                          </div>
                        ))}
                    </div>
                  )}
                </BaseCard>
              </div>

              {/* Rules preview */}
              <BaseCard hover={false} className="border border-gray-200">
                <div className="flex items-center gap-3 mb-4">
                  <div className="w-10 h-10 rounded-lg bg-amber-50 flex items-center justify-center">
                    <span className="text-lg">⚙️</span>
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-gray-900 font-inter">
                      Reglas de Inferencia
                    </h3>
                    <p className="text-xs text-gray-500 font-inter">{rules.length} regla{rules.length !== 1 ? 's' : ''} configurada{rules.length !== 1 ? 's' : ''}</p>
                  </div>
                </div>
                {rules.length === 0 ? (
                  <p className="text-sm text-gray-400 italic py-4 text-center">Sin reglas configuradas</p>
                ) : (
                  <div className="space-y-2">
                    {rules.slice(0, 5).map((r) => (
                      <div key={r.id} className="bg-gray-50 rounded-lg px-4 py-3">
                        <span className="text-sm font-medium text-gray-800">{r.name}</span>
                        {r.description && <p className="text-xs text-gray-500 mt-0.5">{r.description}</p>}
                      </div>
                    ))}
                    {rules.length > 5 && (
                      <p className="text-xs text-gray-400 text-center pt-1">+ {rules.length - 5} regla{rules.length - 5 !== 1 ? 's' : ''} más…</p>
                    )}
                  </div>
                )}
              </BaseCard>
            </div>
          )}

          {activeTab === 'variables' && (
            <VariableSection variables={variables} terms={terms} />
          )}

          {activeTab === 'rules' && (
            <RulesSection rules={rules} variables={variables} terms={terms} />
          )}

          {activeTab === 'simulate' && (
            <SimulationPanel
              systemId={systemId}
              systemName={system.name}
              inputVariables={inputVars}
              onSimulate={handleSimulate}
              simulationResult={simulationResult}
              isLoading={simulationLoading}
              error={simulationError}
              onClearSimulation={clearSimulation}
            />
          )}
        </div>
      </div>
    </PageLayout>
  );
};

export default RoutineDetailPage;
