// BI API Service — extends BaseApiService for DRY request handling
import { BaseApiService, ApiServiceError } from './BaseApiService';
import type {
  CostConfigVersion,
  CreateCostConfigVersionRequest,
  UpdateCostConfigVersionRequest,
  ManualConsumptionEntry,
  CreateManualConsumptionEntryRequest,
  BiSummary,
  ProductionRecord,
  CreateProductionRecordRequest,
  OperationalCostRequest,
  OperationalCostResponse,
  ProfitabilityRequest,
  ProfitabilityResponse,
} from '../types/bi';

// ==================== Error Alias (backward-compatible) ====================

export const BiApiError = ApiServiceError;
export type BiApiError = ApiServiceError;

// ==================== Service Class ====================

export class BiApiService extends BaseApiService {

  // ────────────────────────────────────────
  //  Cost Configuration
  // ────────────────────────────────────────

  async getCurrentCostConfig(): Promise<CostConfigVersion | null> {
    return this.request<CostConfigVersion | null>('/bi/cost-config/current', {
      nullOn404: true,
    });
  }

  async getCostConfigVersions(from?: string, to?: string): Promise<CostConfigVersion[]> {
    const params = new URLSearchParams();
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    const qs = params.toString() ? `?${params.toString()}` : '';
    return await this.request<CostConfigVersion[]>(`/bi/cost-config/versions${qs}`) ?? [];
  }

  async createCostConfigVersion(request: CreateCostConfigVersionRequest): Promise<CostConfigVersion> {
    return this.request<CostConfigVersion>('/bi/cost-config/versions', {
      method: 'POST',
      body: request,
    });
  }

  async updateCostConfigVersion(id: string, request: UpdateCostConfigVersionRequest): Promise<CostConfigVersion> {
    return this.request<CostConfigVersion>(`/bi/cost-config/versions/${id}`, {
      method: 'PUT',
      body: request,
    });
  }

  async deleteCostConfigVersion(id: string): Promise<void> {
    return this.deleteRequest(`/bi/cost-config/versions/${id}`);
  }

  // ────────────────────────────────────────
  //  Consumption Entries
  // ────────────────────────────────────────

  async getConsumptionEntries(from: string, to: string, type?: string): Promise<ManualConsumptionEntry[]> {
    const params = new URLSearchParams({ from, to });
    if (type) params.append('type', type);
    return await this.request<ManualConsumptionEntry[]>(`/bi/consumption-entries?${params.toString()}`) ?? [];
  }

  async createConsumptionEntry(request: CreateManualConsumptionEntryRequest): Promise<ManualConsumptionEntry> {
    return this.request<ManualConsumptionEntry>('/bi/consumption-entries', {
      method: 'POST',
      body: request,
    });
  }

  async deleteConsumptionEntry(id: string): Promise<void> {
    return this.deleteRequest(`/bi/consumption-entries/${id}`);
  }

  async getConsumptionSummary(from: string, to: string): Promise<BiSummary> {
    return this.request<BiSummary>(`/bi/consumption-entries/summary?from=${from}&to=${to}`);
  }

  // ────────────────────────────────────────
  //  Production Records
  // ────────────────────────────────────────

  async getProductionRecords(): Promise<ProductionRecord[]> {
    return await this.request<ProductionRecord[]>('/bi/production-records') ?? [];
  }

  async getProductionRecord(id: string): Promise<ProductionRecord> {
    return this.request<ProductionRecord>(`/bi/production-records/${id}`);
  }

  async createProductionRecord(request: CreateProductionRecordRequest): Promise<ProductionRecord> {
    return this.request<ProductionRecord>('/bi/production-records', {
      method: 'POST',
      body: request,
    });
  }

  async deleteProductionRecord(id: string): Promise<void> {
    return this.deleteRequest(`/bi/production-records/${id}`);
  }

  // ────────────────────────────────────────
  //  Operational Cost & Profitability
  // ────────────────────────────────────────

  async calculateOperationalCost(request: OperationalCostRequest): Promise<OperationalCostResponse> {
    return this.request<OperationalCostResponse>('/bi/operational-cost/calculate', {
      method: 'POST',
      body: request,
    });
  }

  async calculateProfitability(request: ProfitabilityRequest): Promise<ProfitabilityResponse> {
    return this.request<ProfitabilityResponse>('/bi/profitability/calculate', {
      method: 'POST',
      body: request,
    });
  }
}

// ==================== Singleton Export ====================

export const biService = new BiApiService();
