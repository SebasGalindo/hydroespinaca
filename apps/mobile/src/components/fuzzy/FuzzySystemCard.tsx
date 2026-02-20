/**
 * FuzzySystemCard — Tarjeta de sistema fuzzy para la lista.
 * Muestra nombre, status badge, conteo de variables/reglas.
 * Long-press para menú contextual (activar/duplicar/eliminar).
 */
import React, { useState } from 'react';
import { View, TouchableOpacity, StyleSheet, ActionSheetIOS, Platform } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import type { FuzzySystem, IconName } from '@hydroespinaca/shared';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { getStatusLabel, getStatusColor, getStatusTextColor, getStatusIcon } from '../../utils/fuzzyHelpers';
import { hapticLight, hapticMedium } from '../../utils/haptics';

export interface FuzzySystemCardProps {
  system: FuzzySystem;
  onPress: () => void;
  onActivate?: () => void;
  onClone?: () => void;
  onDelete?: () => void;
  onExport?: () => void;
}

export function FuzzySystemCard({
  system,
  onPress,
  onActivate,
  onClone,
  onDelete,
  onExport,
}: FuzzySystemCardProps): React.ReactElement {
  const [menuVisible, setMenuVisible] = useState(false);

  const inputCount = system.inputVariableIds?.length ?? 0;
  const outputCount = system.outputVariableIds?.length ?? 0;
  const ruleCount = system.ruleIds?.length ?? 0;

  const handleLongPress = () => {
    hapticMedium();
    setMenuVisible(true);
  };

  const handleMenuAction = (action: (() => void) | undefined) => {
    setMenuVisible(false);
    action?.();
  };

  return (
    <>
      <TouchableOpacity
        style={styles.card}
        activeOpacity={0.7}
        onPress={() => { hapticLight(); onPress(); }}
        onLongPress={handleLongPress}
        delayLongPress={500}
        accessibilityRole="button"
        accessibilityLabel={`Sistema ${system.name}, ${getStatusLabel(system.status)}`}
        accessibilityHint="Pulsar para ver detalles, mantener pulsado para opciones"
      >
        {/* Header */}
        <View style={styles.header}>
          <View style={styles.nameRow}>
            <Icon name="settings" size={20} color={semanticColors.primary} />
            <Text variant="h3" color={semanticColors.textPrimary} numberOfLines={1} style={styles.name}>
              {system.name}
            </Text>
          </View>
          <View style={[styles.statusBadge, { backgroundColor: getStatusColor(system.status) }]}>
            <Icon
              name={getStatusIcon(system.status) as IconName}
              size={12}
              color={getStatusTextColor(system.status)}
            />
            <Text variant="caption" color={getStatusTextColor(system.status)}>
              {getStatusLabel(system.status)}
            </Text>
          </View>
        </View>

        {/* Stats Row */}
        <View style={styles.statsRow}>
          <View style={styles.stat}>
            <Icon name="arrow-down" size={14} color={colors.info[500]} />
            <Text variant="caption" color={semanticColors.textSecondary}>
              {inputCount} entrada{inputCount !== 1 ? 's' : ''}
            </Text>
          </View>
          <View style={styles.stat}>
            <Icon name="arrow-up" size={14} color={colors.hidro[500]} />
            <Text variant="caption" color={semanticColors.textSecondary}>
              {outputCount} salida{outputCount !== 1 ? 's' : ''}
            </Text>
          </View>
          <View style={styles.stat}>
            <Icon name="git-branch" size={14} color={colors.warning[500]} />
            <Text variant="caption" color={semanticColors.textSecondary}>
              {ruleCount} regla{ruleCount !== 1 ? 's' : ''}
            </Text>
          </View>
        </View>

        {/* Chevron */}
        <View style={styles.chevronContainer}>
          <Icon name="chevron-right" size={18} color={semanticColors.textTertiary} />
        </View>
      </TouchableOpacity>

      {/* Context Menu as a BottomSheet-like overlay */}
      {menuVisible && (
        <TouchableOpacity
          style={styles.menuOverlay}
          activeOpacity={1}
          onPress={() => setMenuVisible(false)}
        >
          <View style={styles.menuContainer}>
            <View style={styles.menuHeader}>
              <Text variant="label" color={semanticColors.textPrimary}>
                {system.name}
              </Text>
              <TouchableOpacity onPress={() => setMenuVisible(false)}>
                <Icon name="close" size={22} color={semanticColors.textSecondary} />
              </TouchableOpacity>
            </View>

            {system.status !== 'ACTIVE' && onActivate && (
              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => handleMenuAction(onActivate)}
              >
                <Icon name="power" size={20} color={colors.hidro[600]} />
                <Text variant="body" color={semanticColors.textPrimary}>Activar</Text>
              </TouchableOpacity>
            )}

            {onClone && (
              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => handleMenuAction(onClone)}
              >
                <Icon name="copy" size={20} color={colors.info[600]} />
                <Text variant="body" color={semanticColors.textPrimary}>Duplicar</Text>
              </TouchableOpacity>
            )}

            {onExport && (
              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => handleMenuAction(onExport)}
              >
                <Icon name="download" size={20} color={semanticColors.primary} />
                <Text variant="body" color={semanticColors.textPrimary}>Exportar JSON</Text>
              </TouchableOpacity>
            )}

            {onDelete && (system.status === 'DRAFT' || system.status === 'INACTIVE') && (
              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => handleMenuAction(onDelete)}
              >
                <Icon name="trash" size={20} color={colors.error[600]} />
                <Text variant="body" color={colors.error[600]}>Eliminar</Text>
              </TouchableOpacity>
            )}
          </View>
        </TouchableOpacity>
      )}
    </>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  nameRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    flex: 1,
    marginRight: spacing.sm,
  },
  name: {
    fontWeight: typography.fontWeight.semibold,
    flex: 1,
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingVertical: 3,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.xl,
  },
  statsRow: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  stat: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  chevronContainer: {
    position: 'absolute',
    right: spacing.md,
    bottom: spacing.md,
  },
  menuOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: semanticColors.overlayMedium,
    justifyContent: 'flex-end',
    zIndex: 100,
  },
  menuContainer: {
    backgroundColor: semanticColors.surface,
    borderTopLeftRadius: borderRadius.xl,
    borderTopRightRadius: borderRadius.xl,
    paddingBottom: spacing.xl,
  },
  menuHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
});
