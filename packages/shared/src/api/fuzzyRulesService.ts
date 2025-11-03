import type { FuzzyRuleSummary } from '../types/fuzzyRules';
import { getApiUrl, detectPlatform } from '../utils/apiConfig';
import { SessionStorage } from '../utils';

/**
 * Service for fetching fuzzy rules information from the BFF
 */
export class FuzzyRulesService {
  private baseUrl: string;

  constructor() {
    // Use centralized API URL configuration
    this.baseUrl = getApiUrl();
  }

  /**
   * Platform-aware request method that adds session headers for mobile
   */
  private async request<T>(url: string, options: RequestInit = {}): Promise<T> {
    const platform = detectPlatform();
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

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Sesión inválida. Por favor, inicia sesión nuevamente.');
      }
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    return response.json() as Promise<T>;
  }

  /**
   * Fetches the list of all fuzzy rules with their names and descriptions.
   * This data is cached on the BFF for 24 hours, so it's safe to call once per session.
   */
  async getFuzzyRules(): Promise<FuzzyRuleSummary[]> {
    return this.request<FuzzyRuleSummary[]>('/system/fuzzy-rules', {
      method: 'GET',
    });
  }
}

// Singleton instance
export const fuzzyRulesService = new FuzzyRulesService();
