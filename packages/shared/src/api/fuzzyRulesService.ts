// Fuzzy Rules Service — extends BaseApiService for DRY request handling
import { BaseApiService } from './BaseApiService';
import type { FuzzyRuleSummary } from '../types/fuzzyRules';

/**
 * Service for fetching fuzzy rules information from the BFF
 */
export class FuzzyRulesService extends BaseApiService {

  /**
   * Fetches the list of all fuzzy rules with their names and descriptions.
   * This data is cached on the BFF for 24 hours, so it's safe to call once per session.
   */
  async getFuzzyRules(): Promise<FuzzyRuleSummary[]> {
    return this.request<FuzzyRuleSummary[]>('/system/fuzzy-rules');
  }
}

// Singleton instance
export const fuzzyRulesService = new FuzzyRulesService();
