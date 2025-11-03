import React, { useState, useCallback } from 'react';
import { View, StyleSheet, Modal, ScrollView, Pressable } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { Spinner } from '../atoms/Spinner';
import { Pressable as CustomPressable } from '../atoms/Pressable';
import { semanticColors, spacing, borderRadius, fuzzyRulesService, type FuzzyRuleSummary } from '@hydroespinaca/shared';

/**
 * Component that displays fuzzy logic rules in a modal dialog.
 * Fetches the rules once per session and caches them locally.
 */
export function FuzzyRulesInfo(): React.ReactElement {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [rules, setRules] = useState<FuzzyRuleSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFuzzyRules = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await fuzzyRulesService.getFuzzyRules();
      setRules(data);
    } catch (err: any) {
      setError(err.message || 'Error al cargar las reglas difusas');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleOpenModal = () => {
    setIsModalOpen(true);
    // Fetch rules when modal opens
    if (rules.length === 0 && !isLoading) {
      fetchFuzzyRules();
    }
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
  };

  return (
    <>
      {/* Info Button */}
      <Button
        variant="ghost"
        size="sm"
        onPress={handleOpenModal}
        leftIcon={<Icon name="info" size={18} color={semanticColors.primary} />}
        style={styles.button}
      >
        Reglas difusas
      </Button>

      {/* Modal */}
      <Modal
        visible={isModalOpen}
        animationType="slide"
        transparent={true}
        onRequestClose={handleCloseModal}
      >
        <Pressable style={styles.modalOverlay} onPress={handleCloseModal}>
          <Pressable style={styles.modalWrapper} onPress={(e) => e.stopPropagation()}>
            <SafeAreaView style={styles.modalContent} edges={['top', 'bottom']}>
              {/* Header */}
              <View style={styles.modalHeader}>
                <Text variant="label" color={semanticColors.textPrimary} style={styles.modalTitle}>
                  Reglas de lógica difusa
                </Text>
                <CustomPressable
                  onPress={handleCloseModal}
                  style={styles.closeButton}
                  accessibilityLabel="Cerrar modal"
                >
                  <Icon name="x" size={24} color={semanticColors.textSecondary} />
                </CustomPressable>
              </View>

              {/* Content Area with fixed height */}
              <View style={styles.contentArea}>
                {/* Loading State */}
                {isLoading && (
                  <View style={styles.centerContent}>
                    <Spinner size="lg" color={semanticColors.primary} />
                    <Text variant="body" color={semanticColors.textSecondary} style={styles.statusText}>
                      Cargando reglas difusas...
                    </Text>
                  </View>
                )}

                {/* Error State */}
                {error && !isLoading && (
                  <View style={styles.centerContent}>
                    <Icon name="alert-triangle" size={48} color={semanticColors.errorText} />
                    <Text variant="body" color={semanticColors.errorText} style={styles.errorTitle}>
                      Error al cargar las reglas
                    </Text>
                    <Text variant="caption" color={semanticColors.errorText} style={styles.errorMessage}>
                      {error}
                    </Text>
                    <Button variant="primary" size="sm" onPress={fetchFuzzyRules} style={styles.retryButton}>
                      Reintentar
                    </Button>
                  </View>
                )}

                {/* Rules List */}
                {!isLoading && !error && rules.length > 0 && (
                  <ScrollView
                    style={styles.scrollView}
                    contentContainerStyle={styles.scrollContent}
                    showsVerticalScrollIndicator={true}
                  >
                    <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
                      El sistema utiliza {rules.length} regla{rules.length !== 1 ? 's' : ''} de
                      lógica difusa para controlar automáticamente las variables del cultivo.
                    </Text>

                    <View style={styles.rulesList}>
                      {rules.map((rule) => (
                        <View key={rule.id} style={styles.ruleCard}>
                          <View style={styles.ruleHeader}>
                            <View style={styles.ruleIconContainer}>
                              <Text style={styles.ruleIcon}>⚡</Text>
                            </View>
                            <Text variant="label" color={semanticColors.textPrimary} style={styles.ruleName}>
                              {rule.name}
                            </Text>
                          </View>
                          <Text variant="body" color={semanticColors.textSecondary} style={styles.ruleDescription}>
                            {rule.description}
                          </Text>
                        </View>
                      ))}
                    </View>
                  </ScrollView>
                )}

                {/* Empty State */}
                {!isLoading && !error && rules.length === 0 && (
                  <View style={styles.centerContent}>
                    <View style={styles.emptyIconContainer}>
                      <Icon name="file-text" size={32} color={semanticColors.textSecondary} />
                    </View>
                    <Text variant="body" color={semanticColors.textPrimary} style={styles.emptyTitle}>
                      No hay reglas difusas configuradas
                    </Text>
                    <Text variant="caption" color={semanticColors.textSecondary} style={styles.emptyMessage}>
                      El sistema aún no tiene reglas de lógica difusa definidas.
                    </Text>
                  </View>
                )}
              </View>

              {/* Footer */}
              <View style={styles.modalFooter}>
                <Button
                  variant="outline"
                  size="md"
                  onPress={handleCloseModal}
                  fullWidth
                >
                  Cerrar
                </Button>
              </View>
            </SafeAreaView>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  button: {
    alignSelf: 'flex-start',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  modalWrapper: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: semanticColors.background,
    borderTopLeftRadius: borderRadius.xl,
    borderTopRightRadius: borderRadius.xl,
    height: '85%',
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: -2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 8,
    elevation: 10,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.borderLight,
  },
  modalTitle: {
    flex: 1,
    fontWeight: 'bold',
    fontSize: 16,
  },
  closeButton: {
    padding: spacing.xs,
    marginLeft: spacing.sm,
  },
  contentArea: {
    flex: 1,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    padding: spacing.lg,
  },
  description: {
    marginBottom: spacing.lg,
    lineHeight: 20,
  },
  rulesList: {
    gap: spacing.md,
    paddingBottom: spacing.md,
  },
  ruleCard: {
    backgroundColor: semanticColors.background,
    borderWidth: 1,
    borderColor: semanticColors.borderLight,
    borderRadius: borderRadius.md,
    padding: spacing.md,
  },
  ruleHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    marginBottom: spacing.sm,
  },
  ruleIconContainer: {
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: '#dcfce7',
    alignItems: 'center',
    justifyContent: 'center',
  },
  ruleIcon: {
    fontSize: 14,
  },
  ruleName: {
    flex: 1,
    fontWeight: '600',
  },
  ruleDescription: {
    lineHeight: 18,
    paddingLeft: 32,
  },
  centerContent: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.md,
  },
  statusText: {
    marginTop: spacing.sm,
  },
  errorTitle: {
    fontWeight: '600',
    marginTop: spacing.sm,
  },
  errorMessage: {
    textAlign: 'center',
  },
  retryButton: {
    marginTop: spacing.sm,
  },
  emptyIconContainer: {
    width: 64,
    height: 64,
    borderRadius: 32,
    backgroundColor: '#f3f4f6',
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyTitle: {
    fontWeight: '600',
  },
  emptyMessage: {
    textAlign: 'center',
  },
  modalFooter: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderTopWidth: 1,
    borderTopColor: semanticColors.borderLight,
    backgroundColor: semanticColors.background,
  },
});
