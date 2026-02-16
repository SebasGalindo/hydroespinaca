// Fuzzy API Service — extends BaseApiService for DRY request handling
import { BaseApiService, ApiServiceError } from './BaseApiService';
import type {
  FuzzySystem,
  FuzzySystemDetail,
  CloneFuzzySystemRequest,
  SimulateFuzzySystemRequest,
  SimulateFuzzySystemResponse,
  FuzzySystemExport,
} from '../types/fuzzy';

// ==================== Error Alias (backward-compatible) ====================

export const FuzzyApiError = ApiServiceError;
export type FuzzyApiError = ApiServiceError;

// ==================== Service Class ====================

export class FuzzyApiService extends BaseApiService {

  // ────────────────────────────────────────
  //  Fuzzy Systems
  // ────────────────────────────────────────

  async getSystems(): Promise<FuzzySystem[]> {
    return await this.request<FuzzySystem[]>('/fuzzy/systems') ?? [];
  }

  async getSystemById(id: string): Promise<FuzzySystem | null> {
    return this.request<FuzzySystem | null>(`/fuzzy/systems/${id}`, {
      nullOn404: true,
    });
  }

  async getSystemDetail(id: string): Promise<FuzzySystemDetail | null> {
    return this.request<FuzzySystemDetail | null>(`/fuzzy/systems/${id}/detail`, {
      nullOn404: true,
    });
  }

  async deleteSystem(id: string): Promise<void> {
    return this.deleteRequest(`/fuzzy/systems/${id}`);
  }

  // ────────────────────────────────────────
  //  Advanced Operations
  // ────────────────────────────────────────

  async activateSystem(id: string): Promise<FuzzySystem> {
    return this.request<FuzzySystem>(`/fuzzy/systems/${id}/activate`, {
      method: 'POST',
    });
  }

  async cloneSystem(id: string, request?: CloneFuzzySystemRequest): Promise<FuzzySystem> {
    return this.request<FuzzySystem>(`/fuzzy/systems/${id}/clone`, {
      method: 'POST',
      body: request,
    });
  }

  async exportSystem(id: string): Promise<FuzzySystemExport> {
    return this.request<FuzzySystemExport>(`/fuzzy/systems/${id}/export`);
  }

  async importSystem(exportData: FuzzySystemExport): Promise<FuzzySystem> {
    return this.request<FuzzySystem>('/fuzzy/systems/import', {
      method: 'POST',
      body: exportData,
    });
  }

  async simulateSystem(
    id: string,
    request: SimulateFuzzySystemRequest
  ): Promise<SimulateFuzzySystemResponse> {
    return this.request<SimulateFuzzySystemResponse>(`/fuzzy/systems/${id}/simulate`, {
      method: 'POST',
      body: request,
    });
  }
}

// ==================== Singleton Export ====================

export const fuzzyService = new FuzzyApiService();
