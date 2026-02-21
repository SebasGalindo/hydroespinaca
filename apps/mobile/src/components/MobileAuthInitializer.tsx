import { useEffect, useRef } from 'react';
import { AppState, AppStateStatus, Platform } from 'react-native';
import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import type { NavigationContainerRef } from '@react-navigation/native';
import {
  setAuthCallbacks,
  setAuthStoreRedirectCallback,
  useAuthStore,
  useNotificationStore,
} from '@hydroespinaca/shared';
import type { RootStackParamList } from '../navigation/types';

// Configuración del refresco de sesión
// IMPORTANTE: Access token expira en 2 minutos, verificamos cada 90 segundos para detectar expiración antes
const SESSION_REFRESH_INTERVAL = 90 * 1000; // 90 segundos (1.5 minutos)
const MIN_TIME_BETWEEN_CHECKS = 30 * 1000; // 30 segundos (throttle)

interface MobileAuthInitializerProps {
  navigationRef: React.RefObject<NavigationContainerRef<RootStackParamList> | null>;
}

export function MobileAuthInitializer({ navigationRef }: MobileAuthInitializerProps) {

  // Ref para rastrear la última vez que se verificó la sesión
  const lastSessionCheck = useRef<number>(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const appStateSubscription = useRef<any>(null);

  const checkSession = useAuthStore(state => state.checkSession);
  const logout = useAuthStore(state => state.logout);
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);

  // Función para verificar sesión con throttle
  const checkSessionThrottled = async () => {
    const now = Date.now();

    // Si la última verificación fue hace menos de MIN_TIME_BETWEEN_CHECKS, saltar
    if (now - lastSessionCheck.current < MIN_TIME_BETWEEN_CHECKS) {
      if (__DEV__) {
        console.info('[MobileAuthInitializer] Session check skipped (throttled)');
      }
      return;
    }

    lastSessionCheck.current = now;

    if (__DEV__) {
      console.info('[MobileAuthInitializer] Refreshing session...');
    }

    await checkSession();

    if (__DEV__) {
      console.info('[MobileAuthInitializer] Session refreshed');
    }
  };

  // Configurar callbacks globales para authFetch y authStore
  useEffect(() => {
    if (__DEV__) {
      console.info('[MobileAuthInitializer] Setting up auth callbacks');
    }

    const redirectHandler = (screen: string) => {
      if (__DEV__) {
        console.info('[MobileAuthInitializer] Redirect triggered to:', screen);
      }
      // Use setTimeout to ensure logout completes and navigation context is ready
      setTimeout(() => {
        try {
          if (navigationRef.current) {
            if (__DEV__) {
              console.info('[MobileAuthInitializer] Executing navigationRef.reset to:', screen);
            }
            // Use reset instead of replace to ensure we clear the navigation stack
            navigationRef.current.reset({
              index: 0,
              routes: [{ name: screen as keyof RootStackParamList }],
            });
          } else {
            if (__DEV__) {
              console.error('[MobileAuthInitializer] navigationRef.current is null');
            }
          }
        } catch (error) {
          if (__DEV__) {
            console.error('[MobileAuthInitializer] Navigation error:', error);
          }
        }
      }, 100);
    };

    setAuthCallbacks(
      async () => {
        if (__DEV__) {
          console.info('[MobileAuthInitializer] authFetch triggered logout (401 detected)');
        }
        await logout();
      },
      redirectHandler
    );

    // Also set redirect callback for authStore (for checkSession 401 handling)
    setAuthStoreRedirectCallback(redirectHandler);
  }, [logout, navigationRef]);

  // Refresco periódico de sesión (cada 90 segundos - antes de que expire el access token de 2 minutos)
  useEffect(() => {
    if (!isAuthenticated) {
      // Limpiar intervalo si el usuario no está autenticado
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      return;
    }

    if (__DEV__) {
      console.info('[MobileAuthInitializer] Setting up periodic session refresh (every 90 seconds)');
    }

    intervalRef.current = setInterval(() => {
      if (__DEV__) {
        console.info('[MobileAuthInitializer] Periodic session refresh triggered');
      }
      checkSessionThrottled();
    }, SESSION_REFRESH_INTERVAL);

    return () => {
      if (intervalRef.current) {
        if (__DEV__) {
          console.info('[MobileAuthInitializer] Cleaning up periodic session refresh');
        }
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [isAuthenticated]);

  // Refresco de sesión cuando la app vuelve a estar en primer plano
  useEffect(() => {
    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      // Solo verificar si la app pasa a estar activa y el usuario está autenticado
      if (nextAppState === 'active' && isAuthenticated) {
        if (__DEV__) {
          console.info('[MobileAuthInitializer] App became active, checking session');
        }
        checkSessionThrottled();
      }
    };

    // Suscribirse a cambios de estado de la app
    appStateSubscription.current = AppState.addEventListener('change', handleAppStateChange);

    return () => {
      // Limpiar suscripción
      if (appStateSubscription.current?.remove) {
        appStateSubscription.current.remove();
      }
    };
  }, [isAuthenticated]);

  // ──── Push Notification Configuration ────
  // Configure how notifications are handled when the app is in the foreground
  useEffect(() => {
    Notifications.setNotificationHandler({
      handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: true,
        shouldShowBanner: true,
        shouldShowList: true,
      }),
    });
  }, []);

  // Register for push notifications when authenticated
  const registerPush = useNotificationStore(s => s.registerPush);
  const userId = useAuthStore(s => s.user?.id);

  useEffect(() => {
    if (!isAuthenticated || !userId) return;

    let cancelled = false;

    const register = async () => {
      try {
        // Only real devices can receive push notifications
        if (!Device.isDevice) {
          if (__DEV__) console.info('[MobileAuthInitializer] Skipping push registration (not a device)');
          return;
        }

        // Check/request permission
        const { status: existingStatus } = await Notifications.getPermissionsAsync();
        let finalStatus = existingStatus;

        if (existingStatus !== 'granted') {
          const { status } = await Notifications.requestPermissionsAsync();
          finalStatus = status;
        }

        if (finalStatus !== 'granted') {
          if (__DEV__) console.info('[MobileAuthInitializer] Push permission denied');
          return;
        }

        // Get Expo push token
        const tokenData = await Notifications.getExpoPushTokenAsync();
        const token = tokenData.data;

        if (__DEV__) console.info('[MobileAuthInitializer] Push token:', token);

        if (!cancelled) {
          await registerPush({
            userId,
            platform: Platform.OS,
            token,
            deviceName: Device.deviceName ?? undefined,
          });
          if (__DEV__) console.info('[MobileAuthInitializer] Push token registered');
        }
      } catch (err) {
        if (__DEV__) console.error('[MobileAuthInitializer] Push registration error:', err);
      }
    };

    register();

    return () => { cancelled = true; };
  }, [isAuthenticated, userId]);

  // Handle notification received while app is foregrounded
  useEffect(() => {
    const receivedSub = Notifications.addNotificationReceivedListener(notification => {
      if (__DEV__) {
        console.info('[MobileAuthInitializer] Notification received:', notification.request.content.title);
      }
    });

    // Handle notification response (user tapped notification)
    const responseSub = Notifications.addNotificationResponseReceivedListener(response => {
      const data = response.notification.request.content.data;
      if (__DEV__) {
        console.info('[MobileAuthInitializer] Notification tapped, data:', data);
      }

      // Navigate based on notification data
      if (data?.screen && navigationRef.current) {
        try {
          // Deep link to specific screen if provided
          const screen = data.screen as string;
          if (screen === 'WeatherAlertDetail' && data.alertId) {
            navigationRef.current.navigate('MainTabs' as any, {
              screen: 'WeatherTab',
              params: {
                screen: 'WeatherAlertDetail',
                params: { alertId: data.alertId },
              },
            });
          } else if (screen === 'NotificationHistory') {
            navigationRef.current.navigate('MainTabs' as any, {
              screen: 'MoreTab',
              params: { screen: 'NotificationHistory' },
            });
          }
        } catch (err) {
          if (__DEV__) console.error('[MobileAuthInitializer] Notification navigation error:', err);
        }
      }
    });

    return () => {
      receivedSub.remove();
      responseSub.remove();
    };
  }, [navigationRef]);

  return null; // Este componente no renderiza nada
}
