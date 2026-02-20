// Secure Storage utilities for managing session tokens

export interface SecureStorageInterface {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

/**
 * This file (.native.ts) is only loaded by Metro bundler in React Native.
 * No runtime detection needed — if this file is executing, we ARE in React Native.
 */
const isReactNative = (): boolean => {
  return true;
};

/**
 * Web storage implementation using localStorage
 */
const webStorage: SecureStorageInterface = {
  async getItem(key: string): Promise<string | null> {
    if (typeof window !== 'undefined' && window.localStorage) {
      // Use local reference to avoid TS18048
      const ls = window.localStorage;
      return ls.getItem(key);
    }
    return null;
  },

  async setItem(key: string, value: string): Promise<void> {
    if (typeof window !== 'undefined' && window.localStorage) {
      // Use local reference to avoid TS18048
      const ls = window.localStorage;
      ls.setItem(key, value);
    }
  },

  async removeItem(key: string): Promise<void> {
    if (typeof window !== 'undefined' && window.localStorage) {
      // Use local reference to avoid TS18048
      const ls = window.localStorage;
      ls.removeItem(key);
    }
  }
};

/**
 * Mobile storage implementation using SecureStore or AsyncStorage
 * Priority: SecureStore > AsyncStorage > localStorage (fallback)
 */
const mobileStorage: SecureStorageInterface = {
  async getItem(key: string): Promise<string | null> {
    // Try expo-secure-store first (most secure)
    try {
      // Dynamically import to avoid errors in web
      const SecureStore = await import('expo-secure-store');

      // Check if SecureStore is available
      if (SecureStore && SecureStore.getItemAsync) {
        // Check availability on device
        if (SecureStore.isAvailableAsync) {
          const isAvailable = await SecureStore.isAvailableAsync();
          if (isAvailable) {
            return await SecureStore.getItemAsync(key);
          }
        } else {
          // If isAvailableAsync doesn't exist, try to use it anyway
          return await SecureStore.getItemAsync(key);
        }
      }
    } catch (error) {
      // SecureStore not available or error, continue to fallback
      console.warn('SecureStore not available, falling back to AsyncStorage');
    }

    // Try AsyncStorage as fallback
    try {
      const AsyncStorage = await import('@react-native-async-storage/async-storage');
      if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.getItem) {
        return await AsyncStorage.default.getItem(key);
      }
    } catch (error) {
      // AsyncStorage not available, continue to fallback
      console.warn('AsyncStorage not available, falling back to localStorage');
    }

    // Final fallback to web localStorage
    return webStorage.getItem(key);
  },

  async setItem(key: string, value: string): Promise<void> {
    // Try expo-secure-store first
    try {
      const SecureStore = await import('expo-secure-store');

      if (SecureStore && SecureStore.setItemAsync) {
        if (SecureStore.isAvailableAsync) {
          const isAvailable = await SecureStore.isAvailableAsync();
          if (isAvailable) {
            await SecureStore.setItemAsync(key, value);
            return;
          }
        } else {
          await SecureStore.setItemAsync(key, value);
          return;
        }
      }
    } catch (error) {
      console.warn('SecureStore not available, falling back to AsyncStorage');
    }

    // Try AsyncStorage as fallback
    try {
      const AsyncStorage = await import('@react-native-async-storage/async-storage');
      if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.setItem) {
        await AsyncStorage.default.setItem(key, value);
        return;
      }
    } catch (error) {
      console.warn('AsyncStorage not available, falling back to localStorage');
    }

    // Final fallback to web localStorage
    await webStorage.setItem(key, value);
  },

  async removeItem(key: string): Promise<void> {
    // Try expo-secure-store first
    try {
      const SecureStore = await import('expo-secure-store');

      if (SecureStore && SecureStore.deleteItemAsync) {
        if (SecureStore.isAvailableAsync) {
          const isAvailable = await SecureStore.isAvailableAsync();
          if (isAvailable) {
            await SecureStore.deleteItemAsync(key);
            return;
          }
        } else {
          await SecureStore.deleteItemAsync(key);
          return;
        }
      }
    } catch (error) {
      console.warn('SecureStore not available, falling back to AsyncStorage');
    }

    // Try AsyncStorage as fallback
    try {
      const AsyncStorage = await import('@react-native-async-storage/async-storage');
      if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.removeItem) {
        await AsyncStorage.default.removeItem(key);
        return;
      }
    } catch (error) {
      console.warn('AsyncStorage not available, falling back to localStorage');
    }

    // Final fallback to web localStorage
    await webStorage.removeItem(key);
  }
};

/**
 * Secure storage instance — this .native.ts file is only loaded in React Native,
 * so we always use mobileStorage (SecureStore > AsyncStorage > localStorage fallback).
 */
export const secureStorage: SecureStorageInterface = mobileStorage;

/**
 * Session storage helper functions
 */
export const SessionStorage = {
  /**
   * Get the session ID
   */
  async getSessionId(): Promise<string | null> {
    return await secureStorage.getItem('sessionId');
  },

  /**
   * Get the CSRF token
   */
  async getCsrfToken(): Promise<string | null> {
    return await secureStorage.getItem('csrfToken');
  },

  /**
   * Store session credentials
   */
  async storeSession(sessionId: string, csrfToken: string): Promise<void> {
    await Promise.all([
      secureStorage.setItem('sessionId', sessionId),
      secureStorage.setItem('csrfToken', csrfToken)
    ]);
  },

  /**
   * Clear all session data
   */
  async clearSession(): Promise<void> {
    await Promise.all([
      secureStorage.removeItem('sessionId'),
      secureStorage.removeItem('csrfToken')
    ]);
  }
};
