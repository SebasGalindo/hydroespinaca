// Export all stores
export { useAuthStore, setAuthStoreRedirectCallback } from './store';
export { useBiStore } from './store';
export { useFuzzyStore } from './store';

// Export store-specific types
export type { User } from './store';

// Export all general types
export * from './types';

// Export theme tokens
export * from './theme';

// Export icons
export { Icon } from './icons/Icon';
export type { IconName, IconProps } from './icons/types';

// Export hooks
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { useNativeAuth } from './hooks/useNativeAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';

// Export utilities
export * from './utils/formatters';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
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
  ManualConsumptionCostDetail,
  ManualConsumptionEntryItem,
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