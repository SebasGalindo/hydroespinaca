// API services exports
export * from './placeholders';
export * from './fuzzyPlaceholders';
export { AuthApiService, authService, ApiError } from './authService';
export type { ApiResponse } from './authService';
export { SystemStatusService, systemStatusService } from './systemStatusService';
export { AnalyticsApiService, analyticsService, AnalyticsApiError } from './analyticsService';
export type {
  GetEnvironmentalAggregatesRequest,
  AggregateSummary,
  AggregateTrendPoint,
  AggregateVariabilityPoint,
  EnvironmentalVariableAggregate,
  EnvironmentalAggregateResponse,
} from './analyticsService';
