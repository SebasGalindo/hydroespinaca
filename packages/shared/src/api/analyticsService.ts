// Analytics API Service
import { getApiUrl } from '../utils/apiConfig';

// ==================== Request Types ====================

export interface GetEnvironmentalAggregatesRequest {
  startDate: string; // ISO 8601 format
  endDate: string;   // ISO 8601 format
  view: 'daily' | 'weekly' | 'monthly';
}

// ==================== Response Types ====================

export interface AggregateSummary {
  min: number;
  max: number;
  avg: number;
  count: number;
}

export interface AggregateTrendPoint {
  timestamp: string; // ISO 8601 format
  avg: number;
}

export interface AggregateVariabilityPoint {
  timestamp: string; // ISO 8601 format
  min: number;
  q1: number;
  median: number;
  q3: number;
  max: number;
  count: number;
}

export interface EnvironmentalVariableAggregate {
  variableCode: string; // e.g., "PH", "EC", "TEMP", etc.
  summary: AggregateSummary;
  trend: AggregateTrendPoint[];
  variability: AggregateVariabilityPoint[];
}

export interface EnvironmentalAggregateResponse {
  variables: EnvironmentalVariableAggregate[];
}

// ==================== Error Types ====================

export class AnalyticsApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string
  ) {
    super(message);
    this.name = 'AnalyticsApiError';
  }
}

// ==================== Service Class ====================

export class AnalyticsApiService {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || getApiUrl();
  }

  /**
   * Get environmental aggregates data
   * @param request Request parameters (startDate, endDate, view)
   * @returns Environmental aggregate response
   * @throws AnalyticsApiError if request fails
   */
  async getEnvironmentalAggregates(
    request: GetEnvironmentalAggregatesRequest
  ): Promise<EnvironmentalAggregateResponse> {
    const url = `${this.baseUrl}/analytics/environmental`;

    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
        credentials: 'include', // Include cookies for web authentication
      });

      const isJson = response.headers.get('content-type')?.includes('application/json');
      const data = isJson ? await response.json() : null;

      if (!response.ok) {
        const errorMessage = data?.message || data?.error || `HTTP ${response.status}`;
        const errorCode = data?.code;
        throw new AnalyticsApiError(response.status, errorMessage, errorCode);
      }

      // Validate response structure
      if (!data || !Array.isArray(data.variables)) {
        throw new AnalyticsApiError(
          500,
          'Invalid response structure from server',
          'INVALID_RESPONSE'
        );
      }

      return data as EnvironmentalAggregateResponse;
    } catch (error) {
      if (error instanceof AnalyticsApiError) {
        throw error;
      }

      // Network or other errors
      throw new AnalyticsApiError(
        0,
        error instanceof Error ? error.message : 'Network error',
        'NETWORK_ERROR'
      );
    }
  }
}

// Export a singleton instance
export const analyticsService = new AnalyticsApiService();
