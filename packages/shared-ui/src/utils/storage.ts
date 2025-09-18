import { Session, Platform } from '../types/auth';
import { getCookie } from './cookies';

const STORAGE_KEY = 'hydroespinaca_session';

export interface StorageAdapter {
  getItem(key: string): Promise<string | null> | string | null;
  setItem(key: string, value: string): Promise<void> | void;
  removeItem(key: string): Promise<void> | void;
}

export interface SecureStorageAdapter {
  getItemAsync(key: string): Promise<string | null>;
  setItemAsync(key: string, value: string): Promise<void>;
  deleteItemAsync(key: string): Promise<void>;
}

// Web storage adapter (cookies for session management)
export const webStorageAdapter: StorageAdapter = {
  getItem: (key: string) => {
    if (typeof window === 'undefined') return null;
    
    // For web, try to read CSRF token from cookies (SessionId is HttpOnly)
    // This is mainly used as fallback when page is refreshed
    const csrfToken = getCookie('CsrfToken');
    
    if (csrfToken) {
      // For web, sessionId is null since it's HttpOnly and not accessible
      return JSON.stringify({ 
        sessionId: null, // HttpOnly, no accesible en JS
        csrfToken 
      });
    }
    
    return null;
  },
  
  setItem: (key: string, value: string) => {
    // For web, cookies are set by the server
    // This method is not used for web platform
  },
  
  removeItem: (key: string) => {
    if (typeof window === 'undefined') return;
    // Clear cookies by setting them to expire
    document.cookie = 'SessionId=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
    document.cookie = 'CsrfToken=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
  }
};

// Mobile storage adapter (Expo SecureStore)
let secureStoreAdapter: SecureStorageAdapter | null = null;

export const setSecureStoreAdapter = (adapter: SecureStorageAdapter) => {
  secureStoreAdapter = adapter;
};

const mobileStorageAdapter: StorageAdapter = {
  getItem: async (key: string) => {
    if (!secureStoreAdapter) return null;
    return await secureStoreAdapter.getItemAsync(key);
  },
  
  setItem: async (key: string, value: string) => {
    if (!secureStoreAdapter) return;
    await secureStoreAdapter.setItemAsync(key, value);
  },
  
  removeItem: async (key: string) => {
    if (!secureStoreAdapter) return;
    await secureStoreAdapter.deleteItemAsync(key);
  }
};

// Legacy AsyncStorage support
let asyncStorageAdapter: StorageAdapter | null = null;

export const setAsyncStorageAdapter = (adapter: StorageAdapter) => {
  asyncStorageAdapter = adapter;
};

export const getStorageAdapter = (platform?: Platform): StorageAdapter => {
  // If platform is explicitly specified
  if (platform === 'web') {
    return webStorageAdapter;
  }
  
  if (platform === 'mobile') {
    return mobileStorageAdapter;
  }
  
  // Auto-detect platform
  if (typeof window === 'undefined') {
    // React Native environment
    if (secureStoreAdapter) {
      return mobileStorageAdapter;
    }
    if (asyncStorageAdapter) {
      return asyncStorageAdapter;
    }
  }
  
  // Web environment
  return webStorageAdapter;
};

export const createSessionStorage = (platform?: Platform) => ({
  async save(session: Session): Promise<void> {
    const adapter = getStorageAdapter(platform);
    await adapter.setItem(STORAGE_KEY, JSON.stringify(session));
  },
  
  async load(): Promise<Session | null> {
    try {
      const adapter = getStorageAdapter(platform);
      const stored = await adapter.getItem(STORAGE_KEY);
      
      if (!stored) return null;
      
      const parsed = JSON.parse(stored) as Session;
      
      // Validate session structure
      if (!parsed.sessionId || !parsed.csrfToken) {
        await this.clear();
        return null;
      }
      
      return parsed;
    } catch (error) {
      console.warn('Failed to load session from storage:', error);
      await this.clear();
      return null;
    }
  },
  
  async clear(): Promise<void> {
    const adapter = getStorageAdapter(platform);
    await adapter.removeItem(STORAGE_KEY);
  }
});

// Default session storage (auto-detect platform)
export const sessionStorage = createSessionStorage();