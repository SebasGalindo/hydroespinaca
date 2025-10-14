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

// Export all general types (including User and Alert from types)
export * from './types';
export * from './types/sensorTypes';

// Export hooks
export * from './hooks/useLoginForm';
export { useAuth } from './hooks/useAuth';
export { useWebAuth } from './hooks/useWebAuth';
export { useNativeAuth } from './hooks/useNativeAuth';
export { ProtectedRoute } from './hooks/ProtectedRoute';

// Export utilities
export * from './utils/formatters';
export * from './utils/validators';
export { getApiUrl, detectPlatform, isDevelopmentMode } from './utils/apiConfig';
export { SessionStorage, secureStorage } from './utils/secureStorage';

// Export design tokens (includes colors, typography, spacing)
export * from './tokens';

// Export API utilities
export * from './api/placeholders';
export { AuthApiService, authService, ApiError } from './api/authService';
export type { ApiResponse } from './api/authService';

// Export components
export * from './components/WelcomeMessage';

// Export icons
export { Icon, svgPaths } from './icons';
export type { IconProps, IconComponent, IconName } from './icons';

// Export navigation config
export * from './config/navigation';

// Export drawer menu config
export * from './config/drawerMenu';