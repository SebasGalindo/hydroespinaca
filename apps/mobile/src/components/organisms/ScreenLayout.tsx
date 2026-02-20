import React from 'react';
import { View, ScrollView, StyleSheet, ViewStyle, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { colors, spacing, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';

export interface ScreenLayoutProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  scrollable?: boolean;
  refreshing?: boolean;
  onRefresh?: () => void;
  loading?: boolean;
  headerRight?: React.ReactNode;
  contentStyle?: ViewStyle;
  testID?: string;
}

export function ScreenLayout({
  children,
  title,
  subtitle,
  scrollable = true,
  refreshing = false,
  onRefresh,
  loading = false,
  headerRight,
  contentStyle,
  testID,
}: ScreenLayoutProps): React.ReactElement {
  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Spinner size="lg" color={semanticColors.primary} />
          <Text variant="body" color={semanticColors.textSecondary} style={styles.loadingText}>
            Cargando...
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  const headerSection = (title || subtitle) ? (
    <View style={styles.header}>
      {(title || headerRight) && (
        <View style={styles.headerRow}>
          {title && (
            <Text variant="h2" color={semanticColors.primary} style={styles.title}>
              {title}
            </Text>
          )}
          {headerRight}
        </View>
      )}
      {subtitle && (
        <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
          {subtitle}
        </Text>
      )}
    </View>
  ) : null;

  if (!scrollable) {
    return (
      <SafeAreaView style={styles.container} testID={testID}>
        {headerSection}
        <View style={[styles.content, contentStyle]}>
          {children}
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container} testID={testID}>
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={[styles.scrollContent, contentStyle]}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
        refreshControl={
          onRefresh ? (
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
          ) : undefined
        }
      >
        {headerSection}
        {children}
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
    paddingBottom: spacing.xl,
  },
  content: {
    flex: 1,
  },
  header: {
    padding: spacing.lg,
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
  subtitle: {
    lineHeight: 20,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
  loadingText: {
    marginTop: spacing.md,
  },
});
