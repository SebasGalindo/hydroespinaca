import React, { ReactNode, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { Platform } from '../types/auth';

export interface AuthRedirectProps {
  children: ReactNode;
  platform?: Platform;
  authenticatedRedirect?: string;
  unauthenticatedRedirect?: string;
  loadingComponent?: ReactNode;
}

export const AuthRedirect: React.FC<AuthRedirectProps> = ({ 
  children,
  platform,
  authenticatedRedirect = '/protected',
  unauthenticatedRedirect = '/login',
  loadingComponent
}) => {
  const { isAuthenticated, isLoading } = useAuth(platform ? { platform } : undefined);

  useEffect(() => {
    if (!isLoading) {
      if (isAuthenticated && authenticatedRedirect && typeof window !== 'undefined') {
        window.location.href = authenticatedRedirect;
      } else if (!isAuthenticated && unauthenticatedRedirect && typeof window !== 'undefined') {
        window.location.href = unauthenticatedRedirect;
      }
    }
  }, [isAuthenticated, isLoading, authenticatedRedirect, unauthenticatedRedirect]);

  if (isLoading) {
    return loadingComponent ? <>{loadingComponent}</> : <div className="loading">Cargando...</div>;
  }

  return <>{children}</>;
};