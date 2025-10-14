// API Configuration utilities

/**
 * Detect the platform we're running on
 */
export function detectPlatform(): 'web' | 'mobile' | 'unknown' {
  // Check for React Native
  if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
    return 'mobile';
  }

  // Check for browser environment
  if (typeof window !== 'undefined' && typeof document !== 'undefined') {
    return 'web';
  }

  return 'unknown';
}

/**
 * Check if we're in development mode
 */
export function isDevelopmentMode(): boolean {
  // Node environment
  if (typeof process !== 'undefined' && process.env) {
    const nodeEnv = process.env.NODE_ENV;
    if (nodeEnv === 'development' || nodeEnv === 'dev') {
      return true;
    }
  }

  // Vite environment variables
  if (typeof import.meta !== 'undefined' && import.meta.env) {
    const mode = import.meta.env.MODE || import.meta.env.VITE_MODE;
    if (mode === 'development' || mode === 'dev') {
      return true;
    }
  }

  // Default to production
  return false;
}

/**
 * Get API URL from environment variables
 */
export function getApiUrlFromEnv(): string | null {
  // Check Vite environment variables (web)
  if (typeof import.meta !== 'undefined' && import.meta.env) {
    const viteApiUrl = import.meta.env.VITE_API_URL;
    if (viteApiUrl) {
      return viteApiUrl;
    }
  }

  // Check Next.js environment variables (web)
  if (typeof process !== 'undefined' && process.env) {
    const nextApiUrl = process.env.NEXT_PUBLIC_API_URL;
    if (nextApiUrl) {
      return nextApiUrl;
    }
  }

  // Check Expo environment variables (mobile)
  if (typeof process !== 'undefined' && process.env) {
    const expoApiUrl = process.env.EXPO_PUBLIC_API_URL;
    if (expoApiUrl) {
      return expoApiUrl;
    }
  }

  return null;
}

/**
 * Get default API URL based on platform and environment
 */
function getDefaultApiUrl(platform: 'web' | 'mobile' | 'unknown', isDev: boolean): string {
  if (platform === 'mobile') {
    // For mobile, use IP address that works for both Android emulator and physical devices
    return isDev
      ? 'http://localhost/api'  // Development IP
      : 'https://api.hydroespinaca.online/api';  // Production
  } else if (platform === 'web') {
    return isDev
      ? 'http://localhost/api'  // Development IP
      : 'https://api.hydroespinaca.online/api';  // Production
  }

  // Fallback to production URL
  return 'https://api.hydroespinaca.online/api';
}

/**
 * Get the API URL to use for requests
 * Priority: Environment Variables > Default based on platform/env
 */
export function getApiUrl(): string {
  const platform = detectPlatform();
  const isDev = isDevelopmentMode();

  // Try to get from environment variables first
  const envApiUrl = getApiUrlFromEnv();
  if (envApiUrl) {
    return envApiUrl;
  }

  // Fall back to default
  return getDefaultApiUrl(platform, isDev);
}
