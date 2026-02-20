/**
 * UserList — Lista de usuarios con avatar, rol y acciones.
 * Soporta editar y eliminar (con protección de auto-eliminación).
 */
import React from 'react';
import { View, FlatList, TouchableOpacity, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Card } from '../molecules/Card';
import type { UserResponseDto, RoleResponseDto } from '@hydroespinaca/shared';
import { colors, spacing, semanticColors, borderRadius, typography } from '@hydroespinaca/shared';

export interface UserListProps {
  users: UserResponseDto[];
  roles: RoleResponseDto[];
  currentUserId?: string;
  onEdit: (user: UserResponseDto) => void;
  onDelete: (user: UserResponseDto) => void;
}

export function UserList({
  users,
  roles,
  currentUserId,
  onEdit,
  onDelete,
}: UserListProps): React.ReactElement {
  const getRoleName = (roleId?: string | null): string => {
    if (!roleId) return 'Sin rol';
    const role = roles.find((r) => r.id === roleId);
    return role?.name ?? 'Desconocido';
  };

  const isAdminRole = (roleId?: string | null): boolean => {
    if (!roleId) return false;
    const role = roles.find((r) => r.id === roleId);
    return role?.name?.toLowerCase() === 'administrador' || role?.code?.toLowerCase() === 'admin';
  };

  const isSelf = (userId: string): boolean => userId === currentUserId;

  const renderUser = ({ item }: { item: UserResponseDto }) => {
    const admin = isAdminRole(item.roleId);
    const self = isSelf(item.id);

    return (
      <Card variant="outlined" padding="md" style={styles.userCard}>
        <View style={styles.userRow}>
          {/* Avatar */}
          <View style={[styles.avatar, admin && styles.avatarAdmin]}>
            <Text variant="body" color={semanticColors.textInverse} style={styles.avatarText}>
              {item.username.charAt(0).toUpperCase()}
            </Text>
          </View>

          {/* User info */}
          <View style={styles.userInfo}>
            <View style={styles.nameRow}>
              <Text variant="body" color={semanticColors.textPrimary} style={styles.userName} numberOfLines={1}>
                {item.username}
              </Text>
              {self && (
                <View style={styles.selfBadge}>
                  <Text variant="caption" color={colors.hidro[700]} style={styles.selfBadgeText}>
                    Tú
                  </Text>
                </View>
              )}
            </View>
            <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={1}>
              {item.email}
            </Text>
            <View style={[styles.roleBadge, admin && styles.roleBadgeAdmin]}>
              <Text
                variant="caption"
                color={admin ? colors.warning[700] : colors.hidro[700]}
                style={styles.roleBadgeText}
              >
                {getRoleName(item.roleId)}
              </Text>
            </View>
          </View>

          {/* Actions */}
          <View style={styles.actions}>
            <TouchableOpacity
              onPress={() => onEdit(item)}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              accessibilityLabel={`Editar ${item.username}`}
            >
              <Icon name="edit" size={20} color={semanticColors.textSecondary} />
            </TouchableOpacity>
            <TouchableOpacity
              onPress={() => onDelete(item)}
              disabled={self}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              accessibilityLabel={`Eliminar ${item.username}`}
              style={self ? styles.disabledAction : undefined}
            >
              <Icon
                name="trash"
                size={20}
                color={self ? colors.gray[300] : colors.error[500]}
              />
            </TouchableOpacity>
          </View>
        </View>
      </Card>
    );
  };

  return (
    <FlatList
      data={users}
      keyExtractor={(item) => item.id}
      renderItem={renderUser}
      contentContainerStyle={styles.listContent}
      showsVerticalScrollIndicator={false}
      ListEmptyComponent={
        <View style={styles.empty}>
          <Icon name="user" size={40} color={semanticColors.textTertiary} />
          <Text variant="body" color={semanticColors.textTertiary}>
            No hay usuarios
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
  userCard: {
    marginBottom: 0,
  },
  userRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: colors.hidro[500],
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarAdmin: {
    backgroundColor: colors.warning[500],
  },
  avatarText: {
    fontWeight: typography.fontWeight.bold,
    fontSize: typography.fontSize.lg,
  },
  userInfo: {
    flex: 1,
    gap: 2,
  },
  nameRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  userName: {
    fontWeight: typography.fontWeight.semibold,
  },
  selfBadge: {
    backgroundColor: colors.hidro[100],
    paddingHorizontal: spacing.xs,
    paddingVertical: 1,
    borderRadius: borderRadius.md,
  },
  selfBadgeText: {
    fontWeight: typography.fontWeight.semibold,
    fontSize: typography.fontSize.xxs,
  },
  roleBadge: {
    alignSelf: 'flex-start',
    backgroundColor: colors.hidro[100],
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.lg,
    marginTop: 2,
  },
  roleBadgeAdmin: {
    backgroundColor: colors.warning[100],
  },
  roleBadgeText: {
    fontWeight: typography.fontWeight.semibold,
    fontSize: typography.fontSize.xs,
  },
  actions: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  disabledAction: {
    opacity: 0.4,
  },
  empty: {
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.sm,
  },
});
