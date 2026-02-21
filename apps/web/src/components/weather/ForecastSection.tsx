'use client';

import React from 'react';
import type { DailyForecast } from '@hydroespinaca/shared';

interface ForecastSectionProps {
  daily: DailyForecast[];
  isLoading: boolean;
  error: string | null;
}

const WEATHER_EMOJI: Record<string, string> = {
  Clear: '☀️', Clouds: '☁️', Rain: '🌧️', Drizzle: '🌦️',
  Thunderstorm: '⛈️', Snow: '❄️', Mist: '🌫️', Fog: '🌫️',
  Haze: '🌫️', Smoke: '🌫️', Dust: '🌫️', Sand: '🌫️',
  Tornado: '🌪️', Squall: '💨',
};

const ForecastSection: React.FC<ForecastSectionProps> = ({ daily, isLoading, error }) => {
  const formatDate = (iso: string) => {
    const d = new Date(iso);
    return d.toLocaleDateString('es-CO', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'America/Bogota' });
  };

  if (error) {
    return (
      <div className="p-6 bg-red-50 border border-red-200 rounded-xl">
        <p className="text-sm text-red-700">Error al cargar pronóstico: {error}</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="bg-white/60 rounded-xl p-4 animate-pulse h-40" />
        ))}
      </div>
    );
  }

  if (!daily.length) {
    return (
      <div className="p-6 bg-gray-50 border border-gray-200 rounded-xl text-center">
        <p className="text-sm text-gray-500">No hay datos de pronóstico disponibles.</p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
      {daily.map((day, idx) => {
        const emoji = WEATHER_EMOJI[day.main] || '🌤️';
        const isToday = idx === 0;
        const popPercent = Math.round(day.pop * 100);
        return (
          <div
            key={day.dateTime}
            className={`rounded-xl p-4 text-center transition-all hover:scale-[1.03] hover:shadow-lg
              ${isToday
                ? 'bg-gradient-to-b from-green-100 to-green-50 border-2 border-green-300 shadow-md'
                : 'bg-white border border-gray-200 shadow-sm'}`}
          >
            <p className={`text-xs font-semibold mb-1 ${isToday ? 'text-green-700' : 'text-gray-500'}`}>
              {isToday ? 'Hoy' : formatDate(day.dateTime)}
            </p>
            <span className="text-3xl block mb-2">{emoji}</span>
            <p className="text-sm font-bold text-gray-800">
              {Math.round(day.tempMax)}° / <span className="text-gray-500">{Math.round(day.tempMin)}°</span>
            </p>
            <p className="text-xs text-gray-500 capitalize mt-1 truncate" title={day.description}>
              {day.description}
            </p>
            {popPercent > 0 && (
              <p className="text-xs text-blue-600 mt-1">💧 {popPercent}%</p>
            )}
            {day.uvi >= 6 && (
              <p className="text-xs text-orange-600 mt-0.5">☀️ UV {Math.round(day.uvi)}</p>
            )}
          </div>
        );
      })}
    </div>
  );
};

export default ForecastSection;
