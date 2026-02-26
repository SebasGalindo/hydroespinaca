// API Configuration utilities

/**
 * Detect the platform we're running on.
 * Uses multiple heuristics to work on both JSC and Hermes engines.
 */
export function detectPlatform(): 'web' | 'mobile' | 'unknown' {
  // Check 1: navigator.product (works on JSC, not on Hermes)
  if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
    return 'mobile';
  }
  // Check 2: Hermes engine global (React Native with Hermes)
  if (typeof globalThis !== 'undefined' && typeof (globalThis as any).HermesInternal !== 'undefined') {
    return 'mobile';
  }
  // Check 3: ExpoModules global (Expo environment)
  if (typeof globalThis !== 'undefined' && typeof (globalThis as any).expo !== 'undefined') {
    return 'mobile';
  }
  // Check 4: __DEV__ is defined AND no document (React Native dev mode)
  if (typeof (globalThis as any).__DEV__ !== 'undefined' && typeof document === 'undefined') {
    return 'mobile';
  }
  // Check 5: Standard browser environment
  if (typeof window !== 'undefined' && typeof document !== 'undefined') {
    return 'web';
  }
  return 'unknown';
}

/**
 * Check if we're in development mode
 */
export function isDevelopmentMode(): boolean {
  // __DEV__ is the most reliable check in React Native
  if (typeof globalThis !== 'undefined' && (globalThis as any).__DEV__) {
    return true;
  }

  if (typeof process !== 'undefined' && process.env) {
    const nodeEnv = process.env.NODE_ENV;
    if (nodeEnv === 'development') {
      return true;
    }
  }

  if (typeof import.meta !== 'undefined' && import.meta.env) {
    const mode = import.meta.env.MODE || import.meta.env.VITE_MODE;
    if (mode === 'development' || mode === 'dev') {
      return true;
    }
  }

  return false;
}

/**
 * Get API URL from environment variables
 */
export function getApiUrlFromEnv(): string | null {
  if (typeof import.meta !== 'undefined' && import.meta.env) {
    const viteApiUrl = import.meta.env.VITE_API_URL;
    if (viteApiUrl) return viteApiUrl;
  }
  if (typeof process !== 'undefined' && process.env) {
    const nextApiUrl = process.env.NEXT_PUBLIC_API_URL;
    if (nextApiUrl) return nextApiUrl;
    const expoApiUrl = process.env.EXPO_PUBLIC_API_URL;
    if (expoApiUrl) return expoApiUrl;
  }
  return null;
}

/**
 * Get default API URL based on platform and environment
 */
function getDefaultApiUrl(platform: 'web' | 'mobile' | 'unknown', isDev: boolean): string {
  if (platform === 'mobile') {
    // Android emulator uses 10.0.2.2 to reach host machine's localhost
    return isDev
      ? 'http://10.0.2.2/api'
      : 'https://api.hydroespinaca.online/api';
  } else if (platform === 'web') {
    return isDev
      ? 'http://localhost/api'
      : 'https://api.hydroespinaca.online/api';
  }
  return 'https://api.hydroespinaca.online/api';
}

/**
 * Get the API URL to use for requests
 * Priority: Environment Variables > Default based on platform/env
 */
export function getApiUrl(): string {
  const platform = detectPlatform();
  const isDev = isDevelopmentMode();
  const envApiUrl = getApiUrlFromEnv();
  const result = envApiUrl ?? getDefaultApiUrl(platform, isDev);
  if (isDev) {
    console.log(`[apiConfig] getApiUrl() => platform=${platform}, isDev=${isDev}, envUrl=${envApiUrl}, result=${result}`);
  }
  return result;
}
