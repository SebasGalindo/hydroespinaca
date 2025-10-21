/**
 * Tipos para el servicio de clima usando OpenWeather API v2.5 (free tier)
 * Documentación: https://openweathermap.org/current
 */

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
