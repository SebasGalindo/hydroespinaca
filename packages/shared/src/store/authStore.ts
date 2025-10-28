import { create } from "zustand";
import { AuthApiService, ApiError } from "../api/authService";
import { SessionStorage } from "../utils"; // Import from index to use platform-specific version
import { detectPlatform } from "../utils/apiConfig";
import type { Session } from "../types/auth";

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
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  checkSession: () => Promise<void>;
}

// Create API service instance
const authService = new AuthApiService();
const platform = detectPlatform();

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  session: null,
  isAuthenticated: false,
  isLoading: true, // Start as true to prevent premature redirects during SSR/hydration
  error: null,
  isLoggingOut: false,

  login: async (email: string, password: string) => {
    set({ isLoading: true, error: null });

    try {
      const credentials = { Email: email, Password: password };

      if (platform === 'web') {
        // Web: cookies are set automatically by server
        await authService.loginWeb(credentials);

        // Get user session data
        const userSession = await authService.getCurrentSession('web');

        const session: Session = {
          sessionId: userSession.sessionId, // Available from backend response
          csrfToken: null, // Cookie
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
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
        // Mobile: get tokens from response and store them
        const mobileResponse = await authService.loginMobile(credentials);

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
        sessionId: userSession.sessionId, // Use sessionId from backend response
        csrfToken,
        username: userSession.username,
        email: userSession.email,
        role: userSession.role,
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
      // Session is invalid or expired - just clear local state
      // Backend will handle cookie cleanup when user explicitly logs out
      if (process.env.NODE_ENV === 'development') {
        if (error instanceof ApiError && error.status === 401) {
          console.info('[AuthStore] No valid session found (401)');
        } else {
          console.warn('[AuthStore] Session check failed:', error);
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

      // No valid session found - clear local state only
      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  },
}));
