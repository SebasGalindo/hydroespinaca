import React from 'react';
import { View, StyleSheet } from 'react-native';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  chartColors,
  typography,
  CONSUMPTION_TYPE_LABELS,
  CONSUMPTION_TYPE_UNITS,
  type ProfitabilityResponse,
  type ConsumptionType,
} from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Badge } from '../atoms/Badge';
import { StatCard } from '../molecules/StatCard';
import { Card } from '../molecules/Card';
import { formatCurrency, formatDateShort, getDaysBetween } from '../../utils/biHelpers';

interface ProfitabilityResultProps {
  result: ProfitabilityResponse;
}

export function ProfitabilityResult({ result }: ProfitabilityResultProps): React.ReactElement {
  const {
    production, expenses, revenue, netBenefit,
    profitMarginPercent, roiPercent, costPerKiloProduced,
    waterFootprintLitersPerKg, currency,
  } = result;
  const isProfit = netBenefit >= 0;

  return (
    <View style={styles.container}>
      {/* Net result banner */}
      <Card
        variant="filled"
        padding="lg"
        style={{
          ...styles.resultBanner,
          backgroundColor: isProfit ? colors.hidro[50] : colors.error[50],
        }}
      >
        <View style={styles.resultContent}>
          <Icon
            name={isProfit ? 'arrow-up' : 'arrow-down'}
            size={32}
            color={isProfit ? colors.hidro[600] : colors.error[600]}
          />
          <View style={styles.resultText}>
            <Text
              variant="h2"
              color={isProfit ? colors.hidro[700] : colors.error[700]}
              style={styles.resultValue}
            >
              {formatCurrency(Math.abs(netBenefit), currency)}
            </Text>
            <Text
              variant="caption"
              color={isProfit ? colors.hidro[600] : colors.error[600]}
            >
              {isProfit ? 'Ganancia neta' : 'Pérdida neta'} · Margen {profitMarginPercent.toFixed(1)}%
              {roiPercent !== undefined && roiPercent !== null
                ? `  ·  ROI ${roiPercent.toFixed(1)}%`
                : ''}
            </Text>
          </View>
        </View>

        {/* Explanatory captions */}
        <View style={styles.captionBlock}>
          <Text variant="caption" color={semanticColors.textTertiary}>
            <Text variant="caption" color={semanticColors.textSecondary} style={styles.boldText}>Resultado Neto: </Text>
            {isProfit
              ? 'Positivo — tus ingresos superaron los gastos. El ciclo fue rentable.'
              : 'Negativo — los gastos superaron los ingresos. Revisa costos o ajusta el precio de venta.'}
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary} style={styles.captionLine}>
            <Text variant="caption" color={semanticColors.textSecondary} style={styles.boldText}>Margen ({profitMarginPercent.toFixed(1)}%): </Text>
            {'De cada peso ingresado, ese porcentaje es ganancia neta. '}
            {profitMarginPercent >= 20
              ? 'Bueno — superior al 20%, considerado saludable en hidropónicos.'
              : profitMarginPercent >= 5
              ? 'Moderado — entre 5% y 20%. Busca reducir costos operacionales.'
              : 'Bajo — inferior al 5%. Revisa precios, consumo y desperdicios.'}
          </Text>
          {roiPercent !== undefined && roiPercent !== null && (
            <Text variant="caption" color={semanticColors.textTertiary} style={styles.captionLine}>
              <Text variant="caption" color={semanticColors.textSecondary} style={styles.boldText}>ROI ({roiPercent.toFixed(1)}%): </Text>
              {`Retorno sobre la inversión — por cada $100 invertidos recuperas $${(100 + roiPercent).toFixed(0)}. `}
              {roiPercent >= 30
                ? 'Excelente — supera el 30%, muy rentable para el período.'
                : roiPercent >= 10
                ? 'Aceptable — entre 10% y 30%. Hay margen de mejora.'
                : roiPercent >= 0
                ? 'Bajo — recuperas la inversión pero con poco margen.'
                : 'Negativo — aún no recuperas la inversión inicial en este ciclo.'}
            </Text>
          )}
        </View>
      </Card>

      {/* Production info */}
      <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionTitle}>
        Producción
      </Text>
      <Card variant="outlined" padding="md">
        <View style={styles.detailsGrid}>
          <DetailRow label="Cultivo" value={production.cropName} />
          <DetailRow
            label="Período"
            value={`${formatDateShort(production.startDate)} → ${formatDateShort(production.harvestDate)} (${getDaysBetween(production.startDate, production.harvestDate)}d)`}
          />
          <DetailRow
            label="Producción"
            value={`${production.kilosProduced.toLocaleString('es-CO')} kg`}
          />
          <DetailRow
            label="Precio/kg"
            value={formatCurrency(production.pricePerKilo, currency)}
          />
        </View>
      </Card>

      {/* Key DSS indicators */}
      <View style={styles.kpiColumn}>
        <View style={[styles.kpiCard, { backgroundColor: '#eff6ff', borderColor: '#bfdbfe' }]}>
          <View style={styles.kpiHeader}>
            <Text variant="caption" color={semanticColors.textSecondary}>Costo por kg producido</Text>
            <Text variant="label" color="#1d4ed8" style={styles.boldText}>
              {formatCurrency(costPerKiloProduced, currency)}/kg
            </Text>
          </View>
          <View style={styles.kpiDivider} />
          <Text variant="caption" color={semanticColors.textTertiary}>
            <Text variant="caption" color={semanticColors.textSecondary} style={styles.boldText}>¿Qué indica? </Text>
            {'Lo que te cuesta producir cada kilogramo. Debe ser menor al precio de venta para ser rentable. '}
            {costPerKiloProduced <= production.pricePerKilo * 0.8
              ? '✅ Eficiente — costo significativamente por debajo del precio de venta.'
              : costPerKiloProduced <= production.pricePerKilo
              ? '⚠️ Ajustado — costo cercano al precio de venta. Margen estrecho.'
              : '🔴 Crítico — costo supera el precio de venta. Opera a pérdida por kg.'}
          </Text>
        </View>
        {waterFootprintLitersPerKg !== undefined && waterFootprintLitersPerKg !== null && (
          <View style={[styles.kpiCard, { backgroundColor: '#ecfeff', borderColor: '#a5f3fc' }]}>
            <View style={styles.kpiHeader}>
              <Text variant="caption" color={semanticColors.textSecondary}>Huella hídrica aprox.</Text>
              <Text variant="label" color="#0e7490" style={styles.boldText}>
                {waterFootprintLitersPerKg.toFixed(1)} L/kg
              </Text>
            </View>
            <View style={styles.kpiDivider} />
            <Text variant="caption" color={semanticColors.textTertiary}>
              <Text variant="caption" color={semanticColors.textSecondary} style={styles.boldText}>¿Qué indica? </Text>
              {'Litros de agua usados por kg producido. Menor = más eficiente. '}
              {waterFootprintLitersPerKg <= 4
                ? '✅ Excelente — inferior a 4 L/kg. Hidropónico muy eficiente (vs. ~250 L/kg en tierra).'
                : waterFootprintLitersPerKg <= 8
                ? '⚠️ Aceptable — entre 4 y 8 L/kg. Revisa fugas o recirculación.'
                : '🔴 Alto — superior a 8 L/kg. Optimiza el sistema de riego o recirculación.'}
            </Text>
          </View>
        )}
      </View>

      {/* Revenue */}
      <StatCard
        label="Ingreso Total"
        value={formatCurrency(revenue.totalRevenue, currency)}
        icon="cash"
        iconColor={colors.hidro[600]}
      />

      {/* Expenses */}
      <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionTitle}>
        Gastos
      </Text>
      <View style={styles.expensesGrid}>
        <StatCard
          label="Costos Operativos"
          value={formatCurrency(expenses.operationalCost?.totalOperationalCost ?? 0, currency)}
          icon="bolt"
          iconColor={chartColors.amber}
          style={styles.expenseCard}
        />
        <StatCard
          label="Consumo Manual"
          value={formatCurrency(expenses.manualConsumptionCost?.totalManualCost ?? 0, currency)}
          icon="receipt"
          iconColor={chartColors.blue}
          style={styles.expenseCard}
        />
        <StatCard
          label="Total Gastos"
          value={formatCurrency(expenses.totalExpenses, currency)}
          icon="wallet"
          iconColor={colors.error[600]}
        />
      </View>

      {/* Operational cost detail — actuators */}
      {(expenses.operationalCost?.actuators?.length ?? 0) > 0 && (
        <>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionTitle}>
            Detalle operativo
          </Text>
          <Card variant="outlined" padding="md">
            <View style={styles.detailsGrid}>
              {expenses.operationalCost.actuators.map((item) => (
                <View key={item.actuatorCode} style={styles.operationalRow}>
                  <Badge variant="info" size="sm">{item.actuatorCode}</Badge>
                  <Text variant="caption" color={semanticColors.textSecondary}>
                    {item.totalHours.toFixed(1)} hrs · {item.estimatedKwh.toFixed(2)} kWh
                  </Text>
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
                    {formatCurrency(item.estimatedCost, currency)}
                  </Text>
                </View>
              ))}
            </View>
          </Card>
        </>
      )}

      {/* Manual consumption cost detail */}
      {expenses.manualConsumptionCost && (
        <>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.sectionTitle}>
            Detalle consumo manual
          </Text>
          <Card variant="outlined" padding="md">
            <View style={styles.detailsGrid}>
              {expenses.manualConsumptionCost.totalElectricityKwh > 0 && (
                <View style={styles.operationalRow}>
                  <Badge variant="info" size="sm">Electricidad</Badge>
                  <Text variant="caption" color={semanticColors.textSecondary}>
                    {expenses.manualConsumptionCost.totalElectricityKwh.toFixed(2)} kWh
                  </Text>
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
                    {formatCurrency(expenses.manualConsumptionCost.costElectricity, currency)}
                  </Text>
                </View>
              )}
              {expenses.manualConsumptionCost.totalWaterLiters > 0 && (
                <View style={styles.operationalRow}>
                  <Badge variant="info" size="sm">Agua</Badge>
                  <Text variant="caption" color={semanticColors.textSecondary}>
                    {expenses.manualConsumptionCost.totalWaterLiters.toFixed(2)} L
                  </Text>
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
                    {formatCurrency(expenses.manualConsumptionCost.costWater, currency)}
                  </Text>
                </View>
              )}
              {expenses.manualConsumptionCost.totalNutrientLiters > 0 && (
                <View style={styles.operationalRow}>
                  <Badge variant="info" size="sm">Nutrientes</Badge>
                  <Text variant="caption" color={semanticColors.textSecondary}>
                    {expenses.manualConsumptionCost.totalNutrientLiters.toFixed(2)} L
                  </Text>
                  <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
                    {formatCurrency(expenses.manualConsumptionCost.costNutrients, currency)}
                  </Text>
                </View>
              )}
            </View>
          </Card>

          {/* Individual entries with notes */}
          {(expenses.manualConsumptionCost.entries?.length ?? 0) > 0 && (
            <>
              <Text variant="caption" color={semanticColors.textTertiary} style={styles.entriesTitle}>
                Entradas individuales
              </Text>
              <Card variant="outlined" padding="md">
                <View style={styles.detailsGrid}>
                  {expenses.manualConsumptionCost.entries.map((entry, idx) => {
                    const typeLabel = CONSUMPTION_TYPE_LABELS[entry.type as ConsumptionType] ?? 'Otro';
                    const unit = CONSUMPTION_TYPE_UNITS[entry.type as ConsumptionType] ?? '';
                    const noteTruncated = entry.note
                      ? entry.note.length > 50 ? `${entry.note.slice(0, 50)}…` : entry.note
                      : null;
                    return (
                      <View key={idx} style={styles.entryRow}>
                        <View style={styles.entryHeader}>
                          <Badge variant="default" size="sm">{typeLabel}</Badge>
                          <Text variant="caption" color={semanticColors.textSecondary}>
                            {entry.amount.toFixed(2)} {unit}
                          </Text>
                          <Text variant="body" color={semanticColors.textPrimary} style={styles.boldText}>
                            {formatCurrency(entry.costAmount, currency)}
                          </Text>
                        </View>
                        {noteTruncated && (
                          <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={2}>
                            {noteTruncated}
                          </Text>
                        )}
                      </View>
                    );
                  })}
                </View>
              </Card>
            </>
          )}
        </>
      )}
    </View>
  );
}

