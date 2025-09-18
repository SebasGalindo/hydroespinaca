import React, { useState, useEffect } from 'react';
import { ProtectedRoute } from '@hydroespinaca/shared-ui';
import { PublicPage } from '../pages/PublicPage';
import { LoginPage } from '../pages/LoginPage';
import { ProtectedPage } from '../pages/ProtectedPage';

export const Router: React.FC = () => {
  const [currentPath, setCurrentPath] = useState(window.location.pathname);

  useEffect(() => {
    const handlePopState = () => {
      setCurrentPath(window.location.pathname);
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  const navigate = (path: string) => {
    window.history.pushState({}, '', path);
    setCurrentPath(path);
  };

  // Make navigate available globally for the auth components
  useEffect(() => {
    (window as Window & { __navigate?: (path: string) => void }).__navigate = navigate;
  }, []);

  const renderRoute = () => {
    switch (currentPath) {
      case '/':
        return <PublicPage />;
      
      case '/login':
        return <LoginPage />;
      
      case '/protected':
      case '/dashboard':
        return (
          <ProtectedRoute redirectTo="/login">
            <ProtectedPage />
          </ProtectedRoute>
        );
      
      default:
        return (
          <div className="not-found">
            <h1>404 - Página no encontrada</h1>
            <p>La página que buscas no existe.</p>
            <a href="/">Volver al inicio</a>
          </div>
        );
    }
  };

  return <div className="app">{renderRoute()}</div>;
};