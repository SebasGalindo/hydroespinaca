/**
 * NotificationHistoryScreen — Historial paginado de notificaciones enviadas.
 */
import React, { useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { ScreenLayout } from '../../components/organisms/ScreenLayout';
import { NotificationHistory } from '../../components/notifications/NotificationHistory';
import {
  useNotificationStore,
  useAuthStore,
} from '@hydroespinaca/shared';

export function NotificationHistoryScreen(): React.ReactElement {
  const userId = useAuthStore(s => s.user?.id) ?? '';

  const history = useNotificationStore(s => s.history);
  const historyLoading = useNotificationStore(s => s.historyLoading);
  const historyError = useNotificationStore(s => s.historyError);
  const fetchHistory = useNotificationStore(s => s.fetchHistory);

  useFocusEffect(
    useCallback(() => {
      if (userId) {
        fetchHistory(userId, { limit: 50 });
      }
    }, [userId])
  );

  const handleRefresh = useCallback(() => {
    if (userId) fetchHistory(userId, { limit: 50 });
  }, [userId]);

  return (
    <ScreenLayout
      title="Historial"
      subtitle="Notificaciones enviadas"
      scrollable={false}
      refreshing={historyLoading}
      onRefresh={handleRefresh}
      testID="notification-history-screen"
    >
      <NotificationHistory
        entries={history}
        loading={historyLoading}
        error={historyError}
      />
    </ScreenLayout>
  );
}
