'use client';

import React from 'react';
import type { WeatherSummary } from '@hydroespinaca/shared';

interface WeatherCardProps {
  weather: WeatherSummary | null;
  isLoading?: boolean;
  error?: string | null;
}

const WeatherCard: React.FC<WeatherCardProps> = ({ weather, isLoading = false, error = null }) => {
  /**
   * Obtiene el emoji del clima según el código de icono de OpenWeather
   */
  const getWeatherEmoji = (icon: string): string => {
    const iconMap: { [key: string]: string } = {
      '01d': '☀️', // clear sky day
      '01n': '🌙', // clear sky night
      '02d': '🌤️', // few clouds day
      '02n': '☁️', // few clouds night
      '03d': '☁️', // scattered clouds
      '03n': '☁️',
      '04d': '☁️', // broken clouds
      '04n': '☁️',
      '09d': '🌧️', // shower rain
      '09n': '🌧️',
      '10d': '🌦️', // rain day
      '10n': '🌧️', // rain night
      '11d': '⛈️', // thunderstorm
      '11n': '⛈️',
      '13d': '❄️', // snow
      '13n': '❄️',
      '50d': '🌫️', // mist
      '50n': '🌫️',
    };

    return iconMap[icon] || '🌤️';
  };

  /**
   * Formatea hora desde ISO string a formato legible
   */
  const formatTime = (isoString: string): string => {
    try {
      const date = new Date(isoString);
      return date.toLocaleTimeString('es-CO', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: true
      });
    } catch {
      return 'N/A';
    }
  };

  if (error) {
    return (
      <article className="w-full p-6 bg-gradient-to-br from-red-50 to-red-100 rounded-xl shadow-md border border-red-200">
        <header className="mb-3">
          <h3 className="text-lg font-semibold text-red-800 font-inter">
            Clima en Mosquera, Cundinamarca
          </h3>
        </header>
        <div className="text-center py-4">
          <p className="text-sm text-red-600 font-inter">{error}</p>
        </div>
      </article>
    );
  }

  if (isLoading || !weather) {
    return (
      <article className="w-full p-6 bg-gradient-to-br from-blue-50 to-cyan-50 rounded-xl shadow-md">
        <header className="mb-3">
          <h3 className="text-lg font-semibold text-gray-700 font-inter">
            Clima en Mosquera, Cundinamarca
          </h3>
        </header>
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-4 border-blue-600 mx-auto"></div>
          <p className="text-sm text-gray-500 mt-3 font-inter">Cargando datos del clima...</p>
        </div>
      </article>
    );
  }

  return (
    <article className="w-full p-6 bg-gradient-to-br from-sky-50 via-blue-50 to-cyan-50 rounded-xl shadow-lg border border-sky-200">
      {/* Header con ubicación y emoji principal */}
      <header className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-xl font-bold text-gray-800 font-inter">
            Clima en Mosquera, Cundinamarca
          </h3>
          <p className="text-sm text-gray-600 font-inter capitalize mt-1">
            {getWeatherEmoji(weather.icon)} {weather.description}
          </p>
        </div>
        <span className="text-6xl">{getWeatherEmoji(weather.icon)}</span>
      </header>

      {/* Temperatura principal */}
      <div className="mb-6 text-center">
        <div className="flex items-center justify-center gap-2">
          <span className="text-6xl font-bold text-gray-900 font-inter">
            {weather.temperature.toFixed(1)}°C
          </span>
        </div>
        <p className="text-lg text-gray-600 font-inter mt-2">
          Sensación térmica: {weather.feelsLike.toFixed(1)}°C
        </p>
      </div>

      {/* Grid de datos del clima */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="flex items-center gap-3 p-3 bg-white/70 backdrop-blur-sm rounded-lg shadow-sm">
          <span className="text-2xl">💧</span>
          <div>
            <p className="text-xs text-gray-600 font-inter">Humedad</p>
            <p className="text-lg font-semibold text-gray-900 font-inter">{weather.humidity}%</p>
          </div>
        </div>

        <div className="flex items-center gap-3 p-3 bg-white/70 backdrop-blur-sm rounded-lg shadow-sm">
          <span className="text-2xl">💨</span>
          <div>
            <p className="text-xs text-gray-600 font-inter">Viento</p>
            <p className="text-lg font-semibold text-gray-900 font-inter">{weather.windSpeed.toFixed(1)} m/s</p>
          </div>
        </div>

        <div className="flex items-center gap-3 p-3 bg-white/70 backdrop-blur-sm rounded-lg shadow-sm">
          <span className="text-2xl">☁️</span>
          <div>
            <p className="text-xs text-gray-600 font-inter">Nubosidad</p>
            <p className="text-lg font-semibold text-gray-900 font-inter">{weather.cloudiness}%</p>
          </div>
        </div>

        <div className="flex items-center gap-3 p-3 bg-white/70 backdrop-blur-sm rounded-lg shadow-sm">
          <span className="text-2xl">🌧️</span>
          <div>
            <p className="text-xs text-gray-600 font-inter">Lluvia última hora</p>
            <p className="text-lg font-semibold text-gray-900 font-inter">
              {weather.rain1h !== null ? `${weather.rain1h.toFixed(1)} mm` : '0.0 mm'}
            </p>
          </div>
        </div>
      </div>

      {/* Amanecer y Atardecer */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <div className="flex items-center gap-3 p-4 bg-gradient-to-br from-amber-100 to-orange-100 rounded-lg shadow-sm">
          <span className="text-3xl">🌅</span>
          <div>
            <p className="text-xs text-amber-800 font-inter font-semibold">Amanecer</p>
            <p className="text-lg font-bold text-amber-900 font-inter">{formatTime(weather.sunrise)}</p>
          </div>
        </div>

        <div className="flex items-center gap-3 p-4 bg-gradient-to-br from-indigo-100 to-purple-100 rounded-lg shadow-sm">
          <span className="text-3xl">🌇</span>
          <div>
            <p className="text-xs text-indigo-800 font-inter font-semibold">Atardecer</p>
            <p className="text-lg font-bold text-indigo-900 font-inter">{formatTime(weather.sunset)}</p>
          </div>
        </div>
      </div>

      {/* Footer con última actualización */}
      <footer className="pt-4 border-t border-sky-200">
        <div className="flex items-center justify-center gap-2 text-sm text-gray-600 font-inter">
          <span>⏱</span>
          <span>Actualizado: {formatTime(weather.lastUpdate)}</span>
        </div>
      </footer>
    </article>
  );
};

export default WeatherCard;
