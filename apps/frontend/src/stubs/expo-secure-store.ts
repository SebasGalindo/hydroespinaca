// Expo SecureStore stub for web builds
// This file replaces expo-secure-store when building for web

export const setItemAsync = async (key: string, value: string): Promise<void> => {
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem(key, value);
  }
};

export const getItemAsync = async (key: string): Promise<string | null> => {
  if (typeof localStorage !== 'undefined') {
    return localStorage.getItem(key);
  }
  return null;
};

export const deleteItemAsync = async (key: string): Promise<void> => {
  if (typeof localStorage !== 'undefined') {
    localStorage.removeItem(key);
  }
};

export const isAvailableAsync = async (): Promise<boolean> => {
  return typeof localStorage !== 'undefined';
};

// Default export
export default {
  setItemAsync,
  getItemAsync,
  deleteItemAsync,
  isAvailableAsync,
};