/**
 * API client utilities for making requests to the BFF
 * Uses centralized API URL configuration
 */
import { getApiUrl } from '@hydroespinaca/shared/utils/apiConfig';

/**
 * Get the base API URL
 * In browser context with proper nginx proxy, we can use relative URLs
 * Otherwise, use the configured API URL
 */
export function getBaseUrl(): string {
  // In browser with proper domain (behind nginx proxy), use relative paths
  // This works because nginx proxies /api to the BFF service
  if (typeof window !== 'undefined' && window.location.hostname !== 'localhost') {
    return '/api';
  }
  
  // Otherwise, use the configured API URL
  return getApiUrl();
}

/**
 * Make an authenticated request to the BFF
 * Automatically includes credentials and proper headers
 */
export async function apiRequest<T = any>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const baseUrl = getBaseUrl();
  const url = `${baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const response = await fetch(url, {
    ...options,
    credentials: 'include', // Always include cookies
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error = new Error(`API request failed: ${response.statusText}`);
    (error as any).status = response.status;
    (error as any).response = response;
    throw error;
  }

  // Handle empty responses (204 No Content, etc)
  const contentType = response.headers.get('content-type');
  if (!contentType || !contentType.includes('application/json')) {
    return null as T;
  }

  return response.json();
}

/**
 * GET request helper
 */
export async function apiGet<T = any>(endpoint: string): Promise<T> {
  return apiRequest<T>(endpoint, { method: 'GET' });
}

/**
 * POST request helper
 */
export async function apiPost<T = any>(endpoint: string, data?: any): Promise<T> {
  const options: RequestInit = {
    method: 'POST',
  };
  
  if (data !== undefined) {
    options.body = JSON.stringify(data);
  }
  
  return apiRequest<T>(endpoint, options);
}

/**
 * PUT request helper
 */
export async function apiPut<T = any>(endpoint: string, data?: any): Promise<T> {
  const options: RequestInit = {
    method: 'PUT',
  };
  
  if (data !== undefined) {
    options.body = JSON.stringify(data);
  }
  
  return apiRequest<T>(endpoint, options);
}

/**
 * DELETE request helper
 */
export async function apiDelete<T = any>(endpoint: string): Promise<T> {
  return apiRequest<T>(endpoint, { method: 'DELETE' });
}
