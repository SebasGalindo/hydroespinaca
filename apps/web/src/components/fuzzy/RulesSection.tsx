'use client';

import React from 'react';
import Table from '@/components/ui/Table';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import type { FuzzyRule, FuzzyVariable, FuzzyTerm } from '@hydroespinaca/shared';

interface RulesSectionProps {
  rules: FuzzyRule[];
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
  onAddRule?: () => void;
  onEditRule?: (rule: FuzzyRule) => void;
  onDeleteRule?: (id: string) => void;
}

/**
 * Renders fuzzy rules with Mamdani consequents.
 * Desktop: Table layout. Mobile: Card layout.
 */
const RulesSection: React.FC<RulesSectionProps> = ({ rules, variables, terms, onAddRule, onEditRule, onDeleteRule }) => {
  const variableMap = React.useMemo(
    () => new Map(variables.map((v) => [v.id, v])),
    [variables]
  );

  const termMap = React.useMemo(
    () => new Map(terms.map((t) => [t.id, t])),
    [terms]
  );

  const getVariableName = (id: string) => variableMap.get(id)?.name ?? id;
  const getTermLabel = (id: string) => termMap.get(id)?.label ?? id;

  /**
   * Build human-readable rule text:
   * "SI Temp IS Alta AND Humedad IS Baja ENTONCES Ventilador = {Alto, Máximo} (max)"
   */
  const buildRuleText = (rule: FuzzyRule): string => {
    // Always build locally to resolve variable names (ruleText from backend uses raw IDs)

    // Conditions
    const condParts = rule.conditions.map((c, i) => {
      const varName = getVariableName(c.variableId);
      const op = c.operator === 'IS_NOT' ? 'NO ES' : 'ES';
      const connector = i < rule.connectors.length ? (rule.connectors[i] === 'AND' ? ' Y ' : ' O ') : '';
      return `${varName} ${op} ${c.value}${connector}`;
    });

    // Consequents (Mamdani)
    const consqParts = rule.consequents.map((cq) => {
      const varName = getVariableName(cq.variableId);
      const termLabels = cq.terms.map((tId) => getTermLabel(tId)).join(', ');
      return `${varName} = {${termLabels}}`;
    });

    return `SI ${condParts.join('')} ENTONCES ${consqParts.join('; ')}`;
  };

  if (rules.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <p className="text-gray-400 text-2xl mb-2">📋</p>
        <h3 className="text-lg font-medium text-gray-900 font-inter mb-1">
          No hay reglas definidas
        </h3>
        <p className="text-gray-500 font-inter mb-4">
          Este sistema fuzzy no tiene reglas configuradas.
        </p>
        {onAddRule && (
          <Button variant="primary" size="sm" onClick={onAddRule}>
            ➕ Crear regla
          </Button>
        )}
      </div>
    );
  }

  // Table columns
  const columns = [
    {
      key: 'index',
      label: '#',
      className: 'w-16',
      render: (_: unknown, __: unknown, idx?: number) =>
        String((idx ?? 0) + 1),
    },
    {
      key: 'name',
      label: 'Nombre',
      className: 'w-48',
      render: (_: unknown, row: unknown) => {
        const r = row as FuzzyRule;
        return (
          <span className="font-medium text-gray-900">{r.name}</span>
        );
      },
    },
    {
      key: 'description',
      label: 'Descripción',
      render: (_: unknown, row: unknown) => {
        const r = row as FuzzyRule;
        return (
          <div className="max-w-md xl:max-w-lg text-sm text-gray-700 whitespace-normal">
            {buildRuleText(r)}
          </div>
        );
      },
    },
    {
      key: 'consequents',
      label: 'Consecuentes',
      render: (_: unknown, row: unknown) => {
        const r = row as FuzzyRule;
        return (
          <div className="flex flex-wrap gap-1">
            {r.consequents.map((cq, i) => (
              <Badge key={i} variant="success" size="sm">
                {getVariableName(cq.variableId)}
              </Badge>
            ))}
          </div>
        );
      },
    },
    ...((onEditRule || onDeleteRule) ? [{
      key: 'actions',
      label: '',
      className: 'w-24',
      render: (_: unknown, row: unknown) => {
        const r = row as FuzzyRule;
        return (
          <div className="flex items-center gap-1">
            {onEditRule && (
              <button
                onClick={() => onEditRule(r)}
                className="p-1.5 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded transition-colors"
                title="Editar regla"
              >
                ✏️
              </button>
            )}
            {onDeleteRule && (
              <button
                onClick={() => onDeleteRule(r.id)}
                className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                title="Eliminar regla"
              >
                🗑️
              </button>
            )}
          </div>
        );
      },
    }] : []),
  ];

  // Prepare data with index for table
  const tableData = rules.map((rule, index) => ({ ...rule, index }));

  const mobileCardRender = (item: unknown) => {
    const rule = item as FuzzyRule & { index: number };
    return (
      <div className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-semibold text-gray-900">
            {rule.name}
          </h3>
          <Badge variant="default" size="sm">
            Regla {rule.index + 1}
          </Badge>
        </div>

        <div>
          <h4 className="text-xs font-medium text-gray-500 uppercase mb-1">
            Descripción
          </h4>
          <p className="text-sm text-gray-700">{buildRuleText(rule)}</p>
        </div>

        <div>
          <h4 className="text-xs font-medium text-gray-500 uppercase mb-1">
            Consecuentes
          </h4>
          <div className="flex flex-wrap gap-1">
            {rule.consequents.map((cq, i) => (
              <Badge key={i} variant="success" size="sm">
                {getVariableName(cq.variableId)} → {cq.terms.map((t) => getTermLabel(t)).join(', ')}
              </Badge>
            ))}
          </div>
        </div>

        {(onEditRule || onDeleteRule) && (
          <div className="flex gap-2 pt-2 border-t border-gray-100">
            {onEditRule && (
              <Button variant="ghost" size="sm" onClick={() => onEditRule(rule)}>
                ✏️ Editar
              </Button>
            )}
            {onDeleteRule && (
              <Button variant="ghost" size="sm" onClick={() => onDeleteRule(rule.id)} className="text-red-500 hover:bg-red-50">
                🗑️ Eliminar
              </Button>
            )}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 font-inter">Reglas</h2>
          <p className="text-sm text-gray-600 font-inter mt-1">
            Reglas de inferencia Mamdani del sistema
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Badge variant="default" size="md">
            {rules.length} regla{rules.length !== 1 ? 's' : ''}
          </Badge>
          {onAddRule && (
            <Button variant="primary" size="sm" onClick={onAddRule}>
              ➕ Regla
            </Button>
          )}
        </div>
      </div>

      <Table
        columns={columns}
        data={tableData}
        emptyMessage="No hay reglas definidas"
        responsive={true}
        mobileCardRender={mobileCardRender}
      />
    </div>
  );
};

export default RulesSection;
