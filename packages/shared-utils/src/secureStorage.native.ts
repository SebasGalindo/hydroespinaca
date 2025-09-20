// Secure storage specifically for React Native with static imports
import * as SecureStore from 'expo-secure-store';
import AsyncStorage from '@react-native-async-storage/async-storage';

export interface SecureStorageInterface {
  getItem: (key: string) => Promise<string | null>;
  setItem: (key: string, value: string) => Promise<void>;
  removeItem: (key: string) => Promise<void>;
}

// Native storage implementation with proper fallback hierarchy
const nativeStorage: SecureStorageInterface = {
  async getItem(key: string): Promise<string | null> {
    // Try SecureStore first (most secure)
    try {
      const isAvailable = await SecureStore.isAvailableAsync();
      if (isAvailable) {
        return await SecureStore.getItemAsync(key);
      }
    } catch (error) {
      // SecureStore failed, continue to fallback
    }

    // Fallback to AsyncStorage (less secure but persistent)
    try {
      return await AsyncStorage.getItem(key);
    } catch (error) {
      throw error; // Re-throw because AsyncStorage should always work in RN
    }
  },

  async setItem(key: string, value: string): Promise<void> {
    // Try SecureStore first (most secure)
    try {
      const isAvailable = await SecureStore.isAvailableAsync();
      if (isAvailable) {
        await SecureStore.setItemAsync(key, value);
        return;
      }
    } catch (error) {
      // SecureStore failed, continue to fallback
    }

    // Fallback to AsyncStorage (less secure but persistent)
    try {
      await AsyncStorage.setItem(key, value);
    } catch (error) {
      throw error; // Re-throw because AsyncStorage should always work in RN
    }
  },

  async removeItem(key: string): Promise<void> {
    // Try SecureStore first
    try {
      const isAvailable = await SecureStore.isAvailableAsync();
      if (isAvailable) {
        await SecureStore.deleteItemAsync(key);
        return;
      }
    } catch (error) {
      // SecureStore failed, continue to fallback
    }

    // Fallback to AsyncStorage
    try {
      await AsyncStorage.removeItem(key);
    } catch (error) {
      throw error; // Re-throw because AsyncStorage should always work in RN
    }
  }
};

// Export storage and helper functions
export const secureStorage: SecureStorageInterface = nativeStorage;

// Session storage constants
export const STORAGE_KEYS = {
  SESSION_ID: 'sessionId',
  CSRF_TOKEN: 'csrfToken',
} as const;

// Helper functions for session management
export const SessionStorage = {
  async getSessionId(): Promise<string | null> {
    return await secureStorage.getItem(STORAGE_KEYS.SESSION_ID);
  },

  async getCsrfToken(): Promise<string | null> {
    return await secureStorage.getItem(STORAGE_KEYS.CSRF_TOKEN);
  },

  async storeSession(sessionId: string, csrfToken: string): Promise<void> {
    await Promise.all([
      secureStorage.setItem(STORAGE_KEYS.SESSION_ID, sessionId),
      secureStorage.setItem(STORAGE_KEYS.CSRF_TOKEN, csrfToken)
    ]);
  },

  async clearSession(): Promise<void> {
    await Promise.all([
      secureStorage.removeItem(STORAGE_KEYS.SESSION_ID),
      secureStorage.removeItem(STORAGE_KEYS.CSRF_TOKEN)
    ]);
  }
};