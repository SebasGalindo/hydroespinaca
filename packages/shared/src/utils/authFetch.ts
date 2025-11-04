// Auth-aware fetch wrapper that handles 401 automatically
import { detectPlatform } from './apiConfig';
import { SessionStorage } from './secureStorage';

type LogoutCallback = () => Promise<void>;
type RedirectCallback = (path: string) => void;

let globalLogoutCallback: LogoutCallback | null = null;
let redirectCallback: RedirectCallback | null = null;

/**
 * Set global logout callback (called from app initialization)
 */
export function setAuthCallbacks(logout: LogoutCallback, redirect: RedirectCallback) {
  globalLogoutCallback = logout;
  redirectCallback = redirect;
}

/**
 * Clears storage based on platform
 * - Web: clears localStorage and sessionStorage (cookies handled by backend)
 * - Mobile: clears secure storage with session tokens
 */
async function clearPlatformStorage() {
  const platform = detectPlatform();

  try {
    if (platform === 'web') {
      // Clear browser storage for web
      if (typeof window !== 'undefined') {
        if (typeof (window as any).localStorage !== 'undefined') {
          (window as any).localStorage.clear();
        }
        if (typeof (window as any).sessionStorage !== 'undefined') {
          (window as any).sessionStorage.clear();
        }
      }

      if (process.env.NODE_ENV === 'development') {
        console.info('[authFetch] Browser storage cleared (localStorage, sessionStorage)');
      }
    } else {
      // Clear secure storage for mobile
      await SessionStorage.clearSession();

      if (process.env.NODE_ENV === 'development') {
        console.info('[authFetch] Mobile secure storage cleared (sessionId, csrfToken)');
      }
    }
  } catch (error) {
    if (process.env.NODE_ENV === 'development') {
      console.warn('[authFetch] Failed to clear platform storage:', error);
    }
  }
}

/**
 * Fetch wrapper that automatically handles 401 responses
 * by logging out and redirecting to login
 * Works for both web and mobile platforms
 */
export async function authFetch(
  input: string | Request | URL,
  init?: RequestInit
): Promise<Response> {
  try {
    const response = await fetch(input, init);

    // If 401 Unauthorized, logout and redirect
    if (response.status === 401) {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.href : 'unknown';
      const platform = detectPlatform();

      if (process.env.NODE_ENV === 'development') {
        console.warn(`[authFetch] Received 401 Unauthorized from: ${url} (platform: ${platform})`);
      }

      // Clear platform-specific storage (web: localStorage/sessionStorage, mobile: secure storage)
      await clearPlatformStorage();

      // Call logout callback to clear server-side session
      if (globalLogoutCallback) {
        try {
          await globalLogoutCallback();
        } catch (error) {
          // Log but don't fail - the session is already invalid
          if (process.env.NODE_ENV === 'development') {
            console.error('[authFetch] Error during logout callback (session already invalid):', error);
          }
        }
      } else if (process.env.NODE_ENV === 'development') {
        console.warn('[authFetch] No logout callback registered - session may not be fully cleared on server');
      }

      // Redirect to login
      if (redirectCallback) {
        redirectCallback(platform === 'web' ? '/login' : 'Login');
      } else if (platform === 'web' && typeof window !== 'undefined' && window.location) {
        // Fallback for web: use window.location
        window.location.href = '/login';
      }

      // Throw error to stop further processing
      throw new Error('Sesión inválida. Por favor, inicia sesión nuevamente.');
    }

    return response;
  } catch (error) {
    // If fetch itself fails (network error, etc), rethrow
    if (error instanceof Error && error.message.includes('Sesión inválida')) {
      throw error;
    }
    throw error;
  }
}
