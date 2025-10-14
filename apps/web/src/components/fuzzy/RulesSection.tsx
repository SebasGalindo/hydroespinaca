'use client';

import React from 'react';
import { SimpleFuzzyRule, SimpleFuzzyTerm, SimpleFuzzyVariable, SimpleFuzzyRoutine } from '@hidroespinaca/shared';

interface RulesSectionProps {
  rules: SimpleFuzzyRule[];
  terms: SimpleFuzzyTerm[];
  variables: SimpleFuzzyVariable[];
  routines: SimpleFuzzyRoutine[];
}

const RulesSection: React.FC<RulesSectionProps> = ({ rules, terms, variables, routines }) => {
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

  // Función para obtener el nombre de una rutina por su ID
  const getRoutineName = (routineId: string): string => {
    const routine = routines.find(r => r.id === routineId);
    return routine ? routine.routine_name : `Rutina ${routineId}`;
  };

  // Función para construir la descripción de una regla
  const buildRuleDescription = (rule: SimpleFuzzyRule): string => {
    const conditions = rule.conditions.map(condition => {
      const variableName = getVariableName(condition.variableId);
      const termName = condition.value;
      return `${variableName} es ${termName}`;
    }).join(' Y ');
    
    return `SI ${conditions} ENTONCES ejecutar ${rule.routine.routine_name}`;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Reglas</h2>
        <span className="text-sm text-gray-500">{rules.length} reglas definidas</span>
      </div>

      {/* Vista de tabla para pantallas grandes */}
      <div className="hidden md:block">
        <div className="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
          <table className="min-w-full divide-y divide-gray-300">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-24">
                  Número
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Descripción
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-48">
                  Consecuencia
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {rules.map((rule, index) => (
                <tr key={rule.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {index + 1}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-900">
                    <div className="max-w-xs lg:max-w-md xl:max-w-lg">
                      {buildRuleDescription(rule)}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      {rule.routine.routine_name}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Vista de cards para pantallas pequeñas */}
      <div className="md:hidden space-y-4">
        {rules.map((rule, index) => (
          <div key={rule.id} className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-lg font-semibold text-gray-900">Regla {index + 1}</h3>
            </div>
            
            <div className="space-y-3">
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-1">Descripción:</h4>
                <p className="text-sm text-gray-900">{buildRuleDescription(rule)}</p>
              </div>
              
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-1">Consecuencia:</h4>
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  {rule.routine.routine_name}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {rules.length === 0 && (
        <div className="text-center py-12">
          <div className="text-gray-400 text-lg mb-2">📋</div>
          <h3 className="text-lg font-medium text-gray-900 mb-1">No hay reglas definidas</h3>
          <p className="text-gray-500">Este sistema fuzzy no tiene reglas configuradas.</p>
        </div>
      )}
    </div>
  );
};

export default RulesSection;