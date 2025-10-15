import React, { useEffect, useState } from 'react';
import { View, ScrollView, RefreshControl, StyleSheet, FlatList } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFuzzyStore } from '@hydroespinaca/shared';
import type { SimpleFuzzySystem } from '@hydroespinaca/shared';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { EmptyState } from '../components/molecules/EmptyState';
import { FuzzySystemCard } from '../components/molecules/FuzzySystemCard';

interface FuzzySystemsScreenProps {
  onNavigateToDetail: (systemId: string) => void;
}

export const FuzzySystemsScreen: React.FC<FuzzySystemsScreenProps> = ({
  onNavigateToDetail,
}) => {
  const { fuzzySystems, initializeFuzzyData } = useFuzzyStore();
  const [refreshing, setRefreshing] = React.useState(false);

  useEffect(() => {
    initializeFuzzyData();
  }, [initializeFuzzyData]);

  const onRefresh = React.useCallback(async () => {
    setRefreshing(true);
    await initializeFuzzyData();
    setRefreshing(false);
  }, [initializeFuzzyData]);

  const handleSystemPress = (system: SimpleFuzzySystem) => {
    onNavigateToDetail(system.id);
  };

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      <Text style={styles.emptyTitle}>No hay sistemas fuzzy</Text>
      <Text style={styles.emptyDescription}>
        Los sistemas fuzzy aparecerán aquí cuando estén disponibles.
      </Text>
    </View>
  );

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Sistemas Fuzzy</Text>
        <Text style={styles.subtitle}>
          Control inteligente de hidroponía
        </Text>
      </View>

      <FlatList
        data={fuzzySystems}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <FuzzySystemCard
            system={item}
            onPress={handleSystemPress}
          />
        )}
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[semanticColors.primary]}
            tintColor={semanticColors.primary}
          />
        }
        ListEmptyComponent={renderEmptyState}
      />
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  header: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    backgroundColor: semanticColors.surface,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
    marginTop: spacing.md,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: 16,
    color: semanticColors.textSecondary,
    fontWeight: '400',
  },
  listContainer: {
    padding: spacing.lg,
    flexGrow: 1,
  },
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.xl,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: semanticColors.textPrimary,
    marginBottom: spacing.sm,
    textAlign: 'center',
  },
  emptyDescription: {
    fontSize: 16,
    color: semanticColors.textSecondary,
    textAlign: 'center',
    lineHeight: 24,
  },
});