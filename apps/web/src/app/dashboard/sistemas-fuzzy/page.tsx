'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import PageLayout from '@/components/layout/PageLayout';
import FuzzySystemCard from '@/components/ui/FuzzySystemCard';
import Button from '@/components/ui/Button';
import { useFuzzyStore, FuzzySystemState } from '@hidroespinaca/shared';

const SistemasFuzzyPage: React.FC = () => {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(true);
  const [selectedSystem, setSelectedSystem] = useState<string | null>(null);
  
  // Usar el store real
  const store = useFuzzyStore();
  const { fuzzySystems, fuzzyVariables, fuzzyRules, fuzzyRoutines } = store;

  useEffect(() => {
    // Inicializar datos del store si están vacíos
    if (fuzzySystems.length === 0) {
      (store as any).initializeFuzzyData();
    }
    setIsLoading(false);
  }, [fuzzySystems.length]);

  // Agrupar datos por sistema
  const getSystemsData = () => {
    const systemsMap = new Map();

    // Procesar sistemas del store
    fuzzySystems.forEach((fuzzySystem) => {
      const systemId = fuzzySystem.id;
      systemsMap.set(systemId, {
        id: systemId,
        name: fuzzySystem.name,
        description: fuzzySystem.name,
        status: getSystemStatus(systemId),
        defuzzificationMethod: fuzzySystem.defuzzification_method,
        operators: {
          and: fuzzySystem.operators?.and || 'min',
          or: fuzzySystem.operators?.or || 'max'
        },
        ruleNames: [],
        variableNames: [],
        routineNames: [],
        lastUpdated: fuzzySystem.created_at
      });
    });

    // Agregar nombres de variables por sistema
    fuzzyVariables.forEach((variable) => {
      const systemId = variable.system_id;
      if (systemsMap.has(systemId)) {
        const system = systemsMap.get(systemId);
        if (system) {
          system.variableNames.push(variable.name);
        }
      }
    });

    // Agregar nombres de reglas por sistema
    fuzzyRules.forEach((rule) => {
      const systemId = rule.system_id;
      if (systemsMap.has(systemId)) {
        const system = systemsMap.get(systemId);
        if (system) {
          system.ruleNames.push(rule.name);
        }
      }
    });

    // Agregar nombres de rutinas por sistema
    fuzzyRoutines.forEach((routine) => {
      const systemId = routine.system_id;
      if (systemsMap.has(systemId)) {
        const system = systemsMap.get(systemId);
        if (system) {
          system.routineNames.push(routine.routine_name);
        }
      }
    });

    return Array.from(systemsMap.values());
  };

  const getSystemStatus = (systemId: string): 'active' | 'inactive' | 'warning' | 'error' => {
    // Lógica para determinar el estado del sistema
    const rulesCount = fuzzyRules.filter((rule) => rule.system_id === systemId).length;
    const variablesCount = fuzzyVariables.filter((variable) => variable.system_id === systemId).length;
    
    if (rulesCount > 0 && variablesCount > 0) {
      return 'active';
    } else if (variablesCount > 0) {
      return 'warning';
    } else {
      return 'inactive';
    }
  };

  const handleSystemClick = (systemId: string) => {
    setSelectedSystem(systemId);
    // Navegar a la página de detalles del sistema
    router.push(`/dashboard/sistemas-fuzzy/${systemId}`);
  };

  const handleCreateSystem = () => {
    // Lógica para crear un nuevo sistema
    console.log('Crear nuevo sistema fuzzy');
  };

  const systemsData = getSystemsData();

  if (isLoading) {
    return (
      <PageLayout>
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-600">Cargando sistemas fuzzy...</p>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout>
      <div className="space-y-6 mt-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Sistemas Fuzzy</h1>
            <p className="text-gray-600">Gestión de sistemas de lógica difusa</p>
          </div>
          
          <div className="flex space-x-3">
            <Button onClick={handleCreateSystem}>
              <span className="mr-2">+</span>
              Nuevo Sistema
            </Button>
          </div>
        </div>
        
        {/* Grid de sistemas */}
        {systemsData.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {systemsData.map((system) => (
              <FuzzySystemCard
                key={system.id}
                id={system.id}
                name={system.name}
                description={system.description}
                status={system.status}
                defuzzificationMethod={system.defuzzificationMethod}
                operators={system.operators}
                ruleNames={system.ruleNames}
                variableNames={system.variableNames}
                routineNames={system.routineNames}
                lastUpdated={system.lastUpdated}
                onClick={() => handleSystemClick(system.id)}
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-12">
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              No hay sistemas configurados
            </h3>
            <p className="text-gray-600">
              Comienza creando tu primer sistema de lógica fuzzy
            </p>
          </div>
        )}
      </div>
    </PageLayout>
  );
};

export default SistemasFuzzyPage;