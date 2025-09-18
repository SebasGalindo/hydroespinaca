import React, { useMemo, useEffect } from 'react';
import { useAuth } from '@hydroespinaca/shared-ui';

export const PublicPage: React.FC = () => {
  // Memoize the config object to prevent useAuth from re-running on every render
  const authConfig = useMemo(() => ({ platform: 'web' as const }), []);
  const { isAuthenticated, isLoading } = useAuth(authConfig);

  // Auto-redirect to dashboard if already authenticated
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      console.log('PublicPage: User is authenticated, redirecting to dashboard');
      if (typeof window !== 'undefined' && (window as any).__navigate) {
        (window as any).__navigate('/dashboard');
      }
    }
  }, [isAuthenticated, isLoading]);

  // Show loading while checking auth status
  if (isLoading) {
    return <div className="loading">Verificando sesión...</div>;
  }

  return (
    <div className="public-page">
      <h1>Página Pública - HydroEspinaca</h1>
      <p>Esta página es accesible sin autenticación.</p>
      
      <div className="content">
        <h2>Bienvenido al Sistema de Control de Cultivo Hidropónico</h2>
        <p>
          HydroEspinaca es un sistema inteligente para el control y monitoreo 
          de cultivos hidropónicos utilizando tecnología IoT.
        </p>
        
        <div className="features">
          <h3>Características:</h3>
          <ul>
            <li>Monitoreo en tiempo real de sensores</li>
            <li>Control automático de actuadores</li>
            <li>Notificaciones por email</li>
            <li>Interfaz web y móvil</li>
          </ul>
        </div>
      </div>

      <div className="actions">
        {isAuthenticated ? (
          <div>
            <p>Ya tienes una sesión activa.</p>
            <a href="/dashboard" className="btn btn-primary">
              Ir al Panel de Control
            </a>
          </div>
        ) : (
          <div>
            <p>Para acceder al sistema completo, inicia sesión:</p>
            <a href="/login" className="btn btn-primary">
              Iniciar Sesión
            </a>
          </div>
        )}
      </div>
    </div>
  );
};