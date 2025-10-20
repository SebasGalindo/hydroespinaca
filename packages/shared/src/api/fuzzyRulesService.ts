import type { FuzzyRuleSummary } from '../types/fuzzyRules';
import { getApiUrl } from '../utils/apiConfig';
import { authFetch } from '../utils/authFetch';

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
   * Fetches the list of all fuzzy rules with their names and descriptions.
   * This data is cached on the BFF for 24 hours, so it's safe to call once per session.
   * Uses authFetch for automatic 401 handling and redirect
   */
  async getFuzzyRules(): Promise<FuzzyRuleSummary[]> {
    const response = await authFetch(`${this.baseUrl}/system/fuzzy-rules`, {
      method: 'GET',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch fuzzy rules: ${response.statusText}`);
    }

    return response.json();
  }
}

// Singleton instance
export const fuzzyRulesService = new FuzzyRulesService();
