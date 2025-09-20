// API Service for authentication
import { getApiUrl } from './config';
import { SessionStorage } from './secureStorage';

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
      const sessionId = await SessionStorage.getSessionId();
      const csrfToken = await SessionStorage.getCsrfToken();
      
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

    if (!response.data) {
      throw new Error('No data in mobile login response');
    }

    if (!response.data.sessionId || !response.data.csrfToken) {
      throw new Error('Missing sessionId or csrfToken in mobile login response');
    }

    return response.data;
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

  // Métodos públicos para storage usando secure storage
  async storeSession(sessionId: string, csrfToken: string): Promise<void> {
    await SessionStorage.storeSession(sessionId, csrfToken);
  }

  async clearSession(): Promise<void> {
    await SessionStorage.clearSession();
  }
}