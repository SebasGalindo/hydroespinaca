// Authentication API Service
// NOTE: AuthApiService does NOT extend BaseApiService because it has
// a fundamentally different request pattern:
//  - Platform-aware (web/mobile) with different credentials modes
//  - Mobile session headers (X-Session-Id, X-CSRF-Token)
//  - Returns ApiResponse<T> wrapper instead of raw T
// These requirements make it unsuitable for the shared base class.
import { getApiUrl } from '../utils/apiConfig';
import { SessionStorage } from '../utils'; // Import from index to use platform-specific version
import { authFetch } from '../utils/authFetch';
import { ApiServiceError } from './BaseApiService';
import type {
  LoginRequest,
  MobileLoginResponse,
  UserSession,
} from '../types/auth';

export interface ApiResponse<T = unknown> {
  data?: T;
  error?: string;
  status: number;
}

export const ApiError = ApiServiceError;
export type ApiError = ApiServiceError;

export class AuthApiService {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    // Use provided baseUrl or get from centralized config
    this.baseUrl = baseUrl || getApiUrl();
  }

  /**
   * Internal request method that handles platform-specific authentication
   * Uses authFetch wrapper to automatically handle 401 responses
   */
  private async request<T>(
    url: string,
    options: RequestInit = {},
    platform: 'web' | 'mobile' = 'web'
  ): Promise<ApiResponse<T>> {
    const fullUrl = `${this.baseUrl}${url}`;

    if (__DEV__) {
      console.log(`[AuthService.request] ${options.method || 'GET'} ${fullUrl} (platform=${platform})`);
    }

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
      // Use authFetch instead of fetch directly - it handles 401 automatically
      const response = await authFetch(fullUrl, {
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
      const data: any = isJson ? await response.json() : null;

      if (!response.ok) {
        // authFetch already handled 401, so this handles other errors
        throw new ApiServiceError(response.status, data?.message || `HTTP ${response.status}`);
      }

      return {
        data: data as T | undefined,
        status: response.status,
      };
    } catch (error) {
      if (error instanceof ApiServiceError) {
        throw error;
      }

      throw new ApiServiceError(0, error instanceof Error ? error.message : 'Network error');
    }
  }

  /**
   * Web login - uses HttpOnly cookies set by the server
   */
  async loginWeb(credentials: LoginRequest): Promise<void> {
    if (__DEV__) console.log('[AuthService] loginWeb called');
    await this.request('/auth/login/web', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }, 'web');
  }

  /**
   * Mobile login - returns tokens in response body
   */
  async loginMobile(credentials: LoginRequest): Promise<MobileLoginResponse> {
    console.log('[AuthService] loginMobile called with URL:', `${this.baseUrl}/auth/login/mobile`);
    console.log('[AuthService] Credentials:', credentials);
    const response = await this.request<MobileLoginResponse>('/auth/login/mobile', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }, 'mobile');
    console.log('[AuthService] loginMobile response:', response);

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
   * Backend always clears cookies regardless of session validity
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
