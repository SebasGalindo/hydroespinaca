import React from 'react';
import { View, StyleSheet } from 'react-native';
import { spacing, semanticColors, chartColors, typography, type BiSummary } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { StatCard } from '../molecules/StatCard';
import { formatCurrency, formatAmount } from '../../utils/biHelpers';

interface ConsumptionSummaryProps {
  summary: BiSummary;
}

export function ConsumptionSummary({ summary }: ConsumptionSummaryProps): React.ReactElement {
  const currency = summary.currency;

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textSecondary} style={styles.title}>
        Resumen del período
      </Text>
      <View style={styles.grid}>
        <StatCard
          label="Electricidad"
          value={formatAmount(summary.totalElectricityKwh, 'kWh')}
          icon="bolt"
          iconColor={chartColors.amber}
          trendValue={formatCurrency(summary.costElectricity, currency)}
          trend="neutral"
          style={styles.card}
        />
        <StatCard
          label="Agua"
          value={formatAmount(summary.totalWaterLiters, 'L')}
          icon="droplet"
          iconColor={chartColors.blue}
          trendValue={formatCurrency(summary.costWater, currency)}
          trend="neutral"
          style={styles.card}
        />
        <StatCard
          label="Nutrientes"
          value={formatAmount(summary.totalNutrientLiters, 'L')}
          icon="plant"
          iconColor={chartColors.green}
          trendValue={formatCurrency(summary.costNutrients, currency)}
          trend="neutral"
          style={styles.card}
        />
        <StatCard
          label="Costo Total"
          value={formatCurrency(summary.costTotal, currency)}
          icon="wallet"
          iconColor={semanticColors.primary}
          style={styles.card}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.sm,
  },
  title: {
    fontWeight: typography.fontWeight.semibold,
  },
  grid: {
    gap: spacing.sm,
  },
  card: {
    flex: 1,
  },
});
