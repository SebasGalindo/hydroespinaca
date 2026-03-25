import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { View, StyleSheet, FlatList, TouchableOpacity } from 'react-native';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  typography,
  useBiStore,
  CONSUMPTION_TYPE_LABELS,
  CONSUMPTION_TYPE_UNITS,
  type ManualConsumptionEntry,
  type ConsumptionType,
} from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Badge } from '../atoms/Badge';
import { Card } from '../molecules/Card';
import { Alert } from '../molecules/Alert';
import { SkeletonLoader } from '../organisms/SkeletonLoader';
import { ConfirmationSheet } from '../organisms/ConfirmationSheet';
import { FloatingActionButton } from '../atoms/FloatingActionButton';
import { ConsumptionSummary } from './ConsumptionSummary';
import { ConsumptionForm } from './ConsumptionForm';
import {
  formatCurrency,
  formatDate,
  getDaysAgoISO,
  getTodayISO,
  getConsumptionIcon,
  getConsumptionColor,
  getConsumptionShortLabel,
} from '../../utils/biHelpers';

type TypeFilter = 'all' | ConsumptionType;

const TYPE_FILTERS: { label: string; value: TypeFilter }[] = [
  { label: 'Todos', value: 'all' },
  { label: '⚡ Electricidad', value: 1 },
  { label: '💧 Agua', value: 2 },
  { label: '🌱 Nutrientes', value: 3 },
];

export function ConsumptionSection(): React.ReactElement {
  const [showForm, setShowForm] = useState(false);
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all');
  const [deleteTarget, setDeleteTarget] = useState<ManualConsumptionEntry | null>(null);

  const {
    consumptionEntries,
    consumptionSummary,
    consumptionLoading,
    consumptionError,
    fetchConsumptionEntries,
    fetchConsumptionSummary,
    deleteConsumptionEntry,
  } = useBiStore();

  const from = getDaysAgoISO(30);
  const to = getTodayISO();

  const loadData = useCallback(() => {
    const typeParam = typeFilter === 'all' ? undefined : String(typeFilter);
    fetchConsumptionEntries(from, to, typeParam);
    fetchConsumptionSummary(from, to);
  }, [from, to, typeFilter, fetchConsumptionEntries, fetchConsumptionSummary]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCreated = useCallback(() => {
    setShowForm(false);
    loadData();
  }, [loadData]);

  const handleDelete = useCallback(async () => {
    if (!deleteTarget) return;
    try {
      await deleteConsumptionEntry(deleteTarget.id);
      setDeleteTarget(null);
      loadData();
    } catch {
      // Error handled by store
    }
  }, [deleteTarget, deleteConsumptionEntry, loadData]);

  const filteredEntries = useMemo(() => {
    if (typeFilter === 'all') return consumptionEntries;
    return consumptionEntries.filter((e) => e.type === typeFilter);
  }, [consumptionEntries, typeFilter]);

  if (consumptionLoading && consumptionEntries.length === 0) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={80} style={styles.skeleton} />
        <SkeletonLoader height={200} style={styles.skeleton} />
      </View>
    );
  }

  if (consumptionError) {
    return (
      <View style={styles.container}>
        <Alert type="error" title="Error" message={consumptionError} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Type filter chips */}
      <View style={styles.filterRow}>
        {TYPE_FILTERS.map((f) => {
          const active = typeFilter === f.value;
          return (
            <TouchableOpacity
              key={String(f.value)}
              style={[styles.filterChip, active && styles.filterChipActive]}
              onPress={() => setTypeFilter(f.value)}
            >
              <Text
                variant="caption"
                color={active ? colors.white : semanticColors.textSecondary}
                style={styles.filterChipText}
              >
                {f.label}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>

      {/* Summary */}
      {consumptionSummary && (
        <ConsumptionSummary summary={consumptionSummary} />
      )}

      {/* Entries list */}
      <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionLabel}>
        Registros ({filteredEntries.length})
      </Text>

      {filteredEntries.length === 0 ? (
        <Alert
          type="info"
          message="No hay registros de consumo en el período seleccionado."
        />
      ) : (
        <FlatList
          data={filteredEntries}
          keyExtractor={(item) => item.id}
          scrollEnabled={false}
          renderItem={({ item }) => (
            <EntryItem
              entry={item}
              onDelete={() => setDeleteTarget(item)}
            />
          )}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
        />
      )}

      {/* FAB */}
      <FloatingActionButton
        onPress={() => setShowForm(true)}
        accessibilityLabel="Nuevo consumo"
      />

      {/* Form bottom sheet */}
      <ConsumptionForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onCreated={handleCreated}
      />

      {/* Delete confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteTarget}
        title="Eliminar registro"
        message="¿Estás seguro de que deseas eliminar este registro de consumo? Esta acción no se puede deshacer."
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </View>
  );
}

function EntryItem({
  entry,
  onDelete,
}: {
  entry: ManualConsumptionEntry;
  onDelete: () => void;
}): React.ReactElement {
  const typeColor = getConsumptionColor(entry.type);
  const typeLabel = getConsumptionShortLabel(entry.type);
  const unit = CONSUMPTION_TYPE_UNITS[entry.type];

  return (
    <Card variant="outlined" padding="md">
      <View style={styles.entryRow}>
        <View style={[styles.typeDot, { backgroundColor: typeColor }]} />
        <View style={styles.entryContent}>
          <View style={styles.entryHeader}>
            <Text variant="label" color={semanticColors.textPrimary}>
              {typeLabel}
            </Text>
            <Text variant="caption" color={semanticColors.textTertiary}>
              {entry.dateFrom === entry.dateTo
                ? formatDate(entry.dateFrom)
                : `${formatDate(entry.dateFrom)} — ${formatDate(entry.dateTo)}`}
            </Text>
          </View>
          <View style={styles.entryValues}>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.entryAmount}>
              {entry.amount.toLocaleString('es-CO', { maximumFractionDigits: 2 })} {unit}
            </Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {formatCurrency(entry.costAmount, entry.currencySnapshot)}
            </Text>
          </View>
          {entry.note && (
            <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={1}>
              {entry.note}
            </Text>
          )}
        </View>
        <TouchableOpacity
          onPress={onDelete}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          accessibilityLabel="Eliminar registro"
        >
          <Icon name="trash" size={18} color={colors.error[500]} />
        </TouchableOpacity>
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
    paddingBottom: 80,
  },
  skeleton: {
    borderRadius: borderRadius.lg,
  },
  filterRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  filterChip: {
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius.full,
    borderWidth: 1,
    borderColor: colors.gray[300],
    backgroundColor: colors.white,
  },
  filterChipActive: {
    backgroundColor: colors.hidro[600],
    borderColor: colors.hidro[600],
  },
  filterChipText: {
    fontWeight: typography.fontWeight.medium,
  },
  sectionLabel: {
    fontWeight: typography.fontWeight.semibold,
    marginTop: spacing.sm,
  },
  separator: {
    height: spacing.sm,
  },
  entryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  typeDot: {
    width: 8,
    height: 8,
    borderRadius: borderRadius.sm,
  },
  entryContent: {
    flex: 1,
    gap: 2,
  },
  entryHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  entryValues: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  entryAmount: {
    fontWeight: typography.fontWeight.semibold,
  },
});
