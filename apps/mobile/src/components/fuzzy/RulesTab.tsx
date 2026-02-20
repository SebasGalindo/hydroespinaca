/**
 * RulesTab — Tab de reglas del sistema fuzzy.
 * Lista de reglas con texto en lenguaje natural.
 * Cada regla como card expandible.
 */
import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';
import { Card } from '../molecules/Card';
import { ConfirmationSheet } from '../organisms/ConfirmationSheet';
import { RuleForm } from './RuleForm';
import type {
  FuzzySystemDetail,
  FuzzyRule,
  FuzzyVariable,
  FuzzyTerm,
} from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, typography, useFuzzyStore } from '@hydroespinaca/shared';
import { generateRuleText } from '../../utils/fuzzyHelpers';

export interface RulesTabProps {
  detail: FuzzySystemDetail;
}

export function RulesTab({ detail }: RulesTabProps): React.ReactElement {
  const { rules, variables, terms } = detail;
  const systemId = detail.system.id;

  const { deleteRule } = useFuzzyStore();

  const [expandedRuleId, setExpandedRuleId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editRule, setEditRule] = useState<FuzzyRule | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<FuzzyRule | null>(null);

  const toggleExpand = (ruleId: string) => {
    setExpandedRuleId((prev) => (prev === ruleId ? null : ruleId));
  };

  const handleDeleteRule = async () => {
    if (!deleteTarget) return;
    try {
      await deleteRule(deleteTarget.id);
    } catch { /* handled by store */ }
    setDeleteTarget(null);
  };

  const handleEdit = (r: FuzzyRule) => {
    setEditRule(r);
    setShowForm(true);
  };

  return (
    <View style={styles.container}>
      {/* Add Rule Button */}
      <Button
        variant="outline"
        size="sm"
        onPress={() => { setEditRule(null); setShowForm(true); }}
        leftIcon={<Icon name="add" size={16} color={semanticColors.textPrimary} />}
        style={styles.addButton}
      >
        Regla
      </Button>

      {rules.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Icon name="git-branch" size={48} color={semanticColors.textTertiary} />
          <Text variant="body" color={semanticColors.textSecondary}>
            No hay reglas definidas
          </Text>
        </View>
      ) : (
        rules.map((r: FuzzyRule, index: number) => {
          const isExpanded = expandedRuleId === r.id;
          const ruleText = r.ruleText || generateRuleText(r, variables, terms);

          return (
            <Card key={r.id} variant="outlined" padding="sm">
              <TouchableOpacity
                style={styles.ruleHeader}
                onPress={() => toggleExpand(r.id)}
                accessibilityRole="button"
              >
                <View style={styles.ruleInfo}>
                  <View style={styles.ruleNumber}>
                    <Text variant="caption" color={semanticColors.textInverse}>
                      {index + 1}
                    </Text>
                  </View>
                  <View style={styles.ruleTextContainer}>
                    <Text variant="body" color={semanticColors.textPrimary} numberOfLines={1} style={styles.ruleName}>
                      {r.name}
                    </Text>
                    <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={isExpanded ? undefined : 2}>
                      {ruleText}
                    </Text>
                  </View>
                </View>
                <Icon
                  name={isExpanded ? 'chevron-up' : 'chevron-down'}
                  size={16}
                  color={semanticColors.textTertiary}
                />
              </TouchableOpacity>

              {isExpanded && (
                <View style={styles.expandedContent}>
                  {r.description && (
                    <Text variant="caption" color={semanticColors.textTertiary} style={styles.description}>
                      {r.description}
                    </Text>
                  )}

                  {/* Conditions Detail */}
                  <View style={styles.detailSection}>
                    <Text variant="label" color={semanticColors.textSecondary}>
                      Condiciones ({r.conditions.length})
                    </Text>
                    {r.conditions.map((c, i) => {
                      const varName = variables.find((v) => v.id === c.variableId)?.name ?? '?';
                      const termLabel = terms.find((t) => t.id === c.value)?.label ?? '?';
                      return (
                        <Text key={`c-${i}`} variant="caption" color={semanticColors.textPrimary}>
                          {i > 0 ? `${r.connectors[i - 1] || 'AND'} ` : ''}
                          {varName} {c.operator === 'IS_NOT' ? 'NO ES' : 'ES'} {termLabel}
                        </Text>
                      );
                    })}
                  </View>

                  {/* Consequents Detail */}
                  <View style={styles.detailSection}>
                    <Text variant="label" color={semanticColors.textSecondary}>
                      Consecuentes ({r.consequents.length})
                    </Text>
                    {r.consequents.map((c, i) => {
                      const varName = variables.find((v) => v.id === c.variableId)?.name ?? '?';
                      const termLabels = c.terms.map((tid) => terms.find((t) => t.id === tid)?.label ?? '?').join(', ');
                      return (
                        <Text key={`q-${i}`} variant="caption" color={semanticColors.textPrimary}>
                          {varName} = {termLabels}
                        </Text>
                      );
                    })}
                  </View>

                  <View style={styles.ruleActions}>
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={() => handleEdit(r)}
                      leftIcon={<Icon name="edit" size={14} color={semanticColors.primary} />}
                    >
                      Editar
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={() => setDeleteTarget(r)}
                      leftIcon={<Icon name="trash" size={14} color={colors.error[600]} />}
                    >
                      Eliminar
                    </Button>
                  </View>
                </View>
              )}
            </Card>
          );
        })
      )}

      {/* Rule Form */}
      <RuleForm
        isOpen={showForm}
        onClose={() => { setShowForm(false); setEditRule(null); }}
        systemId={systemId}
        variables={variables}
        terms={terms}
        rule={editRule}
      />

      {/* Delete Confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteTarget}
        title="Eliminar regla"
        message={`¿Eliminar la regla "${deleteTarget?.name}"?`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDeleteRule}
        onCancel={() => setDeleteTarget(null)}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  addButton: {
    alignSelf: 'flex-start',
  },
  emptyContainer: {
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.sm,
  },
  ruleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  ruleInfo: {
    flexDirection: 'row',
    flex: 1,
    gap: spacing.sm,
  },
  ruleNumber: {
    width: 22,
    height: 22,
    borderRadius: 11,
    backgroundColor: colors.hidro[500],
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 2,
  },
  ruleTextContainer: {
    flex: 1,
  },
  ruleName: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: 2,
  },
  expandedContent: {
    marginTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.gray[100],
    paddingTop: spacing.sm,
    gap: spacing.sm,
  },
  description: {
    fontStyle: 'italic',
  },
  detailSection: {
    gap: 2,
  },
  ruleActions: {
    flexDirection: 'row',
    gap: spacing.xs,
  },
});
