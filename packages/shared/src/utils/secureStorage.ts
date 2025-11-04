// Barrel file for secure storage - exports types and implementations
// Platform-specific bundlers will resolve .web.ts or .native.ts automatically

export interface SecureStorageInterface {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

export interface SessionStorageInterface {
  getSessionId(): Promise<string | null>;
  getCsrfToken(): Promise<string | null>;
  storeSession(sessionId: string, csrfToken: string): Promise<void>;
  clearSession(): Promise<void>;
}

// This will be resolved to .web.ts or .native.ts by the bundler
// TypeScript will use this file for type checking
export const secureStorage: SecureStorageInterface = {} as SecureStorageInterface;
export const SessionStorage: SessionStorageInterface = {} as SessionStorageInterface;
