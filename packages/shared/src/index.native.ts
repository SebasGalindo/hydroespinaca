/**
 * React Native entry point for @hydroespinaca/shared
 * Includes all features including native-specific dependencies
 */

// Export all stores
export { useAuthStore, setAuthStoreRedirectCallback } from './store';
export { useBiStore } from './store';
export { useFuzzyStore } from './store';
export { useWeatherStore } from './store';
export { useNotificationStore } from './store';
export { useChatStore } from './store';

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
export { useChatSSE } from './hooks/useChatSSE';

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
export { BaseApiService, ApiServiceError } from './api/BaseApiService';
export type { RequestOptions } from './api/BaseApiService';
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';
export { SystemStatusService, systemStatusService } from './api/systemStatusService';
export { FuzzyRulesService, fuzzyRulesService } from './api/fuzzyRulesService';
export type { FuzzyRuleSummary } from './types/fuzzyRules';
export { WeatherService, weatherService } from './api/weatherService';
export type {
  WeatherSummary, ForecastResponse, ForecastCurrent, HourlyForecast,
  DailyForecast, GovernmentAlert, WeatherAlertConfig, AlertThreshold,
  UpdateAlertConfigRequest, SeedAlertConfigRequest, WeatherAlert,
  NotifiedUser, AlertFilterParams, AlertType, AlertSeverity,
} from './types/weather';
export { ALERT_TYPE_LABELS, ALERT_TYPE_ICONS, ALERT_SEVERITY_COLORS } from './types/weather';
export { NotificationApiService, notificationService } from './api/notificationService';
export type {
  NotificationChannel, ChannelPreference, DailySummaryConfig,
  WeatherAlertSubscription, QuietHoursConfig, NotificationPreferences,
  UpdatePreferencesRequest, PushSubscriptionInfo, RegisterPushRequest,
  NotificationLogEntry, HistoryParams, SendMultiChannelRequest,
  ChannelResult, SendMultiChannelResponse,
} from './types/notification';
export { CHANNEL_LABELS, CHANNEL_ICONS } from './types/notification';
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
export { BiApiService, biService, BiApiError } from './api/biService';
export { FuzzyApiService, fuzzyService, FuzzyApiError } from './api/fuzzyService';
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

// Chat
export { ChatApiService, chatService, ChatApiError } from './api/chatService';
export type {
  ChatSession,
  ChatMessage,
  ChatContextFilters,
  SendMessageRequest,
  CreateSessionRequest,
  CreateSessionResponse,
  StreamTokenEvent,
  StreamDoneEvent,
} from './types/chat';
