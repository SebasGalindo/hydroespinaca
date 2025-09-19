import React, { ReactNode } from 'react';
import { useWebAuth } from './useWebAuth';

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
  const { isAuthenticated, isLoading } = useWebAuth();

  if (isLoading) {
    return <div className="loading">Cargando...</div>;
  }

  if (!isAuthenticated) {
    if (fallback) {
      return <>{fallback}</>;
    }
    
    // For web, we'll handle redirect in the Router component
    // For now, just show redirect message
    if (typeof window !== 'undefined') {
      window.location.href = redirectTo;
    }
    return <div>Redirigiendo al login...</div>;
  }

  return <>{children}</>;
};