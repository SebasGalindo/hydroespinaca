'use client';

import { ReactNode, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@hydroespinaca/shared';

interface AdminRouteProps {
  children: ReactNode;
  fallbackPath?: string;
}

/**
 * AdminRoute - Wrapper component to protect admin-only routes
 * Redirects non-admin users to dashboard or specified fallback path
 */
export const AdminRoute: React.FC<AdminRouteProps> = ({
  children,
  fallbackPath = '/dashboard'
}) => {
  const router = useRouter();
  const { user, isAuthenticated, isLoading } = useAuthStore();

  useEffect(() => {
    // Wait for session check to complete before redirecting
    if (isLoading) {
      return;
    }

    if (!isAuthenticated || !user) {
      router.push('/login');
      return;
    }

    // Check if user has Administrador role - only after user is fully loaded
    if (user.role !== 'Administrador') {
      router.push(fallbackPath);
    }
  }, [isAuthenticated, user, router, fallbackPath, isLoading]);

  // Show loading state while checking session OR while user data is not yet available
  if (isLoading || !user) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <div className="inline-flex items-center justify-center w-16 h-16 mb-4">
            <svg className="animate-spin h-12 w-12 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          </div>
          <p className="text-gray-600">Verificando permisos...</p>
        </div>
      </div>
    );
  }

  // Don't render content if not admin (user is guaranteed to be defined here)
  if (!isAuthenticated || user.role !== 'Administrador') {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <div className="inline-flex items-center justify-center w-16 h-16 mb-4 rounded-full bg-red-100">
            <svg className="w-8 h-8 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Acceso Denegado</h2>
          <p className="text-gray-600 mb-4">
            No tienes permisos para acceder a esta página.
          </p>
          <p className="text-sm text-gray-500">
            Redirigiendo...
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};
