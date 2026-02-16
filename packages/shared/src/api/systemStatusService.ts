// System Status Service — extends BaseApiService for DRY request handling
import { BaseApiService } from './BaseApiService';
import type { SystemStatusResponse } from '../types/systemStatus';

/**
 * Service for fetching system status from the BFF
 */
export class SystemStatusService extends BaseApiService {

  /**
   * Fetches the current system status including sensor readings and actuator jobs
   */
  async getSystemStatus(): Promise<SystemStatusResponse> {
    return this.request<SystemStatusResponse>('/system/status');
  }
}

// Singleton instance
export const systemStatusService = new SystemStatusService();
