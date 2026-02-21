import { create } from 'zustand';
import { weatherService } from '../api/weatherService';
import { ApiServiceError } from '../api/BaseApiService';
import type {
  ForecastResponse,
  DailyForecast,
  WeatherAlertConfig,
  WeatherAlert,
  UpdateAlertConfigRequest,
  SeedAlertConfigRequest,
  AlertFilterParams,
} from '../types/weather';

// ==================== State Interface ====================

interface WeatherState {
  // Forecast
  forecast: ForecastResponse | null;
  forecastLoading: boolean;
  forecastError: string | null;

  // Daily forecast (lightweight, 8 days)
  dailyForecast: DailyForecast[];
  dailyForecastLoading: boolean;
  dailyForecastError: string | null;

  // Alert config (per fuzzy system)
  alertConfig: WeatherAlertConfig | null;
  alertConfigLoading: boolean;
  alertConfigError: string | null;

  // Generated alerts
  alerts: WeatherAlert[];
  alertsLoading: boolean;
  alertsError: string | null;
  unreadAlertCount: number;
}

interface WeatherActions {
  fetchForecast: () => Promise<void>;
  fetchDailyForecast: () => Promise<void>;
  fetchAlertConfig: (fuzzySystemId: string) => Promise<void>;
  updateAlertConfig: (fuzzySystemId: string, config: UpdateAlertConfigRequest) => Promise<void>;
  seedAlertConfig: (fuzzySystemId: string, req: SeedAlertConfigRequest) => Promise<void>;
  fetchAlerts: (params?: AlertFilterParams) => Promise<void>;
  markAlertRead: (alertId: string, userId: string) => Promise<void>;
  clearErrors: () => void;
  resetWeatherStore: () => void;
}

// ==================== Initial State ====================

const initialState: WeatherState = {
  forecast: null,
  forecastLoading: false,
  forecastError: null,

  dailyForecast: [],
  dailyForecastLoading: false,
  dailyForecastError: null,

  alertConfig: null,
  alertConfigLoading: false,
  alertConfigError: null,

  alerts: [],
  alertsLoading: false,
  alertsError: null,
  unreadAlertCount: 0,
};

// ==================== Error Helper ====================

function extractError(err: unknown): string {
  if (err instanceof ApiServiceError) {
    if (err.status === 401 || err.status === 403) return 'No autorizado';
    return err.message;
  }
  if (err instanceof Error) return err.message;
  return 'Error desconocido';
}

// ==================== Store ====================

export const useWeatherStore = create<WeatherState & WeatherActions>()((set, get) => ({
  ...initialState,

  fetchForecast: async () => {
    set({ forecastLoading: true, forecastError: null });
    try {
      const data = await weatherService.getForecast();
      set({ forecast: data, forecastLoading: false });
    } catch (err) {
      set({ forecastError: extractError(err), forecastLoading: false });
    }
  },

  fetchDailyForecast: async () => {
    set({ dailyForecastLoading: true, dailyForecastError: null });
    try {
      const data = await weatherService.getDailyForecast();
      set({ dailyForecast: data, dailyForecastLoading: false });
    } catch (err) {
      set({ dailyForecastError: extractError(err), dailyForecastLoading: false });
    }
  },

  fetchAlertConfig: async (fuzzySystemId: string) => {
    set({ alertConfigLoading: true, alertConfigError: null });
    try {
      const data = await weatherService.getAlertConfig(fuzzySystemId);
      set({ alertConfig: data, alertConfigLoading: false });
    } catch (err) {
      set({ alertConfigError: extractError(err), alertConfigLoading: false });
    }
  },

  updateAlertConfig: async (fuzzySystemId: string, config: UpdateAlertConfigRequest) => {
    set({ alertConfigLoading: true, alertConfigError: null });
    try {
      await weatherService.updateAlertConfig(fuzzySystemId, config);
      // Re-fetch updated config
      const data = await weatherService.getAlertConfig(fuzzySystemId);
      set({ alertConfig: data, alertConfigLoading: false });
    } catch (err) {
      set({ alertConfigError: extractError(err), alertConfigLoading: false });
      throw err;
    }
  },

  seedAlertConfig: async (fuzzySystemId: string, req: SeedAlertConfigRequest) => {
    set({ alertConfigLoading: true, alertConfigError: null });
    try {
      const data = await weatherService.seedAlertConfig(fuzzySystemId, req);
      set({ alertConfig: data, alertConfigLoading: false });
    } catch (err) {
      set({ alertConfigError: extractError(err), alertConfigLoading: false });
      throw err;
    }
  },

  fetchAlerts: async (params?: AlertFilterParams) => {
    set({ alertsLoading: true, alertsError: null });
    try {
      const data = await weatherService.getAlerts(params);
      const unread = data.filter(a =>
        !a.notifiedUsers.some(u => u.userId === params?.userId && u.isRead)
      ).length;
      set({ alerts: data, unreadAlertCount: unread, alertsLoading: false });
    } catch (err) {
      set({ alertsError: extractError(err), alertsLoading: false });
    }
  },

  markAlertRead: async (alertId: string, userId: string) => {
    try {
      await weatherService.markAlertRead(alertId, userId);
      // Optimistic update: mark it read locally
      const { alerts } = get();
      const updated = alerts.map(a => {
        if (a.id !== alertId) return a;
        return {
          ...a,
          notifiedUsers: a.notifiedUsers.map(u =>
            u.userId === userId ? { ...u, isRead: true } : u
          ),
        };
      });
      const unread = updated.filter(a =>
        !a.notifiedUsers.some(u => u.userId === userId && u.isRead)
      ).length;
      set({ alerts: updated, unreadAlertCount: unread });
    } catch (err) {
      set({ alertsError: extractError(err) });
    }
  },

  clearErrors: () => {
    set({
      forecastError: null,
      dailyForecastError: null,
      alertConfigError: null,
      alertsError: null,
    });
  },

  resetWeatherStore: () => {
    set(initialState);
  },
}));
