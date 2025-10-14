'use client';

import { useState, useEffect } from 'react';
import { fetchSession, type SessionData } from '@/lib/session';

/**
 * Hook para acceder a la sesión del usuario desde componentes cliente
 *
 * Uso:
 * ```tsx
 * const { session, isLoading, error } = useSession();
 *
 * if (isLoading) return <div>Loading...</div>;
 * if (!session) return <div>Not authenticated</div>;
 *
 * return <div>Welcome {session.email}</div>;
 * ```
 */
export function useSession() {
  const [session, setSession] = useState<SessionData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function loadSession() {
      try {
        setIsLoading(true);
        setError(null);
        const sessionData = await fetchSession();

        if (mounted) {
          setSession(sessionData);
        }
      } catch (err) {
        if (mounted) {
          setError(err instanceof Error ? err.message : 'Failed to load session');
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    }

    loadSession();

    return () => {
      mounted = false;
    };
  }, []);

  return {
    session,
    isLoading,
    error,
    isAuthenticated: session !== null,
  };
}
