import React, { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';

export interface ProtectedRouteProps {
  children: ReactNode;
  fallback?: ReactNode;
  redirectTo?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  fallback,
  redirectTo = '/login'
}) => {
  const { isAuthenticated, isLoading } = useAuth({ platform: 'web' });

  if (isLoading) {
    return <div className="loading">Cargando...</div>;
  }

  if (!isAuthenticated) {
    if (fallback) {
      return <>{fallback}</>;
    }
    
    // Redirect to login - this should be handled by the router
    if (typeof window !== 'undefined') {
      window.location.href = redirectTo;
    }
    
    return <div>Redirigiendo al login...</div>;
  }

  return <>{children}</>;
};