import type { SystemStatusResponse } from '../types/systemStatus';
import { getApiUrl } from '../utils/apiConfig';
import { authFetch } from '../utils/authFetch';

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
   * Uses authFetch for automatic 401 handling and redirect
   */
  async getSystemStatus(): Promise<SystemStatusResponse> {
    const response = await authFetch(`${this.baseUrl}/system/status`, {
      method: 'GET',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch system status: ${response.statusText}`);
    }

    return response.json();
  }
}

// Singleton instance
export const systemStatusService = new SystemStatusService();
