import React, { useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { LoginForm } from '@hydroespinaca/shared-ui';
import { useWebAuth } from '@hydroespinaca/shared-hooks';
import { createWebNavigationService, WEB_ROUTES } from '@hydroespinaca/shared-utils';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLoading, error, clearError, isAuthenticated } = useWebAuth();
  const navigationService = createWebNavigationService(navigate);

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      navigationService.navigate(WEB_ROUTES.DASHBOARD);
    }
  }, [isAuthenticated, isLoading, navigationService]);

  const handleLogin = useCallback(async (email: string, password: string) => {
    try {
      await login(email, password);
      // Redirect will happen via useEffect when isAuthenticated becomes true
    } catch (err) {
      // Error is already handled by the hook
      console.error('Login failed:', err);
    }
  }, [login]);

  // Don't render anything while checking authentication or redirecting
  if (isAuthenticated && !isLoading) {
    return <div>Redirigiendo...</div>;
  }

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-header">
          <h1>HydroEspinaca</h1>
          <p>Sistema de Control Hidropónico</p>
        </div>

        <LoginForm
          onSubmit={handleLogin}
          isLoading={isLoading}
          error={error}
          onClearError={clearError}
        />
      </div>
    </div>
  );
};