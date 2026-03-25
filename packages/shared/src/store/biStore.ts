import { create } from 'zustand';
import { biService, BiApiError } from '../api/biService';
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

// ==================== State Interface ====================

interface BiState {
  // Cost configuration
  currentCostConfig: CostConfigVersion | null;
  costConfigVersions: CostConfigVersion[];
  costConfigLoading: boolean;
  costConfigError: string | null;

  // Consumption entries
  consumptionEntries: ManualConsumptionEntry[];
  consumptionSummary: BiSummary | null;
  consumptionLoading: boolean;
  consumptionError: string | null;

  // Production records
  productionRecords: ProductionRecord[];
  productionLoading: boolean;
  productionError: string | null;

  // Operational cost
  operationalCostResult: OperationalCostResponse | null;
  operationalCostLoading: boolean;
  operationalCostError: string | null;

  // Profitability
  profitabilityResult: ProfitabilityResponse | null;
  profitabilityLoading: boolean;
  profitabilityError: string | null;
}

// ==================== Actions Interface ====================

interface BiActions {
  // Cost configuration actions
  fetchCurrentCostConfig: () => Promise<void>;
  fetchCostConfigVersions: (from?: string, to?: string) => Promise<void>;
  createCostConfigVersion: (request: CreateCostConfigVersionRequest) => Promise<CostConfigVersion>;
  updateCostConfigVersion: (id: string, request: UpdateCostConfigVersionRequest) => Promise<CostConfigVersion>;
  deleteCostConfigVersion: (id: string) => Promise<void>;

  // Consumption entry actions
  fetchConsumptionEntries: (from: string, to: string, type?: string) => Promise<void>;
  createConsumptionEntry: (request: CreateManualConsumptionEntryRequest) => Promise<ManualConsumptionEntry>;
  deleteConsumptionEntry: (id: string) => Promise<void>;
  fetchConsumptionSummary: (from: string, to: string) => Promise<void>;

  // Production record actions
  fetchProductionRecords: () => Promise<void>;
  createProductionRecord: (request: CreateProductionRecordRequest) => Promise<ProductionRecord>;
  deleteProductionRecord: (id: string) => Promise<void>;

  // Operational cost actions
  calculateOperationalCost: (request: OperationalCostRequest) => Promise<void>;

  // Profitability actions
  calculateProfitability: (request: ProfitabilityRequest) => Promise<void>;

  // Utility
  clearErrors: () => void;
  resetBiStore: () => void;
}

// ==================== Initial State ====================

const initialState: BiState = {
  currentCostConfig: null,
  costConfigVersions: [],
  costConfigLoading: false,
  costConfigError: null,

  consumptionEntries: [],
  consumptionSummary: null,
  consumptionLoading: false,
  consumptionError: null,

  productionRecords: [],
  productionLoading: false,
  productionError: null,

  operationalCostResult: null,
  operationalCostLoading: false,
  operationalCostError: null,

  profitabilityResult: null,
  profitabilityLoading: false,
  profitabilityError: null,
};

// ==================== Store ====================

