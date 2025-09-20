import { useMemo } from 'react';
import { useAuth } from './useAuth';
import type { AuthConfig } from '@hydroespinaca/shared-types';

// Web-specific auth hook
export const useWebAuth = (apiBaseUrl?: string) => {
  const authConfig: AuthConfig = useMemo(() => ({
    platform: 'web',
    apiBaseUrl,
  }), [apiBaseUrl]);

  return useAuth(authConfig);
};