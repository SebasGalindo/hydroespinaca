'use client';

import React, { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { 
  SimpleFuzzySystem, 
  SimpleFuzzyVariable, 
  SimpleFuzzyTerm, 
  SimpleFuzzyRule, 
  SimpleFuzzyRoutine,
  useFuzzyStore 
} from '@hydroespinaca/shared';

import PageLayout from '@/components/layout/PageLayout';
import Button from '@/components/ui/Button';
import VariableSection from '@/components/fuzzy/VariableSection';
import RulesSection from '@/components/fuzzy/RulesSection';
import RoutinesSection from '@/components/fuzzy/RoutinesSection';

const SystemDetailPage: React.FC = () => {
  const params = useParams();
  const router = useRouter();
  const systemId = params?.id as string;

  // Usar el store de Zustand en lugar de API calls
  const store = useFuzzyStore() as any;

  const [system, setSystem] = useState<SimpleFuzzySystem | null>(null);
  const [variables, setVariables] = useState<SimpleFuzzyVariable[]>([]);
  const [terms, setTerms] = useState<SimpleFuzzyTerm[]>([]);
  const [rules, setRules] = useState<SimpleFuzzyRule[]>([]);
  const [routines, setRoutines] = useState<SimpleFuzzyRoutine[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadSystemData = () => {
      try {
        setLoading(true);
        setError(null);

        // Inicializar datos mock si es necesario
        store.initializeFuzzyData();

        // Obtener el sistema
        const systemData = store.getFuzzySystemById(systemId);
        if (!systemData) {
          setError('Sistema no encontrado');
          return;
        }
        setSystem(systemData);

        // Obtener variables del sistema
        const systemVariables = store.getVariablesBySystemId(systemId);
        setVariables(systemVariables);

        // Obtener términos para cada variable
        const allTerms: SimpleFuzzyTerm[] = [];
        systemVariables.forEach((variable: SimpleFuzzyVariable) => {
          const variableTerms = store.getTermsByVariableId(variable.id);
          allTerms.push(...variableTerms);
        });
        setTerms(allTerms);

        // Obtener reglas del sistema
        const systemRules = store.getRulesBySystemId(systemId);
        setRules(systemRules);

        // Obtener rutinas del sistema
        const systemRoutines = store.getRoutinesBySystemId(systemId);
        setRoutines(systemRoutines);

      } catch (err) {
        console.error('Error loading system data:', err);
        setError('Error al cargar el sistema fuzzy');
      } finally {
        setLoading(false);
      }
    };

    if (systemId) {
      loadSystemData();
    }
  }, [systemId]);

  const handleGoBack = () => {
    router.push('/dashboard/sistemas-fuzzy');
  };

  if (loading) {
    return (
      <PageLayout title="Cargando..." subtitle="Sistema Fuzzy">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-600">Cargando sistema fuzzy...</p>
          </div>
        </div>
      </PageLayout>
    );
  }

  if (error) {
    return (
      <PageLayout title="Error" subtitle="Sistema Fuzzy">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="text-red-500 text-6xl mb-4">⚠️</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Error al cargar el sistema</h2>
            <p className="text-gray-600 mb-4">{error}</p>
            <Button
              onClick={handleGoBack}
              variant="primary"
            >
              ← Volver a sistemas
            </Button>
          </div>
        </div>
      </PageLayout>
    );
  }

  if (!system) {
    return (
      <PageLayout title="No encontrado" subtitle="Sistema Fuzzy">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="text-gray-400 text-6xl mb-4">🔍</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Sistema no encontrado</h2>
            <p className="text-gray-600 mb-4">El sistema fuzzy solicitado no existe.</p>
            <Button
              onClick={handleGoBack}
              variant="primary"
            >
              ← Volver a sistemas
            </Button>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout 
      title={system.name}
      subtitle={`Método: ${system.defuzzification_method} • ${variables.length} variables • ${rules.length} reglas • ${routines.length} rutinas`}
      maxWidth="xl"
    >
      {/* Botón de volver */}
      <div className="mb-6">
        <Button
          onClick={handleGoBack}
          variant="primary"
          size="md"
        >
          ← Volver a sistemas
        </Button>
      </div>

      {/* Content */}
      <div className="space-y-8">
        {/* Variables Section */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <VariableSection variables={variables} terms={terms} />
        </div>

        {/* Rules Section */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <RulesSection 
            rules={rules} 
            terms={terms} 
            variables={variables} 
            routines={routines} 
          />
        </div>

        {/* Routines Section */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <RoutinesSection 
            routines={routines} 
            terms={terms} 
            variables={variables} 
          />
        </div>
      </div>
    </PageLayout>
  );
};

export default SystemDetailPage;