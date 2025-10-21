// Secure Storage utilities for WEB - localStorage only
// No React Native dependencies

export interface SecureStorageInterface {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

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
 * Secure storage instance for web (uses localStorage)
 */
export const secureStorage: SecureStorageInterface = webStorage;

/**
 * Session storage helper functions for web
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
