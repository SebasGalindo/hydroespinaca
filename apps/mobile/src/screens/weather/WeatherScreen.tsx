/**
 * WeatherScreen — Pronóstico 8 días, alertas gubernamentales,
 * config de alertas y lista de alertas generadas.
 */
import React, { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ScreenLayout } from '../../components/organisms/ScreenLayout';
import { AlertConfigSheet } from '../../components/organisms/AlertConfigSheet';
import { ForecastSection } from '../../components/weather/ForecastSection';
import { GovernmentAlertBanner } from '../../components/weather/GovernmentAlertBanner';
import { AlertConfigList } from '../../components/weather/AlertConfigList';
import { WeatherAlertList } from '../../components/weather/WeatherAlertList';
import {
  useWeatherStore,
  useAuthStore,
  useFuzzyStore,
} from '@hydroespinaca/shared';
import type { WeatherStackParamList } from '../../navigation/types';
import type { AlertThreshold } from '@hydroespinaca/shared';

type WeatherNav = NativeStackNavigationProp<WeatherStackParamList, 'Weather'>;

export function WeatherScreen(): React.ReactElement {
  const navigation = useNavigation<WeatherNav>();
  const userId = useAuthStore(s => s.user?.id) ?? '';

  // Weather store
  const forecast = useWeatherStore(s => s.forecast);
  const dailyForecast = useWeatherStore(s => s.dailyForecast);
  const forecastLoading = useWeatherStore(s => s.forecastLoading);
  const forecastError = useWeatherStore(s => s.forecastError);
  const fetchForecast = useWeatherStore(s => s.fetchForecast);
  const fetchDailyForecast = useWeatherStore(s => s.fetchDailyForecast);

  const alertConfig = useWeatherStore(s => s.alertConfig);
  const alertConfigLoading = useWeatherStore(s => s.alertConfigLoading);
  const alertConfigError = useWeatherStore(s => s.alertConfigError);
  const fetchAlertConfig = useWeatherStore(s => s.fetchAlertConfig);
  const updateAlertConfig = useWeatherStore(s => s.updateAlertConfig);

  const alerts = useWeatherStore(s => s.alerts);
  const alertsLoading = useWeatherStore(s => s.alertsLoading);
  const alertsError = useWeatherStore(s => s.alertsError);
  const fetchAlerts = useWeatherStore(s => s.fetchAlerts);
  const markAlertRead = useWeatherStore(s => s.markAlertRead);

  // Get first active fuzzy system for alert config
  const systems = useFuzzyStore(s => s.systems);
  const fetchSystems = useFuzzyStore(s => s.fetchSystems);
  const activeFuzzyId = systems.find(s => s.status === 'ACTIVE')?.id;

  const [configSheetOpen, setConfigSheetOpen] = useState(false);

  // Load data on focus
  useFocusEffect(
    useCallback(() => {
      fetchForecast();
      fetchDailyForecast();
      fetchSystems();
      if (activeFuzzyId) {
        fetchAlertConfig(activeFuzzyId);
        fetchAlerts({ fuzzySystemId: activeFuzzyId, userId, pageSize: 20 });
      }
    }, [activeFuzzyId, userId])
  );

  const handleRefresh = useCallback(() => {
    fetchForecast();
    fetchDailyForecast();
    if (activeFuzzyId) {
      fetchAlertConfig(activeFuzzyId);
      fetchAlerts({ fuzzySystemId: activeFuzzyId, userId, pageSize: 20 });
    }
  }, [activeFuzzyId, userId]);

  const handleSaveConfig = useCallback(
    async (isActive: boolean, thresholds: AlertThreshold[]) => {
      if (!activeFuzzyId) return;
      await updateAlertConfig(activeFuzzyId, {
        userId,
        isActive,
        alerts: thresholds,
      });
    },
    [activeFuzzyId, userId, updateAlertConfig]
  );

  const handleMarkRead = useCallback(
    (alertId: string) => {
      markAlertRead(alertId, userId);
    },
    [userId, markAlertRead]
  );

  const days = dailyForecast.length > 0 ? dailyForecast : forecast?.daily ?? [];

  return (
    <>
      <ScreenLayout
        title="Clima"
        subtitle="Pronóstico y alertas meteorológicas"
        scrollable
        refreshing={forecastLoading}
        onRefresh={handleRefresh}
        testID="weather-screen"
      >
        {/* Government alerts (IDEAM) */}
        <GovernmentAlertBanner
          alerts={forecast?.governmentAlerts ?? []}
          testID="gov-alert-banner"
        />

        {/* 8-day forecast carousel */}
        <ForecastSection
          days={days}
          loading={forecastLoading && days.length === 0}
          error={forecastError}
        />

        {/* Alert configuration summary */}
        <AlertConfigList
          config={alertConfig}
          loading={alertConfigLoading}
          error={alertConfigError}
          onConfigure={() => setConfigSheetOpen(true)}
        />

        {/* Generated weather alerts */}
        <WeatherAlertList
          alerts={alerts}
          userId={userId}
          loading={alertsLoading}
          error={alertsError}
          onPress={(alert) => navigation.navigate('WeatherAlertDetail', { alertId: alert.id })}
          onMarkRead={handleMarkRead}
        />
      </ScreenLayout>

      {/* Alert config bottom sheet */}
      <AlertConfigSheet
        isOpen={configSheetOpen}
        onClose={() => setConfigSheetOpen(false)}
        config={alertConfig}
        loading={alertConfigLoading}
        onSave={handleSaveConfig}
        testID="alert-config-sheet"
      />
    </>
  );
}
