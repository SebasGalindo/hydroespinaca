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

  return <>{children}</>;
}
