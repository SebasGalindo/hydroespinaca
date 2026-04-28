import React, { useEffect, useState, useCallback } from 'react';
import { View, StyleSheet, TouchableOpacity, FlatList, TextInput, Switch } from 'react-native';
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
import { Button } from '../atoms/Button';
import { Card } from '../molecules/Card';
import { Alert } from '../molecules/Alert';
import { SkeletonLoader } from '../organisms/SkeletonLoader';
import { ProfitabilityResult } from './ProfitabilityResult';
import { formatCurrency, formatDateShort, getDaysBetween } from '../../utils/biHelpers';

export function ProfitabilitySection(): React.ReactElement {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [initialInvestmentCost, setInitialInvestmentCost] = useState<string>('');
  const [includeAutoEnergy, setIncludeAutoEnergy] = useState(true);

  const {
    productionRecords,
    productionLoading,
    profitabilityResult,
    profitabilityLoading,
    profitabilityError,
    fetchProductionRecords,
    calculateProfitability,
  } = useBiStore();

  useEffect(() => {
    if (productionRecords.length === 0) {
      fetchProductionRecords();
    }
  }, [productionRecords.length, fetchProductionRecords]);

  const handleCalculate = useCallback(async () => {
    if (!selectedId) return;
    const parsed = initialInvestmentCost.trim() !== '' ? parseFloat(initialInvestmentCost.replace(',', '.')) : undefined;
    await calculateProfitability({
      productionRecordId: selectedId,
      initialInvestmentCost: parsed !== undefined && !isNaN(parsed) ? parsed : undefined,
      includeAutomaticEnergyCalculation: includeAutoEnergy,
    });
  }, [selectedId, initialInvestmentCost, calculateProfitability]);

  if (productionLoading && productionRecords.length === 0) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={100} style={styles.skeleton} />
        <SkeletonLoader height={100} style={styles.skeleton} />
      </View>
    );
  }

  if (productionRecords.length === 0) {
    return (
      <View style={styles.container}>
        <Alert
          type="info"
          title="Sin registros"
          message="Primero debes agregar un registro de producción en la pestaña de Producción."
        />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionLabel}>
        Selecciona una cosecha
      </Text>

      <FlatList
        data={productionRecords}
        keyExtractor={(item) => item.id}
        scrollEnabled={false}
        renderItem={({ item }) => (
          <ProductionSelector
            record={item}
            isSelected={selectedId === item.id}
            onSelect={() => setSelectedId(selectedId === item.id ? null : item.id)}
          />
        )}
        ItemSeparatorComponent={() => <View style={styles.separator} />}
      />

      {/* Optional initial investment input */}
      <View style={styles.investmentInputWrapper}>
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.inputLabel}>
          Inversión inicial (opcional)
        </Text>
        <TextInput
          style={styles.investmentInput}
          value={initialInvestmentCost}
          onChangeText={setInitialInvestmentCost}
          placeholder="Ej: 850000 (COP)"
          placeholderTextColor={colors.gray[400]}
          keyboardType="numeric"
        />
        <Text variant="caption" color={semanticColors.textTertiary}>
          Hardware, infraestructura e instalación. Permite calcular el ROI real del ciclo.
        </Text>
      </View>

      {/* Auto energy switch */}
      <View style={styles.switchRow}>
        <View style={styles.switchTextContainer}>
          <Text variant="label" color={semanticColors.textPrimary}>
            Consumo automático de actuadores
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary}>
            Desactívalo si ya registraste ese consumo manualmente
          </Text>
        </View>
        <Switch
          value={includeAutoEnergy}
          onValueChange={setIncludeAutoEnergy}
          trackColor={{ false: colors.gray[300], true: colors.hidro[400] }}
          thumbColor={includeAutoEnergy ? colors.hidro[600] : colors.gray[50]}
        />
      </View>

      {/* Calculate button */}
      <Button
        variant="primary"
        size="lg"
        onPress={handleCalculate}
        disabled={!selectedId || profitabilityLoading}
        loading={profitabilityLoading}
        leftIcon={<Icon name="calculator" size={18} color={colors.white} />}
        style={styles.calcButton}
      >
        Calcular Rentabilidad
      </Button>

      {/* Error */}
      {profitabilityError && (
        <Alert type="error" title="Error" message={profitabilityError} />
      )}

      {/* Loading skeleton */}
      {profitabilityLoading && (
        <View style={styles.skeletonGroup}>
          <SkeletonLoader height={100} style={styles.skeleton} />
          <SkeletonLoader height={80} style={styles.skeleton} />
          <SkeletonLoader height={120} style={styles.skeleton} />
        </View>
      )}

      {/* Result */}
      {profitabilityResult && !profitabilityLoading && (
        <ProfitabilityResult result={profitabilityResult} />
      )}
    </View>
  );
}

function ProductionSelector({
  record,
  isSelected,
  onSelect,
}: {
  record: ProductionRecord;
  isSelected: boolean;
  onSelect: () => void;
}): React.ReactElement {
  const days = getDaysBetween(record.startDate, record.harvestDate);
  const estimatedRevenue = record.kilosProduced * record.pricePerKilo;

  return (
    <TouchableOpacity onPress={onSelect} activeOpacity={0.7}>
      <Card
        variant="outlined"
        padding="md"
        style={isSelected ? styles.selectedCard : undefined}
      >
        <View style={styles.selectorContent}>
          <View style={styles.radioOuter}>
            {isSelected && <View style={styles.radioInner} />}
          </View>
          <View style={styles.selectorInfo}>
            <Text variant="label" color={semanticColors.textPrimary} style={styles.boldText}>
              {record.cropName}
            </Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {formatDateShort(record.startDate)} → {formatDateShort(record.harvestDate)} · {days}d
            </Text>
            <View style={styles.selectorStats}>
              <Text variant="caption" color={semanticColors.textSecondary}>
                {record.kilosProduced.toLocaleString('es-CO')} kg
              </Text>
              <Text variant="caption" color={colors.hidro[700]} style={styles.boldText}>
                {formatCurrency(estimatedRevenue, record.currency)}
              </Text>
            </View>
          </View>
        </View>
      </Card>
    </TouchableOpacity>
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
  skeletonGroup: {
    gap: spacing.sm,
  },
  sectionLabel: {
    fontWeight: typography.fontWeight.semibold,
  },
  separator: {
    height: spacing.sm,
  },
  selectedCard: {
    borderColor: colors.hidro[500],
    borderWidth: 2,
  },
  selectorContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  radioOuter: {
    width: 22,
    height: 22,
    borderRadius: 11,
    borderWidth: 2,
    borderColor: colors.gray[400],
    justifyContent: 'center',
    alignItems: 'center',
  },
  radioInner: {
    width: 12,
    height: 12,
    borderRadius: 6,
    backgroundColor: colors.hidro[600],
  },
  selectorInfo: {
    flex: 1,
    gap: 2,
  },
  selectorStats: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 2,
  },
  boldText: {
    fontWeight: typography.fontWeight.semibold,
  },
  calcButton: {
    marginTop: spacing.sm,
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  switchTextContainer: {
    flex: 1,
    gap: 2,
  },
  investmentInputWrapper: {
    gap: spacing.xs,
  },
  inputLabel: {
    fontWeight: typography.fontWeight.semibold,
  },
  investmentInput: {
    borderWidth: 1,
    borderColor: colors.gray[300],
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    fontSize: 14,
    color: colors.gray[900],
    backgroundColor: colors.white,
    fontFamily: typography.fontFamily.regular,
  },
});
