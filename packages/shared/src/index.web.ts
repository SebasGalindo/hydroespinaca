/**
 * Web-only entry point for @hydroespinaca/shared
 * Excludes React Native specific dependencies
 */

// Export all stores (platform agnostic)
export { useAuthStore, setAuthStoreRedirectCallback } from './store';
export { useBiStore } from './store';
export { useFuzzyStore } from './store';
export { useWeatherStore } from './store';
export { useNotificationStore } from './store';
export { useChatStore } from './store';

// Export store-specific types
export type { User } from './store';

// Export all general types (platform agnostic)
export * from './types';

// Export theme tokens (platform agnostic)
export * from './theme';

// Export icons
export { Icon } from './icons/Icon';
export type { IconName, IconProps } from './icons/types';

// Export web-compatible hooks only
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';
// Note: useNativeAuth is excluded for web

// Export web-compatible utilities (excludes native storage)
export * from './utils/formatters';
export { formatRelativeDate, formatRelativeTime, formatDateTimestamp } from './utils/dateHelpers';
export { isUserAdmin } from './utils/authHelpers';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
export { setAuthCallbacks, authFetch } from './utils/authFetch';
export {
  getAlertConfig,
  calculateVariableStatus,
  calculateTrend,
  requiresArtificialLight,
  VARIABLE_ALERT_CONFIG,
} from './utils/variableAlerts';
export type {
  AlertDirection,
  VariableStatus,
  TrendDirection,
  VariableAlertConfig,
} from './utils/variableAlerts';
// Note: SessionStorage and secureStorage excluded - web should use browser APIs

// Export API utilities (platform agnostic)
export { BaseApiService, ApiServiceError } from './api/BaseApiService';
export type { RequestOptions } from './api/BaseApiService';
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';
export { SystemStatusService, systemStatusService } from './api/systemStatusService';
export { FuzzyRulesService, fuzzyRulesService } from './api/fuzzyRulesService';
export type { FuzzyRuleSummary } from './types/fuzzyRules';
export { WeatherService, weatherService } from './api/weatherService';
export type {
  WeatherSummary,
  ForecastCurrent,
  HourlyForecast,
  DailyForecast,
  GovernmentAlert,
  ForecastResponse,
  AlertType,
  AlertThreshold,
  WeatherAlertConfig,
  UpdateAlertConfigRequest,
  SeedAlertConfigRequest,
  NotifiedUser,
  WeatherAlert,
  AlertFilterParams,
  AlertSeverity,
} from './types/weather';
export {
  ALERT_TYPE_LABELS,
  ALERT_TYPE_ICONS,
  ALERT_SEVERITY_COLORS,
} from './types/weather';
export { NotificationApiService, notificationService } from './api/notificationService';
export type {
  NotificationChannel,
  ChannelPreference,
  DailySummaryConfig,
  WeatherAlertSubscription,
  QuietHoursConfig,
  NotificationPreferences,
  UpdatePreferencesRequest,
  PushSubscriptionInfo,
  RegisterPushRequest,
  NotificationLogEntry,
  HistoryParams,
  SendMultiChannelRequest,
  ChannelResult,
  SendMultiChannelResponse,
} from './types/notification';
export {
  CHANNEL_LABELS,
  CHANNEL_ICONS,
} from './types/notification';
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
  CostConfigVersion,
  CreateCostConfigVersionRequest,
  ManualConsumptionEntry,
  CreateManualConsumptionEntryRequest,
  ConsumptionType,
  BiSummary,
  ProductionRecord,
  CreateProductionRecordRequest,
  OperationalCostRequest,
  OperationalCostResponse,
  CostConfigPeriodUsed,
  ActuatorOperationalCostItem,
  ProfitabilityRequest,
  ProfitabilityResponse,
  ProductionInfo,
  ExpensesInfo,
  RevenueInfo,
} from './types/bi';
export {
  CONSUMPTION_TYPE_LABELS,
  CONSUMPTION_TYPE_UNITS,
  CONSUMPTION_TYPE_ICONS,
} from './types/bi';
export type {
  FuzzySystem,
  FuzzySystemDetail,
  FuzzyVariable,
  FuzzyTerm,
  FuzzyRule,
  FuzzySystemStatus,
  MembershipFunction,
  MembershipFunctionType,
  OperatorsConfig,
  RuleCondition,
  RuleConsequent,
  CloneFuzzySystemRequest,
  CreateFuzzySystemRequest,
  UpdateFuzzySystemRequest,
  UpdateFuzzySystemStatusRequest,
  CreateFuzzyVariableRequest,
  UpdateFuzzyVariableRequest,
  CreateFuzzyTermRequest,
  UpdateFuzzyTermRequest,
  CreateFuzzyRuleRequest,
  UpdateFuzzyRuleRequest,
  SimulateFuzzySystemRequest,
  SimulateFuzzySystemResponse,
  SimulateInput,
  SimulateRuleActivation,
  SimulateOutputValue,
  SimulateOutput,
  FuzzySystemExport,
  VariableType,
  ActuatorType,
  LogicalOperator,
  RuleConnector,
  AggregationMethod,
} from './types/fuzzy';
export {
  FUZZY_STATUS_LABELS,
  FUZZY_STATUS_COLORS,
  FUZZY_STATUS_ICONS,
  VARIABLE_TYPE_LABELS,
  VARIABLE_TYPE_ICONS,
  MEMBERSHIP_FUNCTION_LABELS,
  MF_PARAM_COUNTS,
  MF_PARAM_LABELS,
  DEFUZZIFICATION_METHODS,
  AND_METHODS,
  OR_METHODS,
  AGGREGATION_METHODS,
  NOT_METHODS,
} from './types/fuzzy';
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

// Chat (useChatSSE excluded — web uses ReadableStream via useChatStream in apps/web)
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
