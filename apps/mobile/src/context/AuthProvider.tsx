// AuthProvider context for mobile app
// Bridges directly to useAuthStore (Zustand) so that login (useLoginForm → Zustand)
// and logout (screens → useAuth → Zustand) share THE SAME auth state.
import React, { createContext, useContext, ReactNode, useEffect } from 'react';
import { useAuthStore } from '@hydroespinaca/shared';
import type { UseAuthReturn } from '@hydroespinaca/shared';

const AuthContext = createContext<UseAuthReturn | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * AuthProvider component
 * Bridges Zustand auth store into React context so existing screens
 * that consume useAuth() keep working unchanged.
 */
export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const session = useAuthStore(s => s.session);
  const isLoading = useAuthStore(s => s.isLoading);
  const error = useAuthStore(s => s.error);
  const isAuthenticated = useAuthStore(s => s.isAuthenticated);
  const login = useAuthStore(s => s.login);
  const logout = useAuthStore(s => s.logout);
  const clearError = useAuthStore(s => s.clearError);
  const checkSession = useAuthStore(s => s.checkSession);

  // On mount, verify if there's an existing session (e.g. stored tokens)
  useEffect(() => {
    checkSession();
  }, []);

  const value: UseAuthReturn = {
    session,
    isLoading,
    error,
    isAuthenticated,
    login,
    logout,
    clearError,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

/**
 * useAuth hook
 * Access authentication state and methods from any component
 */
export const useAuth = (): UseAuthReturn => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
