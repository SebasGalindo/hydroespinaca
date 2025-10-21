'use client';

import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/Modal';
import BaseCard from '@/components/ui/BaseCard';
import Button from '@/components/ui/Button';
import { fuzzyRulesService, type FuzzyRuleSummary } from '@hydroespinaca/shared';

/**
 * Component that displays fuzzy logic rules in a modal dialog.
 * Fetches the rules once per session and caches them locally.
 */
const FuzzyRulesInfo: React.FC = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [rules, setRules] = useState<FuzzyRuleSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasFetched, setHasFetched] = useState(false);

  /**
   * Fetch fuzzy rules when modal opens for the first time
   */
  useEffect(() => {
    if (isModalOpen && !hasFetched) {
      fetchFuzzyRules();
    }
  }, [isModalOpen, hasFetched]);

  const fetchFuzzyRules = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await fuzzyRulesService.getFuzzyRules();
      setRules(data);
      setHasFetched(true);
    } catch (err: any) {
      console.error('Error fetching fuzzy rules:', err);
      setError(err.message || 'Error al cargar las reglas difusas');
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenModal = () => {
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
  };

  return (
    <>
      {/* Info Button */}
      <Button
        variant="ghost"
        size="sm"
        onClick={handleOpenModal}
        className="inline-flex items-center gap-2"
        title="Ver reglas difusas del sistema"
      >
        <svg
          className="w-5 h-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <span>Reglas difusas</span>
      </Button>

      {/* Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        title="Reglas de lógica difusa del sistema"
        maxWidth="2xl"
      >
        {/* Loading State */}
        {isLoading && (
          <div className="flex items-center justify-center py-12">
            <div className="flex flex-col items-center gap-3">
              <svg
                className="animate-spin h-8 w-8 text-green-600"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              <p className="text-sm text-gray-600">Cargando reglas difusas...</p>
            </div>
          </div>
        )}

        {/* Error State */}
        {error && !isLoading && (
          <div className="flex flex-col items-center justify-center py-12 gap-4">
            <div className="flex items-center gap-2 text-red-600">
              <svg
                className="w-6 h-6"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <p className="font-medium">Error al cargar las reglas</p>
            </div>
            <p className="text-sm text-gray-600">{error}</p>
            <Button variant="primary" size="sm" onClick={fetchFuzzyRules}>
              Reintentar
            </Button>
          </div>
        )}

        {/* Rules List */}
        {!isLoading && !error && rules.length > 0 && (
          <div className="space-y-4">
            <p className="text-sm text-gray-600 mb-4">
              El sistema utiliza {rules.length} regla{rules.length !== 1 ? 's' : ''} de
              lógica difusa para controlar automáticamente las variables del cultivo.
            </p>

            <div className="grid gap-4">
              {rules.map((rule) => (
                <BaseCard
                  key={rule.id}
                  padding="md"
                  hover={false}
                  className="border border-gray-200"
                >
                  <div className="flex flex-col gap-2">
                    {/* Rule Name */}
                    <h3 className="text-lg font-semibold text-gray-900 flex items-start gap-2">
                      <span className="flex-shrink-0 w-6 h-6 rounded-full bg-green-100 text-green-700 flex items-center justify-center text-xs font-bold mt-0.5">
                        ⚡
                      </span>
                      <span className="flex-1">{rule.name}</span>
                    </h3>

                    {/* Rule Description */}
                    <p className="text-sm text-gray-700 leading-relaxed pl-8">
                      {rule.description}
                    </p>
                  </div>
                </BaseCard>
              ))}
            </div>
          </div>
        )}

        {/* Empty State */}
        {!isLoading && !error && rules.length === 0 && (
          <div className="flex flex-col items-center justify-center py-12 gap-3">
            <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center">
              <svg
                className="w-8 h-8 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <p className="text-gray-600 font-medium">No hay reglas difusas configuradas</p>
            <p className="text-sm text-gray-500">
              El sistema aún no tiene reglas de lógica difusa definidas.
            </p>
          </div>
        )}
      </Modal>
    </>
  );
};

export default FuzzyRulesInfo;
