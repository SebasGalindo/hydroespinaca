// Fuzzy API Service — extends BaseApiService for DRY request handling
import { BaseApiService, ApiServiceError } from './BaseApiService';
import type {
  FuzzySystem,
  FuzzyVariable,
  FuzzyTerm,
  FuzzyRule,
  FuzzySystemDetail,
  CloneFuzzySystemRequest,
  CreateFuzzySystemRequest,
  UpdateFuzzySystemRequest,
  UpdateFuzzySystemStatusRequest,
  CreateFuzzyVariableRequest,
  UpdateFuzzyVariableRequest,
  CreateFuzzyTermRequest,
  UpdateFuzzyTermRequest,
  CreateFuzzyRuleRequest,
  UpdateFuzzyRuleRequest,
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

  async createSystem(request: CreateFuzzySystemRequest): Promise<FuzzySystem> {
    return this.request<FuzzySystem>('/fuzzy/systems', {
      method: 'POST',
      body: request,
    });
  }

  async updateSystem(id: string, request: UpdateFuzzySystemRequest): Promise<FuzzySystem> {
    return this.request<FuzzySystem>(`/fuzzy/systems/${id}`, {
      method: 'PUT',
      body: request,
    });
  }

  async updateSystemStatus(id: string, request: UpdateFuzzySystemStatusRequest): Promise<FuzzySystem> {
    return this.request<FuzzySystem>(`/fuzzy/systems/${id}/status`, {
      method: 'PATCH',
      body: request,
    });
  }

  // ────────────────────────────────────────
  //  Fuzzy Variables
  // ────────────────────────────────────────

  async getVariablesBySystem(systemId: string): Promise<FuzzyVariable[]> {
    return await this.request<FuzzyVariable[]>(`/fuzzy/systems/${systemId}/variables`) ?? [];
  }

  async createVariable(request: CreateFuzzyVariableRequest): Promise<FuzzyVariable> {
    return this.request<FuzzyVariable>('/fuzzy/variables', {
      method: 'POST',
      body: request,
    });
  }

  async updateVariable(id: string, request: UpdateFuzzyVariableRequest): Promise<FuzzyVariable> {
    return this.request<FuzzyVariable>(`/fuzzy/variables/${id}`, {
      method: 'PUT',
      body: request,
    });
  }

  async deleteVariable(id: string): Promise<void> {
    return this.deleteRequest(`/fuzzy/variables/${id}`);
  }

  // ────────────────────────────────────────
  //  Fuzzy Terms
  // ────────────────────────────────────────

  async getTermsByVariable(variableId: string): Promise<FuzzyTerm[]> {
    return await this.request<FuzzyTerm[]>(`/fuzzy/variables/${variableId}/terms`) ?? [];
  }

  async createTerm(request: CreateFuzzyTermRequest): Promise<FuzzyTerm> {
    return this.request<FuzzyTerm>('/fuzzy/terms', {
      method: 'POST',
      body: request,
    });
  }

  async updateTerm(id: string, request: UpdateFuzzyTermRequest): Promise<FuzzyTerm> {
    return this.request<FuzzyTerm>(`/fuzzy/terms/${id}`, {
      method: 'PUT',
      body: request,
    });
  }

  async deleteTerm(id: string): Promise<void> {
    return this.deleteRequest(`/fuzzy/terms/${id}`);
  }

  // ────────────────────────────────────────
  //  Fuzzy Rules
  // ────────────────────────────────────────

  async getRulesBySystem(systemId: string): Promise<FuzzyRule[]> {
    return await this.request<FuzzyRule[]>(`/fuzzy/systems/${systemId}/rules`) ?? [];
  }

  async createRule(request: CreateFuzzyRuleRequest): Promise<FuzzyRule> {
    return this.request<FuzzyRule>('/fuzzy/rules', {
      method: 'POST',
      body: request,
    });
  }

  async updateRule(id: string, request: UpdateFuzzyRuleRequest): Promise<FuzzyRule> {
    return this.request<FuzzyRule>(`/fuzzy/rules/${id}`, {
      method: 'PUT',
      body: request,
    });
  }

  async deleteRule(id: string): Promise<void> {
    return this.deleteRequest(`/fuzzy/rules/${id}`);
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
