'use client';

import React, { useState, useEffect } from 'react';
import type { CreateRoleRequestDto, UpdateRoleRequestDto, RoleResponseDto, GroupedPermissionResponseDto } from '@hydroespinaca/shared';
import { PermissionTree } from './PermissionTree';

interface RoleFormProps {
  role?: RoleResponseDto | null;
  groupedPermissions: GroupedPermissionResponseDto[];
  onSubmit: (data: CreateRoleRequestDto | UpdateRoleRequestDto) => Promise<void>;
  onCancel: () => void;
  isOpen: boolean;
}

export const RoleForm: React.FC<RoleFormProps> = ({
  role,
  groupedPermissions,
  onSubmit,
  onCancel,
  isOpen
}) => {
  const [formData, setFormData] = useState({
    code: '',
    name: '',
    permissionCodes: [] as string[]
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<{
    code?: string;
    name?: string;
  }>({});

  const isEditMode = !!role;

  useEffect(() => {
    if (role) {
      setFormData({
        code: role.code,
        name: role.name,
        permissionCodes: [...role.permissionCodes]
      });
    } else {
      setFormData({
        code: '',
        name: '',
        permissionCodes: []
      });
    }
    setError(null);
    setValidationErrors({});
  }, [role, isOpen]);

  const validateForm = (): boolean => {
    const errors: typeof validationErrors = {};

    // Code validation (only for create mode)
    if (!isEditMode) {
      if (!formData.code.trim()) {
        errors.code = 'El código del rol es requerido';
      } else {
        const formatted = formatRoleCode(formData.code);
        if (formatted.length < 5 || formatted.length > 100) {
          errors.code = 'El código del rol debe tener entre 5 y 100 caracteres';
        }
      }
    }

    // Name validation
    if (!formData.name.trim()) {
      errors.name = 'El nombre del rol es requerido';
    } else if (formData.name.length < 2 || formData.name.length > 100) {
      errors.name = 'El nombre del rol debe tener entre 2 y 100 caracteres';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const formatRoleCode = (code: string): string => {
    // Remove spaces and convert to lowercase
    let formatted = code.trim().toLowerCase().replace(/\s+/g, '_');

    // Add "role_" prefix if not present
    if (!formatted.startsWith('role_')) {
      formatted = 'role_' + formatted;
    }

    return formatted;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setValidationErrors({});

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      if (isEditMode) {
        const updateData: UpdateRoleRequestDto = {
          name: formData.name,
          permissionCodes: formData.permissionCodes
        };
        await onSubmit(updateData);
      } else {
        const createData: CreateRoleRequestDto = {
          code: formatRoleCode(formData.code), // Auto-format code with "role_" prefix
          name: formData.name,
          permissionCodes: formData.permissionCodes
        };
        await onSubmit(createData);
      }
    } catch (err: any) {
      setError(err.message || 'Error al guardar el rol');
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
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              onCancel();
            }
          }}
          aria-label="Cerrar modal"
        ></div>

        {/* Modal panel */}
        <div className="relative z-10 inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-3xl sm:w-full">
          <form onSubmit={handleSubmit}>
            {/* Header */}
            <div className="bg-blue-600 px-6 py-4">
              <h3 className="text-lg font-semibold text-white">
                {isEditMode ? 'Editar Rol' : 'Nuevo Rol'}
              </h3>
            </div>

            {/* Body */}
            <div className="px-6 py-4 space-y-4 max-h-[70vh] overflow-y-auto">
              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-600">{error}</p>
                </div>
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="code" className="block text-sm font-medium text-gray-700 mb-1">
                    Código *
                  </label>
                  <input
                    type="text"
                    id="code"
                    value={formData.code}
                    onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                    required
                    disabled={isEditMode}
                    className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed font-mono text-sm ${
                      validationErrors.code ? 'border-red-500' : 'border-gray-300'
                    }`}
                    placeholder="admin, operador, etc."
                  />
                  {validationErrors.code && (
                    <p className="mt-1 text-xs text-red-600">{validationErrors.code}</p>
                  )}
                  {isEditMode && !validationErrors.code && (
                    <p className="mt-1 text-xs text-gray-500">
                      El código no puede ser modificado
                    </p>
                  )}
                  {!isEditMode && !validationErrors.code && (
                    <p className="mt-1 text-xs text-gray-500">
                      Se agregará automáticamente el prefijo "role_"
                    </p>
                  )}
                </div>

                <div>
                  <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                    Nombre *
                  </label>
                  <input
                    type="text"
                    id="name"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    required
                    className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
                      validationErrors.name ? 'border-red-500' : 'border-gray-300'
                    }`}
                    placeholder="Administrador"
                  />
                  {validationErrors.name && (
                    <p className="mt-1 text-xs text-red-600">{validationErrors.name}</p>
                  )}
                </div>
              </div>

              <div>
                <label htmlFor="role-permissions" className="block text-sm font-medium text-gray-700 mb-2">
                  Permisos
                </label>
                <p className="text-xs text-gray-500 mb-3" id="role-permissions-description">
                  Selecciona los permisos que tendrá este rol. Puedes expandir las categorías para ver todos los permisos disponibles.
                </p>
                <div id="role-permissions" aria-describedby="role-permissions-description">
                  <PermissionTree
                    groupedPermissions={groupedPermissions}
                    selectedPermissionCodes={formData.permissionCodes}
                    onChange={(permissionCodes) => setFormData({ ...formData, permissionCodes })}
                  />
                </div>
                <div className="mt-2 text-sm text-gray-600">
                  <strong>{formData.permissionCodes.length}</strong> permiso(s) seleccionado(s)
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="bg-gray-50 px-6 py-4 flex justify-end space-x-3">
              <button
                type="button"
                onClick={onCancel}
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
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
