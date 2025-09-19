import { useState, useCallback, useEffect } from 'react';
import type { AuthState, UseAuthReturn, AuthConfig, Session } from '@hydroespinaca/shared-types';
import { AuthApiService, ApiError } from '@hydroespinaca/shared-utils';

// Core auth hook - platform agnostic logic
export function useAuth(config?: AuthConfig): UseAuthReturn {
  const [state, setState] = useState<AuthState>({
    session: null,
    isLoading: true,
    error: null,
  });

  const apiService = new AuthApiService(config?.apiBaseUrl);
  const platform = config?.platform || 'web';

  // Initialize auth state - check for existing session
  useEffect(() => {
    const checkAuthStatus = async () => {
      try {
        const userSession = await apiService.getCurrentSession(platform);
        
        // Para web, los tokens están en cookies HttpOnly, no los tenemos en JS
        // Para mobile, necesitamos recuperar los tokens del secure storage
        const session: Session = {
          sessionId: platform === 'mobile' ? getStoredSessionId() : null,
          csrfToken: platform === 'mobile' ? getStoredCsrfToken() : null,
          userId: userSession.userId,
          userRole: userSession.userRole,
        };
        
        setState({
          session,
          isLoading: false,
          error: null,
        });
      } catch (error) {
        // Si no hay sesión válida, simplemente no autenticamos
        setState({
          session: null,
          isLoading: false,
          error: null,
        });
      }
    };

    checkAuthStatus();
  }, [platform]);

  const login = useCallback(async (email: string, password: string) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      if (!email.trim() || !password.trim()) {
        throw new Error('Email y contraseña son requeridos');
      }

      const credentials = { Email: email, Password: password };

      if (platform === 'web') {
        // Login web - tokens van a cookies HttpOnly
        await apiService.loginWeb(credentials);
        
        // Obtener información de la sesión después del login
        const userSession = await apiService.getCurrentSession('web');
        
        const session: Session = {
          sessionId: null, // HttpOnly cookie, no accesible desde JS
          csrfToken: null, // Cookie no HttpOnly, pero lo maneja el browser
          userId: userSession.userId,
          userRole: userSession.userRole,
        };
        
        setState({
          session,
          isLoading: false,
          error: null,
        });
      } else {
        // Login mobile - tokens en response body y headers
        const mobileResponse = await apiService.loginMobile(credentials);
        
        // Almacenar tokens en secure storage
        storeSession(mobileResponse.sessionId, mobileResponse.csrfToken);
        
        // Obtener información de la sesión
        const userSession = await apiService.getCurrentSession('mobile');
        
        const session: Session = {
          sessionId: mobileResponse.sessionId,
          csrfToken: mobileResponse.csrfToken,
          userId: userSession.userId,
          userRole: userSession.userRole,
        };
        
        setState({
          session,
          isLoading: false,
          error: null,
        });
      }
    } catch (error) {
      let errorMessage = 'Error de login';
      
      if (error instanceof ApiError) {
        if (error.status === 401) {
          errorMessage = 'Credenciales inválidas';
        } else if (error.status === 0) {
          errorMessage = 'Error de conexión';
        } else {
          errorMessage = error.message;
        }
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }
      
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: errorMessage,
      }));
      throw error;
    }
  }, [platform, apiService]);

  const logout = useCallback(async () => {
    setState(prev => ({ ...prev, isLoading: true }));
    
    try {
      const sessionId = platform === 'mobile' ? state.session?.sessionId || undefined : undefined;
      await apiService.logout(platform, sessionId);
      
      // Limpiar storage local para mobile
      if (platform === 'mobile') {
        clearStoredSession();
      }
      
      setState({
        session: null,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      // Incluso si el logout falla en el servidor, limpiamos la sesión local
      if (platform === 'mobile') {
        clearStoredSession();
      }
      
      setState({
        session: null,
        isLoading: false,
        error: null,
      });
      
      console.error('Logout error:', error);
    }
  }, [platform, state.session?.sessionId, apiService]);

  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  // Helper functions para storage multiplataforma
  const getFromStorage = (key: string): string | null => {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        return localStorage.getItem(key);
      }
      
      // React Native - para funcionar ahora, retornamos null
      // En el futuro esto debería usar AsyncStorage/SecureStore
      return null;
    } catch (error) {
      console.warn(`Error accessing storage for key ${key}:`, error);
      return null;
    }
  };

  const setInStorage = (key: string, value: string): void => {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        localStorage.setItem(key, value);
        return;
      }
      
      // React Native - log para debugging
      console.log(`Would store ${key} in React Native storage:`, value);
    } catch (error) {
      console.warn(`Error storing ${key}:`, error);
    }
  };

  const removeFromStorage = (key: string): void => {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        localStorage.removeItem(key);
        return;
      }
      
      // React Native - log para debugging
      console.log(`Would remove ${key} from React Native storage`);
    } catch (error) {
      console.warn(`Error removing ${key}:`, error);
    }
  };

  const getStoredSessionId = (): string | null => getFromStorage('sessionId');
  const getStoredCsrfToken = (): string | null => getFromStorage('csrfToken');
  const storeSession = (sessionId: string, csrfToken: string): void => {
    setInStorage('sessionId', sessionId);
    setInStorage('csrfToken', csrfToken);
  };
  const clearStoredSession = (): void => {
    removeFromStorage('sessionId');
    removeFromStorage('csrfToken');
  };

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