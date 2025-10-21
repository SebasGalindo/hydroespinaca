// Mobile/Native-specific authentication hook
import { useMemo } from 'react';
import { useAuth } from './useAuth';
import type { AuthConfig } from '../types/auth';

/**
 * Hook for mobile/native authentication
 * Uses SecureStore/AsyncStorage for session management
 */
export const useNativeAuth = (apiBaseUrl?: string) => {
  const authConfig: AuthConfig = useMemo(() => ({
    platform: 'mobile',
    apiBaseUrl,
  }), [apiBaseUrl]);

  return useAuth(authConfig);
};
