// Weather API Service — extends BaseApiService for DRY request handling
import { BaseApiService } from './BaseApiService';
import type { WeatherSummary, ForecastResponse, DailyForecast, WeatherAlertConfig, UpdateAlertConfigRequest, SeedAlertConfigRequest, WeatherAlert, AlertFilterParams } from '../types/weather';

/**
 * Servicio para obtener información meteorológica del BFF.
 * Proxy a weather-service (One Call API 3.0) vía BFF.
 */
export class WeatherService extends BaseApiService {

  /** Clima actual (backward-compatible v2.5) */
  async getWeather(): Promise<WeatherSummary> {
    return this.request<WeatherSummary>('/weather');
  }

  /** Pronóstico completo: current + hourly (48h) + daily (8d) + gov alerts */
  async getForecast(): Promise<ForecastResponse> {
    return this.request<ForecastResponse>('/weather/forecast');
  }

  /** Solo pronóstico diario (8 días) */
  async getDailyForecast(): Promise<DailyForecast[]> {
    return this.request<DailyForecast[]>('/weather/forecast/daily');
  }

  /** Configuración de alertas para un fuzzy system */
  async getAlertConfig(fuzzySystemId: string): Promise<WeatherAlertConfig | null> {
    return this.request<WeatherAlertConfig | null>(`/weather/alerts/config/${fuzzySystemId}`, { nullOn404: true });
  }

  /** Actualizar umbrales de alerta */
  async updateAlertConfig(fuzzySystemId: string, config: UpdateAlertConfigRequest): Promise<void> {
    await this.request<void>(`/weather/alerts/config/${fuzzySystemId}`, {
      method: 'PUT',
      body: config,
    });
  }

  /** Inicializar config por defecto para un sistema fuzzy */
  async seedAlertConfig(fuzzySystemId: string, req: SeedAlertConfigRequest): Promise<WeatherAlertConfig> {
    return this.request<WeatherAlertConfig>(`/weather/alerts/config/${fuzzySystemId}/seed`, {
      method: 'POST',
      body: req,
    });
  }

  /** Obtener alertas generadas (filtrable) */
  async getAlerts(params?: AlertFilterParams): Promise<WeatherAlert[]> {
    const qs = new URLSearchParams();
    if (params?.fuzzySystemId) qs.set('fuzzySystemId', params.fuzzySystemId);
    if (params?.userId) qs.set('userId', params.userId);
    if (params?.from) qs.set('from', params.from);
    if (params?.to) qs.set('to', params.to);
    if (params?.unreadOnly) qs.set('unreadOnly', 'true');
    const query = qs.toString();
    return this.request<WeatherAlert[]>(`/weather/alerts${query ? `?${query}` : ''}`);
  }

  /** Marcar alerta como leída */
  async markAlertRead(alertId: string, userId: string): Promise<void> {
    await this.request<void>(`/weather/alerts/${alertId}/read?userId=${encodeURIComponent(userId)}`, {
      method: 'PATCH',
    });
  }
}

// Singleton instance
export const weatherService = new WeatherService();
