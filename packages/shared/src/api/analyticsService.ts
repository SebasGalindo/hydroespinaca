// Analytics API Service — extends BaseApiService for DRY request handling
import { BaseApiService, ApiServiceError } from './BaseApiService';

// ==================== Request Types ====================

export interface EnvironmentalAnalyticsRequest {
  startDate: string; // ISO 8601 format
  endDate: string;   // ISO 8601 format
  view: 'hourly' | 'daily' | 'weekly' | 'monthly';
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
  variableName: string; // e.g., "pH", "Conductividad", "Temperatura", etc.
  summary: AggregateSummary;
  trend: AggregateTrendPoint[];
  variability: AggregateVariabilityPoint[];
}

export interface EnvironmentalAggregateResponse {
  variables: EnvironmentalVariableAggregate[];
}

// ==================== Actuator Analytics Types ====================

export interface ActuatorAnalyticsRequest {
  startDate: string; // ISO 8601 format
  endDate: string;   // ISO 8601 format
  view: 'hourly' | 'daily' | 'weekly' | 'monthly';
}

export interface ActuatorTimelineItem {
  timestamp: string; // ISO 8601 format
  actuatorCode: string;
  totalDurationSeconds: number;
  activationCount: number;
}

export interface ActuatorTotalDurationItem {
  actuatorCode: string;
  totalDurationSeconds: number;
  activationCount: number;
}

export interface ActuatorActiveTimeProportionItem {
  actuatorCode: string;
  percentage: number;
}

export interface ActuatorAnalyticsResponse {
  timeline: ActuatorTimelineItem[];
  totalDurationByActuator: ActuatorTotalDurationItem[];
  activeTimeProportion: ActuatorActiveTimeProportionItem[];
}

// ==================== Error Alias (backward-compatible) ====================

/** @deprecated Use `ApiServiceError` instead — kept for backward compatibility */
export const AnalyticsApiError = ApiServiceError;
export type AnalyticsApiError = ApiServiceError;

// ==================== Service Class ====================

export class AnalyticsApiService extends BaseApiService {

  /**
   * Get environmental aggregates data
   */
  async getEnvironmentalAggregates(
    request: EnvironmentalAnalyticsRequest
  ): Promise<EnvironmentalAggregateResponse> {
    const data = await this.request<EnvironmentalAggregateResponse>('/analytics/environmental', {
      method: 'POST',
      body: request,
    });

    // Validate response structure
    if (!data || !Array.isArray(data.variables)) {
      throw new ApiServiceError(
        500,
        'Invalid response structure from server',
        'INVALID_RESPONSE'
      );
    }

    return data;
  }

  /**
   * Get actuator analytics data
   */
  async getActuatorAnalytics(
    request: ActuatorAnalyticsRequest
  ): Promise<ActuatorAnalyticsResponse> {
    const data = await this.request<ActuatorAnalyticsResponse>('/analytics/actuators', {
      method: 'POST',
      body: request,
    });

    // Validate response structure
    if (
      !data ||
      !Array.isArray(data.timeline) ||
      !Array.isArray(data.totalDurationByActuator) ||
      !Array.isArray(data.activeTimeProportion)
    ) {
      throw new ApiServiceError(
        500,
        'Invalid response structure from server',
        'INVALID_RESPONSE'
      );
    }

    return data;
  }
}

// Export a singleton instance
export const analyticsService = new AnalyticsApiService();
