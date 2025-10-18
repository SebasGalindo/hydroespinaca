// API services exports
export * from './placeholders';
export * from './fuzzyPlaceholders';
export { AuthApiService, authService, ApiError } from './authService';
export type { ApiResponse } from './authService';
export { SystemStatusService, systemStatusService } from './systemStatusService';
export { AnalyticsApiService, analyticsService, AnalyticsApiError } from './analyticsService';
export type {
  EnvironmentalAnalyticsRequest,
  AggregateSummary,
  AggregateTrendPoint,
  AggregateVariabilityPoint,
  EnvironmentalVariableAggregate,
  EnvironmentalAggregateResponse,
  ActuatorAnalyticsRequest,
  ActuatorTimelineItem,
  ActuatorTotalDurationItem,
  ActuatorActiveTimeProportionItem,
  ActuatorAnalyticsResponse,
} from './analyticsService';
