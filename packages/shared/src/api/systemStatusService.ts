import type { SystemStatusResponse } from '../types/systemStatus';
import { getApiUrl } from '../utils/apiConfig';

/**
 * Service for fetching system status from the BFF
 */
export class SystemStatusService {
  private baseUrl: string;

  constructor() {
    // Use centralized API URL configuration
    // This handles environment variables correctly across platforms
    this.baseUrl = getApiUrl();
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
