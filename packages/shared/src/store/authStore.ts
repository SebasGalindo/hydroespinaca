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
  isLoading: false,
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
          sessionId: null, // HttpOnly cookie
          csrfToken: null, // Cookie
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
        };

        const user: User = {
          id: userSession.email,
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
          id: userSession.email,
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

    // Validación más estricta: solo llamar al endpoint si realmente hay datos de sesión
    // Verificamos múltiples condiciones para asegurar que hay una sesión válida
    const hasUserData = currentState.user !== null && currentState.user.email !== '';
    const hasSessionData = currentState.session !== null;
    const isMarkedAuthenticated = currentState.isAuthenticated;

    const hasActiveSession = isMarkedAuthenticated && (hasUserData || hasSessionData);

    set({ isLoading: true, isLoggingOut: true });

    try {
      // Solo llamar al endpoint de logout si hay una sesión activa
      // Esto evita errores 401/400 innecesarios cuando no hay sesión
      if (hasActiveSession) {
        const sessionId = platform === 'mobile' && currentState.session
          ? currentState.session.sessionId || undefined
          : undefined;

        // Call logout endpoint solo si hay sesión activa
        await authService.logout(platform === 'web' ? 'web' : 'mobile', sessionId);
      } else {
        // No hay sesión activa, solo limpiar localmente
        if (process.env.NODE_ENV === 'development') {
          console.info('[AuthStore] No active session, skipping logout endpoint call');
        }
      }

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
      // Incluso si el logout falla en el servidor (ej: 400 por sesión ya inválida),
      // limpiamos la sesión local de todos modos
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
        sessionId,
        csrfToken,
        username: userSession.username,
        email: userSession.email,
        role: userSession.role,
      };

      const user: User = {
        id: userSession.email,
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
      if (error instanceof ApiError && error.status === 401) {
        // 401 significa que no hay sesión válida
        // No intentamos hacer logout porque generaría otro 401/400 innecesario
        if (process.env.NODE_ENV === 'development') {
          console.info('[AuthStore] No valid session found (401), clearing local state');
        }

        // Solo limpiar tokens móviles si aplica
        try {
          if (platform === 'mobile') {
            await SessionStorage.clearSession();
          }
        } catch (cleanupError) {
          if (process.env.NODE_ENV === 'development') {
            console.warn('[AuthStore] Failed to clear mobile session storage:', cleanupError);
          }
        }
      } else if (process.env.NODE_ENV === 'development') {
        // Otros errores (red, servidor, etc.)
        console.warn('[AuthStore] Session check failed:', error);
      }

      // No valid session found
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
