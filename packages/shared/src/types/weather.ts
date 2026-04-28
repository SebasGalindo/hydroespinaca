/**
 * Tipos para el servicio de clima
 * - WeatherSummary: datos básicos del dashboard (v2.5 compat)
 * - Forecast*: pronóstico extendido vía One Call API 3.0
 * - WeatherAlert*: alertas meteorológicas configurables por fuzzy system
 */

// ==================== Current Weather (legacy + backward compat) ====================

/**
 * Datos del clima para el dashboard
 * Sincronizado con WeatherDto del backend
 */
export interface WeatherSummary {
  /** Temperatura actual en Celsius */
  temperature: number;
  /** Sensación térmica en Celsius */
  feelsLike: number;
  /** Humedad relativa (%) */
  humidity: number;
  /** Condición principal del clima (ej: "Clear", "Clouds", "Rain") */
  main: string;
  /** Descripción detallada del clima en español */
  description: string;
  /** Código de ícono de OpenWeather */
  icon: string;
  /** Velocidad del viento en m/s */
  windSpeed: number;
  /** Porcentaje de nubosidad (0-100%) */
  cloudiness: number;
  /** Volumen de lluvia de la última hora en mm (null si no hay datos) */
  rain1h: number | null;
  /** Hora de amanecer en formato ISO */
  sunrise: string;
  /** Hora de atardecer en formato ISO */
  sunset: string;
  /** Última actualización en formato ISO */
  lastUpdate: string;
}

// ==================== One Call 3.0 — Forecast ====================

/** Clima actual extendido (superset de WeatherSummary) */
export interface ForecastCurrent {
  temperature: number;
  feelsLike: number;
  humidity: number;
  pressure: number;
  dewPoint: number;
  uvi: number;
  cloudiness: number;
  visibility: number;
  windSpeed: number;
  windGust: number | null;
  windDeg: number;
  main: string;
  description: string;
  icon: string;
  weatherId: number;
  rain1h: number | null;
  snow1h: number | null;
  sunrise: string;
  sunset: string;
  lastUpdate: string;
}

/** Punto horario del pronóstico (48h) */
export interface HourlyForecast {
  dateTime: string;
  temperature: number;
  feelsLike: number;
  pressure: number;
  humidity: number;
  dewPoint: number;
  uvi: number;
  cloudiness: number;
  visibility: number;
  windSpeed: number;
  windGust: number | null;
  windDeg: number;
  /** Probabilidad de precipitación (0-1) */
  pop: number;
  rain1h: number | null;
  snow1h: number | null;
  main: string;
  description: string;
  icon: string;
  weatherId: number;
}

/** Pronóstico diario (8 días: hoy + 7) */
export interface DailyForecast {
  dateTime: string;
  sunrise: string;
  sunset: string;
  moonrise: string;
  moonset: string;
  moonPhase: number;
  tempMin: number;
  tempMax: number;
  tempMorn: number;
  tempDay: number;
  tempEve: number;
  tempNight: number;
  feelsLikeMorn: number;
  feelsLikeDay: number;
  feelsLikeEve: number;
  feelsLikeNight: number;
  humidity: number;
  pressure: number;
  dewPoint: number;
  cloudiness: number;
  windSpeed: number;
  windGust: number | null;
  windDeg: number;
  uvi: number;
  /** Probabilidad de precipitación (0-1) */
  pop: number;
  rain: number | null;
  snow: number | null;
  main: string;
  description: string;
  icon: string;
  weatherId: number;
  /** Resumen AI del día */
  summary: string;
}

/** Alerta gubernamental (IDEAM Colombia) */
export interface GovernmentAlert {
  senderName: string;
  event: string;
  start: string;
  end: string;
  description: string;
  tags: string[];
}

/** Respuesta completa de /weather/forecast */
export interface ForecastResponse {
  current: ForecastCurrent;
  hourly: HourlyForecast[];
  daily: DailyForecast[];
  governmentAlerts: GovernmentAlert[];
  fetchedAt: string;
  timezone: string;
  timezoneOffset: number;
}

// ==================== Weather Alert Configuration ====================

/** Tipos de alerta meteorológica */
export type AlertType =
  | 'extreme_heat'
  | 'extreme_cold'
  | 'high_humidity'
  | 'low_humidity'
  | 'heavy_rain'
  | 'thunderstorm'
  | 'high_cloudiness'
  | 'strong_wind'
  | 'extreme_uv'
  | 'government';

export const ALERT_TYPE_LABELS: Record<AlertType, string> = {
  extreme_heat: 'Calor Extremo',
  extreme_cold: 'Frío Extremo',
  high_humidity: 'Humedad Alta',
  low_humidity: 'Humedad Baja',
  heavy_rain: 'Lluvia Intensa',
  thunderstorm: 'Tormenta Eléctrica',
  high_cloudiness: 'Nubosidad Alta',
  strong_wind: 'Viento Fuerte',
  extreme_uv: 'UV Extremo',
  government: 'Alerta Gubernamental',
};

export const ALERT_TYPE_ICONS: Record<AlertType, string> = {
  extreme_heat: '🔥',
  extreme_cold: '🥶',
  high_humidity: '💧',
  low_humidity: '🏜️',
  heavy_rain: '🌧️',
  thunderstorm: '⛈️',
  high_cloudiness: '☁️',
  strong_wind: '💨',
  extreme_uv: '☀️',
  government: '🏛️',
};

export type AlertSeverity = 'info' | 'warning' | 'critical';

export const ALERT_SEVERITY_COLORS: Record<AlertSeverity, string> = {
  info: 'blue',
  warning: 'amber',
  critical: 'red',
};

/** Umbral individual de una alerta */
export interface AlertThreshold {
  type: AlertType;
  enabled: boolean;
  thresholdValue: number | null;
  comparison: string | null;
  recommendation: string;
}

/** Configuración de alertas por fuzzy system */
export interface WeatherAlertConfig {
  id: string;
  fuzzySystemId: string;
  fuzzySystemName: string;
  alerts: AlertThreshold[];
  isActive: boolean;
  maxForecastDays: number;
  allowDuplicateAlerts: boolean;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateAlertConfigRequest {
  userId: string;
  isActive: boolean;
  alerts: AlertThreshold[];
  maxForecastDays?: number;
  allowDuplicateAlerts?: boolean;
}

export interface SeedAlertConfigRequest {
  fuzzySystemName: string;
  userId: string;
}

// ==================== Weather Alerts (Generated) ====================

/** Usuario notificado de una alerta */
export interface NotifiedUser {
  userId: string;
  channels: string[];
  sentAt: string;
  isRead: boolean;
}

/** Alerta meteorológica generada */
export interface WeatherAlert {
  id: string;
  fuzzySystemId: string;
  alertType: AlertType;
  severity: AlertSeverity;
  title: string;
  message: string;
  recommendation: string;
  forecastDatetime: string;
  forecastValue: number | null;
  forecastCondition: string | null;
  governmentAlert: boolean;
  notifiedUsers: NotifiedUser[];
  createdAt: string;
}

/** Parámetros de filtro para alertas */
export interface AlertFilterParams {
  fuzzySystemId?: string;
  userId?: string;
  from?: string;
  to?: string;
  unreadOnly?: boolean;
  isRead?: boolean;
  pageSize?: number;
  page?: number;
}
