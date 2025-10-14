// Web-specific authentication hook
import { useMemo } from 'react';
import { useAuth } from './useAuth';
import type { AuthConfig } from '../types/auth';

/**
 * Hook for web authentication
 * Uses HttpOnly cookies for session management
 */
export const useWebAuth = (apiBaseUrl?: string) => {
  const authConfig: AuthConfig = useMemo(() => ({
    platform: 'web',
    apiBaseUrl,
  }), [apiBaseUrl]);

  return useAuth(authConfig);
};
