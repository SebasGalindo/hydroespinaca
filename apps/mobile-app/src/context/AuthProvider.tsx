import React, { createContext, useContext, ReactNode } from 'react';
import { useNativeAuth } from '@hydroespinaca/shared-hooks';
import type { UseAuthReturn } from '@hydroespinaca/shared-types';

// Create the Auth Context
const AuthContext = createContext<UseAuthReturn | null>(null);

// Provider Props
interface AuthProviderProps {
  children: ReactNode;
}

// Auth Provider Component
export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const authState = useNativeAuth();

  return (
    <AuthContext.Provider value={authState}>
      {children}
    </AuthContext.Provider>
  );
};

// Custom hook to use the Auth Context
export const useAuth = (): UseAuthReturn => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};