const extractErrorMessage = (error: unknown): string => {
  if (error instanceof BiApiError) {
    if (error.status === 401) return 'Sesión expirada. Por favor, inicia sesión nuevamente.';
    if (error.status === 403) return 'No tienes permisos para realizar esta acción.';
    if (error.status === 404) return 'Recurso no encontrado.';
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'Error desconocido';
};

export const useBiStore = create<BiState & BiActions>()((set, get) => ({
  ...initialState,

  // ────────────────────────────────────────
  //  Cost Configuration
  // ────────────────────────────────────────

  fetchCurrentCostConfig: async () => {
    set({ costConfigLoading: true, costConfigError: null });
    try {
      const data = await biService.getCurrentCostConfig();
      set({ currentCostConfig: data, costConfigLoading: false });
    } catch (error) {
      set({ costConfigError: extractErrorMessage(error), costConfigLoading: false });
    }
  },

  fetchCostConfigVersions: async (from?: string, to?: string) => {
    set({ costConfigLoading: true, costConfigError: null });
    try {
      const data = await biService.getCostConfigVersions(from, to);
      set({ costConfigVersions: data, costConfigLoading: false });
    } catch (error) {
      set({ costConfigError: extractErrorMessage(error), costConfigLoading: false });
    }
  },

  createCostConfigVersion: async (request: CreateCostConfigVersionRequest) => {
    set({ costConfigLoading: true, costConfigError: null });
    try {
      const created = await biService.createCostConfigVersion(request);
      // Re-fetch all versions and current since the backend recalculates
      // EffectiveTo and IsActive for all versions on create
      const [current, versions] = await Promise.all([
        biService.getCurrentCostConfig(),
        biService.getCostConfigVersions(),
      ]);
      set({
        costConfigVersions: versions,
        currentCostConfig: current,
        costConfigLoading: false,
      });
      return created;
    } catch (error) {
      set({ costConfigError: extractErrorMessage(error), costConfigLoading: false });
      throw error;
    }
  },

  updateCostConfigVersion: async (id: string, request: UpdateCostConfigVersionRequest) => {
    set({ costConfigLoading: true, costConfigError: null });
    try {
      const updated = await biService.updateCostConfigVersion(id, request);
      const [current, versions] = await Promise.all([
        biService.getCurrentCostConfig(),
        biService.getCostConfigVersions(),
      ]);
      set({
        costConfigVersions: versions,
        currentCostConfig: current,
        costConfigLoading: false,
      });
      return updated;
    } catch (error) {
      set({ costConfigError: extractErrorMessage(error), costConfigLoading: false });
      throw error;
    }
  },

  deleteCostConfigVersion: async (id: string) => {
    set({ costConfigLoading: true, costConfigError: null });
    try {
      await biService.deleteCostConfigVersion(id);
      const [current, versions] = await Promise.all([
        biService.getCurrentCostConfig(),
        biService.getCostConfigVersions(),
      ]);
      set({
        costConfigVersions: versions,
        currentCostConfig: current,
        costConfigLoading: false,
      });
    } catch (error) {
      set({ costConfigError: extractErrorMessage(error), costConfigLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Consumption Entries
  // ────────────────────────────────────────

  fetchConsumptionEntries: async (from: string, to: string, type?: string) => {
    set({ consumptionLoading: true, consumptionError: null });
    try {
      const data = await biService.getConsumptionEntries(from, to, type);
      set({ consumptionEntries: data, consumptionLoading: false });
    } catch (error) {
      set({ consumptionError: extractErrorMessage(error), consumptionLoading: false });
    }
  },

  createConsumptionEntry: async (request: CreateManualConsumptionEntryRequest) => {
    set({ consumptionLoading: true, consumptionError: null });
    try {
      const created = await biService.createConsumptionEntry(request);
      set((state) => ({
        consumptionEntries: [created, ...state.consumptionEntries],
        consumptionLoading: false,
      }));
      return created;
    } catch (error) {
      set({ consumptionError: extractErrorMessage(error), consumptionLoading: false });
      throw error;
    }
  },

  deleteConsumptionEntry: async (id: string) => {
    set({ consumptionLoading: true, consumptionError: null });
    try {
      await biService.deleteConsumptionEntry(id);
      set((state) => ({
        consumptionEntries: state.consumptionEntries.filter((e) => e.id !== id),
        consumptionLoading: false,
      }));
    } catch (error) {
      set({ consumptionError: extractErrorMessage(error), consumptionLoading: false });
      throw error;
    }
  },

  fetchConsumptionSummary: async (from: string, to: string) => {
    set({ consumptionLoading: true, consumptionError: null });
    try {
      const data = await biService.getConsumptionSummary(from, to);
      set({ consumptionSummary: data, consumptionLoading: false });
    } catch (error) {
      set({ consumptionError: extractErrorMessage(error), consumptionLoading: false });
    }
  },

  // ────────────────────────────────────────
  //  Production Records
  // ────────────────────────────────────────

  fetchProductionRecords: async () => {
    set({ productionLoading: true, productionError: null });
    try {
      const data = await biService.getProductionRecords();
      set({ productionRecords: data, productionLoading: false });
    } catch (error) {
      set({ productionError: extractErrorMessage(error), productionLoading: false });
    }
  },

  createProductionRecord: async (request: CreateProductionRecordRequest) => {
    set({ productionLoading: true, productionError: null });
    try {
      const created = await biService.createProductionRecord(request);
      set((state) => ({
        productionRecords: [created, ...state.productionRecords],
        productionLoading: false,
      }));
      return created;
    } catch (error) {
      set({ productionError: extractErrorMessage(error), productionLoading: false });
      throw error;
    }
  },

  deleteProductionRecord: async (id: string) => {
    set({ productionLoading: true, productionError: null });
    try {
      await biService.deleteProductionRecord(id);
      set((state) => ({
        productionRecords: state.productionRecords.filter((r) => r.id !== id),
        productionLoading: false,
      }));
    } catch (error) {
      set({ productionError: extractErrorMessage(error), productionLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Operational Cost
  // ────────────────────────────────────────

  calculateOperationalCost: async (request: OperationalCostRequest) => {
    set({ operationalCostLoading: true, operationalCostError: null, operationalCostResult: null });
    try {
      const data = await biService.calculateOperationalCost(request);
      set({ operationalCostResult: data, operationalCostLoading: false });
    } catch (error) {
      set({ operationalCostError: extractErrorMessage(error), operationalCostLoading: false });
    }
  },

  // ────────────────────────────────────────
  //  Profitability
  // ────────────────────────────────────────

  calculateProfitability: async (request: ProfitabilityRequest) => {
    set({ profitabilityLoading: true, profitabilityError: null, profitabilityResult: null });
    try {
      const data = await biService.calculateProfitability(request);
      set({ profitabilityResult: data, profitabilityLoading: false });
    } catch (error) {
      set({ profitabilityError: extractErrorMessage(error), profitabilityLoading: false });
    }
  },

  // ────────────────────────────────────────
  //  Utility
  // ────────────────────────────────────────

  clearErrors: () => {
    set({
      costConfigError: null,
      consumptionError: null,
      productionError: null,
      operationalCostError: null,
      profitabilityError: null,
    });
  },

  resetBiStore: () => {
    set(initialState);
  },
}));
