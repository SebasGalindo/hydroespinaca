'use client';

import React from 'react';
import Badge from '@/components/ui/Badge';
import BaseCard from '@/components/ui/BaseCard';
import MembershipFunctionChart from './MembershipFunctionChart';
import {
  VARIABLE_TYPE_LABELS,
  MEMBERSHIP_FUNCTION_LABELS,
} from '@hydroespinaca/shared';
import type { FuzzyVariable, FuzzyTerm } from '@hydroespinaca/shared';

interface VariableSectionProps {
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
}

const VariableSection: React.FC<VariableSectionProps> = ({ variables, terms }) => {
  const getTermsByVariable = (variableId: string): FuzzyTerm[] =>
    terms.filter((t) => t.variableId === variableId);

  const describeMF = (term: FuzzyTerm): string => {
    const { functionType, parameters } = term.membershipFunction;
    const label = MEMBERSHIP_FUNCTION_LABELS[functionType] ?? functionType;

    switch (functionType) {
      case 'triangular':
        return `${label} (${parameters[0]}, ${parameters[1]}, ${parameters[2]})`;
      case 'trapezoidal':
      case 'pi_shaped':
        return `${label} (${parameters[0]}, ${parameters[1]}, ${parameters[2]}, ${parameters[3]})`;
      case 'gaussian':
        return `${label} (μ=${parameters[0]}, σ=${parameters[1]})`;
      case 'bell':
        return `${label} (a=${parameters[0]}, b=${parameters[1]}, c=${parameters[2]})`;
      case 'sigmoid':
        return `${label} (a=${parameters[0]}, c=${parameters[1]})`;
      case 'z_shaped':
      case 's_shaped':
        return `${label} (a=${parameters[0]}, b=${parameters[1]})`;
      case 'linear':
        return `${label} (m=${parameters[0]}, b=${parameters[1]})`;
      case 'constant':
        return `${label} (${parameters[0]})`;
      default:
        return `${label} (${parameters.join(', ')})`;
    }
  };

  if (variables.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <h3 className="text-lg font-medium text-gray-900 font-inter mb-2">
          No hay variables definidas
        </h3>
        <p className="text-gray-600 font-inter">
          Este sistema no tiene variables configuradas.
        </p>
      </div>
    );
  }

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
        const universeRange =
          variable.universeMin != null && variable.universeMax != null
            ? `[${variable.universeMin}, ${variable.universeMax}]`
            : variableTerms.length > 0
              ? `[${variableTerms[0]!.membershipFunction.universeMin}, ${variableTerms[0]!.membershipFunction.universeMax}]`
              : null;

        return (
          <BaseCard key={variable.id} hover={false} className="border border-gray-200">
            {/* Variable header */}
            <div className="mb-6">
              <div className="flex items-center justify-between mb-2 flex-wrap gap-2">
                <h3 className="text-lg font-semibold text-gray-900 font-inter">
                  {variable.name}
                </h3>
                <Badge
                  variant={variable.variableType === 'input' ? 'success' : 'error'}
                  size="md"
                >
                  {VARIABLE_TYPE_LABELS[variable.variableType]}
                </Badge>
              </div>

              {variable.description && (
                <p className="text-sm text-gray-600 font-inter mb-2">
                  {variable.description}
                </p>
              )}

              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-gray-500">
                {variable.referenceCode && (
                  <span>
                    Código: <strong className="text-gray-700">{variable.referenceCode}</strong>
                  </span>
                )}
                <span>Términos: {variableTerms.length}</span>
                {universeRange && (
                  <span className="font-medium text-gray-500">Rango: {universeRange}</span>
                )}
                {variable.actuatorType && (
                  <Badge variant="info" size="sm">
                    {variable.actuatorType}
                  </Badge>
                )}
              </div>
            </div>

            {/* Term list */}
            {variableTerms.length > 0 && (
              <div className="mb-6">
                <h4 className="text-md font-medium text-gray-800 font-inter mb-4">
                  Términos Lingüísticos
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {variableTerms.map((term) => (
                    <div key={term.id} className="bg-gray-50 p-4 rounded-lg">
                      <h5 className="font-medium text-gray-900 mb-1">{term.label}</h5>
                      <p className="text-sm text-gray-600">{describeMF(term)}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Chart */}
            {variableTerms.length > 0 && (
              <div className="mt-4">
                <MembershipFunctionChart variable={variable} terms={variableTerms} />
              </div>
            )}

            {variableTerms.length === 0 && (
              <div className="text-center py-8 text-gray-500 font-inter">
                No hay términos definidos para esta variable
              </div>
            )}
          </BaseCard>
        );
      })}
    </div>
  );
};

export default VariableSection;
