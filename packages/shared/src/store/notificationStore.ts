import { create } from 'zustand';
import { notificationService } from '../api/notificationService';
import { ApiServiceError } from '../api/BaseApiService';
import type {
  NotificationPreferences,
  UpdatePreferencesRequest,
  PushSubscriptionInfo,
  RegisterPushRequest,
  NotificationLogEntry,
  HistoryParams,
} from '../types/notification';

// ==================== State Interface ====================

interface NotificationState {
  // Preferences
  preferences: NotificationPreferences | null;
  preferencesLoading: boolean;
  preferencesError: string | null;

  // Push devices
  devices: PushSubscriptionInfo[];
  devicesLoading: boolean;
  devicesError: string | null;

  // History
  history: NotificationLogEntry[];
  historyLoading: boolean;
  historyError: string | null;
}

interface NotificationActions {
  fetchPreferences: (userId: string) => Promise<void>;
  updatePreferences: (userId: string, prefs: UpdatePreferencesRequest) => Promise<void>;
  registerPush: (registration: RegisterPushRequest) => Promise<void>;
  unregisterPush: (subscriptionId: string, userId: string) => Promise<void>;
  fetchDevices: (userId: string) => Promise<void>;
  fetchHistory: (userId: string, params?: HistoryParams) => Promise<void>;
  clearErrors: () => void;
  resetNotificationStore: () => void;
}

// ==================== Initial State ====================

const initialState: NotificationState = {
  preferences: null,
  preferencesLoading: false,
  preferencesError: null,

  devices: [],
  devicesLoading: false,
  devicesError: null,

  history: [],
  historyLoading: false,
  historyError: null,
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

export const useNotificationStore = create<NotificationState & NotificationActions>()((set, get) => ({
  ...initialState,

  fetchPreferences: async (userId: string) => {
    set({ preferencesLoading: true, preferencesError: null });
    try {
      const data = await notificationService.getPreferences(userId);
      set({ preferences: data, preferencesLoading: false });
    } catch (err) {
      set({ preferencesError: extractError(err), preferencesLoading: false });
    }
  },

  updatePreferences: async (userId: string, prefs: UpdatePreferencesRequest) => {
    set({ preferencesLoading: true, preferencesError: null });
    try {
      await notificationService.updatePreferences(userId, prefs);
      // Re-fetch to get server-applied defaults
      const data = await notificationService.getPreferences(userId);
      set({ preferences: data, preferencesLoading: false });
    } catch (err) {
      set({ preferencesError: extractError(err), preferencesLoading: false });
      throw err;
    }
  },

  registerPush: async (registration: RegisterPushRequest) => {
    set({ devicesLoading: true, devicesError: null });
    try {
      await notificationService.registerPush(registration);
      // Re-fetch devices
      const devices = await notificationService.getDevices(registration.userId);
      set({ devices, devicesLoading: false });
    } catch (err) {
      set({ devicesError: extractError(err), devicesLoading: false });
      throw err;
    }
  },

  unregisterPush: async (subscriptionId: string, userId: string) => {
    set({ devicesLoading: true, devicesError: null });
    try {
      await notificationService.unregisterPush(subscriptionId);
      // Optimistic: remove from local list
      const { devices } = get();
      set({
        devices: devices.filter(d => d.id !== subscriptionId),
        devicesLoading: false,
      });
    } catch (err) {
      set({ devicesError: extractError(err), devicesLoading: false });
      throw err;
    }
  },

  fetchDevices: async (userId: string) => {
    set({ devicesLoading: true, devicesError: null });
    try {
      const data = await notificationService.getDevices(userId);
      set({ devices: data, devicesLoading: false });
    } catch (err) {
      set({ devicesError: extractError(err), devicesLoading: false });
    }
  },

  fetchHistory: async (userId: string, params?: HistoryParams) => {
    set({ historyLoading: true, historyError: null });
    try {
      const data = await notificationService.getHistory(userId, params);
      set({ history: data, historyLoading: false });
    } catch (err) {
      set({ historyError: extractError(err), historyLoading: false });
    }
  },

  clearErrors: () => {
    set({
      preferencesError: null,
      devicesError: null,
      historyError: null,
    });
  },

  resetNotificationStore: () => {
    set(initialState);
  },
}));
