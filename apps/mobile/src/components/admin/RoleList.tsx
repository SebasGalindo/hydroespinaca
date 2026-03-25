/**
 * RoleList — Lista de roles con permisos y acciones.
 */
import React from 'react';
import { View, FlatList, TouchableOpacity, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Card } from '../molecules/Card';
import type { RoleResponseDto } from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, borderRadius, typography } from '@hydroespinaca/shared';

export interface RoleListProps {
  roles: RoleResponseDto[];
  onEdit: (role: RoleResponseDto) => void;
  onDelete: (role: RoleResponseDto) => void;
}

export function RoleList({
  roles,
  onEdit,
  onDelete,
}: RoleListProps): React.ReactElement {
  const isAdminRole = (role: RoleResponseDto): boolean =>
    role.name?.toLowerCase() === 'administrador' || role.code?.toLowerCase() === 'admin';

  const renderRole = ({ item }: { item: RoleResponseDto }) => {
    const admin = isAdminRole(item);
    const permCount = item.permissionCodes?.length ?? 0;
    const displayPerms = item.permissionCodes?.slice(0, 3) ?? [];
    const remaining = permCount - displayPerms.length;

    return (
      <Card variant="outlined" padding="md" style={styles.roleCard}>
        <View style={styles.roleHeader}>
          {/* Icon */}
          <View style={[styles.roleIcon, admin && styles.roleIconAdmin]}>
            <Icon
              name={admin ? 'lock' : 'user'}
              size={20}
              color={admin ? colors.warning[600] : colors.hidro[600]}
            />
          </View>

          {/* Info */}
          <View style={styles.roleInfo}>
            <Text variant="body" color={semanticColors.textPrimary} style={styles.roleName}>
              {item.name}
            </Text>
            <View style={styles.codeBadge}>
              <Text variant="caption" color={semanticColors.textTertiary} style={styles.codeText}>
                {item.code}
              </Text>
            </View>
          </View>

          {/* Actions */}
          <View style={styles.actions}>
            <TouchableOpacity
              onPress={() => onEdit(item)}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              accessibilityLabel={`Editar ${item.name}`}
            >
              <Icon name="edit" size={20} color={semanticColors.textSecondary} />
            </TouchableOpacity>
            <TouchableOpacity
              onPress={() => onDelete(item)}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              accessibilityLabel={`Eliminar ${item.name}`}
            >
              <Icon name="trash" size={20} color={colors.error[500]} />
            </TouchableOpacity>
          </View>
        </View>

        {/* Permissions */}
        {permCount > 0 && (
          <View style={styles.permissionsRow}>
            <Icon name="lock" size={14} color={semanticColors.textTertiary} />
            <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={1} style={styles.permText}>
              {displayPerms.join(', ')}
              {remaining > 0 ? ` +${remaining} más` : ''}
            </Text>
          </View>
        )}
        {permCount === 0 && (
          <Text variant="caption" color={semanticColors.textTertiary} style={styles.noPerms}>
            Sin permisos asignados
          </Text>
        )}
      </Card>
    );
  };

  return (
    <FlatList
      data={roles}
      keyExtractor={(item) => item.id}
      renderItem={renderRole}
      contentContainerStyle={styles.listContent}
      showsVerticalScrollIndicator={false}
      ListEmptyComponent={
        <View style={styles.empty}>
          <Icon name="lock" size={40} color={semanticColors.textTertiary} />
          <Text variant="body" color={semanticColors.textTertiary}>
            No hay roles
          </Text>
        </View>
      }
    />
  );
}

const styles = StyleSheet.create({
  listContent: {
    gap: spacing.sm,
    paddingBottom: spacing.lg,
  },
  roleCard: {
    marginBottom: 0,
  },
  roleHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  roleIcon: {
    width: 40,
    height: 40,
    borderRadius: borderRadius.xl,
    backgroundColor: colors.hidro[100],
    justifyContent: 'center',
    alignItems: 'center',
  },
  roleIconAdmin: {
    backgroundColor: colors.warning[100],
  },
  roleInfo: {
    flex: 1,
    gap: 2,
  },
  roleName: {
    fontWeight: typography.fontWeight.semibold,
  },
  codeBadge: {
    alignSelf: 'flex-start',
    backgroundColor: colors.gray[100],
    paddingHorizontal: spacing.xs,
    paddingVertical: 1,
    borderRadius: borderRadius.sm,
  },
  codeText: {
    fontFamily: 'monospace',
    fontSize: typography.fontSize.xs,
  },
  actions: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  permissionsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.gray[100],
  },
  permText: {
    flex: 1,
  },
  noPerms: {
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.gray[100],
    fontStyle: 'italic',
  },
  empty: {
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.sm,
  },
});
