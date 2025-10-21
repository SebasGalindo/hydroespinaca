'use client';

import React from 'react';
import PageLayout from '@/components/layout/PageLayout';
import { useAuthStore } from '@hydroespinaca/shared';
import { ShieldIcon, SproutIcon } from '@/components/ui/icons/Icons';

export default function PerfilPage() {
  const { user } = useAuthStore();

  return (
    <PageLayout
      title="Perfil de Usuario"
      subtitle="Información de tu cuenta"
      maxWidth="lg"
    >
      <div className="bg-white rounded-lg shadow-md p-8">
        <div className="flex flex-col items-center">
          <div className="w-24 h-24 bg-green-600 rounded-full flex items-center justify-center mb-4">
            {user?.role === 'Administrador' ? (
              <ShieldIcon size={48} color="white" />
            ) : (
              <SproutIcon size={48} color="white" />
            )}
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-1">
            {user?.name || 'Usuario'}
          </h2>
          <p className="text-sm text-gray-500 mb-6">
            {user?.role || 'Usuario'}
          </p>
          <div className="w-full pt-6 border-t border-gray-200">
            <div className="space-y-4">
              <div>
                <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold mb-1">
                  Email
                </p>
                <p className="text-base text-gray-900 break-all">
                  {user?.email || 'correo@ejemplo.com'}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </PageLayout>
  );
}
