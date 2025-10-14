/**
 * Utilidades de almacenamiento multiplataforma
 * Funciona tanto en React Native (AsyncStorage) como en Web (localStorage)
 */

// Tipo para el storage adapter
interface StorageAdapter {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
  clear(): Promise<void>;
}

// Adapter para React Native
const createAsyncStorageAdapter = (): StorageAdapter | null => {
  try {
    // Importación dinámica para evitar errores en web
    const AsyncStorage = require('@react-native-async-storage/async-storage').default;
    return {
      getItem: (key: string) => AsyncStorage.getItem(key),
      setItem: (key: string, value: string) => AsyncStorage.setItem(key, value),
      removeItem: (key: string) => AsyncStorage.removeItem(key),
      clear: () => AsyncStorage.clear(),
    };
  } catch {
    return null;
  }
};

// Adapter para Web
const createLocalStorageAdapter = (): StorageAdapter | null => {
  if (typeof window !== 'undefined' && window.localStorage) {
    // Usar referencia local para evitar warning TS18048
    const ls = window.localStorage;
    return {
      getItem: (key: string) => Promise.resolve(ls.getItem(key)),
      setItem: (key: string, value: string) => {
        ls.setItem(key, value);
        return Promise.resolve();
      },
      removeItem: (key: string) => {
        ls.removeItem(key);
        return Promise.resolve();
      },
      clear: () => {
        ls.clear();
        return Promise.resolve();
      },
    };
  }
  return null;
};

// Seleccionar el adapter apropiado
const getStorageAdapter = (): StorageAdapter => {
  const asyncStorageAdapter = createAsyncStorageAdapter();
  if (asyncStorageAdapter) {
    return asyncStorageAdapter;
  }

  const localStorageAdapter = createLocalStorageAdapter();
  if (localStorageAdapter) {
    return localStorageAdapter;
  }

  // Fallback: storage en memoria (para testing o SSR)
  const memoryStorage = new Map<string, string>();
  return {
    getItem: (key: string) => Promise.resolve(memoryStorage.get(key) || null),
    setItem: (key: string, value: string) => {
      memoryStorage.set(key, value);
      return Promise.resolve();
    },
    removeItem: (key: string) => {
      memoryStorage.delete(key);
      return Promise.resolve();
    },
    clear: () => {
      memoryStorage.clear();
      return Promise.resolve();
    },
  };
};

const storage = getStorageAdapter();

// API pública
export const StorageUtils = {
  /**
   * Obtiene un valor del storage
   */
  async getString(key: string): Promise<string | null> {
    try {
      return await storage.getItem(key);
    } catch (error) {
      console.warn(`Error getting storage item "${key}":`, error);
      return null;
    }
  },

  /**
   * Obtiene un objeto JSON del storage
   */
  async getObject<T>(key: string): Promise<T | null> {
    try {
      const value = await storage.getItem(key);
      return value ? JSON.parse(value) : null;
    } catch (error) {
      console.warn(`Error getting storage object "${key}":`, error);
      return null;
    }
  },

  /**
   * Guarda un string en el storage
   */
  async setString(key: string, value: string): Promise<boolean> {
    try {
      await storage.setItem(key, value);
      return true;
    } catch (error) {
      console.warn(`Error setting storage item "${key}":`, error);
      return false;
    }
  },

  /**
   * Guarda un objeto JSON en el storage
   */
  async setObject(key: string, value: any): Promise<boolean> {
    try {
      await storage.setItem(key, JSON.stringify(value));
      return true;
    } catch (error) {
      console.warn(`Error setting storage object "${key}":`, error);
      return false;
    }
  },

  /**
   * Elimina un item del storage
   */
  async remove(key: string): Promise<boolean> {
    try {
      await storage.removeItem(key);
      return true;
    } catch (error) {
      console.warn(`Error removing storage item "${key}":`, error);
      return false;
    }
  },

  /**
   * Limpia todo el storage
   */
  async clear(): Promise<boolean> {
    try {
      await storage.clear();
      return true;
    } catch (error) {
      console.warn('Error clearing storage:', error);
      return false;
    }
  },
};

// Constantes para keys comunes
export const StorageKeys = {
  AUTH_TOKEN: 'auth_token',
  USER_PREFERENCES: 'user_preferences',
  THEME_MODE: 'theme_mode',
  LAST_SYNC: 'last_sync',
  OFFLINE_DATA: 'offline_data',
} as const;