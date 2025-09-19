// Secure storage abstraction for mobile and web platforms

// Platform detection
const isReactNative = (): boolean => {
  return typeof navigator !== 'undefined' && navigator.product === 'ReactNative';
};

// Storage modules - initialized lazily
let SecureStore: any = null;
let AsyncStorage: any = null;
let storageInitialized = false;

// Improved module loading with direct imports for React Native
const initializeStorageModules = () => {
  if (storageInitialized) return;
  
  try {
    if (isReactNative()) {
      // Try to load SecureStore
      try {
        SecureStore = require('expo-secure-store');
      } catch (error) {
        // SecureStore not available, will use fallback
      }
      
      // Try to load AsyncStorage
      try {
        AsyncStorage = require('@react-native-async-storage/async-storage');
        // Some packages export default, others don't
        if (AsyncStorage.default) {
          AsyncStorage = AsyncStorage.default;
        }
      } catch (error) {
        // AsyncStorage not available, will use fallback
      }
    }
  } catch (error) {
    // Storage initialization failed, will use web fallback
  }
  
  storageInitialized = true;
};

export interface SecureStorageInterface {
  getItem: (key: string) => Promise<string | null>;
  setItem: (key: string, value: string) => Promise<void>;
  removeItem: (key: string) => Promise<void>;
}

// Web fallback using localStorage
const webStorage: SecureStorageInterface = {
  async getItem(key: string): Promise<string | null> {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem(key);
    }
    return null;
  },

  async setItem(key: string, value: string): Promise<void> {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.setItem(key, value);
    }
  },

  async removeItem(key: string): Promise<void> {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem(key);
    }
  }
};

// Mobile secure storage with proper fallback hierarchy
const mobileStorage: SecureStorageInterface = {
  async getItem(key: string): Promise<string | null> {
    // Ensure storage modules are initialized
    initializeStorageModules();
    
    // Try SecureStore first (most secure)
    if (SecureStore && SecureStore.getItemAsync) {
      try {
        // Check if SecureStore is actually available on the device
        if (SecureStore.isAvailableAsync && await SecureStore.isAvailableAsync()) {
          return await SecureStore.getItemAsync(key);
        }
      } catch (error) {
        // SecureStore failed, continue to fallback
      }
    }

    // Fallback to AsyncStorage (less secure but persistent)
    if (AsyncStorage && AsyncStorage.getItem) {
      try {
        return await AsyncStorage.getItem(key);
      } catch (error) {
        // AsyncStorage failed, continue to fallback
      }
    }

    // Last resort: web storage (least secure, may not persist)
    return webStorage.getItem(key);
  },

  async setItem(key: string, value: string): Promise<void> {
    // Ensure storage modules are initialized
    initializeStorageModules();
    
    // Try SecureStore first (most secure)
    if (SecureStore && SecureStore.setItemAsync) {
      try {
        if (SecureStore.isAvailableAsync && await SecureStore.isAvailableAsync()) {
          await SecureStore.setItemAsync(key, value);
          return;
        }
      } catch (error) {
        // SecureStore failed, continue to fallback
      }
    }

    // Fallback to AsyncStorage (less secure but persistent)
    if (AsyncStorage && AsyncStorage.setItem) {
      try {
        await AsyncStorage.setItem(key, value);
        return;
      } catch (error) {
        // AsyncStorage failed, continue to fallback
      }
    }

    // Last resort: web storage
    return webStorage.setItem(key, value);
  },

  async removeItem(key: string): Promise<void> {
    // Ensure storage modules are initialized
    initializeStorageModules();
    
    // Try SecureStore first
    if (SecureStore && SecureStore.deleteItemAsync) {
      try {
        if (SecureStore.isAvailableAsync && await SecureStore.isAvailableAsync()) {
          await SecureStore.deleteItemAsync(key);
          return;
        }
      } catch (error) {
        // SecureStore failed, continue to fallback
      }
    }

    // Fallback to AsyncStorage
    if (AsyncStorage && AsyncStorage.removeItem) {
      try {
        await AsyncStorage.removeItem(key);
        return;
      } catch (error) {
        // AsyncStorage failed, continue to fallback
      }
    }

    // Last resort: web storage
    return webStorage.removeItem(key);
  }
};

// Export the appropriate storage based on platform
export const secureStorage: SecureStorageInterface = isReactNative() ? mobileStorage : webStorage;

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