import {
  LoginRequest,
  MobileLoginResponse,
  WebLoginResponse,
  Platform,
  Session
} from '../types/auth';
import { getCookie } from './cookies';

const getApiBaseUrl = (): string => {
  // Check if we're in a browser environment
  if (typeof window !== 'undefined') {
    // Browser environment
    const isProduction = window.location.protocol === 'https:';

    if (isProduction) {
      return 'https://api.hydroespinaca.online';
    }

    // Development: Connect to nginx-proxy (port 80) which proxies to bff-service
    const isDevelopmentServer = window.location.port === '3000';
    return isDevelopmentServer ? 'http://localhost' : window.location.origin;
  }

  // React Native or Node.js environment
  const isDevelopment = typeof process !== 'undefined'
    ? process.env.NODE_ENV !== 'production'
    : true;

  if (isDevelopment) {
    // For tunnel mode (ngrok), use production URL
    // For Android emulator, use 10.0.2.2, for iOS simulator use localhost
    // This can be configured via environment variables if needed
    return 'https://api.hydroespinaca.online'; // Use production URL for tunnel mode
  }

  return 'https://api.hydroespinaca.online';
};

const createApiClient = (platform: Platform, baseUrl?: string) => {
  const apiBaseUrl = baseUrl || getApiBaseUrl();

  // Helper to create requests with timeout
  const fetchWithTimeout = async (url: string, options: RequestInit = {}, timeoutMs = 10000) => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    try {
  const response = await fetch(url, {
    ...options,
    signal: controller.signal,
  });
  clearTimeout(timeoutId);

  // 👇 aquí filtras el 401 para no generar error en consola
  if (!response.ok) {
    if (response.status === 401) {
      // Manejas el caso de sesión inválida sin lanzar error
      return response;
    }

    // Para otros códigos sí puedes lanzar error
    throw new Error(`HTTP error: ${response.status}`);
  }

  return response;
} catch (error) {
  clearTimeout(timeoutId);
  if (error instanceof Error && error.name === "AbortError") {
    throw new Error("Request timeout - backend may not be available");
  }
  throw error;
}

  };

  return {
    baseURL: apiBaseUrl,

    async loginWeb(email: string, password: string): Promise<{ session: Session | null; response: WebLoginResponse }> {
      const response = await fetchWithTimeout(`${apiBaseUrl}/api/auth/login/web`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // Imprescindible para cookies
        body: JSON.stringify({ Email: email, Password: password }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Login failed' }));
        throw new Error(error.message || 'Login failed');
      }

      // Para web, no esperamos tokens en el body - solo leemos desde cookies
      const csrfToken = getCookie('CsrfToken');
      if (!csrfToken) {
        return { session: null, response: { Message: 'Login successful' } };
      }

      const session: Session = {
        sessionId: null, // HttpOnly, no accesible en JS
        csrfToken,
      };

      return { session, response: { Message: 'Login successful' } };
    },

    async loginMobile(email: string, password: string): Promise<{ session: Session; response: MobileLoginResponse }> {
      const response = await fetchWithTimeout(`${apiBaseUrl}/api/auth/login/mobile`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ Email: email, Password: password }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Login failed' }));
        throw new Error(error.message || 'Login failed');
      }

      const data = await response.json() as MobileLoginResponse;

      // For mobile, session data comes in response body
      const session: Session = {
        sessionId: data.SessionId,
        csrfToken: data.CsrfToken,
      };

      return { session, response: data };
    },

    async logout(sessionId?: string, csrfToken?: string): Promise<void> {
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };

      // Add session headers for mobile or if explicitly provided
      if (sessionId && csrfToken) {
        headers['X-Session-Id'] = sessionId;
        headers['X-CSRF-Token'] = csrfToken;
      }

      // For web: send empty body (BFF reads SessionId from HttpOnly cookie)
      // For mobile: send sessionId in body
      const body = platform === 'web' ? '{}' : JSON.stringify({ sessionId });

      const response = await fetchWithTimeout(`${apiBaseUrl}/api/auth/logout`, {
        method: 'POST',
        headers,
        credentials: platform === 'web' ? 'include' : 'omit',
        body,
      });

      if (!response.ok) {
        // Don't throw on logout failure, silently continue with local cleanup
      }
    },

    async validateSession(sessionId?: string, csrfToken?: string): Promise<boolean> {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };

        // Add session headers for mobile or if explicitly provided
        if (sessionId && csrfToken) {
          headers['X-Session-Id'] = sessionId;
          headers['X-CSRF-Token'] = csrfToken;
        }

        const response = await fetchWithTimeout(`${apiBaseUrl}/api/auth/session`, {
          method: 'GET',
          headers,
          credentials: platform === 'web' ? 'include' : 'omit',
        });

        return response.ok;
      } catch (error) {
        // Session validation failed, return false
        return false;
      }
    },

    async getCurrentSession(sessionId?: string, csrfToken?: string): Promise<{ user?: { userId: string; role: string }; isValid: boolean }> {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };

        if (sessionId && csrfToken) {
          headers['X-Session-Id'] = sessionId;
          headers['X-CSRF-Token'] = csrfToken;
        }

        const response = await fetchWithTimeout(`${apiBaseUrl}/api/auth/session`, {
          method: 'GET',
          headers,
          credentials: platform === 'web' ? 'include' : 'omit',
        });

        if (response.status === 401) {
          // Sesión no encontrada → estado esperado
          return { isValid: false };
        }

        if (!response.ok) {
          // Otro error real
          throw new Error(`Failed to fetch session: ${response.status}`);
        }

        const sessionInfo = await response.json() as { userId: string; role: string };
        return { user: sessionInfo, isValid: true };
      } catch (error) {
        // Solo loggear en dev
        if (process.env.NODE_ENV === 'development') {
          console.warn("Session check failed", error);
        }
        return { isValid: false };
      }
    }
  };
};

// Default API client (auto-detects platform)
export const apiClient = createApiClient(
  typeof window !== 'undefined' ? 'web' : 'mobile'
);

// Platform-specific API clients
export const webApiClient = createApiClient('web');
export const mobileApiClient = createApiClient('mobile');

// Factory function for custom configuration
export { createApiClient };