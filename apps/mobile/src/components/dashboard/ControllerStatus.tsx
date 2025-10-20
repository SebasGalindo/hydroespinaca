import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { semanticColors, spacing, borderRadius, colors } from '@hydroespinaca/shared';
import type { JobStatus, Stats, InternalRoutine } from '@hydroespinaca/shared';

interface ControllerStatusProps {
  timeSinceUpdate: number;
  jobStatus: JobStatus;
  stats: Stats;
  internalRoutines: InternalRoutine[];
}

export function ControllerStatus({
  timeSinceUpdate,
  jobStatus,
  stats,
  internalRoutines,
}: ControllerStatusProps): React.ReactElement {

  const getStatusEmoji = (status: string): string => {
    switch (status) {
      case 'running':
        return '🟢';
      case 'scheduled':
        return '🟡';
      case 'queued':
        return '🟠';
      case 'failed':
        return '🔴';
      default:
        return '⚪';
    }
  };

  const getStatusLabel = (status: string): string => {
    const labels: { [key: string]: string } = {
      running: 'En ejecución',
      scheduled: 'Programado',
      queued: 'En cola',
      failed: 'Fallido',
    };
    return labels[status] || status;
  };

  const getStatusBgColor = (status: string): string => {
    switch (status) {
      case 'running':
        return '#dcfce7';
      case 'scheduled':
        return '#fef3c7';
      case 'queued':
        return '#fed7aa';
      case 'failed':
        return '#fee2e2';
      default:
        return '#f3f4f6';
    }
  };

  const getStatusTextColor = (status: string): string => {
    switch (status) {
      case 'running':
        return '#15803d';
      case 'scheduled':
        return '#a16207';
      case 'queued':
        return '#c2410c';
      case 'failed':
        return '#b91c1c';
      default:
        return '#6b7280';
    }
  };

  const formatNextExecution = (isoDate: string): string => {
    const date = new Date(isoDate);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 60) return `en ${diffMins}m`;
    if (diffMins < 1440) return `en ${Math.floor(diffMins / 60)}h`;
    return date.toLocaleDateString('es-ES', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <View style={styles.container}>
      {/* Header */}
      <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
        Estado actual del controlador
      </Text>

      {/* Stats Grid */}
      <View style={styles.statsGrid}>
        <View style={styles.statCard}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Activos
          </Text>
          <Text variant="h2" color="#16a34a" style={styles.statValue}>
            {stats.activeCount}
          </Text>
        </View>

        <View style={styles.statCard}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Pendientes
          </Text>
          <Text variant="h2" color="#f59e0b" style={styles.statValue}>
            {stats.pendingCount}
          </Text>
        </View>

        <View style={styles.statCard}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Pines Bloqueados
          </Text>
          <Text variant="h2" color={semanticColors.textPrimary} style={styles.statValue}>
            {stats.totalLockedPins}
          </Text>
        </View>

        <View style={styles.statCard}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            Controladores
          </Text>
          <Text variant="h2" color="#3b82f6" style={styles.statValue}>
            {stats.esp32Ids.length}
          </Text>
        </View>
      </View>

      {/* Command Queue */}
      <View style={styles.section}>
        <Text variant="label" color={semanticColors.textPrimary} style={styles.subsectionTitle}>
          Cola de Comandos - ESP32
        </Text>
        {jobStatus.queue.length > 0 ? (
          <View style={styles.commandList}>
            {jobStatus.queue.map((command) => (
              <View key={command.commandId} style={styles.commandItem}>
                <View style={styles.commandLeft}>
                  <Text style={styles.commandEmoji}>
                    {getStatusEmoji(command.status)}
                  </Text>
                  <View style={styles.commandInfo}>
                    <Text variant="body" color={semanticColors.textPrimary} style={styles.commandName}>
                      {command.commandId.split('_')[0]?.replace(/-/g, ' ') || command.commandId}
                    </Text>
                    <Text variant="caption" color={semanticColors.textSecondary}>
                      ID: {command.commandId.split('_')[1] || command.commandId}
                    </Text>
                  </View>
                </View>
                <View
                  style={[
                    styles.statusBadge,
                    { backgroundColor: getStatusBgColor(command.status) },
                  ]}
                >
                  <Text
                    variant="caption"
                    color={getStatusTextColor(command.status)}
                    style={styles.statusBadgeText}
                  >
                    {getStatusLabel(command.status)}
                  </Text>
                </View>
              </View>
            ))}
          </View>
        ) : (
          <Text variant="body" color={semanticColors.textSecondary} style={styles.emptyText}>
            No hay comandos en la cola
          </Text>
        )}
      </View>

      {/* Internal Routines */}
      <View style={styles.section}>
        <Text variant="label" color={semanticColors.textPrimary} style={styles.subsectionTitle}>
          Rutinas Internas
        </Text>
        {internalRoutines.length > 0 ? (
          <ScrollView horizontal showsHorizontalScrollIndicator={true}>
            <View style={styles.table}>
              {/* Table Header */}
              <View style={styles.tableRow}>
                <View style={[styles.tableCell, styles.tableHeaderCell, styles.statusColumn]}>
                  <Text variant="caption" color={semanticColors.textSecondary} style={styles.tableHeaderText}>
                    Estado
                  </Text>
                </View>
                <View style={[styles.tableCell, styles.tableHeaderCell, styles.routineColumn]}>
                  <Text variant="caption" color={semanticColors.textSecondary} style={styles.tableHeaderText}>
                    Rutina
                  </Text>
                </View>
                <View style={[styles.tableCell, styles.tableHeaderCell, styles.descriptionColumn]}>
                  <Text variant="caption" color={semanticColors.textSecondary} style={styles.tableHeaderText}>
                    Descripción
                  </Text>
                </View>
                <View style={[styles.tableCell, styles.tableHeaderCell, styles.intervalColumn]}>
                  <Text variant="caption" color={semanticColors.textSecondary} style={styles.tableHeaderText}>
                    Intervalo
                  </Text>
                </View>
                <View style={[styles.tableCell, styles.tableHeaderCell, styles.nextExecColumn]}>
                  <Text variant="caption" color={semanticColors.textSecondary} style={styles.tableHeaderText}>
                    Próxima ejecución
                  </Text>
                </View>
              </View>

              {/* Table Body */}
              {internalRoutines.map((routine) => (
                <View key={routine.name} style={styles.tableRow}>
                  <View style={[styles.tableCell, styles.statusColumn]}>
                    <Icon
                      name={routine.isActive ? 'check-circle' : 'x-circle'}
                      size={18}
                      color={routine.isActive ? '#16a34a' : '#9ca3af'}
                    />
                  </View>
                  <View style={[styles.tableCell, styles.routineColumn]}>
                    <Text variant="caption" color={semanticColors.textPrimary} style={styles.tableCellText}>
                      {routine.name}
                    </Text>
                  </View>
                  <View style={[styles.tableCell, styles.descriptionColumn]}>
                    <Text variant="caption" color={semanticColors.textSecondary}>
                      {routine.description}
                    </Text>
                  </View>
                  <View style={[styles.tableCell, styles.intervalColumn]}>
                    <View style={styles.intervalContainer}>
                      <Icon name="clock" size={12} color="#9ca3af" />
                      <Text variant="caption" color={semanticColors.textSecondary}>
                        {routine.interval}
                      </Text>
                    </View>
                  </View>
                  <View style={[styles.tableCell, styles.nextExecColumn]}>
                    <Text variant="caption" color={semanticColors.textSecondary}>
                      {formatNextExecution(routine.nextExecutionEstimate)}
                    </Text>
                  </View>
                </View>
              ))}
            </View>
          </ScrollView>
        ) : (
          <Text variant="body" color={semanticColors.textSecondary} style={styles.emptyText}>
            No hay rutinas configuradas
          </Text>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.lg,
  },
  sectionTitle: {
    fontWeight: 'bold',
    marginBottom: spacing.md,
  },
  statsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  statCard: {
    flex: 1,
    minWidth: '45%',
    backgroundColor: semanticColors.background,
    padding: spacing.md,
    borderRadius: borderRadius.md,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  statValue: {
    fontWeight: 'bold',
    marginTop: spacing.xs,
  },
  section: {
    backgroundColor: semanticColors.background,
    padding: spacing.md,
    borderRadius: borderRadius.lg,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  subsectionTitle: {
    fontWeight: '600',
    marginBottom: spacing.md,
  },
  commandList: {
    gap: spacing.sm,
  },
  commandItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: spacing.sm,
    backgroundColor: '#f9fafb',
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.hidro.borderLight,
  },
  commandLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    flex: 1,
  },
  commandEmoji: {
    fontSize: 20,
  },
  commandInfo: {
    flex: 1,
  },
  commandName: {
    fontWeight: '500',
    textTransform: 'capitalize',
  },
  statusBadge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: borderRadius.full,
  },
  statusBadgeText: {
    fontWeight: '500',
    fontSize: 11,
  },
  emptyText: {
    textAlign: 'center',
    paddingVertical: spacing.md,
  },
  table: {
    borderWidth: 1,
    borderColor: colors.hidro.borderLight,
    borderRadius: borderRadius.md,
    overflow: 'hidden',
  },
  tableRow: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: colors.hidro.borderLight,
  },
  tableCell: {
    padding: spacing.sm,
    justifyContent: 'center',
  },
  tableHeaderCell: {
    backgroundColor: '#f9fafb',
  },
  tableHeaderText: {
    fontWeight: '600',
  },
  tableCellText: {
    fontWeight: '500',
  },
  statusColumn: {
    width: 60,
    alignItems: 'center',
  },
  routineColumn: {
    width: 120,
  },
  descriptionColumn: {
    width: 180,
  },
  intervalColumn: {
    width: 100,
  },
  nextExecColumn: {
    width: 120,
  },
  intervalContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
});
