import { useMemo } from 'react';
import { useAuth } from './useAuth';
import type { AuthConfig } from '@hydroespinaca/shared-types';

// Native-specific auth hook
// TODO: En una implementación real de React Native, este hook debería:
// 1. Usar expo-secure-store o @react-native-async-storage/async-storage
// 2. Sobrescribir los métodos de storage del hook base
// 3. Manejar permisos específicos de la plataforma
export const useNativeAuth = (apiBaseUrl?: string) => {
  const authConfig: AuthConfig = useMemo(() => ({
    platform: 'mobile',
    apiBaseUrl,
    // En el futuro, aquí podríamos agregar configuración específica de RN
  }), [apiBaseUrl]);

  return useAuth(authConfig);
};