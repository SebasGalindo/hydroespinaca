import React, { useState, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, ScrollView, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  typography,
  useBiStore,
  type IconName,
} from '@hydroespinaca/shared';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { CostConfigSection } from '../../components/bi/CostConfigSection';
import { ConsumptionSection } from '../../components/bi/ConsumptionSection';
import { ProductionSection } from '../../components/bi/ProductionSection';
import { ProfitabilitySection } from '../../components/bi/ProfitabilitySection';

type BiTab = 'costs' | 'consumption' | 'production' | 'profitability';

interface TabConfig {
  id: BiTab;
  label: string;
  icon: IconName;
}

const TABS: TabConfig[] = [
  { id: 'costs', label: 'Costos', icon: 'pricetag' },
  { id: 'consumption', label: 'Consumo', icon: 'receipt' },
  { id: 'production', label: 'Producción', icon: 'leaf' },
  { id: 'profitability', label: 'Rentabilidad', icon: 'stats' },
];

export function BiScreen(): React.ReactElement {
  const [activeTab, setActiveTab] = useState<BiTab>('costs');
  const [refreshing, setRefreshing] = useState(false);

  const { resetBiStore } = useBiStore();

  const handleRefresh = useCallback(async () => {
    setRefreshing(true);
    resetBiStore();
    // Brief delay to let the store reset and children re-fetch
    await new Promise<void>((resolve) => setTimeout(() => resolve(), 300));
    setRefreshing(false);
  }, [resetBiStore]);

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerRow}>
          <Text variant="h2" color={semanticColors.primary} style={styles.title}>
            Consumo y Costos
          </Text>
        </View>
        <Text variant="caption" color={semanticColors.textSecondary}>
          Configuración de costos, consumo, producción y rentabilidad
        </Text>
      </View>

      {/* Tab bar */}
      <View style={styles.tabBar} accessibilityRole="tablist">
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.tabScrollContent}
        >
          {TABS.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <TouchableOpacity
                key={tab.id}
                style={[styles.tab, isActive && styles.tabActive]}
                onPress={() => setActiveTab(tab.id)}
                accessibilityRole="tab"
                accessibilityState={{ selected: isActive }}
              >
                <Icon
                  name={tab.icon}
                  size={16}
                  color={isActive ? colors.hidro[700] : semanticColors.textTertiary}
                />
                <Text
                  variant="caption"
                  color={isActive ? colors.hidro[700] : semanticColors.textTertiary}
                  style={isActive ? styles.tabLabelActive : undefined}
                >
                  {tab.label}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      </View>

      {/* Content */}
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
        }
      >
        {activeTab === 'costs' && <CostConfigSection />}
        {activeTab === 'consumption' && <ConsumptionSection />}
        {activeTab === 'production' && <ProductionSection />}
        {activeTab === 'profitability' && <ProfitabilitySection />}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  header: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.sm,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: spacing.xs,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    flex: 1,
  },
  tabBar: {
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
    backgroundColor: semanticColors.surface,
  },
  tabScrollContent: {
    paddingHorizontal: spacing.md,
    gap: spacing.xs,
  },
  tab: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabActive: {
    borderBottomColor: colors.hidro[600],
  },
  tabLabelActive: {
    fontWeight: typography.fontWeight.semibold,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    padding: spacing.lg,
    paddingBottom: spacing.xl * 2,
  },
});
