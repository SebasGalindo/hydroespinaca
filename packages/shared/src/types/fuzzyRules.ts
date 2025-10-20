/**
 * Type definitions for fuzzy logic rules
 */

/**
 * Summary information for a fuzzy logic rule
 */
export interface FuzzyRuleSummary {
  /**
   * Unique identifier of the fuzzy rule
   */
  id: string;

  /**
   * Human-readable name of the fuzzy rule
   */
  name: string;

  /**
   * Detailed description explaining the rule's logic and purpose
   */
  description: string;
}
