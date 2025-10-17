'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useVariableStore, useActuatorStore, useSensorStore, useReadingsStore, useAuthStore } from '@hydroespinaca/shared';

export function StoreInitializer() {
  const [isClient, setIsClient] = useState(false);
  const [sessionChecked, setSessionChecked] = useState(false);
  const router = useRouter();
  const pathname = usePathname();

  // Ref para asegurar que checkSession solo se llame UNA vez
  const sessionCheckAttempted = useRef(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const initializeVariables = useVariableStore(state => state.initializeVariables);
  const initializeActuadores = useActuatorStore(state => state.initializeActuadores);
  const generateMockData = useSensorStore(state => state.generateMockData);
  const initializeSystemComponents = useSensorStore(state => state.initializeSystemComponents);
  const initializeReadings = useReadingsStore(state => state.initializeReadings);
  const checkSession = useAuthStore(state => state.checkSession);
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);
  const isLoading = useAuthStore(state => state.isLoading);

  // Check session on mount - SOLO UNA VEZ
  useEffect(() => {
    if (isClient && !sessionChecked && !sessionCheckAttempted.current) {
      sessionCheckAttempted.current = true;

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

  // Redirect to login if session check completed and user is not authenticated
  useEffect(() => {
    if (sessionChecked && !isLoading && !isAuthenticated) {
      const publicPaths = ['/', '/login', '/forgot-password', '/reset-password'];
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


  // Initialize other stores only after successful authentication
  useEffect(() => {
    if (isClient && sessionChecked && isAuthenticated) {
      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] User authenticated, initializing app stores...');
      }

      initializeVariables();
      initializeActuadores();
      generateMockData();
      initializeSystemComponents();
      initializeReadings();

      if (process.env.NODE_ENV === 'development') {
        console.info('[StoreInitializer] App stores initialized successfully');
      }
    }
  }, [isClient, sessionChecked, isAuthenticated, initializeVariables, initializeActuadores, generateMockData, initializeSystemComponents, initializeReadings]);

  return null;
}