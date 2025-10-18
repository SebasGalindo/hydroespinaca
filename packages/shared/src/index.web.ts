/**
 * Web-only entry point for @hydroespinaca/shared
 * Excludes React Native specific dependencies
 */

// Export all stores (platform agnostic)
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

// Export all general types (platform agnostic)
export * from './types';
export * from './types/sensorTypes';

// Export web-compatible hooks only
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';
// Note: useNativeAuth is excluded for web

// Export web-compatible utilities (excludes native storage)
export * from './utils/formatters';
export * from './utils/validators';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
export { setAuthCallbacks, authFetch } from './utils/authFetch';
// Note: SessionStorage and secureStorage excluded - web should use browser APIs

// Export design tokens (platform agnostic)
export * from './tokens';

// Export API utilities (platform agnostic)
export * from './api/placeholders';
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';
export { SystemStatusService, systemStatusService } from './api/systemStatusService';
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
} from './api/analyticsService';

// Export components (platform agnostic)
export * from './components/WelcomeMessage';

// Export web icons (uses standard SVG)
export { Icon, svgPaths } from './icons/index.web';
export type { IconProps, IconComponent, IconName } from './icons';

// Export navigation config (platform agnostic)
export * from './config/navigation';

// Export drawer menu config (platform agnostic)
export * from './config/drawerMenu';
