'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { setAuthCallbacks, useAuthStore } from '@hydroespinaca/shared';


export function Providers({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const logout = useAuthStore((state) => state.logout);

  // Set auth callbacks on mount
  useEffect(() => {
    setAuthCallbacks(logout, (path: string) => router.push(path));
  }, [logout, router]);

  // Register Service Worker for Web Push notifications
  useEffect(() => {
    if (typeof window !== 'undefined' && 'serviceWorker' in navigator) {
      navigator.serviceWorker.register('/sw.js').catch((err) => {
        console.warn('SW registration failed:', err);
      });
    }
  }, []);

  return <>{children}</>;
}
