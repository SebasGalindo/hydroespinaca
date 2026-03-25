import React, { useEffect, useState, useCallback } from 'react';
import { View, StyleSheet, FlatList, TouchableOpacity } from 'react-native';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  typography,
  useBiStore,
  type ProductionRecord,
} from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Card } from '../molecules/Card';
import { Alert } from '../molecules/Alert';
import { SkeletonLoader } from '../organisms/SkeletonLoader';
import { ConfirmationSheet } from '../organisms/ConfirmationSheet';
import { FloatingActionButton } from '../atoms/FloatingActionButton';
import { ProductionForm } from './ProductionForm';
import { formatCurrency, formatDateShort, getDaysBetween } from '../../utils/biHelpers';

export function ProductionSection(): React.ReactElement {
  const [showForm, setShowForm] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<ProductionRecord | null>(null);

  const {
    productionRecords,
    productionLoading,
    productionError,
    fetchProductionRecords,
    deleteProductionRecord,
  } = useBiStore();

  useEffect(() => {
    fetchProductionRecords();
  }, [fetchProductionRecords]);

  const handleCreated = useCallback(() => {
    setShowForm(false);
  }, []);

  const handleDelete = useCallback(async () => {
    if (!deleteTarget) return;
    try {
      await deleteProductionRecord(deleteTarget.id);
      setDeleteTarget(null);
    } catch {
      // Error handled by store
    }
  }, [deleteTarget, deleteProductionRecord]);

  if (productionLoading && productionRecords.length === 0) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={100} style={styles.skeleton} />
        <SkeletonLoader height={100} style={styles.skeleton} />
      </View>
    );
  }

  if (productionError) {
    return (
      <View style={styles.container}>
        <Alert type="error" title="Error" message={productionError} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionLabel}>
        Cosechas ({productionRecords.length})
      </Text>

      {productionRecords.length === 0 ? (
        <Alert
          type="info"
          message="No hay registros de producción. Agrega tu primera cosecha."
        />
      ) : (
        <FlatList
          data={productionRecords}
          keyExtractor={(item) => item.id}
          scrollEnabled={false}
          renderItem={({ item }) => (
            <ProductionItem
              record={item}
              onDelete={() => setDeleteTarget(item)}
            />
          )}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
        />
      )}

      {/* FAB */}
      <FloatingActionButton
        onPress={() => setShowForm(true)}
        accessibilityLabel="Nueva producción"
      />

      {/* Form bottom sheet */}
      <ProductionForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onCreated={handleCreated}
      />

      {/* Delete confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteTarget}
        title="Eliminar cosecha"
        message={`¿Eliminar el registro de "${deleteTarget?.cropName ?? ''}"? Esta acción no se puede deshacer.`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </View>
  );
}

function ProductionItem({
  record,
  onDelete,
}: {
  record: ProductionRecord;
  onDelete: () => void;
}): React.ReactElement {
  const days = getDaysBetween(record.startDate, record.harvestDate);
  const estimatedRevenue = record.kilosProduced * record.pricePerKilo;

  return (
    <Card variant="outlined" padding="md">
      <View style={styles.itemContent}>
        <View style={styles.itemHeader}>
          <View style={styles.itemTitleRow}>
            <Icon name="leaf" size={18} color={colors.hidro[600]} />
            <Text variant="label" color={semanticColors.textPrimary} style={styles.cropName}>
              {record.cropName}
            </Text>
          </View>
          <TouchableOpacity
            onPress={onDelete}
            hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            accessibilityLabel="Eliminar cosecha"
          >
            <Icon name="trash" size={18} color={colors.error[500]} />
          </TouchableOpacity>
        </View>

        <View style={styles.itemDetails}>
          <View style={styles.detailItem}>
            <Text variant="caption" color={semanticColors.textTertiary}>Período</Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {formatDateShort(record.startDate)} → {formatDateShort(record.harvestDate)} ({days}d)
            </Text>
          </View>
          <View style={styles.detailItem}>
            <Text variant="caption" color={semanticColors.textTertiary}>Producción</Text>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
              {record.kilosProduced.toLocaleString('es-CO')} kg
            </Text>
          </View>
          <View style={styles.detailItem}>
            <Text variant="caption" color={semanticColors.textTertiary}>Precio/kg</Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {formatCurrency(record.pricePerKilo, record.currency)}
            </Text>
          </View>
          <View style={styles.detailItem}>
            <Text variant="caption" color={semanticColors.textTertiary}>Ingreso est.</Text>
            <Text variant="body" color={colors.hidro[700]} style={styles.boldText}>
              {formatCurrency(estimatedRevenue, record.currency)}
            </Text>
          </View>
        </View>

        {record.note && (
          <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={2}>
            {record.note}
          </Text>
        )}
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
  sectionLabel: {
    fontWeight: typography.fontWeight.semibold,
  },
  separator: {
    height: spacing.sm,
  },
  itemContent: {
    gap: spacing.sm,
  },
  itemHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  itemTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  cropName: {
    fontWeight: typography.fontWeight.semibold,
  },
  itemDetails: {
    gap: spacing.xs,
  },
  detailItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  boldText: {
    fontWeight: typography.fontWeight.semibold,
  },
});
