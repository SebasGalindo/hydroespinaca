/**
 * FuzzyDetailScreen — Detalle de un sistema fuzzy.
 * 4 tabs: Info · Variables · Reglas · Simular
 */
import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  ScrollView,
  Pressable,
  StyleSheet,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { Spinner } from '../../components/atoms/Spinner';
import { Alert } from '../../components/molecules/Alert';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import { SystemForm } from '../../components/fuzzy/SystemForm';
import { InfoTab } from '../../components/fuzzy/InfoTab';
import { VariablesTab } from '../../components/fuzzy/VariablesTab';
import { RulesTab } from '../../components/fuzzy/RulesTab';
import { SimulationTab } from '../../components/fuzzy/SimulationTab';
import { EvaluationHistoryTab } from '../../components/fuzzy/EvaluationHistoryTab';
import { getStatusLabel, getStatusColor, getStatusIcon } from '../../utils/fuzzyHelpers';
import type { FuzzyStackParamList } from '../../navigation/types';
import type { FuzzySystemStatus, IconName } from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, typography, borderRadius, useFuzzyStore } from '@hydroespinaca/shared';

type Props = NativeStackScreenProps<FuzzyStackParamList, 'FuzzyDetail'>;

type TabKey = 'info' | 'variables' | 'rules' | 'simulation' | 'history';

const TABS: { key: TabKey; label: string; icon: IconName }[] = [
  { key: 'info', label: 'Info', icon: 'info' },
  { key: 'variables', label: 'Variables', icon: 'layers' },
  { key: 'rules', label: 'Reglas', icon: 'list' },
  { key: 'simulation', label: 'Simular', icon: 'play' },
  { key: 'history', label: 'Historial', icon: 'bar-chart' },
];

