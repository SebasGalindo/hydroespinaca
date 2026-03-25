'use client';

import { useCallback } from 'react';
import { useNotificationStore } from '@hydroespinaca/shared';
import type { ChannelPreference } from '@hydroespinaca/shared';

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}

interface UseWebPushRegistrationOptions {
  userId: string | undefined;
  setLocalChannels: React.Dispatch<React.SetStateAction<ChannelPreference[]>>;
  markDirty: () => void;
}

export function useWebPushRegistration({
  userId,
  setLocalChannels,
  markDirty,
}: UseWebPushRegistrationOptions) {
  const { fetchDevices } = useNotificationStore();

  const handleWebPushRegister = useCallback(async () => {
    if (!userId) return;
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      alert('Tu navegador no soporta notificaciones push o está en modo incógnito/privado.');
      return;
    }

    try {
      const vapidKey = process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY?.trim();
      if (!vapidKey) {
        console.error('[WebPush] NEXT_PUBLIC_VAPID_PUBLIC_KEY is empty or undefined');
        alert('Error de configuración: No se encontró la clave pública VAPID.');
        return;
      }

      console.log(`[WebPush] Key detected. Length: ${vapidKey.length} chars. Content: ${vapidKey.substring(0, 10)}...`);

      const reg = await navigator.serviceWorker.ready;

      const existingSub = await reg.pushManager.getSubscription();
      if (existingSub) {
        console.log('[WebPush] Existing subscription found, unsubscribing first to refresh...');
        await existingSub.unsubscribe();
      }

      const subscription = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidKey) as BufferSource,
      });

      console.log('[WebPush] Subscription successful:', subscription.endpoint);

      const p256dh = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('p256dh')!)));
      const auth = btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('auth')!)));

      await useNotificationStore.getState().registerPush({
        userId,
        platform: 'web_push',
        token: JSON.stringify({ endpoint: subscription.endpoint, keys: { p256dh, auth } }),
        deviceName: navigator.userAgent.includes('Chrome')
          ? 'Chrome'
          : navigator.userAgent.includes('Firefox')
            ? 'Firefox'
            : 'Navegador',
      });

      setLocalChannels((prev) => {
        const hasWebPush = prev.some((c) => c.channel === 'web_push');
        if (hasWebPush) {
          return prev.map((c) => (c.channel === 'web_push' ? { ...c, enabled: true } : c));
        }
        return [...prev, { channel: 'web_push', enabled: true }];
      });
      markDirty();

      alert('¡Notificaciones push habilitadas con éxito en este navegador! Recuerda guardar tus cambios.');
      fetchDevices(userId);
    } catch (err: any) {
      console.error('[WebPush] Detailed registration error:', err);

      if (err.name === 'AbortError') {
        alert(
          'Error: El servicio de push del navegador abortó la petición. Esto suele deberse a una clave VAPID inválida o problemas de red con los servidores de Google/Mozilla.',
        );
      } else if (err.name === 'NotAllowedError') {
        alert('Permiso denegado: Has bloqueado las notificaciones en este sitio.');
      } else {
        alert(`Error al registrar notificaciones: ${err.message || 'Error desconocido'}`);
      }
    }
  }, [userId, setLocalChannels, markDirty, fetchDevices]);

  return handleWebPushRegister;
}
