import React, { useEffect, useCallback, useMemo } from 'react';
import { LoginForm, useAuth } from '@hydroespinaca/shared-ui';

export const LoginPage: React.FC = () => {
  // Memoize the config object to prevent useAuth from re-running on every render
  const authConfig = useMemo(() => ({ platform: 'web' as const }), []);
  const { login, isLoading, error, clearError, isAuthenticated } = useAuth(authConfig);

  // Redirect if already authenticated using the navigate function
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      // Use SPA navigation
      if (typeof window !== 'undefined' && (window as any).__navigate) {
        (window as any).__navigate('/dashboard');
      }
    }
  }, [isAuthenticated, isLoading]);

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
          style="web"
        />
      </div>
    </div>
  );
};