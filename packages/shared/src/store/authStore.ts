import { create } from "zustand";
import { AuthApiService, ApiError } from "../api/authService";
import { SessionStorage } from "../utils"; // Import from index to use platform-specific version
import { detectPlatform, isDevelopmentMode } from "../utils/apiConfig";
import type { Session } from "../types/auth";

// Import to access redirect callback
let redirectCallback: ((path: string) => void) | null = null;

export function setAuthStoreRedirectCallback(redirect: (path: string) => void) {
  redirectCallback = redirect;
}

export interface User {
  id: string;
  email: string;
  name: string;
  role?: string;
}

interface AuthState {
  user: User | null;
  session: Session | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  isLoggingOut: boolean;
  login: (email: string, password: string, acceptTerms?: boolean) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  checkSession: () => Promise<void>;
  acceptTerms: () => Promise<void>;
}

// Create API service instance
const authService = new AuthApiService();
const platform = detectPlatform();

if (isDevelopmentMode()) {
  console.log(`[authStore] Module loaded — platform=${platform}`);
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  session: null,
  isAuthenticated: false,
  isLoading: false, // Start as false - checkSession will be called explicitly
  error: null,
  isLoggingOut: false,

  login: async (email: string, password: string, acceptTerms?: boolean) => {
    set({ isLoading: true, error: null });

    if (isDevelopmentMode()) {
      console.log(`[authStore.login] Starting login — platform=${platform}, email=${email}`);
    }

    try {
      const credentials = { Email: email, Password: password };

      if (platform === 'web') {
        if (isDevelopmentMode()) console.log('[authStore.login] Taking WEB branch');
        // Web: cookies are set automatically by server
        await authService.loginWeb(credentials, acceptTerms);

        // Get user session data
        const userSession = await authService.getCurrentSession('web');

        const session: Session = {
          sessionId: userSession.sessionId,
          csrfToken: null, // Cookie
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
          hasAcceptedTerms: userSession.hasAcceptedTerms,
        };

        const user: User = {
          id: userSession.userId,
          email: userSession.email,
          name: userSession.username,
          role: userSession.role,
        };

        set({
          user,
          session,
          isAuthenticated: true,
          isLoading: false,
          error: null,
        });
      } else {
        if (isDevelopmentMode()) console.log('[authStore.login] Taking MOBILE branch');
        // Mobile: get tokens from response and store them
        const mobileResponse = await authService.loginMobile(credentials, acceptTerms);

        // Store tokens in secure storage
        await SessionStorage.storeSession(
          mobileResponse.sessionId,
          mobileResponse.csrfToken
        );

        // Get user session data
        const userSession = await authService.getCurrentSession('mobile');

        const session: Session = {
          sessionId: mobileResponse.sessionId,
          csrfToken: mobileResponse.csrfToken,
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
          hasAcceptedTerms: userSession.hasAcceptedTerms,
        };

        const user: User = {
          id: userSession.userId,
          email: userSession.email,
          name: userSession.username,
          role: userSession.role,
        };

        set({
          user,
          session,
          isAuthenticated: true,
          isLoading: false,
          error: null,
        });
      }
    } catch (error) {
      if (isDevelopmentMode()) {
        console.error('[authStore.login] ERROR caught:', error);
        if (error instanceof ApiError) {
          console.error(`[authStore.login] ApiError status=${error.status}, message=${error.message}`);
        }
      }
      let errorMessage = 'Error al iniciar sesión';

      if (error instanceof ApiError) {
        if (error.status === 401) {
          errorMessage = 'Credenciales inválidas';
        } else if (error.status === 0) {
          errorMessage = 'Error de conexión con el servidor';
        } else {
          errorMessage = error.message;
        }
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }

      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
        error: errorMessage,
      });

      // Re-throw so callers (e.g. LoginForm) can detect failure
      throw error;
    }
  },

  logout: async () => {
    // Prevenir llamadas múltiples simultáneas a logout
    if (get().isLoggingOut) {
      if (process.env.NODE_ENV === 'development') {
        console.info('[AuthStore] Logout already in progress, skipping');
      }
      return;
    }

    const currentState = get();
    set({ isLoading: true, isLoggingOut: true });

    try {
      // ALWAYS call logout endpoint - backend handles cookie cleanup centrally
      // Backend will delete cookies even if session doesn't exist
      const sessionId = platform === 'mobile' && currentState.session
        ? currentState.session.sessionId || undefined
        : undefined;

      await authService.logout(platform === 'web' ? 'web' : 'mobile', sessionId);

      // For mobile, clear stored tokens
      if (platform === 'mobile') {
        await SessionStorage.clearSession();
      }

      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
        isLoggingOut: false,
        error: null,
      });
    } catch (error) {
      // Even if logout fails on server, clear local session
      if (process.env.NODE_ENV === 'development') {
        console.warn('[AuthStore] Logout endpoint failed, clearing local session anyway:', error);
      }

      if (platform === 'mobile') {
        await SessionStorage.clearSession();
      }

      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
        isLoggingOut: false,
        error: null,
      });
    }
  },

  clearError: () => {
    set({ error: null });
  },

  // Check if there's an existing session
  checkSession: async () => {
    set({ isLoading: true });

    try {
      // Try to get current session from server
      const userSession = await authService.getCurrentSession(platform === 'web' ? 'web' : 'mobile');

      // For mobile, get tokens from storage
      const sessionId = platform === 'mobile'
        ? await SessionStorage.getSessionId()
        : null;
      const csrfToken = platform === 'mobile'
        ? await SessionStorage.getCsrfToken()
        : null;

      const session: Session = {
        sessionId: userSession.sessionId,
        csrfToken,
        username: userSession.username,
        email: userSession.email,
        role: userSession.role,
        hasAcceptedTerms: userSession.hasAcceptedTerms,
      };

      const user: User = {
        id: userSession.userId,
        email: userSession.email,
        name: userSession.username,
        role: userSession.role,
      };

      set({
        user,
        session,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      // Session is invalid or expired
      if (process.env.NODE_ENV === 'development') {
        if (error instanceof ApiError && error.status === 401) {
          console.info('[AuthStore] No valid session found (401) - executing full cleanup');
        } else {
          console.warn('[AuthStore] Session check failed:', error);
        }
      }

      // If it's a 401 (unauthorized), execute full logout to clean cookies and storage
      if (error instanceof ApiError && error.status === 401) {
        // Clear browser storage for web
        if (platform === 'web' && typeof window !== 'undefined') {
          if (typeof (window as any).localStorage !== 'undefined') {
            (window as any).localStorage.clear();
          }
          if (typeof (window as any).sessionStorage !== 'undefined') {
            (window as any).sessionStorage.clear();
          }
          if (process.env.NODE_ENV === 'development') {
            console.info('[AuthStore] Browser storage cleared (localStorage, sessionStorage)');
          }
        }

        // Clear mobile tokens if applicable
        if (platform === 'mobile') {
          try {
            await SessionStorage.clearSession();
          } catch (cleanupError) {
            if (process.env.NODE_ENV === 'development') {
              console.warn('[AuthStore] Failed to clear mobile session storage:', cleanupError);
            }
          }
        }

        // Call logout to clean server-side cookies (backend will delete cookies even if session doesn't exist)
        try {
          await authService.logout(platform === 'web' ? 'web' : 'mobile');
          if (process.env.NODE_ENV === 'development') {
            console.info('[AuthStore] Logout endpoint called to clean server-side cookies');
          }
        } catch (logoutError) {
          // Ignore errors - session is already invalid
          if (process.env.NODE_ENV === 'development') {
            console.warn('[AuthStore] Logout endpoint failed (expected if session already invalid):', logoutError);
          }
        }

        // Trigger immediate redirect to login using callback
        if (redirectCallback) {
          if (process.env.NODE_ENV === 'development') {
            console.info('[AuthStore] Triggering immediate redirect to login');
          }
          redirectCallback(platform === 'web' ? '/login' : 'Login');
        } else if (platform === 'web' && typeof window !== 'undefined' && window.location) {
          // Fallback for web: use window.location
          if (process.env.NODE_ENV === 'development') {
            console.info('[AuthStore] No redirect callback, using window.location fallback');
          }
          window.location.href = '/login';
        }
      } else {
        // For other errors (network, etc.), just clear mobile tokens
        if (platform === 'mobile') {
          try {
            await SessionStorage.clearSession();
          } catch (cleanupError) {
            if (process.env.NODE_ENV === 'development') {
              console.warn('[AuthStore] Failed to clear mobile session storage:', cleanupError);
            }
          }
        }
      }

      // Clear local state
      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  },

  acceptTerms: async () => {
    const { user, session } = get();
    if (!user) return;

    await authService.acceptTerms(user.id, platform === 'web' ? 'web' : 'mobile');

    if (session) {
      set({ session: { ...session, hasAcceptedTerms: true } });
    }
  },
}));
