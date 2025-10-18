/**
 * React Native entry point for @hydroespinaca/shared
 * Includes all features including native-specific dependencies
 */

// Export all stores
export {
  useAuthStore,
  useSensorStore,
  useAlertStore,
  useActuatorStore,
  useVariableStore,
  useReadingsStore,
  useFuzzyStore,
  useDashboardStore,
  CHART_VARIABLES
} from './store';

// Export store-specific types
export type {
  SensorData,
  MetricData,
  SystemComponent,
  IndividualSensorData,
  SensorState,
  ActuadorData,
  VariableData,
  SensorSummary,
  IndividualReading,
  FuzzySystemState,
  TimeSeriesDataPoint,
  ScatterDataPoint,
  ChartVariable
} from './store';

// Export all general types
export * from './types';
export * from './types/sensorTypes';

// Export all hooks (including native-specific)
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { useNativeAuth } from './hooks/useNativeAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';

// Export all utilities (including native storage)
export * from './utils/formatters';
export * from './utils/validators';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
export { SessionStorage, secureStorage } from './utils/secureStorage.native';
export { StorageUtils, StorageKeys } from './utils/storage';

// Export design tokens
export * from './tokens';

// Export API utilities
export * from './api/placeholders';
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';
export { SystemStatusService, systemStatusService } from './api/systemStatusService';
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

// Export components
export * from './components/WelcomeMessage';

// Export native icons (uses react-native-svg)
export { Icon, svgPaths } from './icons/index.native';
export type { IconProps, IconComponent, IconName } from './icons';

// Export navigation config
export * from './config/navigation';

// Export drawer menu config
export * from './config/drawerMenu';
