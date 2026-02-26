'use client';

import React, { useEffect, useState } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import ForecastSection from '@/components/weather/ForecastSection';
import AlertConfigSection from '@/components/weather/AlertConfigSection';
import WeatherAlertHistory from '@/components/weather/WeatherAlertHistory';
import WeatherCard from '@/components/dashboard/WeatherCard';
import { useWeatherStore, useAuthStore, weatherService } from '@hydroespinaca/shared';
import type { WeatherSummary, FuzzySystem } from '@hydroespinaca/shared';
import { fuzzyService } from '@hydroespinaca/shared';

export default function ClimaPage() {
  const user = useAuthStore(s => s.user);
  const {
    forecast, forecastLoading, forecastError,
    fetchForecast, fetchDailyForecast,
    dailyForecast, dailyForecastLoading, dailyForecastError,
  } = useWeatherStore();

  // Current weather (simple endpoint)
  const [weather, setWeather] = useState<WeatherSummary | null>(null);
  const [weatherLoading, setWeatherLoading] = useState(false);
  const [weatherError, setWeatherError] = useState<string | null>(null);

  // Fuzzy systems for alert config
  const [fuzzySystems, setFuzzySystems] = useState<FuzzySystem[]>([]);
  const [selectedSystemId, setSelectedSystemId] = useState<string>('');
  const [selectedSystemName, setSelectedSystemName] = useState<string>('');

  // Fetch current weather
  useEffect(() => {
    const load = async (showLoading = true) => {
      if (showLoading) setWeatherLoading(true);
      try {
        const data = await weatherService.getWeather();
        setWeather(data);
      } catch (err: any) {
        setWeatherError(err.message || 'Error al cargar el clima');
      } finally {
        if (showLoading) setWeatherLoading(false);
      }
    };
    
    load();
    const intervalId = setInterval(() => load(false), 3 * 60 * 1000); // Auto refresh every 3 minutes
    return () => clearInterval(intervalId);
  }, []);

  // Fetch forecast
  useEffect(() => {
    fetchDailyForecast();
    fetchForecast();
    
    const intervalId = setInterval(() => {
      fetchDailyForecast();
      fetchForecast();
    }, 3 * 60 * 1000); // Auto refresh every 3 minutes
    
    return () => clearInterval(intervalId);
  }, [fetchDailyForecast, fetchForecast]);

  // Fetch active fuzzy systems for the dropdown
  useEffect(() => {
    const loadSystems = async () => {
      try {
        const systems = await fuzzyService.getSystems();
        const active = systems.filter((s: FuzzySystem) => s.status === 'ACTIVE');
        setFuzzySystems(active);
        if (active.length > 0 && active[0]) {
          setSelectedSystemId(active[0].id);
          setSelectedSystemName(active[0].name);
        }
      } catch {
        // Non-critical: fuzzy systems optional
      }
    };
    loadSystems();
  }, []);

  const handleSystemChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const id = e.target.value;
    setSelectedSystemId(id);
    const sys = fuzzySystems.find(s => s.id === id);
    setSelectedSystemName(sys?.name || '');
  };

  return (
    <PageLayout
      title="Clima y Pronóstico"
      subtitle="Pronóstico de 8 días, alertas meteorológicas configurables por sistema fuzzy"
      maxWidth="xl"
    >
      {/* Current weather */}
      <section className="mb-8" aria-labelledby="current-weather-heading">
        <h2 id="current-weather-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Clima Actual
        </h2>
        <WeatherCard weather={weather} isLoading={weatherLoading} error={weatherError} />
      </section>

      {/* 8-day forecast */}
      <section className="mb-8" aria-labelledby="forecast-heading">
        <h2 id="forecast-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Pronóstico 8 Días
        </h2>
        <ForecastSection
          daily={dailyForecast}
          isLoading={dailyForecastLoading}
          error={dailyForecastError}
        />
      </section>

      {/* Hourly details from full forecast */}
      {forecast && forecast.hourly.length > 0 && (
        <section className="mb-8" aria-labelledby="hourly-heading">
          <h2 id="hourly-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
            Próximas 24 Horas
          </h2>
          <div>
            <div className="flex flex-wrap gap-3 pb-2 justify-center sm:justify-start">
              {forecast.hourly.slice(0, 24).map((h: import('@hydroespinaca/shared').HourlyForecast) => {
                const hour = new Date(h.dateTime).toLocaleTimeString('es-CO', {
                  hour: '2-digit', minute: '2-digit', hour12: true, timeZone: 'America/Bogota',
                });
                const popPct = Math.round(h.pop * 100);
                return (
                  <div key={h.dateTime} className="w-28 bg-white border border-gray-200 rounded-xl p-3 text-center shadow-sm transition-all hover:shadow-md hover:scale-[1.02]">
                    <p className="text-sm font-medium text-gray-500 mb-1">{hour}</p>
                    <p className="text-2xl font-bold text-gray-800">{Math.round(h.temperature)}°</p>
                    <p className="text-xs text-gray-500 capitalize line-clamp-2 leading-tight mt-1 h-8" title={h.description}>{h.description}</p>
                    {popPct > 0 && <p className="text-xs font-semibold text-blue-600 mt-1">💧 {popPct}%</p>}
                  </div>
                );
              })}
            </div>
          </div>
        </section>
      )}

      {/* Government alerts */}
      {forecast && forecast.governmentAlerts.length > 0 && (
        <section className="mb-8" aria-labelledby="gov-alert-heading">
          <h2 id="gov-alert-heading" className="text-xl font-bold text-red-700 mb-4 font-inter">
            🏛️ Alertas Gubernamentales
          </h2>
          <div className="space-y-3">
            {forecast.governmentAlerts.map((alert: import('@hydroespinaca/shared').GovernmentAlert, idx: number) => (
              <div key={idx} className="bg-red-50 border border-red-200 rounded-xl p-4">
                <div className="flex items-center gap-2 mb-2">
                  <span className="text-sm font-semibold text-red-800">{alert.event}</span>
                  <span className="text-xs text-red-600">— {alert.senderName}</span>
                </div>
                <p className="text-xs text-red-700">{alert.description}</p>
                <div className="flex gap-3 mt-2 text-xs text-red-500">
                  <span>Inicio: {new Date(alert.start).toLocaleDateString('es-CO')}</span>
                  <span>Fin: {new Date(alert.end).toLocaleDateString('es-CO')}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Alert configuration per fuzzy system */}
      <section className="mb-8" aria-labelledby="alert-config-heading">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
          <h2 id="alert-config-heading" className="text-xl font-bold text-green-800 font-inter">
            Configuración de Alertas
          </h2>
          {fuzzySystems.length > 1 && (
            <select
              value={selectedSystemId}
              onChange={handleSystemChange}
              className="px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 bg-white"
            >
              {fuzzySystems.map(sys => (
                <option key={sys.id} value={sys.id}>{sys.name}</option>
              ))}
            </select>
          )}
        </div>
        {selectedSystemId ? (
          <AlertConfigSection
            fuzzySystemId={selectedSystemId}
            fuzzySystemName={selectedSystemName}
          />
        ) : (
          <div className="bg-gray-50 border border-gray-200 rounded-xl p-6 text-center">
            <p className="text-sm text-gray-500">
              {fuzzySystems.length === 0
                ? 'No hay sistemas fuzzy activos para configurar alertas.'
                : 'Selecciona un sistema fuzzy para configurar sus alertas.'}
            </p>
          </div>
        )}
      </section>

      {/* Alert history */}
      <section className="mb-8" aria-labelledby="alert-history-heading">
        <h2 id="alert-history-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Alertas Recientes
        </h2>
        {selectedSystemId && <WeatherAlertHistory fuzzySystemId={selectedSystemId} />}
        {!selectedSystemId && <WeatherAlertHistory />}
      </section>
    </PageLayout>
  );
}
