'use client';

import React from 'react';
import Table from '@/components/ui/Table';
import Badge from '@/components/ui/Badge';
import type { FuzzyRule, FuzzyVariable, FuzzyTerm } from '@hydroespinaca/shared';

interface RulesSectionProps {
  rules: FuzzyRule[];
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
}

/**
 * Renders fuzzy rules with Mamdani consequents.
 * Desktop: Table layout. Mobile: Card layout.
 */
const RulesSection: React.FC<RulesSectionProps> = ({ rules, variables, terms }) => {
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
    if (rule.ruleText) return rule.ruleText;

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
        <p className="text-gray-500 font-inter">
          Este sistema fuzzy no tiene reglas configuradas.
        </p>
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
        <Badge variant="default" size="md">
          {rules.length} regla{rules.length !== 1 ? 's' : ''}
        </Badge>
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
