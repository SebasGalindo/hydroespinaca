// React Native AsyncStorage stub for web builds
// This file replaces @react-native-async-storage/async-storage when building for web

const AsyncStorage = {
  setItem: async (key: string, value: string): Promise<void> => {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(key, value);
    }
  },

  getItem: async (key: string): Promise<string | null> => {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem(key);
    }
    return null;
  },

  removeItem: async (key: string): Promise<void> => {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(key);
    }
  },

  clear: async (): Promise<void> => {
    if (typeof localStorage !== 'undefined') {
      localStorage.clear();
    }
  },

  getAllKeys: async (): Promise<readonly string[]> => {
    if (typeof localStorage !== 'undefined') {
      return Object.keys(localStorage);
    }
    return [];
  },

  multiGet: async (keys: readonly string[]): Promise<readonly [string, string | null][]> => {
    if (typeof localStorage !== 'undefined') {
      return keys.map((key) => [key, localStorage.getItem(key)]);
    }
    return keys.map((key) => [key, null]);
  },

  multiSet: async (keyValuePairs: readonly [string, string][]): Promise<void> => {
    if (typeof localStorage !== 'undefined') {
      keyValuePairs.forEach(([key, value]) => localStorage.setItem(key, value));
    }
  },

  multiRemove: async (keys: readonly string[]): Promise<void> => {
    if (typeof localStorage !== 'undefined') {
      keys.forEach((key) => localStorage.removeItem(key));
    }
  },
};

export default AsyncStorage;