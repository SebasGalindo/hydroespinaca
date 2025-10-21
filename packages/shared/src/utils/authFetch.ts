// Auth-aware fetch wrapper that handles 401 automatically

type LogoutCallback = () => Promise<void>;

let globalLogoutCallback: LogoutCallback | null = null;
let redirectCallback: ((path: string) => void) | null = null;

/**
 * Set global logout callback (called from app initialization)
 */
export function setAuthCallbacks(logout: LogoutCallback, redirect: (path: string) => void) {
  globalLogoutCallback = logout;
  redirectCallback = redirect;
}

/**
 * Clears browser storage (localStorage, sessionStorage)
 *
 * NOTE: Cookie cleanup is handled entirely by the backend.
 * The logout endpoint on the BFF service will delete all session cookies.
 */
function clearBrowserStorage() {
  if (typeof window === 'undefined') return;

  try {
    // Clear localStorage (if available)
    if (typeof window !== 'undefined' && typeof (window as any).localStorage !== 'undefined') {
      (window as any).localStorage.clear();
    }

    // Clear sessionStorage (if available)
    if (typeof window !== 'undefined' && typeof (window as any).sessionStorage !== 'undefined') {
      (window as any).sessionStorage.clear();
    }

    if (process.env.NODE_ENV === 'development') {
      console.info('[authFetch] Browser storage cleared (localStorage, sessionStorage)');
    }
  } catch (error) {
    if (process.env.NODE_ENV === 'development') {
      console.warn('[authFetch] Failed to clear browser storage:', error);
    }
  }
}

/**
 * Fetch wrapper that automatically handles 401 responses
 * by logging out and redirecting to login
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

      if (process.env.NODE_ENV === 'development') {
        console.warn('[authFetch] Received 401 Unauthorized from:', url);
      }

      // Clear browser storage (localStorage, sessionStorage)
      // Backend handles cookie cleanup via logout endpoint
      clearBrowserStorage();

      // Call logout callback to clear server-side session and trigger backend cookie cleanup
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
        redirectCallback('/login');
      } else if (typeof window !== 'undefined' && window.location) {
        // Fallback: use window.location
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
