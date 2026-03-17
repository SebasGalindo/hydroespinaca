'use client';

import React from 'react';
import type { UserResponseDto, RoleResponseDto } from '@hydroespinaca/shared';
import { useAuthStore } from '@hydroespinaca/shared';
import { UserIcon, ShieldIcon, EditIcon, TrashIcon } from '@/components/ui/icons/Icons';

interface UsersTableProps {
  users: UserResponseDto[];
  roles: RoleResponseDto[];
  onEdit: (user: UserResponseDto) => void;
  onDelete: (user: UserResponseDto) => void;
  isLoading?: boolean;
}

export const UsersTable = React.memo(function UsersTable({
  users,
  roles,
  onEdit,
  onDelete,
  isLoading = false
}: UsersTableProps) {
  // Get current user ID to prevent self-deletion
  const currentUser = useAuthStore(state => state.user);

  const getRoleName = (roleId: string | null | undefined): string => {
    if (!roleId) return 'Sin rol';
    const role = roles.find((r: RoleResponseDto) => r.id === roleId);
    return role ? role.name : 'Rol desconocido';
  };

  const isAdminRole = (roleId: string | null | undefined): boolean => {
    if (!roleId) return false;
    const role = roles.find((r: RoleResponseDto) => r.id === roleId);
    return role ? role.name.toLowerCase().includes('admin') : false;
  };

  const isCurrentUser = (userId: string): boolean => {
    return currentUser?.id === userId;
  };

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-12 bg-gray-200 rounded"></div>
        <div className="h-12 bg-gray-200 rounded"></div>
        <div className="h-12 bg-gray-200 rounded"></div>
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg border border-gray-200">
        <UserIcon className="mx-auto h-12 w-12 text-gray-400" />
        <h3 className="mt-2 text-sm font-medium text-gray-900">No hay usuarios</h3>
        <p className="mt-1 text-sm text-gray-500">Comienza creando un nuevo usuario.</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-white rounded-lg shadow">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Usuario
            </th>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Email
            </th>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Rol
            </th>
            <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Acciones
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {users.map((user: UserResponseDto) => (
            <tr key={user.id} className="hover:bg-gray-50 transition-colors">
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="flex items-center">
                  <div className={`flex-shrink-0 h-10 w-10 rounded-full ${isAdminRole(user.roleId) ? 'bg-purple-100' : 'bg-green-100'} flex items-center justify-center`}>
                    {isAdminRole(user.roleId) ? (
                      <ShieldIcon className="w-5 h-5 text-purple-600" />
                    ) : (
                      <UserIcon className="w-5 h-5 text-green-600" />
                    )}
                  </div>
                  <div className="ml-4">
                    <div className="text-sm font-medium text-gray-900">{user.username}</div>
                    <div className="text-xs text-gray-500">ID: {user.id.substring(0, 8)}...</div>
                  </div>
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">{user.email}</div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  {getRoleName(user.roleId)}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                <button
                  onClick={() => onEdit(user)}
                  className="text-green-600 hover:text-green-900 transition-colors"
                  title="Editar usuario"
                >
                  <EditIcon className="w-5 h-5 inline" />
                </button>
                <button
                  onClick={() => onDelete(user)}
                  disabled={isCurrentUser(user.id)}
                  className={`transition-colors ${
                    isCurrentUser(user.id)
                      ? 'text-gray-400 cursor-not-allowed'
                      : 'text-red-600 hover:text-red-900'
                  }`}
                  title={isCurrentUser(user.id) ? 'No puedes eliminar tu propia cuenta' : 'Eliminar usuario'}
                >
                  <TrashIcon className="w-5 h-5 inline" />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
});
