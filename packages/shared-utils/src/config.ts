// Configuration service for cross-platform API URL management

interface AppConfig {
  apiUrl: string;
  isDevelopment: boolean;
  platform: 'web' | 'mobile' | 'unknown';
}

/**
 * Detects the current platform safely
 */
function detectPlatform(): 'web' | 'mobile' | 'unknown' {
  // React Native check
  if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
    return 'mobile';
  }
  
  // Web check
  if (typeof window !== 'undefined' && typeof document !== 'undefined') {
    return 'web';
  }
  
  // Node.js or other environments
  return 'unknown';
}

/**
 * Checks if we're in development mode
 */
function isDevelopmentMode(): boolean {
  // React Native development check
  try {
    // @ts-ignore - __DEV__ may not be available
    if (typeof __DEV__ !== 'undefined') {
      // @ts-ignore
      return __DEV__;
    }
  } catch (error) {
    // __DEV__ not available
  }
  
  // Web development check
  if (typeof window !== 'undefined' && window.location) {
    const hostname = window.location.hostname;
    return hostname === 'localhost' || 
           hostname.includes('127.0.0.1') || 
           hostname.includes('172.31') ||
           hostname.includes('192.168') ||
           hostname.includes('10.0.2.2');
  }
  
  // Node.js environment check
  if (typeof process !== 'undefined' && process.env.NODE_ENV) {
    return process.env.NODE_ENV === 'development';
  }
  
  // Default to development for safety
  return true;
}

/**
 * Gets API URL from environment variables based on platform
 */
function getApiUrlFromEnv(): string | null {
  // For React Native (Expo) - try EXPO_PUBLIC_API_URL first
  if (typeof process !== 'undefined' && process.env?.EXPO_PUBLIC_API_URL) {
    return process.env.EXPO_PUBLIC_API_URL;
  }
  
  // For React Native (vanilla) 
  if (typeof process !== 'undefined' && process.env?.REACT_NATIVE_API_URL) {
    return process.env.REACT_NATIVE_API_URL;
  }
  
  // For Create React App or Node.js
  if (typeof process !== 'undefined' && process.env?.REACT_APP_API_URL) {
    return process.env.REACT_APP_API_URL;
  }
  
  // For Vite, the variables are handled at build time and injected into process.env
  // or they should be passed explicitly via constructor
  
  return null;
}

/**
 * Gets the default API URL based on platform and environment
 */
function getDefaultApiUrl(platform: 'web' | 'mobile' | 'unknown', isDev: boolean): string {
  if (platform === 'mobile') {
    // React Native URLs
    return isDev 
      ? 'http://172.31.63.54/api'  // Development server
      : 'https://api.hydroespinaca.online/api';  // Production
  } else if (platform === 'web') {
    // Web URLs  
    return isDev
      ? 'http://172.31.63.54/api'  // Development server
      : 'https://api.hydroespinaca.online/api';  // Production
  } else {
    // Unknown platform - assume production
    return 'https://api.hydroespinaca.online/api';
  }
}

/**
 * Creates the application configuration
 */
function createConfig(): AppConfig {
  const platform = detectPlatform();
  const isDev = isDevelopmentMode();
  
  // Try to get API URL from environment variables first
  const envApiUrl = getApiUrlFromEnv();
  const apiUrl = envApiUrl || getDefaultApiUrl(platform, isDev);
  
  return {
    apiUrl,
    isDevelopment: isDev,
    platform,
  };
}

// Create singleton config instance
const config = createConfig();

/**
 * Gets the API base URL for the current environment and platform
 */
export function getApiUrl(): string {
  return config.apiUrl;
}

/**
 * Checks if the app is running in development mode
 */
export function isDevelopment(): boolean {
  return config.isDevelopment;
}

/**
 * Gets the current platform
 */
export function getPlatform(): 'web' | 'mobile' | 'unknown' {
  return config.platform;
}

/**
 * Gets the full configuration object
 */
export function getConfig(): AppConfig {
  return { ...config }; // Return a copy to prevent mutations
}

/**
 * For debugging - logs the current configuration
 */
export function logConfig(): void {
  console.log('App Configuration:', {
    apiUrl: config.apiUrl,
    isDevelopment: config.isDevelopment,
    platform: config.platform,
  });
}