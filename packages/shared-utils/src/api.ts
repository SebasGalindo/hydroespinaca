// API Service for authentication
import { getApiUrl } from './config';

export interface ApiResponse<T = any> {
  data?: T;
  error?: string;
  status: number;
}

export interface LoginRequest {
  Email: string;
  Password: string;
}

export interface MobileLoginResponse {
  sessionId: string;
  csrfToken: string;
  message?: string;
}

export interface UserSession {
  userId: string;
  userRole: string;
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

export class AuthApiService {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    // Use provided baseUrl or get from centralized config
    this.baseUrl = baseUrl || getApiUrl();
  }

  private async request<T>(
    url: string, 
    options: RequestInit = {},
    platform: 'web' | 'mobile' = 'web'
  ): Promise<ApiResponse<T>> {
    const fullUrl = `${this.baseUrl}${url}`;
    
    const defaultHeaders: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    // Para mobile, agregar headers de sesión si existen
    if (platform === 'mobile') {
      const sessionId = this.getStoredSessionId();
      const csrfToken = this.getStoredCsrfToken();
      
      if (sessionId) {
        defaultHeaders['X-Session-Id'] = sessionId;
      }
      if (csrfToken) {
        defaultHeaders['X-CSRF-Token'] = csrfToken;
      }
    }

    try {
      const response = await fetch(fullUrl, {
        ...options,
        headers: {
          ...defaultHeaders,
          ...options.headers,
        },
        credentials: platform === 'web' ? 'include' : 'omit', // Para cookies en web
      });

      const isJson = response.headers.get('content-type')?.includes('application/json');
      const data = isJson ? await response.json() : null;

      if (!response.ok) {
        throw new ApiError(response.status, data?.message || `HTTP ${response.status}`);
      }

      return {
        data,
        status: response.status,
      };
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }
      
      throw new ApiError(0, error instanceof Error ? error.message : 'Network error');
    }
  }

  // Web login - usa cookies HttpOnly
  async loginWeb(credentials: LoginRequest): Promise<void> {
    await this.request('/auth/login/web', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }, 'web');
  }

  // Mobile login - retorna tokens en body y headers
  async loginMobile(credentials: LoginRequest): Promise<MobileLoginResponse> {
    const response = await this.request<MobileLoginResponse>('/auth/login/mobile', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }, 'mobile');

    return response.data!;
  }

  // Obtener sesión actual
  async getCurrentSession(platform: 'web' | 'mobile' = 'web'): Promise<UserSession> {
    const response = await this.request<UserSession>('/auth/session', {
      method: 'GET',
    }, platform);

    return response.data!;
  }

  // Logout
  async logout(platform: 'web' | 'mobile' = 'web', sessionId?: string): Promise<void> {
    const options: RequestInit = {
      method: 'POST',
    };

    // Para mobile, enviar sessionId en el body si se proporciona
    if (platform === 'mobile' && sessionId) {
      options.body = JSON.stringify({ sessionId });
    }

    await this.request('/auth/logout', options, platform);
  }

  // Métodos para manejar storage de forma multiplataforma
  private getStoredSessionId(): string | null {
    return this.getFromStorage('sessionId');
  }

  private getStoredCsrfToken(): string | null {
    return this.getFromStorage('csrfToken');
  }

  // Métodos de storage multiplataforma
  private getFromStorage(key: string): string | null {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        return localStorage.getItem(key);
      }
      
      // React Native - usar AsyncStorage (básico, debería mejorarse con SecureStore)
      // Nota: En una implementación real de RN, aquí usaríamos AsyncStorage
      // Por ahora, returnamos null para React Native
      return null;
    } catch (error) {
      console.warn(`Error accessing storage for key ${key}:`, error);
      return null;
    }
  }

  private setInStorage(key: string, value: string): void {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        localStorage.setItem(key, value);
        return;
      }
      
      // React Native - AsyncStorage/SecureStore
      // En una implementación real, aquí usaríamos AsyncStorage.setItem()
      console.log(`Would store ${key} in React Native storage:`, value);
    } catch (error) {
      console.warn(`Error storing ${key}:`, error);
    }
  }

  private removeFromStorage(key: string): void {
    try {
      // Web - usar localStorage
      if (typeof window !== 'undefined' && window.localStorage) {
        localStorage.removeItem(key);
        return;
      }
      
      // React Native - AsyncStorage/SecureStore
      // En una implementación real, aquí usaríamos AsyncStorage.removeItem()
      console.log(`Would remove ${key} from React Native storage`);
    } catch (error) {
      console.warn(`Error removing ${key}:`, error);
    }
  }

  // Métodos públicos para storage
  storeSession(sessionId: string, csrfToken: string): void {
    this.setInStorage('sessionId', sessionId);
    this.setInStorage('csrfToken', csrfToken);
  }

  clearSession(): void {
    this.removeFromStorage('sessionId');
    this.removeFromStorage('csrfToken');
  }
}