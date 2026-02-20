'use client';

import React from 'react';
import Badge from '@/components/ui/Badge';
import BaseCard from '@/components/ui/BaseCard';
import Button from '@/components/ui/Button';
import MembershipFunctionChart from './MembershipFunctionChart';
import {
  VARIABLE_TYPE_LABELS,
  MEMBERSHIP_FUNCTION_LABELS,
} from '@hydroespinaca/shared';
import type { FuzzyVariable, FuzzyTerm } from '@hydroespinaca/shared';

interface VariableSectionProps {
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
  onAddVariable?: () => void;
  onEditVariable?: (variable: FuzzyVariable) => void;
  onDeleteVariable?: (id: string) => void;
  onAddTerm?: (variable: FuzzyVariable) => void;
  onEditTerm?: (variable: FuzzyVariable, term: FuzzyTerm) => void;
  onDeleteTerm?: (id: string) => void;
}

const VariableSection: React.FC<VariableSectionProps> = ({
  variables,
  terms,
  onAddVariable,
  onEditVariable,
  onDeleteVariable,
  onAddTerm,
  onEditTerm,
  onDeleteTerm,
}) => {
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
        <p className="text-gray-600 font-inter mb-4">
          Este sistema no tiene variables configuradas.
        </p>
        {onAddVariable && (
          <Button variant="primary" size="sm" onClick={onAddVariable}>
            ➕ Agregar variable
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="border-b border-gray-200 pb-4 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 font-inter">
            Variables y Términos
          </h2>
          <p className="text-sm text-gray-600 font-inter mt-1">
            Definición de variables con sus términos lingüísticos y funciones de membresía
          </p>
        </div>
        {onAddVariable && (
          <Button variant="primary" size="sm" onClick={onAddVariable}>
            ➕ Variable
          </Button>
        )}
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
                <div className="flex items-center gap-2">
                  <Badge
                    variant={variable.variableType === 'input' ? 'success' : 'error'}
                    size="md"
                  >
                    {VARIABLE_TYPE_LABELS[variable.variableType]}
                  </Badge>
                  {onEditVariable && (
                    <Button variant="ghost" size="sm" onClick={() => onEditVariable(variable)}>
                      ✏️
                    </Button>
                  )}
                  {onDeleteVariable && (
                    <Button variant="ghost" size="sm" onClick={() => onDeleteVariable(variable.id)} className="text-red-500 hover:bg-red-50">
                      🗑️
                    </Button>
                  )}
                </div>
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
                <div className="flex items-center justify-between mb-4">
                  <h4 className="text-md font-medium text-gray-800 font-inter">
                    Términos Lingüísticos
                  </h4>
                  {onAddTerm && (
                    <Button variant="outline" size="sm" onClick={() => onAddTerm(variable)}>
                      ➕ Término
                    </Button>
                  )}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {variableTerms.map((term) => (
                    <div key={term.id} className="bg-gray-50 p-4 rounded-lg flex items-start justify-between">
                      <div className="flex-1 min-w-0">
                        <h5 className="font-medium text-gray-900 mb-1">{term.label}</h5>
                        <p className="text-sm text-gray-600">{describeMF(term)}</p>
                      </div>
                      <div className="flex items-center gap-1 ml-2 flex-shrink-0">
                        {onEditTerm && (
                          <button
                            onClick={() => onEditTerm(variable, term)}
                            className="p-1.5 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded transition-colors"
                            title="Editar término"
                          >
                            ✏️
                          </button>
                        )}
                        {onDeleteTerm && (
                          <button
                            onClick={() => onDeleteTerm(term.id)}
                            className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                            title="Eliminar término"
                          >
                            🗑️
                          </button>
                        )}
                      </div>
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
                <p className="mb-3">No hay términos definidos para esta variable</p>
                {onAddTerm && (
                  <Button variant="outline" size="sm" onClick={() => onAddTerm(variable)}>
                    ➕ Agregar término
                  </Button>
                )}
              </div>
            )}
          </BaseCard>
        );
      })}
    </div>
  );
};

export default VariableSection;
