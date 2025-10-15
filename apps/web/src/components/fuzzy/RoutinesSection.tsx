'use client';

import React from 'react';
import { SimpleFuzzyRoutine, SimpleFuzzyTerm, SimpleFuzzyVariable } from '@hydroespinaca/shared';

interface RoutinesSectionProps {
  routines: SimpleFuzzyRoutine[];
  terms: SimpleFuzzyTerm[];
  variables: SimpleFuzzyVariable[];
}

const RoutinesSection: React.FC<RoutinesSectionProps> = ({ routines, terms, variables }) => {
  // Función para obtener el nombre de un término por su ID
  const getTermName = (termId: string): string => {
    const term = terms.find(t => t.id === termId);
    return term ? term.label : `Término ${termId}`;
  };

  // Función para obtener el nombre de una variable por su ID
  const getVariableName = (variableId: string): string => {
    const variable = variables.find(v => v.id === variableId);
    return variable ? variable.name : `Variable ${variableId}`;
  };

  // Función para obtener información del término incluyendo la variable
  const getTermInfo = (termId: string): { termName: string; variableName: string } => {
    const term = terms.find(t => t.id === termId);
    if (term) {
      const variable = variables.find(v => v.id === term.variable_id);
      return {
        termName: term.label,
        variableName: variable ? variable.name : `Variable ${term.variable_id}`
      };
    }
    return {
      termName: `Término ${termId}`,
      variableName: 'Variable desconocida'
    };
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Rutinas</h2>
        <span className="text-sm text-gray-500">{routines.length} rutinas definidas</span>
      </div>

      <div className="grid gap-6 md:grid-cols-1 lg:grid-cols-2">
        {routines.map((routine) => (
          <div key={routine.id} className="bg-white border border-gray-200 rounded-lg shadow-sm overflow-hidden">
            {/* Header de la rutina */}
            <div className="bg-gradient-to-r from-green-50 to-emerald-50 px-6 py-4 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900">{routine.routine_name}</h3>
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  {routine.steps.length} pasos
                </span>
              </div>
              {/* ID de la rutina */}
              <p className="mt-2 text-sm text-gray-600">ID: {routine.id}</p>
            </div>

            {/* Contenido de la rutina */}
            <div className="p-6">
              <div className="space-y-4">
                {routine.steps.map((step, stepIndex) => (
                  <div key={stepIndex} className="border border-gray-100 rounded-lg p-4 bg-gray-50">
                    <div className="flex items-start space-x-3">
                      <div className="flex-shrink-0">
                        <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-green-100 text-green-800 text-xs font-medium">
                          {step.step_id + 1}
                        </span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="space-y-3">
                          {/* Condición del paso */}
                          <div>
                            <h4 className="text-sm font-medium text-gray-900 mb-1">Condición:</h4>
                            <p className="text-sm text-gray-700">{step.condition}</p>
                          </div>

                          {/* Potencia */}
                          <div>
                            <h4 className="text-sm font-medium text-gray-900 mb-1">Potencia:</h4>
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-200 text-green-800">
                              {getTermName(step.power_term_id)}
                            </span>
                          </div>

                          {/* Duración */}
                          <div>
                            <h4 className="text-sm font-medium text-gray-900 mb-1">Duración:</h4>
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-200 text-green-800">
                              {getTermName(step.duration_term_id)}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {routine.steps.length === 0 && (
                <div className="text-center py-8">
                  <div className="text-gray-400 text-2xl mb-2">⚙️</div>
                  <p className="text-gray-500 text-sm">Esta rutina no tiene pasos definidos</p>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {routines.length === 0 && (
        <div className="text-center py-12">
          <div className="text-gray-400 text-lg mb-2">🔄</div>
          <h3 className="text-lg font-medium text-gray-900 mb-1">No hay rutinas definidas</h3>
          <p className="text-gray-500">Este sistema fuzzy no tiene rutinas configuradas.</p>
        </div>
      )}
    </div>
  );
};

export default RoutinesSection;