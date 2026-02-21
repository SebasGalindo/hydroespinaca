/**
 * Tipos para el sistema de notificaciones multi-canal.
 * Sincronizado con NotificationDtos.cs del BFF.
 */

// ==================== Channels ====================

export type NotificationChannel = 'email' | 'push' | 'web_push' | 'whatsapp';

export const CHANNEL_LABELS: Record<NotificationChannel, string> = {
  email: 'Correo Electrónico',
  push: 'Push Móvil (Expo)',
  web_push: 'Notificaciones del Navegador',
  whatsapp: 'WhatsApp',
};

export const CHANNEL_ICONS: Record<NotificationChannel, string> = {
  email: '📧',
  push: '📱',
  web_push: '🖥️',
  whatsapp: '💬',
};

// ==================== Preferences ====================

export interface ChannelPreference {
  channel: NotificationChannel;
  enabled: boolean;
  target?: string | null;
}

export interface DailySummaryConfig {
  enabled: boolean;
  hour: number;
  minute: number;
  channels: string[];
  includeFuzzyRules: boolean;
  includeSensorAverages: boolean;
  includeActuatorRuntime: boolean;
  includeWeatherForecast: boolean;
}

export interface WeatherAlertSubscription {
  enabled: boolean;
  fuzzySystemId?: string | null;
  alertTypes: string[];
}

export interface QuietHoursConfig {
  enabled: boolean;
  startHour: number;
  endHour: number;
}

export interface NotificationPreferences {
  id?: string;
  userId: string;
  channels: ChannelPreference[];
  dailySummary?: DailySummaryConfig | null;
  weatherAlertsSubscription?: WeatherAlertSubscription | null;
  quietHours?: QuietHoursConfig | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface UpdatePreferencesRequest {
  channels: ChannelPreference[];
  dailySummary: DailySummaryConfig;
  weatherAlertsSubscription: WeatherAlertSubscription;
  quietHours?: QuietHoursConfig | null;
}

// ==================== Push Subscriptions ====================

export interface PushSubscriptionInfo {
  id?: string;
  userId: string;
  platform: string;
  token: string;
  deviceName?: string | null;
  isActive: boolean;
  failureCount: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface RegisterPushRequest {
  userId: string;
  platform: string;
  token: string;
  deviceName?: string | null;
}

// ==================== Notification Log ====================

export interface NotificationLogEntry {
  id?: string;
  correlationId?: string | null;
  userId: string;
  channel: string;
  templateKey?: string | null;
  title?: string | null;
  status: string;
  provider?: string | null;
  providerMessageId?: string | null;
  error?: string | null;
  createdAt?: string;
  sentAt?: string;
}

export interface HistoryParams {
  channel?: string;
  from?: string;
  to?: string;
  limit?: number;
}

// ==================== Multi-Channel Send ====================

export interface SendMultiChannelRequest {
  userId: string;
  templateKey: string;
  title: string;
  body: string;
  data?: Record<string, string>;
}

export interface ChannelResult {
  channel: string;
  success: boolean;
  provider?: string | null;
  error?: string | null;
}

export interface SendMultiChannelResponse {
  correlationId?: string | null;
  results: ChannelResult[];
}
