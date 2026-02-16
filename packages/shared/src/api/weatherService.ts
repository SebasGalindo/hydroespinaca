// Weather API Service — extends BaseApiService for DRY request handling
import { BaseApiService } from './BaseApiService';
import type { WeatherSummary } from '../types/weather';

/**
 * Servicio para obtener información meteorológica del BFF
 * El BFF se encarga de comunicarse con OpenWeather API v2.5 y cachear los datos
 */
export class WeatherService extends BaseApiService {

  /**
   * Obtiene el clima actual de Mosquera, Cundinamarca
   * Los datos vienen del BFF que implementa cache y se actualiza cada hora
   */
  async getWeather(): Promise<WeatherSummary> {
    return this.request<WeatherSummary>('/weather');
  }
}

// Singleton instance
export const weatherService = new WeatherService();
