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
          userId: userSession.userId,
          userRole: userSession.userRole,
        };

        const user: User = {
          id: userSession.userId,
          email,
          name: email.split('@')[0], // Use email prefix as name temporarily
          role: userSession.userRole,
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
          userId: userSession.userId,
          userRole: userSession.userRole,
        };

        const user: User = {
          id: userSession.userId,
          email,
          name: email.split('@')[0], // Use email prefix as name temporarily
          role: userSession.userRole,
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
    set({ isLoading: true });

    try {
      const currentSession = get().session;
      const sessionId = platform === 'mobile' && currentSession
        ? currentSession.sessionId || undefined
        : undefined;

      // Call logout endpoint
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
        error: null,
      });
    } catch (error) {
      // Even if logout fails on server, clear local session
      if (platform === 'mobile') {
        await SessionStorage.clearSession();
      }

      set({
        user: null,
        session: null,
        isAuthenticated: false,
        isLoading: false,
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
        userId: userSession.userId,
        userRole: userSession.userRole,
      };

      const user: User = {
        id: userSession.userId,
        email: '', // We don't have email from session endpoint
        name: userSession.userId, // Use userId as name temporarily
        role: userSession.userRole,
      };

      set({
        user,
        session,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
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
