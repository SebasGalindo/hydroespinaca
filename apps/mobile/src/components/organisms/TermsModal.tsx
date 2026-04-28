import React, { useEffect, useState } from 'react';
import {
  Modal,
  View,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  TouchableOpacity,
} from 'react-native';
import { authService, semanticColors, spacing, typography, colors } from '@hydroespinaca/shared';
import type { TermsContent } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Heading } from '../atoms/Heading';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';

export interface TermsModalProps {
  visible: boolean;
  onAccept?: () => void;
  onClose?: () => void;
  readOnly?: boolean;
  testID?: string;
}

export function TermsModal({
  visible,
  onAccept,
  onClose,
  readOnly = false,
  testID,
}: TermsModalProps): React.ReactElement {
  const [terms, setTerms] = useState<TermsContent | null>(null);
  const [loading, setLoading] = useState(true);
  const [accepting, setAccepting] = useState(false);

  useEffect(() => {
    if (visible) {
      setLoading(true);
      authService.getTerms()
        .then(setTerms)
        .finally(() => setLoading(false));
    }
  }, [visible]);

  const handleAccept = async () => {
    if (!onAccept) return;
    setAccepting(true);
    try {
      await onAccept();
    } finally {
      setAccepting(false);
    }
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={readOnly ? onClose : undefined}
      testID={testID}
    >
      <View style={styles.overlay}>
        <View style={styles.sheet}>
          {/* Header */}
          <View style={styles.header}>
            <Heading level={4} color={semanticColors.textPrimary} style={styles.title}>
              {terms?.title ?? 'Términos y Condiciones'}
            </Heading>
            {(readOnly && onClose) && (
              <TouchableOpacity onPress={onClose} style={styles.closeButton} accessibilityLabel="Cerrar">
                <Icon name="close" size={20} color={semanticColors.textSecondary} />
              </TouchableOpacity>
            )}
          </View>

          {/* Body */}
          <ScrollView style={styles.body} contentContainerStyle={styles.bodyContent} showsVerticalScrollIndicator>
            {loading && (
              <ActivityIndicator size="large" color={semanticColors.primary} style={styles.loader} />
            )}

            {!loading && terms && (
              <>
                <Text variant="caption" color={semanticColors.textSecondary} style={styles.lastUpdated}>
                  Última actualización: {terms.lastUpdated}
                </Text>
                {terms.sections.map((section, idx) => (
                  <View key={idx} style={styles.section}>
                    <Text variant="label" color={semanticColors.textPrimary} style={styles.sectionTitle}>
                      {section.title}
                    </Text>
                    <Text variant="body" color={semanticColors.textSecondary} style={styles.sectionContent}>
                      {section.content}
                    </Text>
                  </View>
                ))}
              </>
            )}

            {!loading && !terms && (
              <Text variant="body" color={semanticColors.errorText}>
                No se pudieron cargar los términos. Intenta de nuevo.
              </Text>
            )}
          </ScrollView>

          {/* Footer */}
          {!readOnly && (
            <View style={styles.footer}>
              {onClose && (
                <Button
                  onPress={onClose}
                  variant="outline"
                  size="md"
                  style={styles.cancelButton}
                >
                  Cancelar
                </Button>
              )}
              <Button
                onPress={handleAccept}
                variant="primary"
                size="md"
                loading={accepting}
                disabled={loading || accepting}
                style={styles.acceptButton}
              >
                Acepto los términos
              </Button>
            </View>
          )}
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.6)',
    justifyContent: 'flex-end',
  },
  sheet: {
    backgroundColor: colors.white,
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    maxHeight: '90%',
    paddingBottom: spacing.xl,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  title: {
    flex: 1,
  },
  closeButton: {
    padding: spacing.xs,
    marginLeft: spacing.sm,
  },
  body: {
    flex: 1,
  },
  bodyContent: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    gap: spacing.md,
  },
  loader: {
    marginVertical: spacing.xl,
  },
  lastUpdated: {
    marginBottom: spacing.sm,
  },
  section: {
    marginBottom: spacing.md,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  sectionContent: {
    lineHeight: 20,
  },
  footer: {
    flexDirection: 'row',
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    gap: spacing.sm,
  },
  cancelButton: {
    flex: 1,
  },
  acceptButton: {
    flex: 2,
  },
});
