// Authentication API Service
import { getApiUrl } from '../utils/apiConfig';
import { SessionStorage } from '../utils'; // Import from index to use platform-specific version
import type {
  LoginRequest,
  MobileLoginResponse,
  UserSession,
} from '../types/auth';

export interface ApiResponse<T = any> {
  data?: T;
  error?: string;
  status: number;
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

  /**
   * Internal request method that handles platform-specific authentication
   */
  private async request<T>(
    url: string,
    options: RequestInit = {},
    platform: 'web' | 'mobile' = 'web'
  ): Promise<ApiResponse<T>> {
    const fullUrl = `${this.baseUrl}${url}`;

    const defaultHeaders: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    // For mobile: add session headers if they exist
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
        // For web: include cookies automatically
        // For mobile: don't use cookies
        credentials: platform === 'web' ? 'include' : 'omit',
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

  /**
   * Web login - uses HttpOnly cookies set by the server
   */
  async loginWeb(credentials: LoginRequest): Promise<void> {
    await this.request('/auth/login/web', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }, 'web');
  }

  /**
   * Mobile login - returns tokens in response body
   */
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

  /**
   * Get current session information
   */
  async getCurrentSession(platform: 'web' | 'mobile' = 'web'): Promise<UserSession> {
    const response = await this.request<UserSession>('/auth/session', {
      method: 'GET',
    }, platform);

    if (!response.data) {
      throw new Error('No session data received');
    }

    return response.data;
  }

  /**
   * Logout - clear session on server
   */
  async logout(platform: 'web' | 'mobile' = 'web', sessionId?: string): Promise<void> {
    const options: RequestInit = {
      method: 'POST',
    };

    // For mobile, send sessionId in body if provided
    if (platform === 'mobile' && sessionId) {
      options.body = JSON.stringify({ sessionId });
    }

    await this.request('/auth/logout', options, platform);
  }

  /**
   * Store session in secure storage (for mobile)
   */
  async storeSession(sessionId: string, csrfToken: string): Promise<void> {
    await SessionStorage.storeSession(sessionId, csrfToken);
  }

  /**
   * Clear session from secure storage (for mobile)
   */
  async clearSession(): Promise<void> {
    await SessionStorage.clearSession();
  }
}

// Export a singleton instance
export const authService = new AuthApiService();
