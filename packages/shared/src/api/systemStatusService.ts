import type { SystemStatusResponse } from '../types/systemStatus';

/**
 * Service for fetching system status from the BFF
 */
export class SystemStatusService {
  private baseUrl: string;

  constructor() {
    // Determine base URL from environment or window.location
    if (process.env.NEXT_PUBLIC_API_URL) {
      this.baseUrl = process.env.NEXT_PUBLIC_API_URL;
    } else if (typeof window !== 'undefined' && window.location) {
      // Use bracket notation to avoid TypeScript DOM type dependency
      const origin = (window.location as any)['origin'];
      this.baseUrl = origin ? `${origin}/api` : 'http://localhost/api';
    } else {
      this.baseUrl = 'http://localhost/api';
    }
  }

  /**
   * Fetches the current system status including sensor readings and actuator jobs
   * Uses credentials: 'include' to automatically send the X-Session-Id cookie
   */
  async getSystemStatus(): Promise<SystemStatusResponse> {
    const response = await fetch(`${this.baseUrl}/system/status`, {
      method: 'GET',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized: Session expired or invalid');
      }
      throw new Error(`Failed to fetch system status: ${response.statusText}`);
    }

    return response.json();
  }
}

// Singleton instance
export const systemStatusService = new SystemStatusService();
