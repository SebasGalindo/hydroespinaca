// AuthProvider context for mobile app
import React, { createContext, useContext, ReactNode } from 'react';
import { useNativeAuth } from '@hidroespinaca/shared';
import type { UseAuthReturn } from '@hidroespinaca/shared';

const AuthContext = createContext<UseAuthReturn | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * AuthProvider component
 * Provides authentication state and methods to all child components
 */
export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const authState = useNativeAuth();

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
