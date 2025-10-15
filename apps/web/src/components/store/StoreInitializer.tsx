'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useVariableStore, useActuatorStore, useSensorStore, useReadingsStore, useAuthStore } from '@hydroespinaca/shared';
import { logout } from '@/lib/session';

export function StoreInitializer() {
  const [isClient, setIsClient] = useState(false);
  const [sessionChecked, setSessionChecked] = useState(false);
  const router = useRouter();
  const pathname = usePathname();

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

  // Check session on mount
  useEffect(() => {
    if (isClient && !sessionChecked) {
      checkSession().finally(() => {
        setSessionChecked(true);
      });
    }
  }, [isClient, sessionChecked, checkSession]);

  // Redirect to login if session check completed and user is not authenticated
  useEffect(() => {
    if (sessionChecked && !isLoading && !isAuthenticated) {
      const publicPaths = ['/', '/login', '/forgot-password', '/reset-password'];
      if (pathname && !publicPaths.includes(pathname)) {
        console.warn('Sesión inválida detectada. Redirigiendo al login...');
        logout(); // limpia cookies o tokens viejos
        router.push('/login');
      }
    }
  }, [sessionChecked, isLoading, isAuthenticated, pathname, router]);


  // Initialize other stores only after successful authentication
  useEffect(() => {
    if (isClient && sessionChecked && isAuthenticated) {
      initializeVariables();
      initializeActuadores();
      generateMockData();
      initializeSystemComponents();
      initializeReadings();
    }
  }, [isClient, sessionChecked, isAuthenticated, initializeVariables, initializeActuadores, generateMockData, initializeSystemComponents, initializeReadings]);

  return null;
}