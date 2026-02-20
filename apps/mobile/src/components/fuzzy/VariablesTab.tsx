/**
 * VariablesTab — Tab de variables del sistema fuzzy.
 * Lista de variables expandibles con sus términos + MembershipChart inline.
 * Botones: agregar variable, agregar término, editar, eliminar.
 */
import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';
import { Card } from '../molecules/Card';
import { ConfirmationSheet } from '../organisms/ConfirmationSheet';
import { MembershipChart } from './MembershipChart';
import { VariableForm } from './VariableForm';
import { TermForm } from './TermForm';
import type {
  FuzzySystemDetail,
  FuzzyVariable,
  FuzzyTerm,
  IconName,
} from '@hydroespinaca/shared';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  typography,
  useFuzzyStore,
} from '@hydroespinaca/shared';
import { getVariableTypeLabel, getMFLabel } from '../../utils/fuzzyHelpers';

export interface VariablesTabProps {
  detail: FuzzySystemDetail;
}

export function VariablesTab({ detail }: VariablesTabProps): React.ReactElement {
  const { variables, terms } = detail;
  const systemId = detail.system.id;

  const { deleteVariable, deleteTerm, crudLoading } = useFuzzyStore();

  const [expandedVarId, setExpandedVarId] = useState<string | null>(null);
  const [showVarForm, setShowVarForm] = useState(false);
  const [editVariable, setEditVariable] = useState<FuzzyVariable | null>(null);
  const [showTermForm, setShowTermForm] = useState(false);
  const [termFormVariable, setTermFormVariable] = useState<FuzzyVariable | null>(null);
  const [editTerm, setEditTerm] = useState<FuzzyTerm | null>(null);
  const [deleteVarTarget, setDeleteVarTarget] = useState<FuzzyVariable | null>(null);
  const [deleteTermTarget, setDeleteTermTarget] = useState<FuzzyTerm | null>(null);

  const toggleExpand = (varId: string) => {
    setExpandedVarId((prev) => (prev === varId ? null : varId));
  };

  const getVarTerms = (varId: string): FuzzyTerm[] =>
    terms.filter((t) => t.variableId === varId);

  const handleDeleteVariable = async () => {
    if (!deleteVarTarget) return;
    try {
      await deleteVariable(deleteVarTarget.id);
    } catch { /* handled by store */ }
    setDeleteVarTarget(null);
  };

  const handleDeleteTerm = async () => {
    if (!deleteTermTarget) return;
    try {
      await deleteTerm(deleteTermTarget.id);
    } catch { /* handled by store */ }
    setDeleteTermTarget(null);
  };

  const handleEditVariable = (v: FuzzyVariable) => {
    setEditVariable(v);
    setShowVarForm(true);
  };

  const handleAddTerm = (v: FuzzyVariable) => {
    setTermFormVariable(v);
    setEditTerm(null);
    setShowTermForm(true);
  };

  const handleEditTerm = (v: FuzzyVariable, t: FuzzyTerm) => {
    setTermFormVariable(v);
    setEditTerm(t);
    setShowTermForm(true);
  };

  return (
    <View style={styles.container}>
      {/* Add Variable Button */}
      <Button
        variant="outline"
        size="sm"
        onPress={() => { setEditVariable(null); setShowVarForm(true); }}
        leftIcon={<Icon name="add" size={16} color={semanticColors.textPrimary} />}
        style={styles.addButton}
      >
        Variable
      </Button>

      {variables.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Icon name="layers" size={48} color={semanticColors.textTertiary} />
          <Text variant="body" color={semanticColors.textSecondary}>
            No hay variables definidas
          </Text>
        </View>
      ) : (
        variables.map((v: FuzzyVariable) => {
          const varTerms = getVarTerms(v.id);
          const isExpanded = expandedVarId === v.id;
          const typeIcon: IconName = v.variableType === 'input' ? 'arrow-down' : 'arrow-up';
          const typeColor = v.variableType === 'input' ? colors.info[500] : colors.hidro[500];

          return (
            <Card key={v.id} variant="outlined" padding="sm">
              {/* Variable Header */}
              <TouchableOpacity
                style={styles.varHeader}
                onPress={() => toggleExpand(v.id)}
                accessibilityRole="button"
                accessibilityLabel={`Variable ${v.name}`}
              >
                <View style={styles.varInfo}>
                  <Icon name={typeIcon} size={16} color={typeColor} />
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.varName} numberOfLines={1}>
                    {v.name}
                  </Text>
                  <View style={[styles.typeBadge, { backgroundColor: `${typeColor}15` }]}>
                    <Text variant="caption" color={typeColor}>
                      {getVariableTypeLabel(v.variableType)}
                    </Text>
                  </View>
                </View>
                <View style={styles.varActions}>
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {varTerms.length} término{varTerms.length !== 1 ? 's' : ''}
                  </Text>
                  <Icon
                    name={isExpanded ? 'chevron-up' : 'chevron-down'}
                    size={16}
                    color={semanticColors.textTertiary}
                  />
                </View>
              </TouchableOpacity>

              {/* Universe Range */}
              <Text variant="caption" color={semanticColors.textTertiary} style={styles.universeText}>
                Universo: [{v.universeMin ?? 0} — {v.universeMax ?? 100}]
                {v.referenceCode ? ` · ${v.referenceCode}` : ''}
                {v.actuatorType ? ` · ${v.actuatorType}` : ''}
              </Text>

              {/* Expanded Content */}
              {isExpanded && (
                <View style={styles.expandedContent}>
                  {/* Action buttons */}
                  <View style={styles.varActionsRow}>
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={() => handleEditVariable(v)}
                      leftIcon={<Icon name="edit" size={14} color={semanticColors.primary} />}
                    >
                      Editar
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={() => handleAddTerm(v)}
                      leftIcon={<Icon name="add" size={14} color={colors.hidro[600]} />}
                    >
                      Término
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={() => setDeleteVarTarget(v)}
                      leftIcon={<Icon name="trash" size={14} color={colors.error[600]} />}
                    >
                      Eliminar
                    </Button>
                  </View>

                  {/* MF Chart */}
                  {varTerms.length > 0 && (
                    <MembershipChart
                      terms={varTerms}
                      universeMin={v.universeMin ?? undefined}
                      universeMax={v.universeMax ?? undefined}
                      height={150}
                    />
                  )}

                  {/* Term List */}
                  {varTerms.map((t: FuzzyTerm) => (
                    <View key={t.id} style={styles.termRow}>
                      <View style={styles.termInfo}>
                        <Text variant="body" color={semanticColors.textPrimary} style={styles.termLabel}>
                          {t.label}
                        </Text>
                        <Text variant="caption" color={semanticColors.textTertiary}>
                          {getMFLabel(t.membershipFunction.functionType)} ·{' '}
                          [{t.membershipFunction.parameters.map((p) => p.toFixed(1)).join(', ')}]
                        </Text>
                      </View>
                      <View style={styles.termActions}>
                        <TouchableOpacity
                          onPress={() => handleEditTerm(v, t)}
                          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                        >
                          <Icon name="edit" size={16} color={semanticColors.primary} />
                        </TouchableOpacity>
                        <TouchableOpacity
                          onPress={() => setDeleteTermTarget(t)}
                          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                        >
                          <Icon name="trash" size={16} color={colors.error[500]} />
                        </TouchableOpacity>
                      </View>
                    </View>
                  ))}

                  {varTerms.length === 0 && (
                    <Text variant="caption" color={semanticColors.textTertiary} style={styles.noTerms}>
                      Sin términos definidos
                    </Text>
                  )}
                </View>
              )}
            </Card>
          );
        })
      )}

      {/* Variable Form */}
      <VariableForm
        isOpen={showVarForm}
        onClose={() => { setShowVarForm(false); setEditVariable(null); }}
        systemId={systemId}
        variable={editVariable}
      />

      {/* Term Form */}
      {termFormVariable && (
        <TermForm
          isOpen={showTermForm}
          onClose={() => { setShowTermForm(false); setEditTerm(null); setTermFormVariable(null); }}
          variable={termFormVariable}
          existingTerms={getVarTerms(termFormVariable.id)}
          term={editTerm}
        />
      )}

      {/* Delete Variable Confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteVarTarget}
        title="Eliminar variable"
        message={`¿Eliminar "${deleteVarTarget?.name}" y todos sus términos?`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDeleteVariable}
        onCancel={() => setDeleteVarTarget(null)}
      />

      {/* Delete Term Confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteTermTarget}
        title="Eliminar término"
        message={`¿Eliminar el término "${deleteTermTarget?.label}"?`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDeleteTerm}
        onCancel={() => setDeleteTermTarget(null)}
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
  varHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  varInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    flex: 1,
  },
  varName: {
    fontWeight: typography.fontWeight.semibold,
    flex: 1,
  },
  typeBadge: {
    paddingVertical: 2,
    paddingHorizontal: spacing.xs,
    borderRadius: borderRadius.lg,
  },
  varActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  universeText: {
    marginTop: 4,
    marginLeft: spacing.lg + spacing.xs,
  },
  expandedContent: {
    marginTop: spacing.sm,
    gap: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.gray[100],
    paddingTop: spacing.sm,
  },
  varActionsRow: {
    flexDirection: 'row',
    gap: spacing.xs,
  },
  termRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.xs,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.gray[100],
  },
  termInfo: {
    flex: 1,
  },
  termLabel: {
    fontWeight: typography.fontWeight.medium,
    marginBottom: 2,
  },
  termActions: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  noTerms: {
    textAlign: 'center',
    paddingVertical: spacing.sm,
  },
});
