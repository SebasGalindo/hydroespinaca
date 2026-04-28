// Base authentication hook that works across platforms
import { useState, useEffect, useCallback } from 'react';
import { AuthApiService, ApiError } from '../api/authService';
import { SessionStorage } from '../utils'; // Import from index to use platform-specific version
import type {
  UseAuthReturn,
  AuthState,
  Session,
  AuthConfig,
} from '../types/auth';

export function useAuth(config?: AuthConfig): UseAuthReturn {
  const [state, setState] = useState<AuthState>({
    session: null,
    isLoading: true,
    error: null,
  });

  const apiService = new AuthApiService(config?.apiBaseUrl);
  const platform = config?.platform || 'web';

  // Initialize: check existing session
  useEffect(() => {
    const checkAuthStatus = async () => {
      try {
        // Try to get current session from server
        const userSession = await apiService.getCurrentSession(platform);

        // For mobile, get tokens from storage
        const sessionId = platform === 'mobile'
          ? await SessionStorage.getSessionId()
          : null;
        const csrfToken = platform === 'mobile'
          ? await SessionStorage.getCsrfToken()
          : null;

        if (!sessionId && platform === 'mobile') {
          setState({ session: null, isLoading: false, error: null });
          return;
        }

        const session: Session = {
          sessionId,
          csrfToken,
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
          hasAcceptedTerms: userSession.hasAcceptedTerms ?? false,
        };

        setState({ session, isLoading: false, error: null });
      } catch (error) {
        // No valid session found
        // Clear any stored tokens for mobile
        if (platform === 'mobile') {
          try {
            await SessionStorage.clearSession();
          } catch (err) {
            // Silently handle storage errors
          }
        }

        setState({ session: null, isLoading: false, error: null });
      }
    };

    checkAuthStatus();
  }, [platform]);

  /**
   * Login function
   */
  const login = useCallback(async (email: string, password: string) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const credentials = { Email: email, Password: password };

      if (platform === 'web') {
        // Web: cookies are set automatically by the server
        await apiService.loginWeb(credentials);

        // Get user session data
        const userSession = await apiService.getCurrentSession('web');

        const session: Session = {
          sessionId: null, // HttpOnly cookie, not accessible
          csrfToken: null, // Cookie
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
          hasAcceptedTerms: userSession.hasAcceptedTerms ?? false,
        };

        setState({ session, isLoading: false, error: null });
      } else {
        // Mobile: get tokens from response and store them
        const mobileResponse = await apiService.loginMobile(credentials);

        // Store tokens in secure storage
        await SessionStorage.storeSession(
          mobileResponse.sessionId,
          mobileResponse.csrfToken
        );

        // Get user session data
        const userSession = await apiService.getCurrentSession('mobile');

        const session: Session = {
          sessionId: mobileResponse.sessionId,
          csrfToken: mobileResponse.csrfToken,
          username: userSession.username,
          email: userSession.email,
          role: userSession.role,
          hasAcceptedTerms: userSession.hasAcceptedTerms ?? false,
        };

        setState({ session, isLoading: false, error: null });
      }
    } catch (error) {
      let errorMessage = 'Error de login';

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

      setState(prev => ({ ...prev, isLoading: false, error: errorMessage }));
      throw error;
    }
  }, [platform, apiService]);

  /**
   * Logout function
   */
  const logout = useCallback(async () => {
    setState(prev => ({ ...prev, isLoading: true }));

    try {
      const sessionId = platform === 'mobile'
        ? state.session?.sessionId || undefined
        : undefined;

      // Call logout endpoint
      await apiService.logout(platform, sessionId);

      // For mobile, clear stored tokens
      if (platform === 'mobile') {
        await SessionStorage.clearSession();
      }

      setState({ session: null, isLoading: false, error: null });
    } catch (error) {
      // Even if logout fails on server, clear local session
      if (platform === 'mobile') {
        await SessionStorage.clearSession();
      }

      setState({ session: null, isLoading: false, error: null });
    }
  }, [platform, state.session?.sessionId, apiService]);

  /**
   * Clear error function
   */
  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  return {
    session: state.session,
    isLoading: state.isLoading,
    error: state.error,
    login,
    logout,
    clearError,
    isAuthenticated: !!state.session,
  };
}
