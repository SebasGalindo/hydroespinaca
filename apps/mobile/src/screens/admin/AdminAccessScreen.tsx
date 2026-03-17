/**
 * AdminAccessScreen — Gestión de usuarios y roles.
 * Dos tabs: Usuarios / Roles. FAB crea según tab activo.
 */
import React, { useState, useEffect, useCallback } from 'react';
import { View, StyleSheet, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { FloatingActionButton } from '../../components/atoms/FloatingActionButton';
import { Alert } from '../../components/molecules/Alert';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import { AdminGuard } from '../../components/organisms/AdminGuard';
import { SkeletonLoader } from '../../components/organisms/SkeletonLoader';
import { UserList } from '../../components/admin/UserList';
import { UserForm } from '../../components/admin/UserForm';
import { RoleList } from '../../components/admin/RoleList';
import { RoleForm } from '../../components/admin/RoleForm';
import { useAuth } from '../../context/AuthProvider';
import {
  adminService,
  semanticColors,
  spacing,
  colors,
  borderRadius,
  typography,
} from '@hydroespinaca/shared';
import type {
  UserResponseDto,
  RoleResponseDto,
  GroupedPermissionResponseDto,
} from '@hydroespinaca/shared';
import { showToast } from '../../utils/toast';
import { hapticError, hapticHeavy } from '../../utils/haptics';

type Tab = 'users' | 'roles';

export function AdminAccessScreen(): React.ReactElement {
  const { session } = useAuth();
  const [activeTab, setActiveTab] = useState<Tab>('users');

  // Data
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [roles, setRoles] = useState<RoleResponseDto[]>([]);
  const [groupedPermissions, setGroupedPermissions] = useState<GroupedPermissionResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [userFormOpen, setUserFormOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserResponseDto | null>(null);
  const [roleFormOpen, setRoleFormOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleResponseDto | null>(null);

  // Delete confirmation
  const [deleteTarget, setDeleteTarget] = useState<{
    type: 'user' | 'role';
    id: string;
    name: string;
  } | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [usersRes, rolesRes, permsRes] = await Promise.all([
        adminService.getAllUsers(),
        adminService.getAllRoles(),
        adminService.getGroupedPermissions(),
      ]);
      setUsers(usersRes);
      setRoles(rolesRes);
      setGroupedPermissions(permsRes);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar datos');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // ── User handlers ──
  const handleEditUser = useCallback((user: UserResponseDto) => {
    setEditingUser(user);
    setUserFormOpen(true);
  }, []);

  const handleDeleteUser = useCallback((user: UserResponseDto) => {
    setDeleteTarget({ type: 'user', id: user.id, name: user.username });
  }, []);

  // ── Role handlers ──
  const handleEditRole = useCallback((role: RoleResponseDto) => {
    setEditingRole(role);
    setRoleFormOpen(true);
  }, []);

  const handleDeleteRole = useCallback((role: RoleResponseDto) => {
    setDeleteTarget({ type: 'role', id: role.code, name: role.name });
  }, []);

  // ── Confirm delete ──
  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      if (deleteTarget.type === 'user') {
        await adminService.deleteUser(deleteTarget.id);
      } else {
        await adminService.deleteRole(deleteTarget.id);
      }
      setDeleteTarget(null);
      showToast('success', `"${deleteTarget.name}" eliminado`);
      hapticHeavy();
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al eliminar');
      showToast('error', err instanceof Error ? err.message : 'Error al eliminar');
      hapticError();
      setDeleteTarget(null);
    }
  };

  // ── FAB ──
  const handleFabPress = () => {
    if (activeTab === 'users') {
      setEditingUser(null);
      setUserFormOpen(true);
    } else {
      setEditingRole(null);
      setRoleFormOpen(true);
    }
  };

  // Find current user id to prevent self-deletion
  const currentUserId = users.find(
    (u) => u.email === session?.email || u.username === session?.username,
  )?.id;

  return (
    <AdminGuard>
      <SafeAreaView style={styles.container} edges={['top']}>
        {/* Header */}
        <View style={styles.header}>
          <Icon name="shield" size={24} color={semanticColors.primary} />
          <Text variant="h3" color={semanticColors.textPrimary} style={styles.headerTitle}>
            Gestión de Acceso
          </Text>
        </View>

        {/* Tabs */}
        <View style={styles.tabBar} accessibilityRole="tablist">
          <TouchableOpacity
            style={[styles.tab, activeTab === 'users' && styles.tabActive]}
            onPress={() => setActiveTab('users')}
            activeOpacity={0.7}
            accessibilityRole="tab"
            accessibilityState={{ selected: activeTab === 'users' }}
            accessibilityLabel={`Usuarios, ${users.length} total`}
          >
            <Icon
              name="user"
              size={18}
              color={activeTab === 'users' ? colors.hidro[700] : semanticColors.textTertiary}
            />
            <Text
              variant="body"
              color={activeTab === 'users' ? colors.hidro[700] : semanticColors.textTertiary}
              style={activeTab === 'users' ? styles.tabTextActive : undefined}
            >
              Usuarios ({users.length})
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.tab, activeTab === 'roles' && styles.tabActive]}
            onPress={() => setActiveTab('roles')}
            activeOpacity={0.7}
            accessibilityRole="tab"
            accessibilityState={{ selected: activeTab === 'roles' }}
            accessibilityLabel={`Roles, ${roles.length} total`}
          >
            <Icon
              name="key"
              size={18}
              color={activeTab === 'roles' ? colors.hidro[700] : semanticColors.textTertiary}
            />
            <Text
              variant="body"
              color={activeTab === 'roles' ? colors.hidro[700] : semanticColors.textTertiary}
              style={activeTab === 'roles' ? styles.tabTextActive : undefined}
            >
              Roles ({roles.length})
            </Text>
          </TouchableOpacity>
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
            <SkeletonLoader variant="list-item" count={4} />
          </View>
        ) : activeTab === 'users' ? (
          <UserList
            users={users}
            roles={roles}
            currentUserId={currentUserId}
            onEdit={handleEditUser}
            onDelete={handleDeleteUser}
          />
        ) : (
          <RoleList
            roles={roles}
            onEdit={handleEditRole}
            onDelete={handleDeleteRole}
          />
        )}

        {/* FAB */}
        <FloatingActionButton
          icon="add"
          onPress={handleFabPress}
          accessibilityLabel={activeTab === 'users' ? 'Crear usuario' : 'Crear rol'}
          style={styles.fab}
        />

        {/* Forms */}
        <UserForm
          isOpen={userFormOpen}
          onClose={() => setUserFormOpen(false)}
          user={editingUser}
          roles={roles}
          onSuccess={loadData}
        />

        <RoleForm
          isOpen={roleFormOpen}
          onClose={() => setRoleFormOpen(false)}
          role={editingRole}
          groupedPermissions={groupedPermissions}
          onSuccess={loadData}
        />

        {/* Delete confirmation */}
        <ConfirmationSheet
          isOpen={!!deleteTarget}
          title={`Eliminar ${deleteTarget?.type === 'user' ? 'usuario' : 'rol'}`}
          message={`¿Seguro que deseas eliminar "${deleteTarget?.name}"? Esta acción no se puede deshacer.`}
          confirmLabel="Eliminar"
          cancelLabel="Cancelar"
          destructive
          onConfirm={confirmDelete}
          onCancel={() => setDeleteTarget(null)}
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
  // Tabs
  tabBar: {
    flexDirection: 'row',
    marginHorizontal: spacing.lg,
    backgroundColor: colors.gray[100],
    borderRadius: borderRadius.lg,
    padding: 4,
    gap: 4,
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
    gap: spacing.xs,
  },
  tabActive: {
    backgroundColor: colors.white,
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  tabTextActive: {
    fontWeight: typography.fontWeight.semibold,
  },
  // Content
  errorContainer: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.sm,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.md,
  },
  // FAB
  fab: {
    position: 'absolute',
    right: spacing.lg,
    bottom: spacing.xl,
  },
});
