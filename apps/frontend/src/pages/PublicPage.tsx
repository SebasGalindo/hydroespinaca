import React, { useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useWebAuth } from '@hydroespinaca/shared-hooks';
import { createWebNavigationService, WEB_ROUTES } from '@hydroespinaca/shared-utils';

export const PublicPage: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated, isLoading } = useWebAuth();
  const navigationService = createWebNavigationService(navigate);

  // Auto-redirect to dashboard if already authenticated
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      console.log('PublicPage: User is authenticated, redirecting to dashboard');
      navigationService.navigate(WEB_ROUTES.DASHBOARD);
    }
  }, [isAuthenticated, isLoading, navigationService]);

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
            <Link to={WEB_ROUTES.DASHBOARD} className="btn btn-primary">
              Ir al Panel de Control
            </Link>
          </div>
        ) : (
          <div>
            <p>Para acceder al sistema completo, inicia sesión:</p>
            <Link to={WEB_ROUTES.LOGIN} className="btn btn-primary">
              Iniciar Sesión
            </Link>
          </div>
        )}
      </div>
    </div>
  );
};