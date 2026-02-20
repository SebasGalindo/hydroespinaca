import React, { useEffect, useState, useCallback } from 'react';
import { View, StyleSheet, FlatList, TouchableOpacity, Alert as RNAlert } from 'react-native';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  chartColors,
  typography,
  useBiStore,
  type CostConfigVersion,
} from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';
import { Badge } from '../atoms/Badge';
import { StatCard } from '../molecules/StatCard';
import { Card } from '../molecules/Card';
import { Alert } from '../molecules/Alert';
import { SkeletonLoader } from '../organisms/SkeletonLoader';
import { CostConfigForm } from './CostConfigForm';
import { formatCurrency, formatDateShort } from '../../utils/biHelpers';

export function CostConfigSection(): React.ReactElement {
  const [showForm, setShowForm] = useState(false);
  const [editingVersion, setEditingVersion] = useState<CostConfigVersion | null>(null);

  const {
    currentCostConfig,
    costConfigVersions,
    costConfigLoading,
    costConfigError,
    fetchCurrentCostConfig,
    fetchCostConfigVersions,
    deleteCostConfigVersion,
  } = useBiStore();

  useEffect(() => {
    fetchCurrentCostConfig();
    fetchCostConfigVersions();
  }, [fetchCurrentCostConfig, fetchCostConfigVersions]);

  const handleCreated = useCallback(() => {
    setShowForm(false);
    setEditingVersion(null);
    fetchCurrentCostConfig();
  }, [fetchCurrentCostConfig]);

  const handleEdit = useCallback((version: CostConfigVersion) => {
    setEditingVersion(version);
    setShowForm(true);
  }, []);

  const handleDelete = useCallback((version: CostConfigVersion) => {
    RNAlert.alert(
      '¿Eliminar configuración?',
      'Esta acción no se puede deshacer. Las demás versiones serán recalculadas.',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Eliminar',
          style: 'destructive',
          onPress: async () => {
            try {
              await deleteCostConfigVersion(version.id);
            } catch {
              RNAlert.alert('Error', 'No se pudo eliminar la configuración.');
            }
          },
        },
      ],
    );
  }, [deleteCostConfigVersion]);

  const openCreateForm = useCallback(() => {
    setEditingVersion(null);
    setShowForm(true);
  }, []);

  const closeForm = useCallback(() => {
    setShowForm(false);
    setEditingVersion(null);
  }, []);

  if (costConfigLoading && !currentCostConfig && costConfigVersions.length === 0) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={80} style={styles.skeleton} />
        <SkeletonLoader height={80} style={styles.skeleton} />
        <SkeletonLoader height={80} style={styles.skeleton} />
      </View>
    );
  }

  if (costConfigError) {
    return (
      <View style={styles.container}>
        <Alert type="error" title="Error" message={costConfigError} />
      </View>
    );
  }

  const currency = currentCostConfig?.currency ?? 'COP';

  return (
    <View style={styles.container}>
      {/* Current Config Summary */}
      {currentCostConfig ? (
        <>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionLabel}>
            Precios actuales
          </Text>
          <View style={styles.statsGrid}>
            <StatCard
              label="Electricidad / kWh"
              value={formatCurrency(currentCostConfig.electricityCostPerKwh, currency)}
              icon="bolt"
              iconColor={chartColors.amber}
              style={styles.statCard}
            />
            <StatCard
              label="Agua / Litro"
              value={formatCurrency(currentCostConfig.waterCostPerLiter, currency)}
              icon="droplet"
              iconColor={chartColors.blue}
              style={styles.statCard}
            />
            <StatCard
              label="Nutrientes / Litro"
              value={formatCurrency(currentCostConfig.nutrientCostPerLiter, currency)}
              icon="plant"
              iconColor={chartColors.green}
              style={styles.statCard}
            />
          </View>
        </>
      ) : (
        <Alert
          type="info"
          message="No hay configuración de costos activa. Crea una nueva versión para comenzar."
        />
      )}

      {/* New version button */}
      <Button
        variant="primary"
        size="md"
        onPress={openCreateForm}
        style={styles.newButton}
        leftIcon={<Icon name="add" size={18} color={colors.white} />}
      >
        Nueva Versión
      </Button>

      {/* Version History */}
      {costConfigVersions.length > 0 && (
        <>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionLabel}>
            Historial de versiones
          </Text>
          <FlatList
            data={costConfigVersions}
            keyExtractor={(item) => item.id}
            scrollEnabled={false}
            renderItem={({ item }) => (
              <VersionItem version={item} onEdit={handleEdit} onDelete={handleDelete} />
            )}
            ItemSeparatorComponent={() => <View style={styles.separator} />}
          />
        </>
      )}

      {/* Form bottom sheet */}
      <CostConfigForm
        isOpen={showForm}
        onClose={closeForm}
        onCreated={handleCreated}
        editData={editingVersion}
      />
    </View>
  );
}

