'use client';

import React, { useState } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import { useAuthStore } from '@hydroespinaca/shared';
import { useRouter } from 'next/navigation';
import { UserIcon, LockIcon } from '@/components/ui/icons/Icons';

export default function PerfilPage() {
  const { user, logout } = useAuthStore();
  const router = useRouter();
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [showDeleteAccount, setShowDeleteAccount] = useState(false);
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  const handleChangePassword = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: Implementar cambio de contraseña
    console.log('Cambiar contraseña:', passwordData);
    alert('Funcionalidad de cambio de contraseña en desarrollo');
    setShowChangePassword(false);
    setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
  };

  const handleDeleteAccount = async () => {
    if (window.confirm('¿Estás seguro de que deseas eliminar tu cuenta? Esta acción no se puede deshacer.')) {
      // TODO: Implementar eliminación de cuenta
      console.log('Eliminar cuenta');
      alert('Funcionalidad de eliminación de cuenta en desarrollo');
      await logout();
      router.push('/');
    }
  };

  return (
    <PageLayout
      title="Perfil de Usuario"
      subtitle="Administra tu información personal y configuración de cuenta"
      maxWidth="lg"
    >
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Información del Usuario */}
        <div className="md:col-span-1">
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex flex-col items-center">
              <div className="w-24 h-24 bg-green-600 rounded-full flex items-center justify-center mb-4">
                <UserIcon size={48} color="white" />
              </div>
              <h2 className="text-xl font-bold text-gray-900 mb-1">
                {user?.name || 'Usuario'}
              </h2>
              <p className="text-sm text-gray-500 mb-4">
                {user?.role || 'Administrador'}
              </p>
              <div className="w-full pt-4 border-t border-gray-200">
                <div className="space-y-3">
                  <div>
                    <p className="text-xs text-gray-500 uppercase tracking-wide">Email</p>
                    <p className="text-sm font-medium text-gray-900 break-all">
                      {user?.email || 'correo@ejemplo.com'}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500 uppercase tracking-wide">ID de Usuario</p>
                    <p className="text-sm font-medium text-gray-900">
                      {user?.id || 'N/A'}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Acciones y Configuración */}
        <div className="md:col-span-2 space-y-6">
          {/* Cambiar Contraseña */}
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex items-center mb-4">
              <LockIcon size={24} className="text-green-600 mr-3" />
              <h3 className="text-lg font-semibold text-gray-900">Cambiar Contraseña</h3>
            </div>

            {!showChangePassword ? (
              <div>
                <p className="text-gray-600 mb-4">
                  Actualiza tu contraseña regularmente para mantener tu cuenta segura.
                </p>
                <button
                  onClick={() => setShowChangePassword(true)}
                  className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500"
                >
                  Cambiar Contraseña
                </button>
              </div>
            ) : (
              <form onSubmit={handleChangePassword} className="space-y-4">
                <div>
                  <label htmlFor="currentPassword" className="block text-sm font-medium text-gray-700 mb-1">
                    Contraseña Actual
                  </label>
                  <input
                    type="password"
                    id="currentPassword"
                    value={passwordData.currentPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                    required
                  />
                </div>
                <div>
                  <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-1">
                    Nueva Contraseña
                  </label>
                  <input
                    type="password"
                    id="newPassword"
                    value={passwordData.newPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                    required
                  />
                </div>
                <div>
                  <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                    Confirmar Nueva Contraseña
                  </label>
                  <input
                    type="password"
                    id="confirmPassword"
                    value={passwordData.confirmPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                    required
                  />
                </div>
                <div className="flex space-x-3">
                  <button
                    type="submit"
                    className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500"
                  >
                    Guardar Cambios
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setShowChangePassword(false);
                      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
                    }}
                    className="bg-gray-200 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-300 transition-colors"
                  >
                    Cancelar
                  </button>
                </div>
              </form>
            )}
          </div>

          {/* Zona de Peligro */}
          <div className="bg-white rounded-lg shadow-md p-6 border-2 border-red-200">
            <h3 className="text-lg font-semibold text-red-600 mb-4">Zona de Peligro</h3>

            <div className="mb-6">
              <h4 className="text-md font-medium text-gray-900 mb-2">Eliminar Cuenta</h4>
              <p className="text-gray-600 mb-4">
                Una vez que elimines tu cuenta, no hay vuelta atrás. Por favor, está seguro.
              </p>

              {!showDeleteAccount ? (
                <button
                  onClick={() => setShowDeleteAccount(true)}
                  className="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 transition-colors focus:outline-none focus:ring-2 focus:ring-red-500"
                >
                  Eliminar Cuenta
                </button>
              ) : (
                <div className="bg-red-50 p-4 rounded-md">
                  <p className="text-red-800 font-medium mb-4">
                    ⚠️ ¿Estás absolutamente seguro? Esta acción no se puede deshacer.
                  </p>
                  <div className="flex space-x-3">
                    <button
                      onClick={handleDeleteAccount}
                      className="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 transition-colors focus:outline-none focus:ring-2 focus:ring-red-500"
                    >
                      Sí, Eliminar Mi Cuenta
                    </button>
                    <button
                      onClick={() => setShowDeleteAccount(false)}
                      className="bg-gray-200 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-300 transition-colors"
                    >
                      Cancelar
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </PageLayout>
  );
}
