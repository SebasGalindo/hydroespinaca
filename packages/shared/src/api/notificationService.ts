// Notification API Service — multi-channel notification management via BFF proxy
import { BaseApiService } from './BaseApiService';
import type {
  NotificationPreferences,
  UpdatePreferencesRequest,
  PushSubscriptionInfo,
  RegisterPushRequest,
  NotificationLogEntry,
  HistoryParams,
  SendMultiChannelRequest,
  SendMultiChannelResponse,
} from '../types/notification';

/**
 * Servicio para gestión de notificaciones multi-canal vía el BFF proxy.
 * Maneja preferencias, push subscriptions, historial y envío.
 */
export class NotificationApiService extends BaseApiService {

  // ──────────── Preferences ────────────

  /** Obtener preferencias de notificación del usuario */
  async getPreferences(userId: string): Promise<NotificationPreferences | null> {
    return this.request<NotificationPreferences | null>(
      `/notifications/preferences/${encodeURIComponent(userId)}`,
      { nullOn404: true }
    );
  }

  /** Actualizar preferencias de notificación */
  async updatePreferences(userId: string, prefs: UpdatePreferencesRequest): Promise<void> {
    await this.request<void>(`/notifications/preferences/${encodeURIComponent(userId)}`, {
      method: 'PUT',
      body: prefs,
    });
  }

  // ──────────── Push Subscriptions ────────────

  /** Registrar token push (Expo, Web Push, etc) */
  async registerPush(registration: RegisterPushRequest): Promise<void> {
    await this.request<void>('/notifications/push/register', {
      method: 'POST',
      body: registration,
    });
  }

  /** Desregistrar un token push */
  async unregisterPush(subscriptionId: string): Promise<void> {
    await this.request<void>(`/notifications/push/register/${encodeURIComponent(subscriptionId)}`, {
      method: 'DELETE',
    });
  }

  /** Listar dispositivos registrados para push */
  async getDevices(userId: string, platform?: string): Promise<PushSubscriptionInfo[]> {
    const qs = platform ? `?platform=${encodeURIComponent(platform)}` : '';
    return this.request<PushSubscriptionInfo[]>(
      `/notifications/push/subscriptions/${encodeURIComponent(userId)}${qs}`
    );
  }

  // ──────────── Notification History ────────────

  /** Obtener historial de notificaciones enviadas */
  async getHistory(userId: string, params?: HistoryParams): Promise<NotificationLogEntry[]> {
    const qs = new URLSearchParams();
    if (params?.channel) qs.set('channel', params.channel);
    if (params?.from) qs.set('from', params.from);
    if (params?.to) qs.set('to', params.to);
    if (params?.limit) qs.set('limit', String(params.limit));
    const query = qs.toString();
    return this.request<NotificationLogEntry[]>(
      `/notifications/log/${encodeURIComponent(userId)}${query ? `?${query}` : ''}`
    );
  }

  // ──────────── Send ────────────

  /** Enviar notificación multi-canal */
  async send(req: SendMultiChannelRequest): Promise<SendMultiChannelResponse> {
    return this.request<SendMultiChannelResponse>('/notifications/send', {
      method: 'POST',
      body: req,
    });
  }
}

// Singleton instance
export const notificationService = new NotificationApiService();
