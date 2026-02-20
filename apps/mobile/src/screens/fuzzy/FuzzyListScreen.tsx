/**
 * FuzzyListScreen — Lista de sistemas fuzzy.
 * SearchBar + status filter chips + FlatList de FuzzySystemCard.
 * FAB para crear + importar JSON. Pull-to-refresh.
 */
import React, { useState, useCallback } from 'react';
import {
  View,
  FlatList,
  StyleSheet,
  RefreshControl,
  TouchableOpacity,
  ActivityIndicator,
  Modal,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import * as DocumentPicker from 'expo-document-picker';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { FloatingActionButton } from '../../components/atoms/FloatingActionButton';
import { SearchBar } from '../../components/molecules/SearchBar';
import { Alert } from '../../components/molecules/Alert';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import { SkeletonLoader } from '../../components/organisms/SkeletonLoader';
import { FuzzySystemCard } from '../../components/fuzzy/FuzzySystemCard';
import { SystemForm } from '../../components/fuzzy/SystemForm';
import type { FuzzyStackParamList } from '../../navigation/types';
import type { FuzzySystem, FuzzySystemStatus, FuzzySystemExport } from '@hydroespinaca/shared';
import {
  semanticColors,
  spacing,
  colors,
  typography,
  borderRadius,
  shadows,
  useFuzzyStore,
  FUZZY_STATUS_LABELS,
} from '@hydroespinaca/shared';
import { getStatusColor, getStatusTextColor } from '../../utils/fuzzyHelpers';
import { showToast } from '../../utils/toast';
import { hapticSuccess, hapticError, hapticHeavy } from '../../utils/haptics';

type FuzzyListNavProp = NativeStackNavigationProp<FuzzyStackParamList, 'FuzzyList'>;

const STATUS_FILTERS: (FuzzySystemStatus | 'ALL')[] = ['ALL', 'ACTIVE', 'DRAFT', 'TESTING', 'INACTIVE'];

export function FuzzyListScreen(): React.ReactElement {
  const navigation = useNavigation<FuzzyListNavProp>();
  const {
    systems,
    systemsLoading,
    systemsError,
    fetchSystems,
    activateSystem,
    cloneSystem,
    deleteSystem,
    exportSystem,
    importSystem,
    operationLoading,
    importLoading,
    operationError,
    clearErrors,
  } = useFuzzyStore();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<FuzzySystemStatus | 'ALL'>('ALL');
  const [showForm, setShowForm] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<FuzzySystem | null>(null);
  const [activateTarget, setActivateTarget] = useState<FuzzySystem | null>(null);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [loadingMessage, setLoadingMessage] = useState<string | null>(null);

  useFocusEffect(
    useCallback(() => {
      fetchSystems();
    }, [fetchSystems]),
  );

  const filteredSystems = systems.filter((s) => {
    if (statusFilter !== 'ALL' && s.status !== statusFilter) return false;
    if (search.trim()) {
      return s.name.toLowerCase().includes(search.trim().toLowerCase());
    }
    return true;
  });

  const handleRefresh = () => {
    clearErrors();
    setFeedback(null);
    fetchSystems();
  };

  const handleCreateSuccess = (system: FuzzySystem) => {
    setFeedback({ type: 'success', message: `Sistema "${system.name}" creado` });
    showToast('success', `Sistema "${system.name}" creado`);
    hapticSuccess();
    navigation.navigate('FuzzyDetail', { systemId: system.id, systemName: system.name });
  };

  const confirmActivate = async () => {
    if (!activateTarget) return;
    try {
      await activateSystem(activateTarget.id);
      setFeedback({ type: 'success', message: `"${activateTarget.name}" activado` });
      showToast('success', `"${activateTarget.name}" activado`);
      hapticSuccess();
    } catch {
      setFeedback({ type: 'error', message: 'Error al activar sistema' });
      showToast('error', 'Error al activar sistema');
      hapticError();
    }
    setActivateTarget(null);
  };

  const handleClone = async (system: FuzzySystem) => {
    try {
      setLoadingMessage('Duplicando rutina…');
      const cloned = await cloneSystem(system.id, { name: `${system.name} (copia)` });
      setFeedback({ type: 'success', message: `Duplicado como "${cloned.name}"` });
      showToast('success', `Duplicado como "${cloned.name}"`);
      hapticSuccess();
    } catch {
      setFeedback({ type: 'error', message: 'Error al duplicar sistema' });
      showToast('error', 'Error al duplicar sistema');
      hapticError();
    } finally {
      setLoadingMessage(null);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteSystem(deleteTarget.id);
      setFeedback({ type: 'success', message: `"${deleteTarget.name}" eliminado` });
      showToast('success', `"${deleteTarget.name}" eliminado`);
      hapticHeavy();
    } catch {
      setFeedback({ type: 'error', message: 'Error al eliminar sistema' });
      showToast('error', 'Error al eliminar sistema');
      hapticError();
    }
    setDeleteTarget(null);
  };

  const handleExport = async (system: FuzzySystem) => {
    try {
      const data = await exportSystem(system.id);
      const json = JSON.stringify(data, null, 2);
      const filename = `${system.name.replace(/\s+/g, '_')}_export.json`;
      const fileUri = `${FileSystem.cacheDirectory}${filename}`;
      await FileSystem.writeAsStringAsync(fileUri, json, { encoding: FileSystem.EncodingType.UTF8 });
      const canShare = await Sharing.isAvailableAsync();
      if (canShare) {
        await Sharing.shareAsync(fileUri, { mimeType: 'application/json', dialogTitle: 'Exportar Sistema Fuzzy' });
      }
      setFeedback({ type: 'success', message: `"${system.name}" exportado` });
      showToast('success', `"${system.name}" exportado`);
      hapticSuccess();
    } catch {
      setFeedback({ type: 'error', message: 'Error al exportar sistema' });
      showToast('error', 'Error al exportar sistema');
      hapticError();
    }
  };

  const handleImport = async () => {
    try {
      const result = await DocumentPicker.getDocumentAsync({
        type: 'application/json',
        copyToCacheDirectory: true,
      });
      if (result.canceled || !result.assets?.length) return;

      const file = result.assets[0]!;
      const content = await FileSystem.readAsStringAsync(file.uri, { encoding: FileSystem.EncodingType.UTF8 });
      const parsed = JSON.parse(content) as FuzzySystemExport;

      if (!parsed.system || !parsed.variables) {
        setFeedback({ type: 'error', message: 'Formato de archivo inválido' });
        return;
      }

      setLoadingMessage('Importando rutina…');
      const imported = await importSystem(parsed);
      setFeedback({ type: 'success', message: `"${imported.name}" importado exitosamente` });
      showToast('success', `"${imported.name}" importado exitosamente`);
      hapticSuccess();
    } catch {
      setFeedback({ type: 'error', message: 'Error al importar archivo JSON' });
    } finally {
      setLoadingMessage(null);
    }
  };

  const getFilterLabel = (f: FuzzySystemStatus | 'ALL'): string =>
    f === 'ALL' ? 'Todos' : FUZZY_STATUS_LABELS[f];

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.titleRow}>
          <Text variant="h2" color={semanticColors.primary} style={styles.title}>
            Rutinas Fuzzy
          </Text>
          <TouchableOpacity
            onPress={handleImport}
            style={styles.importButton}
            accessibilityLabel="Importar sistema JSON"
          >
            <Icon name="upload" size={22} color={semanticColors.primary} />
          </TouchableOpacity>
        </View>
        <Text variant="body" color={semanticColors.textSecondary}>
          Gestiona los sistemas de lógica difusa
        </Text>
      </View>

      {/* Search */}
      <View style={styles.searchContainer}>
        <SearchBar
          value={search}
          onChangeText={setSearch}
          placeholder="Buscar sistema..."
        />
      </View>

      {/* Status Filters */}
      <View style={styles.filtersContainer}>
        <FlatList
          data={STATUS_FILTERS}
          horizontal
          showsHorizontalScrollIndicator={false}
          keyExtractor={(item) => item}
          contentContainerStyle={styles.filtersList}
          renderItem={({ item }) => {
            const isActive = statusFilter === item;
            return (
              <TouchableOpacity
                style={[
                  styles.filterChip,
                  { backgroundColor: isActive ? colors.hidro[500] : colors.gray[100] },
                ]}
                onPress={() => setStatusFilter(item)}
                accessibilityRole="radio"
                accessibilityState={{ selected: isActive }}
              >
                <Text
                  variant="caption"
                  color={isActive ? colors.white : semanticColors.textSecondary}
                >
                  {getFilterLabel(item)}
                </Text>
              </TouchableOpacity>
            );
          }}
        />
      </View>

      {/* Feedback */}
      {feedback && (
        <View style={styles.feedbackContainer}>
          <Alert
            type={feedback.type}
            message={feedback.message}
            onDismiss={() => setFeedback(null)}
          />
        </View>
      )}

      {(systemsError || operationError) && (
        <View style={styles.feedbackContainer}>
          <Alert
            type="error"
            message={systemsError || operationError || ''}
            onDismiss={clearErrors}
          />
        </View>
      )}

      {/* List */}
      {systemsLoading && systems.length === 0 ? (
        <View style={styles.centerContainer}>
          <SkeletonLoader variant="card" count={3} />
        </View>
      ) : filteredSystems.length === 0 ? (
        <View style={styles.centerContainer}>
          <Icon name="funnel" size={64} color={semanticColors.textTertiary} />
          <Text variant="body" color={semanticColors.textSecondary} style={styles.emptyText}>
            {systems.length === 0
              ? 'No hay sistemas fuzzy creados'
              : 'Sin resultados para el filtro aplicado'}
          </Text>
        </View>
      ) : (
        <FlatList
          data={filteredSystems}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl refreshing={systemsLoading} onRefresh={handleRefresh} />
          }
          renderItem={({ item }) => (
            <FuzzySystemCard
              system={item}
              onPress={() =>
                navigation.navigate('FuzzyDetail', {
                  systemId: item.id,
                  systemName: item.name,
                })
              }
              onActivate={() => setActivateTarget(item)}
              onClone={() => handleClone(item)}
              onDelete={() => setDeleteTarget(item)}
              onExport={() => handleExport(item)}
            />
          )}
        />
      )}

      {/* FAB */}
      <FloatingActionButton
        onPress={() => setShowForm(true)}
        accessibilityLabel="Crear nuevo sistema fuzzy"
      />

      {/* Create Form */}
      <SystemForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSuccess={handleCreateSuccess}
      />

      {/* Delete Confirmation */}
      <ConfirmationSheet
        isOpen={!!deleteTarget}
        title="Eliminar sistema"
        message={`¿Estás seguro de eliminar "${deleteTarget?.name}"? Esta acción no se puede deshacer.`}
        confirmLabel="Eliminar"
        destructive
        onConfirm={confirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />

      {/* Activate Confirmation */}
      <ConfirmationSheet
        isOpen={!!activateTarget}
        title="Activar sistema"
        message={`¿Activar "${activateTarget?.name}"? Los demás sistemas activos pasarán a inactivos.`}
        confirmLabel="Activar"
        onConfirm={confirmActivate}
        onCancel={() => setActivateTarget(null)}
      />

      {/* Loading overlay */}
      <Modal transparent visible={!!(loadingMessage || operationLoading || importLoading)} animationType="fade">
        <View style={styles.overlay}>
          <View style={styles.overlayCard}>
            <ActivityIndicator size="large" color={colors.hidro[600]} />
            <Text variant="body" color={colors.hidro[700]} align="center" style={{ marginTop: spacing.md }}>
              {loadingMessage ?? 'Procesando…'}
            </Text>
            <Text variant="caption" color={colors.hidro[400]} align="center" style={{ marginTop: spacing.xs }}>
              Esto puede tardar unos segundos
            </Text>
          </View>
        </View>
      </Modal>
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
    paddingTop: spacing.lg,
    paddingBottom: spacing.sm,
  },
  titleRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
  },
  importButton: {
    padding: spacing.xs,
  },
  searchContainer: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.sm,
  },
  filtersContainer: {
    marginBottom: spacing.sm,
  },
  filtersList: {
    paddingHorizontal: spacing.lg,
    gap: spacing.xs,
  },
  filterChip: {
    paddingVertical: 6,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius['2xl'],
  },
  feedbackContainer: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.sm,
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.md,
  },
  loadingText: {
    marginTop: spacing.md,
  },
  emptyText: {
    marginTop: spacing.sm,
    textAlign: 'center',
  },
  listContent: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl * 3,
    gap: spacing.md,
  },
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.4)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  overlayCard: {
    backgroundColor: colors.white,
    borderRadius: borderRadius.xl,
    padding: spacing.xl,
    alignItems: 'center',
    width: '75%',
    ...shadows.lg,
  },
});