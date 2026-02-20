/**
 * AdminSessionsScreen — Monitoreo de sesiones activas.
 * Auto-refresh cada 30s. Agrupado por usuario.
 * Revocación con protección anti-auto-revoke.
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  View,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { Badge } from '../../components/atoms/Badge';
import { Button } from '../../components/atoms/Button';
import { StatCard } from '../../components/molecules/StatCard';
import { Alert } from '../../components/molecules/Alert';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import { AdminGuard } from '../../components/organisms/AdminGuard';
import { useAuth } from '../../context/AuthProvider';
import {
  adminService,
  semanticColors,
  spacing,
  colors,
  borderRadius,
  typography,
} from '@hydroespinaca/shared';
import type { UserSessionsDto, SessionMonitorDto } from '@hydroespinaca/shared';
import { showToast } from '../../utils/toast';
import { hapticSuccess, hapticError, hapticHeavy } from '../../utils/haptics';

const AUTO_REFRESH_INTERVAL = 30_000; // 30 seconds

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleString('es-CL', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

function timeUntil(iso: string): string {
  try {
    const diff = new Date(iso).getTime() - Date.now();
    if (diff <= 0) return 'Expirado';
    const mins = Math.floor(diff / 60_000);
    if (mins < 60) return `${mins} min`;
    const hrs = Math.floor(mins / 60);
    return `${hrs}h ${mins % 60}m`;
  } catch {
    return '—';
  }
}

export function AdminSessionsScreen(): React.ReactElement {
  const { session } = useAuth();
  const [userSessions, setUserSessions] = useState<UserSessionsDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const [expandedUsers, setExpandedUsers] = useState<Set<string>>(new Set());
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Revoke state
  const [revokeTarget, setRevokeTarget] = useState<{
    sessionId: string;
    userName: string;
  } | null>(null);

  const loadSessions = useCallback(
    async (silent = false) => {
      if (!silent) setLoading(true);
      setError(null);
      try {
        const data = await adminService.getActiveSessions();
        setUserSessions(data);
        setLastRefresh(new Date());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Error al cargar sesiones');
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    [],
  );

  useEffect(() => {
    loadSessions();
  }, [loadSessions]);

  // Auto-refresh
  useEffect(() => {
    if (autoRefresh) {
      timerRef.current = setInterval(() => loadSessions(true), AUTO_REFRESH_INTERVAL);
    }
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [autoRefresh, loadSessions]);

  const handlePullRefresh = () => {
    setRefreshing(true);
    loadSessions();
  };

  const toggleUserExpand = (userId: string) => {
    setExpandedUsers((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const handleRevoke = async () => {
    if (!revokeTarget) return;
    try {
      await adminService.revokeSession(revokeTarget.sessionId);
      setRevokeTarget(null);
      showToast('success', `Sesión de "${revokeTarget.userName}" revocada`);
      hapticHeavy();
      loadSessions(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al revocar sesión');
      showToast('error', 'Error al revocar sesión');
      hapticError();
      setRevokeTarget(null);
    }
  };

  // Stats
  const totalSessions = userSessions.reduce((sum, u) => sum + u.sessions.length, 0);

  const renderSession = (s: SessionMonitorDto, userName: string) => {
    const isSelf = s.sessionId === session?.sessionId;
    return (
      <View key={s.sessionId} style={[styles.sessionCard, isSelf && styles.sessionCardSelf]}>
        <View style={styles.sessionRow}>
          <View style={styles.sessionInfo}>
            <Text variant="caption" color={semanticColors.textTertiary}>
              ID: {s.sessionId.slice(0, 12)}…
            </Text>
            <Text variant="caption" color={semanticColors.textSecondary}>
              Cliente: {s.clientId}
            </Text>
          </View>
          {isSelf && (
            <Badge variant="success" size="sm">Tu sesión</Badge>
          )}
        </View>

        <View style={styles.sessionDates}>
          <View style={styles.dateItem}>
            <Icon name="clock" size={12} color={semanticColors.textTertiary} />
            <Text variant="caption" color={semanticColors.textTertiary}>
              Creada: {formatDate(s.createdAt)}
            </Text>
          </View>
          <View style={styles.dateItem}>
            <Icon name="activity" size={12} color={semanticColors.textTertiary} />
            <Text variant="caption" color={semanticColors.textTertiary}>
              Actividad: {formatDate(s.lastActivity)}
            </Text>
          </View>
          <View style={styles.dateItem}>
            <Icon name="alert-triangle" size={12} color={semanticColors.textTertiary} />
            <Text variant="caption" color={semanticColors.textTertiary}>
              Expira en: {timeUntil(s.expiresAt)}
            </Text>
          </View>
        </View>

        {!isSelf && (
          <Button
            variant="outline"
            size="sm"
            onPress={() =>
              setRevokeTarget({ sessionId: s.sessionId, userName })
            }
            style={styles.revokeButton}
          >
            Revocar
          </Button>
        )}
      </View>
    );
  };

  const renderUserGroup = ({ item }: { item: UserSessionsDto }) => {
    const isExpanded = expandedUsers.has(item.userId);
    const initial = item.userName.charAt(0).toUpperCase();

    return (
      <View style={styles.userGroupContainer}>
        <TouchableOpacity
          style={styles.userGroupHeader}
          onPress={() => toggleUserExpand(item.userId)}
          activeOpacity={0.7}
        >
          <View style={styles.userAvatar}>
            <Text variant="body" color={colors.white} style={styles.avatarText}>
              {initial}
            </Text>
          </View>
          <View style={styles.userGroupInfo}>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.userName}>
              {item.userName}
            </Text>
            <Text variant="caption" color={semanticColors.textTertiary}>
              {item.sessions.length} sesión{item.sessions.length !== 1 ? 'es' : ''}
            </Text>
          </View>
          <Icon
            name={isExpanded ? 'chevron-up' : 'chevron-down'}
            size={20}
            color={semanticColors.textTertiary}
          />
        </TouchableOpacity>

        {isExpanded && (
          <View style={styles.sessionsContainer}>
            {item.sessions.map((s) => renderSession(s, item.userName))}
          </View>
        )}
      </View>
    );
  };

  return (
    <AdminGuard>
      <SafeAreaView style={styles.container} edges={['top']}>
        {/* Header */}
        <View style={styles.header}>
          <Icon name="activity" size={24} color={semanticColors.primary} />
          <Text variant="h3" color={semanticColors.textPrimary} style={styles.headerTitle}>
            Sesiones Activas
          </Text>
        </View>

        {/* Stats */}
        <View style={styles.statsRow}>
          <StatCard
            label="Sesiones"
            value={totalSessions}
            icon="monitor"
            iconColor={colors.hidro[600]}
            style={styles.stat}
          />
          <StatCard
            label="Usuarios"
            value={userSessions.length}
            icon="users"
            iconColor={colors.hidro[500]}
            style={styles.stat}
          />
        </View>

        {/* Auto-refresh toggle */}
        <View style={styles.refreshBar}>
          <TouchableOpacity
            style={[
              styles.autoRefreshToggle,
              autoRefresh ? styles.autoRefreshOn : styles.autoRefreshOff,
            ]}
            onPress={() => setAutoRefresh((p) => !p)}
            activeOpacity={0.7}
          >
            <Icon
              name={autoRefresh ? 'refresh' : 'power'}
              size={14}
              color={autoRefresh ? colors.hidro[700] : semanticColors.textTertiary}
            />
            <Text
              variant="caption"
              color={autoRefresh ? colors.hidro[700] : semanticColors.textTertiary}
            >
              {autoRefresh ? 'Auto-refresh ON' : 'Auto-refresh OFF'}
            </Text>
          </TouchableOpacity>

          {lastRefresh && (
            <Text variant="caption" color={semanticColors.textTertiary}>
              Últ: {lastRefresh.toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
            </Text>
          )}
        </View>

        {/* Error */}
        {error && (
          <View style={styles.errorContainer}>
            <Alert type="error" message={error} onDismiss={() => setError(null)} />
          </View>
        )}

        {/* Content */}
        {loading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={semanticColors.primary} />
            <Text variant="body" color={semanticColors.textSecondary}>
              Cargando sesiones...
            </Text>
          </View>
        ) : (
          <FlatList
            data={userSessions}
            keyExtractor={(item) => item.userId}
            renderItem={renderUserGroup}
            contentContainerStyle={styles.list}
            refreshControl={
              <RefreshControl
                refreshing={refreshing}
                onRefresh={handlePullRefresh}
                tintColor={semanticColors.primary}
              />
            }
            ListEmptyComponent={
              <View style={styles.emptyContainer}>
                <Icon name="check-circle" size={48} color={semanticColors.textTertiary} />
                <Text variant="body" color={semanticColors.textTertiary}>
                  No hay sesiones activas
                </Text>
              </View>
            }
          />
        )}

        {/* Revoke confirmation */}
        <ConfirmationSheet
          isOpen={!!revokeTarget}
          title="Revocar Sesión"
          message={`¿Revocar la sesión de "${revokeTarget?.userName}"? El usuario será desconectado.`}
          confirmLabel="Revocar"
          cancelLabel="Cancelar"
          destructive
          onConfirm={handleRevoke}
          onCancel={() => setRevokeTarget(null)}
        />
      </SafeAreaView>
    </AdminGuard>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  // Header
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    gap: spacing.sm,
  },
  headerTitle: {
    fontWeight: typography.fontWeight.bold,
  },
  // Stats
  statsRow: {
    flexDirection: 'row',
    paddingHorizontal: spacing.lg,
    gap: spacing.sm,
  },
  stat: {
    flex: 1,
  },
  // Refresh bar
  refreshBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
  },
  autoRefreshToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: 4,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.full,
  },
  autoRefreshOn: {
    backgroundColor: colors.hidro[100],
  },
  autoRefreshOff: {
    backgroundColor: colors.gray[100],
  },
  // Content
  errorContainer: {
    paddingHorizontal: spacing.lg,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.md,
  },
  list: {
    padding: spacing.lg,
    paddingBottom: spacing['2xl'],
    gap: spacing.md,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: spacing['2xl'],
    gap: spacing.md,
  },
  // User group
  userGroupContainer: {
    backgroundColor: colors.white,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    overflow: 'hidden',
  },
  userGroupHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: spacing.md,
    gap: spacing.sm,
  },
  userAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: colors.hidro[500],
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: {
    fontWeight: typography.fontWeight.bold,
    fontSize: typography.fontSize.sm,
  },
  userGroupInfo: {
    flex: 1,
  },
  userName: {
    fontWeight: typography.fontWeight.semibold,
  },
  // Sessions
  sessionsContainer: {
    padding: spacing.sm,
    paddingTop: 0,
    gap: spacing.sm,
  },
  sessionCard: {
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
    padding: spacing.sm,
    gap: spacing.sm,
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  sessionCardSelf: {
    borderColor: colors.hidro[300],
    backgroundColor: colors.hidro[50],
  },
  sessionRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
  },
  sessionInfo: {
    flex: 1,
    gap: 2,
  },
  sessionDates: {
    gap: 4,
  },
  dateItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  revokeButton: {
    alignSelf: 'flex-end',
  },
});
