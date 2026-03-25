import React from 'react';
import { View, FlatList, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { NotificationHistoryItem } from '../molecules/NotificationHistoryItem';
import { spacing, semanticColors, typography } from '@hydroespinaca/shared';
import type { NotificationLogEntry } from '@hydroespinaca/shared';

export interface NotificationHistoryProps {
  entries: NotificationLogEntry[];
  loading?: boolean;
  error?: string | null;
  onLoadMore?: () => void;
}

export function NotificationHistory({
  entries,
  loading,
  error,
  onLoadMore,
}: NotificationHistoryProps): React.ReactElement {
  if (loading && entries.length === 0) {
    return (
      <View style={styles.center}>
        <Spinner size="sm" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.section}>
        <Text variant="caption" color={semanticColors.error}>{error}</Text>
      </View>
    );
  }

  if (entries.length === 0) {
    return (
      <View style={styles.section}>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          No hay notificaciones recientes.
        </Text>
      </View>
    );
  }

  return (
    <FlatList
      data={entries}
      keyExtractor={(item) => item.id ?? item.correlationId ?? `${item.createdAt}-${item.channel}`}
      renderItem={({ item }) => (
        <NotificationHistoryItem
          entry={item}
          testID={`notif-history-${item.id}`}
        />
      )}
      onEndReached={onLoadMore}
      onEndReachedThreshold={0.3}
      ListFooterComponent={loading ? <Spinner size="sm" style={styles.footer} /> : null}
      contentContainerStyle={styles.list}
      testID="notification-history-list"
    />
  );
}

const styles = StyleSheet.create({
  section: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.xl,
  },
  center: {
    alignItems: 'center',
    paddingVertical: spacing.xl,
  },
  empty: {
    textAlign: 'center',
  },
  list: {
    paddingBottom: spacing.xl,
  },
  footer: {
    paddingVertical: spacing.md,
  },
});
