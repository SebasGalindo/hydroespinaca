// API services exports

// Base class & unified error
export { BaseApiService, ApiServiceError } from './BaseApiService';
export type { RequestOptions } from './BaseApiService';

// Auth
export { AuthApiService, authService, ApiError } from './authService';
export type { ApiResponse } from './authService';

// System
export { SystemStatusService, systemStatusService } from './systemStatusService';
export { FuzzyRulesService, fuzzyRulesService } from './fuzzyRulesService';
export { WeatherService, weatherService } from './weatherService';

// Analytics
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

// Admin
export { AdminApiService, adminService } from './adminService';

// BI
export { BiApiService, biService, BiApiError } from './biService';

// Fuzzy
export { FuzzyApiService, fuzzyService, FuzzyApiError } from './fuzzyService';

// Notifications
export { NotificationApiService, notificationService } from './notificationService';
