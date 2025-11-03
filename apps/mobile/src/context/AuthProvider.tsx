// AuthProvider context for mobile app
import React, { createContext, useContext, ReactNode, useMemo } from 'react';
import { useAuth as useBaseAuth } from '@hydroespinaca/shared';
import type { UseAuthReturn, AuthConfig } from '@hydroespinaca/shared';

const AuthContext = createContext<UseAuthReturn | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * AuthProvider component
 * Provides authentication state and methods to all child components
 */
export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  // Configure for mobile platform
  const authConfig: AuthConfig = useMemo(() => ({
    platform: 'mobile',
  }), []);

  const authState = useBaseAuth(authConfig);

  return (
    <AuthContext.Provider value={authState}>
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
