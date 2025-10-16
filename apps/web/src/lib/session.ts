/**
 * Session utilities for client-side session management
 * Interacts with BFF /session endpoint
 */
import { apiGet, apiPost } from './api';

export interface SessionData {
  username: string;
  email: string;
  role: string;
}

/**
 * Verifica si existe una sesión activa consultando el endpoint /session del BFF
 *
 * @returns SessionData si hay sesión activa, null si no hay sesión
 */
export async function fetchSession(): Promise<SessionData | null> {
  try {
    const data = await apiGet<any>('/session');

    return {
      username: data.username,
      email: data.email,
      role: data.role,
    };
  } catch (error: any) {
    // 401 Unauthorized significa que no hay sesión activa
    if (error.status === 401) {
      return null;
    }
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
    await apiPost('/auth/logout');
    return true;
  } catch (error) {
    console.error('Error during logout:', error);
    return false;
  }
}
