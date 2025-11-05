import type { WeatherSummary } from '../types/weather';
import { getApiUrl } from '../utils/apiConfig';
import { authFetch } from '../utils/authFetch';

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
   * Uses authFetch for automatic 401 handling and redirect
   */
  async getWeather(): Promise<WeatherSummary> {
    try {
      const response = await authFetch(`${this.baseUrl}/weather`, {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch weather data: ${response.statusText}`);
      }

      return response.json() as Promise<WeatherSummary>;
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Error fetching weather data from BFF:', error);
      }
      throw error;
    }
  }
}

// Singleton instance
export const weatherService = new WeatherService();
