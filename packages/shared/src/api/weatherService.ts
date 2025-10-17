import type { WeatherSummary } from '../types/weather';
import { getApiUrl } from '../utils/apiConfig';

/**
 * Servicio para obtener información meteorológica del BFF
 * El BFF se encarga de comunicarse con OpenWeather API v2.5 y cachear los datos
 */
export class WeatherService {
  private baseUrl: string;

  constructor() {
    // Use centralized API URL configuration
    this.baseUrl = getApiUrl();
  }

  /**
   * Obtiene el clima actual de Mosquera, Cundinamarca
   * Los datos vienen del BFF que implementa cache y se actualiza cada hora
   */
  async getWeather(): Promise<WeatherSummary> {
    try {
      const response = await fetch(`${this.baseUrl}/weather`, {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch weather data: ${response.statusText}`);
      }

      return response.json();
    } catch (error) {
      console.error('Error fetching weather data from BFF:', error);
      throw error;
    }
  }
}

// Singleton instance
export const weatherService = new WeatherService();