function VersionItem({
  version,
  onEdit,
  onDelete,
}: {
  version: CostConfigVersion;
  onEdit: (v: CostConfigVersion) => void;
  onDelete: (v: CostConfigVersion) => void;
}): React.ReactElement {
  const [expanded, setExpanded] = useState(false);

  return (
    <TouchableOpacity onPress={() => setExpanded(!expanded)} activeOpacity={0.7}>
      <Card variant="outlined" padding="md">
        <View style={styles.versionHeader}>
          <View style={styles.versionInfo}>
            {version.isActive && (
              <Badge variant="success" size="sm">Activa</Badge>
            )}
            <Text variant="caption" color={semanticColors.textTertiary}>
              Desde {formatDateShort(version.effectiveFrom)}
              {version.effectiveTo ? ` hasta ${formatDateShort(version.effectiveTo)}` : ''}
            </Text>
          </View>
          <View style={styles.versionActions}>
            <TouchableOpacity
              onPress={(e) => { e.stopPropagation?.(); onEdit(version); }}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Icon name="edit" size={18} color={chartColors.blue} />
            </TouchableOpacity>
            <TouchableOpacity
              onPress={(e) => { e.stopPropagation?.(); onDelete(version); }}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Icon name="trash" size={18} color={semanticColors.error} />
            </TouchableOpacity>
            <Icon
              name={expanded ? 'chevron-up' : 'chevron-right'}
              size={16}
              color={semanticColors.textTertiary}
            />
          </View>
        </View>
        {expanded && (
          <View style={styles.versionDetails}>
            <DetailRow label="Electricidad / kWh" value={formatCurrency(version.electricityCostPerKwh, version.currency)} />
            <DetailRow label="Agua / Litro" value={formatCurrency(version.waterCostPerLiter, version.currency)} />
            <DetailRow label="Nutrientes / Litro" value={formatCurrency(version.nutrientCostPerLiter, version.currency)} />
          </View>
        )}
      </Card>
    </TouchableOpacity>
  );
}

function DetailRow({ label, value }: { label: string; value: string }): React.ReactElement {
  return (
    <View style={styles.detailRow}>
      <Text variant="caption" color={semanticColors.textSecondary}>{label}</Text>
      <Text variant="body" color={semanticColors.textPrimary} style={styles.detailValue}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
    paddingBottom: spacing.xl,
  },
  skeleton: {
    borderRadius: borderRadius.lg,
  },
  sectionLabel: {
    fontWeight: typography.fontWeight.semibold,
    marginTop: spacing.sm,
  },
  statsGrid: {
    gap: spacing.sm,
  },
  statCard: {
    flex: 1,
  },
  newButton: {
    alignSelf: 'flex-start',
  },
  separator: {
    height: spacing.sm,
  },
  versionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  versionInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    flex: 1,
  },
  versionActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  versionDetails: {
    marginTop: spacing.md,
    gap: spacing.xs,
    borderTopWidth: 1,
    borderTopColor: colors.gray[100],
    paddingTop: spacing.md,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  detailValue: {
    fontWeight: typography.fontWeight.semibold,
  },
});
