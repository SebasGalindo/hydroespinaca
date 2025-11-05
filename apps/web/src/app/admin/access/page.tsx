'use client';

import React, { useState, useEffect } from 'react';
import { AdminRoute } from '@/components/auth/AdminRoute';
import PageLayout from '@/components/layout/PageLayout';
import { UsersTable } from '@/components/admin/UsersTable';
import { RolesTable } from '@/components/admin/RolesTable';
import { UserForm } from '@/components/admin/UserForm';
import { RoleForm } from '@/components/admin/RoleForm';
import { adminService, useAuthStore } from '@hydroespinaca/shared';
import Swal from 'sweetalert2';
import type {
  UserResponseDto,
  UserCreateDto,
  UserUpdateDto,
  RoleResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  GroupedPermissionResponseDto
} from '@hydroespinaca/shared';

type TabType = 'users' | 'roles';

export default function AdminAccessPage() {
  const [activeTab, setActiveTab] = useState<TabType>('users');
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [roles, setRoles] = useState<RoleResponseDto[]>([]);
  const [groupedPermissions, setGroupedPermissions] = useState<GroupedPermissionResponseDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // User form state
  const [isUserFormOpen, setIsUserFormOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserResponseDto | null>(null);

  // Role form state
  const [isRoleFormOpen, setIsRoleFormOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleResponseDto | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [usersData, rolesData, permissionsData] = await Promise.all([
        adminService.getAllUsers(),
        adminService.getAllRoles(),
        adminService.getGroupedPermissions()
      ]);
      setUsers(usersData);
      setRoles(rolesData);
      setGroupedPermissions(permissionsData);
    } catch (err: any) {
      setError(err.message || 'Error al cargar los datos');
      console.error('Error loading admin data:', err);
    } finally {
      setIsLoading(false);
    }
  };

  // ==================== USER OPERATIONS ====================

  const handleCreateUser = () => {
    setEditingUser(null);
    setIsUserFormOpen(true);
  };

  const handleEditUser = (user: UserResponseDto) => {
    setEditingUser(user);
    setIsUserFormOpen(true);
  };

  const handleDeleteUser = async (user: UserResponseDto) => {
    // Additional check to prevent accidental self-deletion (defense in depth)
    const currentUser = useAuthStore.getState().user;
    if (currentUser?.id === user.id) {
      await Swal.fire({
        title: 'Acción no permitida',
        text: 'No puedes eliminar tu propia cuenta de usuario',
        icon: 'warning',
        confirmButtonColor: '#16a34a'
      });
      return;
    }

    const result = await Swal.fire({
      title: '¿Estás seguro?',
      text: `Se eliminará al usuario "${user.username}"`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar'
    });

    if (!result.isConfirmed) {
      return;
    }

    try {
      await adminService.deleteUser(user.id);
      setUsers(users.filter(u => u.id !== user.id));
      await Swal.fire({
        title: '¡Eliminado!',
        text: 'El usuario ha sido eliminado exitosamente.',
        icon: 'success',
        confirmButtonColor: '#16a34a'
      });
    } catch (err: any) {
      // Extract the error message from the ApiError
      let errorMessage = 'Error desconocido al eliminar usuario';

      // Try to extract message from various possible error formats
      if (err.message) {
        errorMessage = err.message;

        // If the message looks like a JSON string, try to parse it
        if (typeof errorMessage === 'string' && errorMessage.trim().startsWith('{')) {
          try {
            const parsedMessage = JSON.parse(errorMessage);
            errorMessage = parsedMessage.message || errorMessage;
          } catch {
            // If parsing fails, use the original message
          }
        }
      }

      // Special handling for 400 errors (likely validation errors)
      if (err.status === 400 && errorMessage.includes('Request failed')) {
        errorMessage = 'No puedes eliminar tu propia cuenta de usuario';
      }

      await Swal.fire({
        title: 'No se pudo eliminar',
        text: errorMessage,
        icon: 'error',
        confirmButtonColor: '#16a34a'
      });
    }
  };

  const handleUserSubmit = async (data: UserCreateDto | UserUpdateDto) => {
    if (editingUser) {
      // Update existing user
      const updated = await adminService.updateUser(editingUser.id, data as UserUpdateDto);
      setUsers(users.map(u => u.id === editingUser.id ? updated : u));
    } else {
      // Create new user
      const created = await adminService.createUser(data as UserCreateDto);
      setUsers([...users, created]);
    }
    setIsUserFormOpen(false);
    setEditingUser(null);
  };

  // ==================== ROLE OPERATIONS ====================

  const handleCreateRole = () => {
    setEditingRole(null);
    setIsRoleFormOpen(true);
  };

  const handleEditRole = (role: RoleResponseDto) => {
    setEditingRole(role);
    setIsRoleFormOpen(true);
  };

  const handleDeleteRole = async (role: RoleResponseDto) => {
    const result = await Swal.fire({
      title: '¿Estás seguro?',
      text: `Se eliminará el rol "${role.name}"`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar'
    });

    if (!result.isConfirmed) {
      return;
    }

    try {
      await adminService.deleteRole(role.code);
      setRoles(roles.filter(r => r.id !== role.id));
      await Swal.fire({
        title: '¡Eliminado!',
        text: 'El rol ha sido eliminado exitosamente.',
        icon: 'success',
        confirmButtonColor: '#16a34a'
      });
    } catch (err: any) {
      await Swal.fire({
        title: 'Error',
        text: `Error al eliminar rol: ${err.message}`,
        icon: 'error',
        confirmButtonColor: '#16a34a'
      });
    }
  };

  const handleRoleSubmit = async (data: CreateRoleRequestDto | UpdateRoleRequestDto) => {
    if (editingRole) {
      // Update existing role
      const updated = await adminService.updateRole(editingRole.code, data as UpdateRoleRequestDto);
      setRoles(roles.map(r => r.id === editingRole.id ? updated : r));
    } else {
      // Create new role
      const created = await adminService.createRole(data as CreateRoleRequestDto);
      setRoles([...roles, created]);
    }
    setIsRoleFormOpen(false);
    setEditingRole(null);
  };

  return (
    <AdminRoute>
      <PageLayout>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Page Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Gestión de Acceso</h1>
            <p className="mt-2 text-sm text-gray-600">
              Administra usuarios, roles y permisos del sistema
            </p>
          </div>

          {/* Tabs */}
          <div className="border-b border-gray-200 mb-6">
            <nav className="-mb-px flex space-x-8">
              <button
                onClick={() => setActiveTab('users')}
                className={`${
                  activeTab === 'users'
                    ? 'border-green-600 text-green-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
              >
                <svg className="inline w-5 h-5 mr-2 -mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                Usuarios ({users.length})
              </button>
              <button
                onClick={() => setActiveTab('roles')}
                className={`${
                  activeTab === 'roles'
                    ? 'border-blue-600 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
              >
                <svg className="inline w-5 h-5 mr-2 -mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
                Roles ({roles.length})
              </button>
            </nav>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-600">{error}</p>
              <button
                onClick={loadData}
                className="mt-2 text-sm font-medium text-red-600 hover:text-red-800 underline"
              >
                Reintentar
              </button>
            </div>
          )}

          {/* Content */}
          <div>
            {activeTab === 'users' && (
              <div>
                <div className="mb-4 flex justify-end">
                  <button
                    onClick={handleCreateUser}
                    className="inline-flex items-center px-4 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-colors"
                  >
                    <svg className="w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Nuevo Usuario
                  </button>
                </div>
                <UsersTable
                  users={users}
                  roles={roles}
                  onEdit={handleEditUser}
                  onDelete={handleDeleteUser}
                  isLoading={isLoading}
                />
              </div>
            )}

            {activeTab === 'roles' && (
              <div>
                <div className="mb-4 flex justify-end">
                  <button
                    onClick={handleCreateRole}
                    className="inline-flex items-center px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                  >
                    <svg className="w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Nuevo Rol
                  </button>
                </div>
                <RolesTable
                  roles={roles}
                  onEdit={handleEditRole}
                  onDelete={handleDeleteRole}
                  isLoading={isLoading}
                />
              </div>
            )}
          </div>
        </div>

        {/* Modals */}
        <UserForm
          user={editingUser}
          roles={roles}
          onSubmit={handleUserSubmit}
          onCancel={() => {
            setIsUserFormOpen(false);
            setEditingUser(null);
          }}
          isOpen={isUserFormOpen}
        />

        <RoleForm
          role={editingRole}
          groupedPermissions={groupedPermissions}
          onSubmit={handleRoleSubmit}
          onCancel={() => {
            setIsRoleFormOpen(false);
            setEditingRole(null);
          }}
          isOpen={isRoleFormOpen}
        />
      </PageLayout>
    </AdminRoute>
  );
}
