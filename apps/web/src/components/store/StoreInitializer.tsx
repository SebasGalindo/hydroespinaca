'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useAuthStore, setAuthCallbacks, setAuthStoreRedirectCallback } from '@hydroespinaca/shared';

// Configuración del refresco de sesión
// IMPORTANTE: Access token expira en 2 minutos, verificamos cada 90 segundos para detectar expiración antes
const SESSION_REFRESH_INTERVAL = 90 * 1000; // 90 segundos (1.5 minutos)
const MIN_TIME_BETWEEN_CHECKS = 30 * 1000; // 30 segundos (throttle)

export function StoreInitializer() {
  const [isClient, setIsClient] = useState(false);
  const [sessionChecked, setSessionChecked] = useState(false);
  const router = useRouter();
  const pathname = usePathname();

  // Ref para asegurar que checkSession solo se llame UNA vez al inicio
  const sessionCheckAttempted = useRef(false);
  // Ref para rastrear la última vez que se verificó la sesión
  const lastSessionCheck = useRef<number>(0);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const checkSession = useAuthStore(state => state.checkSession);
  const logout = useAuthStore(state => state.logout);
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);
  const isLoading = useAuthStore(state => state.isLoading);

  // Configurar callbacks globales para authFetch y authStore
  useEffect(() => {
    const redirectHandler = (path: string) => {
      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] Redirect triggered to:', path);
      }
      router.push(path);
    };

    setAuthCallbacks(
      async () => {
        if (process.env.NODE_ENV === 'development') {
          console.info('[StoreInitializer] authFetch triggered logout (401 detected)');
        }
        await logout();
      },
      redirectHandler
    );

    // Also set redirect callback for authStore (for checkSession 401 handling)
    setAuthStoreRedirectCallback(redirectHandler);
  }, [logout, router]);

  // Función para verificar sesión con throttle
  const checkSessionThrottled = async () => {
    const now = Date.now();

    // Si la última verificación fue hace menos de MIN_TIME_BETWEEN_CHECKS, saltar
    if (now - lastSessionCheck.current < MIN_TIME_BETWEEN_CHECKS) {
      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] Session check skipped (throttled)');
      }
      return;
    }

    lastSessionCheck.current = now;

    if (process.env.NODE_ENV === 'development') {
      console.info('[StoreInitializer] Refreshing session...');
    }

    await checkSession();

    if (process.env.NODE_ENV === 'development') {
      console.info('[StoreInitializer] Session refreshed');
    }
  };

  // Check session on mount - SOLO UNA VEZ
  useEffect(() => {
    if (isClient && !sessionChecked && !sessionCheckAttempted.current) {
      sessionCheckAttempted.current = true;
      lastSessionCheck.current = Date.now();

      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] Checking session...');
      }

      checkSession().finally(() => {
        setSessionChecked(true);
        if (process.env.NODE_ENV === 'development') {
          console.info('[StoreInitializer] Session check completed');
        }
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isClient, sessionChecked]);

  // Refresco periódico de sesión (cada 90 segundos - antes de que expire el access token de 2 minutos)
  useEffect(() => {
    if (!isClient || !sessionChecked || !isAuthenticated) {
      return;
    }

    if (process.env.NODE_ENV === 'development') {
      console.info('[StoreInitializer] Setting up periodic session refresh (every 90 seconds)');
    }

    const intervalId = setInterval(() => {
      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] Periodic session refresh triggered');
      }
      checkSessionThrottled();
    }, SESSION_REFRESH_INTERVAL);

    return () => {
      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] Cleaning up periodic session refresh');
      }
      clearInterval(intervalId);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isClient, sessionChecked, isAuthenticated]);

  // Refresco de sesión cuando la pestaña vuelve a estar visible
  useEffect(() => {
    if (!isClient || !sessionChecked) {
      return;
    }

    const handleVisibilityChange = () => {
      // Solo verificar si la pestaña está visible y el usuario está autenticado
      if (!document.hidden && isAuthenticated) {
        if (process.env.NODE_ENV === 'development') {
          console.info('[StoreInitializer] Tab became visible, checking session');
        }
        checkSessionThrottled();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isClient, sessionChecked, isAuthenticated]);

  // Redirect to login if session check completed and user is not authenticated
  useEffect(() => {
    if (sessionChecked && !isLoading && !isAuthenticated) {
      const publicPaths = ['/', '/login'];
      if (pathname && !publicPaths.includes(pathname)) {
        if (process.env.NODE_ENV === 'development') {
          console.info('[StoreInitializer] No session found, redirecting to login from:', pathname);
        }
        // No llamar a logout() aquí - el authStore ya limpió la sesión en checkSession()
        // Solo redirigir al login
        router.push('/login');
      }
    }
  }, [sessionChecked, isLoading, isAuthenticated, pathname, router]);

  return null;
}