import React, { useEffect, useState } from 'react';
import { View, ScrollView, RefreshControl, StyleSheet, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFuzzyStore } from '@hidroespinaca/shared';
import type { FuzzySystem, SimpleFuzzyVariable, SimpleFuzzyRule, SimpleFuzzyRoutine } from '@hidroespinaca/shared';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { TabBar } from '../components/molecules/TabBar';
import { EmptyState } from '../components/molecules/EmptyState';
import { FuzzyVariableCard } from '../components/molecules/FuzzyVariableCard';
import { FuzzyRuleCard } from '../components/molecules/FuzzyRuleCard';
import { FuzzyRoutineCard } from '../components/molecules/FuzzyRoutineCard';

interface FuzzySystemDetailScreenProps {
  systemId: string;
  onNavigateBack: () => void;
}

export const FuzzySystemDetailScreen: React.FC<FuzzySystemDetailScreenProps> = ({
  systemId,
  onNavigateBack,
}) => {
  const { 
    getFuzzySystemById, 
    getVariablesBySystemId, 
    getTermsByVariableId,
    getRulesBySystemId,
    getRoutinesBySystemId,
    initializeFuzzyData 
  } = useFuzzyStore();

  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'variables' | 'rules' | 'routines'>('variables');

  const system = getFuzzySystemById(systemId);
  const variables = getVariablesBySystemId(systemId);
  const rules = getRulesBySystemId(systemId);
  const routines = getRoutinesBySystemId(systemId);

  useEffect(() => {
    if (!system) {
      initializeFuzzyData();
    }
  }, [system, initializeFuzzyData]);

  const onRefresh = React.useCallback(async () => {
    setRefreshing(true);
    await initializeFuzzyData();
    setRefreshing(false);
  }, [initializeFuzzyData]);

  if (!system) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Sistema no encontrado</Text>
          <TouchableOpacity style={styles.backButton} onPress={onNavigateBack}>
            <Text style={styles.backButtonText}>Volver</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const renderTabButton = (tab: 'variables' | 'rules' | 'routines', label: string, count: number) => (
    <TouchableOpacity
      style={[styles.tabButton, activeTab === tab && styles.activeTabButton]}
      onPress={() => setActiveTab(tab)}
    >
      <Text style={activeTab === tab ? {...styles.tabText, ...styles.activeTabText} : styles.tabText}>
        {label} ({count})
      </Text>
    </TouchableOpacity>
  );

  const renderContent = () => {
    switch (activeTab) {
      case 'variables':
        return variables.map((variable: SimpleFuzzyVariable) => (
          <FuzzyVariableCard
            key={variable.id}
            variable={variable}
          />
        ));
      case 'rules':
        return rules.map((rule: SimpleFuzzyRule) => (
          <FuzzyRuleCard key={rule.id} rule={rule} />
        ));
      case 'routines':
        return routines.map((routine: SimpleFuzzyRoutine) => (
          <FuzzyRoutineCard key={routine.id} routine={routine} />
        ));
      default:
        return null;
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={onNavigateBack}>
          <Text style={styles.backButtonText}>← Volver</Text>
        </TouchableOpacity>
        <View style={styles.headerInfo}>
          <Text style={styles.systemName}>{system.name}</Text>
          <Text style={styles.systemDescription}>Sistema de lógica difusa</Text>
          <View style={styles.statusContainer}>
            <View style={[styles.statusBadge, system.status === 'ACTIVE' && styles.activeStatus]}>
              <Text style={styles.statusText}>
                {system.status === 'ACTIVE' ? 'Activo' : 'Inactivo'}
              </Text>
            </View>
          </View>
        </View>
      </View>

      {/* Tabs */}
      <View style={styles.tabsContainer}>
        {renderTabButton('variables', 'Variables', variables.length)}
        {renderTabButton('rules', 'Reglas', rules.length)}
        {renderTabButton('routines', 'Rutinas', routines.length)}
      </View>

      {/* Content */}
      <ScrollView
        style={styles.content}
        contentContainerStyle={styles.contentContainer}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[semanticColors.primary]}
            tintColor={semanticColors.primary}
          />
        }
      >
        {renderContent()}
      </ScrollView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  header: {
    backgroundColor: semanticColors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  backButton: {
    backgroundColor: '#10b981', // green-500
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: 8,
    alignSelf: 'flex-start',
    marginBottom: spacing.md,
  },
  backButtonText: {
    color: semanticColors.surface,
    fontWeight: '600',
    fontSize: 14,
  },
  headerInfo: {
    gap: spacing.sm,
  },
  systemName: {
    fontSize: 24,
    fontWeight: '700',
    color: semanticColors.textPrimary,
  },
  systemDescription: {
    fontSize: 16,
    color: semanticColors.textSecondary,
    lineHeight: 22,
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusBadge: {
    backgroundColor: semanticColors.destructiveBg,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: 16,
  },
  activeStatus: {
    backgroundColor: semanticColors.successBg,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '500',
    color: semanticColors.surface,
  },
  tabsContainer: {
    flexDirection: 'row',
    backgroundColor: semanticColors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  tabButton: {
    flex: 1,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: 8,
    marginHorizontal: spacing.xs,
    alignItems: 'center',
  },
  activeTabButton: {
    backgroundColor: '#10b981', // green-500
  },
  tabText: {
    fontSize: 14,
    fontWeight: '500',
    color: semanticColors.textSecondary,
  },
  activeTabText: {
    color: semanticColors.surface,
    fontWeight: '600',
  },
  content: {
    flex: 1,
  },
  contentContainer: {
    padding: spacing.lg,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.xl,
  },
  errorText: {
    fontSize: 18,
    fontWeight: '600',
    color: semanticColors.textPrimary,
    marginBottom: spacing.lg,
    textAlign: 'center',
  },
});