function DetailRow({ label, value }: { label: string; value: string }): React.ReactElement {
  return (
    <View style={styles.detailRow}>
      <Text variant="caption" color={semanticColors.textTertiary}>{label}</Text>
      <Text variant="body" color={semanticColors.textPrimary} style={styles.detailRowValue}>
        {value}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  resultBanner: {
    borderRadius: borderRadius.lg,
  },
  resultContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  resultText: {
    flex: 1,
  },
  resultValue: {
    fontWeight: typography.fontWeight.bold,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginTop: spacing.xs,
  },
  detailsGrid: {
    gap: spacing.sm,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  detailRowValue: {
    fontWeight: typography.fontWeight.medium,
    flex: 1,
    textAlign: 'right',
  },
  kpiRow: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  kpiColumn: {
    gap: spacing.sm,
  },
  kpiCard: {
    borderWidth: 1,
    borderRadius: borderRadius.md,
    padding: spacing.sm,
    gap: spacing.xs,
  },
  kpiHeader: {
    gap: 2,
  },
  kpiDivider: {
    height: 1,
    backgroundColor: 'rgba(0,0,0,0.06)',
    marginVertical: 2,
  },
  captionBlock: {
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.08)',
    gap: spacing.xs,
  },
  captionLine: {
    marginTop: 2,
  },
  expensesGrid: {
    gap: spacing.sm,
  },
  expenseCard: {
    flex: 1,
  },
  operationalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: spacing.sm,
  },
  boldText: {
    fontWeight: typography.fontWeight.semibold,
  },
  entriesTitle: {
    fontWeight: typography.fontWeight.semibold,
    textTransform: 'uppercase',
    marginTop: spacing.sm,
  },
  entryRow: {
    gap: spacing.xs,
    paddingVertical: spacing.xs,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
  },
  entryHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: spacing.sm,
  },
});
