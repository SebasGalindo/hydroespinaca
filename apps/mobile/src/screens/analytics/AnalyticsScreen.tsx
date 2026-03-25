import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { View, TouchableOpacity, StyleSheet, RefreshControl, ScrollView } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  colors,
  spacing,
  borderRadius,
  semanticColors,
  typography,
  analyticsService,
  AnalyticsApiError,
  type EnvironmentalVariableAggregate,
} from '@hydroespinaca/shared';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { FiltersBar } from '../../components/analytics/FiltersBar';
import { EnvironmentalLevel } from '../../components/analytics/EnvironmentalLevel';
import { ActuatorsLevel } from '../../components/analytics/ActuatorsLevel';
import { ExportButton } from '../../components/analytics/ExportButton';
import {
  type FilterState,
  getDefaultFilters,
  generateBackendPayload,
} from '../../utils/analyticsFilters';
import {
  type ActuatorActivity,
  mapActuatorAnalytics,
} from '../../utils/actuatorMapper';

type AnalyticsTab = 'environmental' | 'actuators';

interface DataCache {
  [key: string]: {
    environmental: EnvironmentalVariableAggregate[];
    actuators: ActuatorActivity[];
    timestamp: number;
  };
}

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes

function getCacheKey(filters: FilterState): string {
  return `${filters.dateRange.from}|${filters.dateRange.to}|${filters.viewMode}`;
}

