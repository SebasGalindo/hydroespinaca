'use client';

import React, { useState, useEffect } from 'react';
import type { UserCreateDto, UserUpdateDto, UserResponseDto, RoleResponseDto } from '@hydroespinaca/shared';

interface UserFormProps {
  user?: UserResponseDto | null;
  roles: RoleResponseDto[];
  onSubmit: (data: UserCreateDto | UserUpdateDto) => Promise<void>;
  onCancel: () => void;
  isOpen: boolean;
}

export const UserForm: React.FC<UserFormProps> = ({
  user,
  roles,
  onSubmit,
  onCancel,
  isOpen
}) => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    roleId: ''
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditMode = !!user;

  useEffect(() => {
    if (user) {
      setFormData({
        username: user.username,
        email: user.email,
        password: '', // Don't populate password on edit
        roleId: user.roleId || ''
      });
    } else {
      setFormData({
        username: '',
        email: '',
        password: '',
        roleId: ''
      });
    }
    setError(null);
  }, [user, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      if (isEditMode) {
        const updateData: UserUpdateDto = {};
        if (formData.username !== user.username) updateData.username = formData.username;
        if (formData.email !== user.email) updateData.email = formData.email;
        if (formData.password) updateData.password = formData.password;
        if (formData.roleId !== user.roleId) updateData.roleId = formData.roleId || null;

        await onSubmit(updateData);
      } else {
        const createData: UserCreateDto = {
          username: formData.username,
          email: formData.email,
          password: formData.password,
          roleId: formData.roleId || null
        };
        await onSubmit(createData);
      }
    } catch (err: any) {
      setError(err.message || 'Error al guardar el usuario');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:block sm:p-0">
        {/* Background overlay */}
        <div
          className="fixed inset-0 transition-opacity bg-gray-500 bg-opacity-75"
          onClick={onCancel}
        ></div>

        {/* Modal panel */}
        <div className="relative z-10 inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <form onSubmit={handleSubmit}>
            {/* Header */}
            <div className="bg-green-600 px-6 py-4">
              <h3 className="text-lg font-semibold text-white">
                {isEditMode ? 'Editar Usuario' : 'Nuevo Usuario'}
              </h3>
            </div>

            {/* Body */}
            <div className="px-6 py-4 space-y-4">
              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-600">{error}</p>
                </div>
              )}

              <div>
                <label htmlFor="username" className="block text-sm font-medium text-gray-700 mb-1">
                  Nombre de usuario *
                </label>
                <input
                  type="text"
                  id="username"
                  value={formData.username}
                  onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                  required
                  autoComplete="name"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  placeholder="Juan Pérez"
                />
              </div>

              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                  Email *
                </label>
                <input
                  type="email"
                  id="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  required
                  autoComplete="email"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  placeholder="usuario@ejemplo.com"
                />
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                  Contraseña {!isEditMode && '*'}
                </label>
                <input
                  type="password"
                  id="password"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required={!isEditMode}
                  autoComplete={isEditMode ? "new-password" : "new-password"}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  placeholder={isEditMode ? 'Dejar en blanco para no cambiar' : '********'}
                />
                {isEditMode && (
                  <p className="mt-1 text-xs text-gray-500">
                    Dejar en blanco si no desea cambiar la contraseña
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="roleId" className="block text-sm font-medium text-gray-700 mb-1">
                  Rol
                </label>
                <select
                  id="roleId"
                  value={formData.roleId}
                  onChange={(e) => setFormData({ ...formData, roleId: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                >
                  <option value="">Sin rol asignado</option>
                  {roles.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {/* Footer */}
            <div className="bg-gray-50 px-6 py-4 flex justify-end space-x-3">
              <button
                type="button"
                onClick={onCancel}
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-lg hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isSubmitting ? 'Guardando...' : isEditMode ? 'Actualizar' : 'Crear'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
