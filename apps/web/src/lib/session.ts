/**
 * Session utilities for client-side session management
 * Interacts with BFF /session endpoint
 */

export interface SessionData {
  userId: string;
  userRole?: string;
  email?: string;
}

/**
 * Verifica si existe una sesión activa consultando el endpoint /session del BFF
 *
 * @returns SessionData si hay sesión activa, null si no hay sesión
 */
export async function fetchSession(): Promise<SessionData | null> {
  try {
    const response = await fetch('/api/session', {
      method: 'GET',
      credentials: 'include', // Importante: envía cookies (SessionId)
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      // 401 Unauthorized significa que no hay sesión activa
      if (response.status === 401) {
        return null;
      }
      throw new Error(`Session check failed: ${response.status}`);
    }

    const data = await response.json();
    return {
      userId: data.userId || data.id,
      userRole: data.role || data.userRole,
      email: data.email,
    };
  } catch (error) {
    console.error('Error fetching session:', error);
    return null;
  }
}

/**
 * Verifica si el usuario tiene una sesión activa
 * Usa fetchSession internamente
 *
 * @returns true si hay sesión activa, false en caso contrario
 */
export async function hasActiveSession(): Promise<boolean> {
  const session = await fetchSession();
  return session !== null;
}

/**
 * Cierra la sesión actual llamando al endpoint de logout del BFF
 */
export async function logout(): Promise<boolean> {
  try {
    const response = await fetch('/api/auth/logout', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return response.ok;
  } catch (error) {
    console.error('Error during logout:', error);
    return false;
  }
}
