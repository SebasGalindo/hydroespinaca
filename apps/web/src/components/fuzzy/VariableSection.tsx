'use client';

import React from 'react';
import { SimpleFuzzyVariable, SimpleFuzzyTerm } from '@hydroespinaca/shared';
import MembershipChart from './MembershipChart';

interface VariableSectionProps {
  variables: SimpleFuzzyVariable[];
  terms: SimpleFuzzyTerm[];
}

const VariableSection: React.FC<VariableSectionProps> = ({ variables, terms }) => {
  // Agrupar términos por variable
  const getTermsByVariable = (variableId: string) => {
    return terms.filter(term => term.variable_id === variableId);
  };

  // Función para formatear el rango de una función de membresía
  const formatRange = (membershipFunction: any): string => {
    return `[${membershipFunction.universe_min}, ${membershipFunction.universe_max}]`;
  };

  // Función para describir la función de membresía
  const describeMembershipFunction = (membershipFunction: any): string => {
    const { function_type, parameters } = membershipFunction;
    switch (function_type) {
      case 'triangular':
        return `Triangular (${parameters[0]}, ${parameters[1]}, ${parameters[2]})`;
      case 'trapezoidal':
        return `Trapezoidal (${parameters[0]}, ${parameters[1]}, ${parameters[2]}, ${parameters[3]})`;
      case 'gaussian':
        return `Gaussiana (μ=${parameters[0]}, σ=${parameters[1]})`;
      default:
        return `${function_type} (parámetros: ${parameters.join(', ')})`;
    }
  };

  return (
    <div className="space-y-8">
      <div className="border-b border-gray-200 pb-4">
        <h2 className="text-xl font-semibold text-gray-900 font-inter">
          Variables y Términos
        </h2>
        <p className="text-sm text-gray-600 font-inter mt-1">
          Definición de variables con sus términos lingüísticos y funciones de membresía
        </p>
      </div>

      {variables.map((variable) => {
        const variableTerms = getTermsByVariable(variable.id);
        
        return (
          <div key={variable.id} className="bg-white border border-gray-200 rounded-lg p-6">
            {/* Header de la variable */}
            <div className="mb-6">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-lg font-semibold text-gray-900 font-inter">
                  {variable.name}
                </h3>
                <span className={`px-3 py-1 rounded-full text-sm font-bold ${
                  variable.variable_type === 'input' 
                    ? 'bg-green-100 text-green-800 border border-green-200' 
                    : 'bg-red-100 text-red-800 border border-red-200'
                }`}>
                  {variable.variable_type === 'input' ? 'Variable de Entrada' : 'Variable de Salida'}
                </span>
              </div>
              <p className="text-sm text-gray-600 font-inter mb-2">
                {variable.description}
              </p>
              <div className="flex md:flex-nowrap flex-wrap items-center space-x-4 text-sm text-gray-500 ">
                <span>Dispositivo: {variable.device_id}</span>
                <span>Términos: {variableTerms.length}</span>
                {variableTerms.length > 0 && variableTerms[0] && (
                  <span className="font-medium text-gray-500">
                    Rango: {formatRange(variableTerms[0].membership_function)}
                  </span>
                )}
              </div>
            </div>

            {/* Lista de términos */}
            {variableTerms.length > 0 && (
              <div className="mb-6">
                <h4 className="text-md font-medium text-gray-800 font-inter mb-4">
                  Términos Lingüísticos
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {variableTerms.map((term) => (
                    <div key={term.id} className="bg-gray-50 p-4 rounded-lg">
                      <h4 className="font-medium text-gray-900 mb-2">{term.label}</h4>
                      <p className="text-sm text-gray-600">
                        {describeMembershipFunction(term.membership_function)}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Gráfica de funciones de membresía */}
            {variableTerms.length > 0 && (
              <div className="mt-6">
                <MembershipChart variable={variable} terms={variableTerms} />
              </div>
            )}

            {/* Mensaje si no hay términos */}
            {variableTerms.length === 0 && (
              <div className="text-center py-8 text-gray-500">
                <p className="font-inter">No hay términos definidos para esta variable</p>
              </div>
            )}
          </div>
        );
      })}

      {/* Mensaje si no hay variables */}
      {variables.length === 0 && (
        <div className="text-center py-12 bg-gray-50 rounded-lg">
          <h3 className="text-lg font-medium text-gray-900 font-inter mb-2">
            No hay variables definidas
          </h3>
          <p className="text-gray-600 font-inter">
            Este sistema no tiene variables configuradas
          </p>
        </div>
      )}
    </div>
  );
};

export default VariableSection;