export function AnalyticsScreen(): React.ReactElement {
  const [activeTab, setActiveTab] = useState<AnalyticsTab>('environmental');
  const [isLoading, setIsLoading] = useState(false);
  const [appliedFilters, setAppliedFilters] = useState<FilterState>(getDefaultFilters());

  const [environmentalData, setEnvironmentalData] = useState<EnvironmentalVariableAggregate[]>([]);
  const [actuatorData, setActuatorData] = useState<ActuatorActivity[]>([]);
  const [envError, setEnvError] = useState<string | null>(null);
  const [actError, setActError] = useState<string | null>(null);

  const cacheRef = useRef<DataCache>({});

  const isSingleDay = useMemo(() => {
    const from = new Date(appliedFilters.dateRange.from);
    const to = new Date(appliedFilters.dateRange.to);
    const diffHours = (to.getTime() - from.getTime()) / (1000 * 60 * 60);
    return diffHours <= 24;
  }, [appliedFilters.dateRange]);

  const loadData = useCallback(
    async (filters: FilterState) => {
      setIsLoading(true);
      setEnvError(null);
      setActError(null);

      try {
        const cacheKey = getCacheKey(filters);
        const cached = cacheRef.current[cacheKey];
        const now = Date.now();

        if (cached && now - cached.timestamp < CACHE_TTL) {
          setEnvironmentalData(cached.environmental);
          setActuatorData(cached.actuators);
          setAppliedFilters(filters);
          setIsLoading(false);
          return;
        }

        const payload = generateBackendPayload(filters);

        // Fetch environmental data
        let envData: EnvironmentalVariableAggregate[] = [];
        try {
          const response = await analyticsService.getEnvironmentalAggregates({
            startDate: payload.startDate,
            endDate: payload.endDate,
            view: payload.view,
          });
          envData = response.variables;
        } catch (error: unknown) {
          if (error instanceof AnalyticsApiError) {
            if (error.status === 400) setEnvError('Rango de fechas inválido.');
            else if (error.status === 401) setEnvError('Sesión expirada.');
            else if (error.status === 404) setEnvError('No hay datos para el rango seleccionado.');
            else if (error.status === 0) setEnvError('Error de conexión.');
            else setEnvError(`Error del servidor (${error.status}).`);
          } else if (error instanceof Error) {
            setEnvError(error.message);
          } else {
            setEnvError('Error desconocido.');
          }
        }

        // Fetch actuator data
        let actData: ActuatorActivity[] = [];
        try {
          const response = await analyticsService.getActuatorAnalytics({
            startDate: payload.startDate,
            endDate: payload.endDate,
            view: payload.view,
          });
          actData = mapActuatorAnalytics(response);
        } catch (error: unknown) {
          if (error instanceof AnalyticsApiError) {
            if (error.status === 400) setActError('Rango de fechas inválido.');
            else if (error.status === 401) setActError('Sesión expirada.');
            else if (error.status === 404) setActError('No hay datos de actuadores.');
            else if (error.status === 0) setActError('Error de conexión.');
            else setActError(`Error del servidor (${error.status}).`);
          } else if (error instanceof Error) {
            setActError(error.message);
          } else {
            setActError('Error desconocido.');
          }
        }

        // Cache successful results
        if (envData.length > 0 || actData.length > 0) {
          cacheRef.current[cacheKey] = {
            environmental: envData,
            actuators: actData,
            timestamp: now,
          };
        }

        setEnvironmentalData(envData);
        setActuatorData(actData);
        setAppliedFilters(filters);
      } catch {
        setEnvError('Error inesperado al cargar los datos.');
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  // Load on mount
  useEffect(() => {
    loadData(appliedFilters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleApplyFilters = useCallback(
    (newFilters: FilterState) => {
      loadData(newFilters);
    },
    [loadData],
  );

  const handleRefresh = useCallback(() => {
    // Clear cache for current filters and reload
    const key = getCacheKey(appliedFilters);
    delete cacheRef.current[key];
    loadData(appliedFilters);
  }, [appliedFilters, loadData]);

  const tabs: { key: AnalyticsTab; label: string; icon: string }[] = [
    { key: 'environmental', label: 'Ambiental', icon: 'thermometer' },
    { key: 'actuators', label: 'Actuadores', icon: 'flash' },
  ];

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={isLoading} onRefresh={handleRefresh} />
        }
      >
        {/* Header */}
        <View style={styles.header}>
          <View style={styles.headerRow}>
            <Text variant="h2" color={semanticColors.primary} style={styles.title}>
              Análisis de Datos
            </Text>
            <ExportButton
              environmentalData={environmentalData}
              actuatorData={actuatorData}
              filters={appliedFilters}
              activeTab={activeTab}
            />
          </View>
          <Text variant="body" color={semanticColors.textSecondary}>
            Monitoreo ambiental y control de actuadores
          </Text>
        </View>

        {/* Filters */}
        <FiltersBar
          currentFilters={appliedFilters}
          onApplyFilters={handleApplyFilters}
          isLoading={isLoading}
        />

        {/* Tabs */}
        <View style={styles.tabBar} accessibilityRole="tablist">
          {tabs.map((tab) => (
            <TouchableOpacity
              key={tab.key}
              style={[styles.tab, activeTab === tab.key && styles.tabActive]}
              onPress={() => setActiveTab(tab.key)}
              accessibilityRole="tab"
              accessibilityState={{ selected: activeTab === tab.key }}
              accessibilityLabel={`${tab.label}`}
            >
              <Icon
                name={tab.icon as any}
                size={18}
                color={
                  activeTab === tab.key
                    ? colors.hidro[600]
                    : semanticColors.textTertiary
                }
              />
              <Text
                variant="caption"
                color={
                  activeTab === tab.key
                    ? colors.hidro[600]
                    : semanticColors.textTertiary
                }
                style={{
                  ...styles.tabLabel,
                  ...(activeTab === tab.key ? styles.tabLabelActive : {}),
                }}
              >
                {tab.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* Content */}
        {activeTab === 'environmental' && (
          <EnvironmentalLevel
            variables={environmentalData}
            viewMode={appliedFilters.viewMode}
            isLoading={isLoading}
            error={envError}
          />
        )}
        {activeTab === 'actuators' && (
          <ActuatorsLevel
            data={actuatorData}
            isLoading={isLoading}
            error={actError}
            isSingleDay={isSingleDay}
          />
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    paddingBottom: spacing.xl * 2,
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
    flexDirection: 'row',
    marginHorizontal: spacing.md,
    marginBottom: spacing.md,
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    padding: 4,
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
  },
  tabActive: {
    backgroundColor: colors.hidro[50],
  },
  tabLabel: {
    fontSize: 13,
  },
  tabLabelActive: {
    fontWeight: typography.fontWeight.semibold,
  },
});
