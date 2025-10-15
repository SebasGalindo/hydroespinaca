// API Configuration utilities

/**
 * Detect the platform we're running on
 */
export function detectPlatform(): 'web' | 'mobile' | 'unknown' {
  if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
    return 'mobile';
  }
  if (typeof window !== 'undefined' && typeof document !== 'undefined') {
    return 'web';
  }
  return 'unknown';
}

/**
 * Check if we're in development mode
 */
export function isDevelopmentMode(): boolean {
  if (typeof process !== 'undefined' && process.env) {
    const nodeEnv = process.env.NODE_ENV;
    if (nodeEnv === 'development' || nodeEnv === 'dev') {
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
    return isDev
      ? 'http://localhost/api'
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
  return envApiUrl ?? getDefaultApiUrl(platform, isDev);
}