export function FuzzyDetailScreen({ route, navigation }: Props): React.ReactElement {
  const { systemId, systemName } = route.params;
  const [activeTab, setActiveTab] = useState<TabKey>('info');
  const [refreshing, setRefreshing] = useState(false);
  const [editVisible, setEditVisible] = useState(false);
  const [menuVisible, setMenuVisible] = useState(false);
  const [deleteVisible, setDeleteVisible] = useState(false);
  const [activateVisible, setActivateVisible] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const {
    selectedDetail,
    detailLoading,
    detailError,
    fetchSystemDetail,
    deleteSystem,
    activateSystem,
    cloneSystem,
    exportSystem,
    updateSystemStatus,
    clearSelectedDetail,
  } = useFuzzyStore();

  // Fetch on mount
  useEffect(() => {
    fetchSystemDetail(systemId);
    return () => clearSelectedDetail();
  }, [systemId, fetchSystemDetail, clearSelectedDetail]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await fetchSystemDetail(systemId);
    setRefreshing(false);
  }, [systemId, fetchSystemDetail]);

  const system = selectedDetail?.system;
  const status = system?.status ?? 'DRAFT';

  const handleDelete = async () => {
    try {
      await deleteSystem(systemId);
      navigation.goBack();
    } catch {
      setFeedback({ type: 'error', message: 'Error al eliminar el sistema' });
    }
  };

  const handleActivate = async () => {
    try {
      await activateSystem(systemId);
      await fetchSystemDetail(systemId);
      setFeedback({ type: 'success', message: 'Sistema activado correctamente' });
    } catch {
      setFeedback({ type: 'error', message: 'Error al activar el sistema' });
    }
  };

  const handleClone = async () => {
    try {
      await cloneSystem(systemId);
      setFeedback({ type: 'success', message: 'Sistema duplicado correctamente' });
      setMenuVisible(false);
    } catch {
      setFeedback({ type: 'error', message: 'Error al duplicar el sistema' });
    }
  };

  const handleExport = async () => {
    try {
      const data = await exportSystem(systemId);
      if (data) {
        const uri = `${FileSystem.cacheDirectory}${system?.name ?? 'fuzzy'}_export.json`;
        await FileSystem.writeAsStringAsync(uri, JSON.stringify(data, null, 2));
        await Sharing.shareAsync(uri, { mimeType: 'application/json' });
      }
      setMenuVisible(false);
    } catch {
      setFeedback({ type: 'error', message: 'Error al exportar el sistema' });
    }
  };

  const handleStatusChange = async (newStatus: FuzzySystemStatus) => {
    try {
      await updateSystemStatus(systemId, { status: newStatus });
      await fetchSystemDetail(systemId);
      setFeedback({ type: 'success', message: `Estado cambiado a ${getStatusLabel(newStatus)}` });
      setMenuVisible(false);
    } catch {
      setFeedback({ type: 'error', message: 'Error al cambiar el estado' });
    }
  };

  // ─── Loading ─────────────────────────────────────────────
  if (detailLoading && !selectedDetail) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Spinner size="lg" />
          <Text variant="body" color={semanticColors.textSecondary}>
            Cargando sistema...
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  // ─── Error ───────────────────────────────────────────────
  if (detailError && !selectedDetail) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Alert type="error" message={detailError} />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.backButton}>
          <Icon name="arrow-left" size={24} color={semanticColors.textPrimary} />
        </Pressable>
        <View style={styles.headerCenter}>
          <Text variant="h3" color={semanticColors.textPrimary} numberOfLines={1} style={styles.headerTitle}>
            {system?.name ?? systemName}
          </Text>
          <View style={[styles.statusBadge, { backgroundColor: getStatusColor(status) }]}>
            <Icon name={getStatusIcon(status)} size={12} color={colors.white} />
            <Text variant="caption" color={colors.white} style={styles.statusText}>
              {getStatusLabel(status)}
            </Text>
          </View>
        </View>
        <Pressable onPress={() => setMenuVisible(!menuVisible)} style={styles.menuButton}>
          <Icon name="ellipsis" size={24} color={semanticColors.textPrimary} />
        </Pressable>
      </View>

      {/* Context Menu */}
      {menuVisible && (
        <View style={styles.menuOverlay}>
          <Pressable style={styles.menuBackdrop} onPress={() => setMenuVisible(false)} />
          <View style={styles.menu}>
            <Pressable style={styles.menuItem} onPress={() => { setMenuVisible(false); setEditVisible(true); }}>
              <Icon name="edit" size={18} color={semanticColors.textPrimary} />
              <Text variant="body" color={semanticColors.textPrimary}>Editar</Text>
            </Pressable>
            {status !== 'ACTIVE' && (
              <Pressable style={styles.menuItem} onPress={() => { setMenuVisible(false); setActivateVisible(true); }}>
                <Icon name="power" size={18} color={colors.hidro[600]} />
                <Text variant="body" color={colors.hidro[600]}>Activar</Text>
              </Pressable>
            )}
            {status === 'ACTIVE' && (
              <Pressable style={styles.menuItem} onPress={() => handleStatusChange('INACTIVE')}>
                <Icon name="power" size={18} color={colors.warning[600]} />
                <Text variant="body" color={colors.warning[600]}>Desactivar</Text>
              </Pressable>
            )}
            <Pressable style={styles.menuItem} onPress={handleClone}>
              <Icon name="copy" size={18} color={semanticColors.textPrimary} />
              <Text variant="body" color={semanticColors.textPrimary}>Duplicar</Text>
            </Pressable>
            <Pressable style={styles.menuItem} onPress={handleExport}>
              <Icon name="download" size={18} color={semanticColors.textPrimary} />
              <Text variant="body" color={semanticColors.textPrimary}>Exportar JSON</Text>
            </Pressable>
            {(status === 'DRAFT' || status === 'INACTIVE') && (
              <Pressable style={styles.menuItem} onPress={() => { setMenuVisible(false); setDeleteVisible(true); }}>
                <Icon name="trash" size={18} color={colors.error[600]} />
                <Text variant="body" color={colors.error[600]}>Eliminar</Text>
              </Pressable>
            )}
          </View>
        </View>
      )}

      {/* Tab Bar */}
      <View style={styles.tabBar}>
        {TABS.map((tab) => {
          const active = activeTab === tab.key;
          return (
            <Pressable
              key={tab.key}
              onPress={() => setActiveTab(tab.key)}
              style={[styles.tab, active && styles.tabActive]}
            >
              <Icon
                name={tab.icon}
                size={18}
                color={active ? semanticColors.primary : semanticColors.textTertiary}
              />
              <Text
                variant="caption"
                color={active ? semanticColors.primary : semanticColors.textTertiary}
                style={active ? styles.tabLabelActive : undefined}
              >
                {tab.label}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {/* Tab Content */}
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={semanticColors.primary}
          />
        }
      >
        {feedback && (
          <Alert
            type={feedback.type}
            message={feedback.message}
            onDismiss={() => setFeedback(null)}
          />
        )}

        {selectedDetail && activeTab === 'info' && <InfoTab detail={selectedDetail} />}
        {selectedDetail && activeTab === 'variables' && <VariablesTab detail={selectedDetail} />}
        {selectedDetail && activeTab === 'rules' && <RulesTab detail={selectedDetail} />}
        {selectedDetail && activeTab === 'simulation' && <SimulationTab detail={selectedDetail} />}
        {selectedDetail && activeTab === 'history' && <EvaluationHistoryTab detail={selectedDetail} />}
      </ScrollView>

      {/* Forms & Sheets */}
      {system && (
        <SystemForm
          isOpen={editVisible}
          onClose={() => setEditVisible(false)}
          system={system}
        />
      )}

      <ConfirmationSheet
        isOpen={deleteVisible}
        title="Eliminar sistema"
        message={`¿Eliminar "${system?.name}"? Esta acción no se puede deshacer.`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteVisible(false)}
      />

      <ConfirmationSheet
        isOpen={activateVisible}
        title="Activar sistema"
        message={`¿Activar "${system?.name}"? Solo un sistema puede estar activo.`}
        confirmLabel="Activar"
        onConfirm={handleActivate}
        onCancel={() => setActivateVisible(false)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.md,
  },
  // Header
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    backgroundColor: colors.white,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
  },
  backButton: {
    padding: spacing.xs,
  },
  headerCenter: {
    flex: 1,
    marginHorizontal: spacing.sm,
  },
  headerTitle: {
    fontWeight: typography.fontWeight.bold,
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 4,
    paddingVertical: 2,
    paddingHorizontal: spacing.sm,
    borderRadius: 10,
    marginTop: 2,
  },
  statusText: {
    fontWeight: typography.fontWeight.semibold,
  },
  menuButton: {
    padding: spacing.xs,
  },
  // Context Menu
  menuOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 100,
  },
  menuBackdrop: {
    ...StyleSheet.absoluteFillObject,
  },
  menu: {
    position: 'absolute',
    top: 95,
    right: spacing.md,
    backgroundColor: colors.white,
    borderRadius: borderRadius.xl,
    paddingVertical: spacing.xs,
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 12,
    elevation: 8,
    minWidth: 180,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
  },
  // Tab Bar
  tabBar: {
    flexDirection: 'row',
    backgroundColor: colors.white,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
  },
  tab: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: spacing.sm,
    gap: 2,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabActive: {
    borderBottomColor: semanticColors.primary,
  },
  tabLabelActive: {
    fontWeight: typography.fontWeight.semibold,
  },
  // Content
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    padding: spacing.md,
    paddingBottom: spacing.xl,
    gap: spacing.md,
  },
});
