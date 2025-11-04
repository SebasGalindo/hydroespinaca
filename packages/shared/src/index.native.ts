/**
 * React Native entry point for @hydroespinaca/shared
 * Includes all features including native-specific dependencies
 */

// Export all stores
export { useAuthStore } from './store';

// Export store-specific types
export type { User } from './store';

// Export all general types
export * from './types';

// Export theme tokens
export * from './theme';

// Export icons
export { Icon } from './icons';
export type { IconName, IconProps } from './icons/types';

// Export all hooks (including native-specific)
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { useNativeAuth } from './hooks/useNativeAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';

// Export all utilities (including native storage)
export * from './utils/formatters';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
export { SessionStorage, secureStorage } from './utils/secureStorage.native';
export { StorageUtils, StorageKeys } from './utils/storage';
export { setAuthCallbacks, authFetch } from './utils/authFetch';
export {
  getAlertConfig,
  calculateVariableStatus,
  calculateTrend,
  VARIABLE_ALERT_CONFIG,
} from './utils/variableAlerts';
export type {
  AlertDirection,
  VariableStatus,
  TrendDirection,
  VariableAlertConfig,
} from './utils/variableAlerts';

// Export API utilities
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';
export { SystemStatusService, systemStatusService } from './api/systemStatusService';
export { FuzzyRulesService, fuzzyRulesService } from './api/fuzzyRulesService';
export type { FuzzyRuleSummary } from './types/fuzzyRules';
export { WeatherService, weatherService } from './api/weatherService';
export type { WeatherSummary } from './types/weather';
export { AnalyticsApiService, analyticsService, AnalyticsApiError } from './api/analyticsService';
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
} from './api/analyticsService';
export { AdminApiService, adminService } from './api/adminService';
export type {
  UserCreateDto,
  UserUpdateDto,
  UserResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  RoleResponseDto,
  CreatePermissionRequestDto,
  UpdatePermissionRequestDto,
  PermissionResponseDto,
  GroupedPermissionResponseDto,
  SessionMonitorDto,
  UserSessionsDto,
} from './types/admin';
export type {
  SystemStatusResponse,
  ReadingItem,
  ReadingsSnapshot,
  QueueItem,
  JobStatus,
  Stats,
  InternalRoutine,
} from './types/systemStatus';
