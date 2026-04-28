import { create } from 'zustand';
import { fuzzyService, FuzzyApiError } from '../api/fuzzyService';
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
  FuzzyEvaluation,
  FuzzyEvaluationsListResponse,
  FuzzyEvaluationStats,
  FuzzyEvaluationListParams,
} from '../types/fuzzy';

// ==================== State Interface ====================

interface FuzzyState {
  // System list
  systems: FuzzySystem[];
  systemsLoading: boolean;
  systemsError: string | null;

  // Selected system detail
  selectedDetail: FuzzySystemDetail | null;
  detailLoading: boolean;
  detailError: string | null;

  // Simulation
  simulationResult: SimulateFuzzySystemResponse | null;
  simulationLoading: boolean;
  simulationError: string | null;

  // Export / Import
  exportData: FuzzySystemExport | null;
  exportLoading: boolean;
  importLoading: boolean;
  exportImportError: string | null;

  // Generic operation (activate, clone, delete)
  operationLoading: boolean;
  operationError: string | null;

  // CRUD operation state
  crudLoading: boolean;
  crudError: string | null;

  // Evaluation history (RF-F07)
  evaluations: FuzzyEvaluation[];
  evaluationsTotalCount: number;
  evaluationsPage: number;
  evaluationsPageSize: number;
  evaluationsTotalPages: number;
  evaluationsHasNext: boolean;
  evaluationsHasPrevious: boolean;
  evaluationsLoading: boolean;
  evaluationsError: string | null;

  evaluationStats: FuzzyEvaluationStats | null;
  evaluationStatsLoading: boolean;
  evaluationStatsError: string | null;
}

// ==================== Actions Interface ====================

interface FuzzyActions {
  // System list
  fetchSystems: () => Promise<void>;

  // System detail
  fetchSystemDetail: (id: string) => Promise<void>;
  clearSelectedDetail: () => void;

  // System CRUD
  createSystem: (request: CreateFuzzySystemRequest) => Promise<FuzzySystem>;
  updateSystem: (id: string, request: UpdateFuzzySystemRequest) => Promise<FuzzySystem>;
  updateSystemStatus: (id: string, request: UpdateFuzzySystemStatusRequest) => Promise<FuzzySystem>;

  // Advanced operations
  activateSystem: (id: string) => Promise<FuzzySystem>;
  cloneSystem: (id: string, request?: CloneFuzzySystemRequest) => Promise<FuzzySystem>;
  deleteSystem: (id: string) => Promise<void>;

  // Variable CRUD
  createVariable: (request: CreateFuzzyVariableRequest) => Promise<FuzzyVariable>;
  updateVariable: (id: string, request: UpdateFuzzyVariableRequest) => Promise<FuzzyVariable>;
  deleteVariable: (id: string) => Promise<void>;

  // Term CRUD
  createTerm: (request: CreateFuzzyTermRequest) => Promise<FuzzyTerm>;
  updateTerm: (id: string, request: UpdateFuzzyTermRequest) => Promise<FuzzyTerm>;
  deleteTerm: (id: string) => Promise<void>;

  // Rule CRUD
  createRule: (request: CreateFuzzyRuleRequest) => Promise<FuzzyRule>;
  updateRule: (id: string, request: UpdateFuzzyRuleRequest) => Promise<FuzzyRule>;
  deleteRule: (id: string) => Promise<void>;

  // Export / Import
  exportSystem: (id: string) => Promise<FuzzySystemExport>;
  importSystem: (exportData: FuzzySystemExport) => Promise<FuzzySystem>;

  // Simulation
  simulateSystem: (id: string, request: SimulateFuzzySystemRequest) => Promise<void>;
  clearSimulation: () => void;

  // Evaluation history (RF-F07)
  fetchEvaluations: (params?: FuzzyEvaluationListParams) => Promise<void>;
  fetchRecentEvaluations: (hours?: number, systemId?: string, page?: number, pageSize?: number) => Promise<void>;
  fetchEvaluationStats: (systemId?: string, days?: number) => Promise<void>;
  clearEvaluations: () => void;

  // Utility
  clearErrors: () => void;
  resetFuzzyStore: () => void;
}

// ==================== Initial State ====================

const initialState: FuzzyState = {
  systems: [],
  systemsLoading: false,
  systemsError: null,

  selectedDetail: null,
  detailLoading: false,
  detailError: null,

  simulationResult: null,
  simulationLoading: false,
  simulationError: null,

  exportData: null,
  exportLoading: false,
  importLoading: false,
  exportImportError: null,

  operationLoading: false,
  operationError: null,

  crudLoading: false,
  crudError: null,

  evaluations: [],
  evaluationsTotalCount: 0,
  evaluationsPage: 1,
  evaluationsPageSize: 20,
  evaluationsTotalPages: 0,
  evaluationsHasNext: false,
  evaluationsHasPrevious: false,
  evaluationsLoading: false,
  evaluationsError: null,

  evaluationStats: null,
  evaluationStatsLoading: false,
  evaluationStatsError: null,
};

// ==================== Store ====================

const extractErrorMessage = (error: unknown): string => {
  if (error instanceof FuzzyApiError) {
    if (error.status === 401) return 'Sesión expirada. Por favor, inicia sesión nuevamente.';
    if (error.status === 403) return 'No tienes permisos para realizar esta acción.';
    if (error.status === 404) return 'Sistema difuso no encontrado.';
    if (error.status === 409) return 'Ya existe un sistema con ese nombre.';
    if (error.status === 422) return error.message;
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'Error desconocido';
};

export const useFuzzyStore = create<FuzzyState & FuzzyActions>()((set, get) => ({
  ...initialState,

  // ────────────────────────────────────────
  //  System List
  // ────────────────────────────────────────

  fetchSystems: async () => {
    set({ systemsLoading: true, systemsError: null });
    try {
      const data = await fuzzyService.getSystems();
      set({ systems: data, systemsLoading: false });
    } catch (error) {
      set({ systemsError: extractErrorMessage(error), systemsLoading: false });
    }
  },

  // ────────────────────────────────────────
  //  System Detail
  // ────────────────────────────────────────

  fetchSystemDetail: async (id: string) => {
    set({ detailLoading: true, detailError: null });
    try {
      const data = await fuzzyService.getSystemDetail(id);
      if (!data) {
        set({ detailError: 'Sistema difuso no encontrado.', detailLoading: false });
        return;
      }
      set({ selectedDetail: data, detailLoading: false });
    } catch (error) {
      set({ detailError: extractErrorMessage(error), detailLoading: false });
    }
  },

  clearSelectedDetail: () => {
    set({ selectedDetail: null, detailError: null });
  },

  // ────────────────────────────────────────
  //  Advanced Operations
  // ────────────────────────────────────────

  activateSystem: async (id: string) => {
    set({ operationLoading: true, operationError: null });
    try {
      const activated = await fuzzyService.activateSystem(id);
      // Update the system list: set all to INACTIVE except the activated one
      set((state) => ({
        systems: state.systems.map((s) =>
          s.id === id
            ? { ...s, status: 'ACTIVE' as const }
            : s.status === 'ACTIVE'
              ? { ...s, status: 'INACTIVE' as const }
              : s
        ),
        operationLoading: false,
      }));
      return activated;
    } catch (error) {
      set({ operationError: extractErrorMessage(error), operationLoading: false });
      throw error;
    }
  },

  cloneSystem: async (id: string, request?: CloneFuzzySystemRequest) => {
    set({ operationLoading: true, operationError: null });
    try {
      const cloned = await fuzzyService.cloneSystem(id, request);
      set((state) => ({
        systems: [cloned, ...state.systems],
        operationLoading: false,
      }));
      return cloned;
    } catch (error) {
      set({ operationError: extractErrorMessage(error), operationLoading: false });
      throw error;
    }
  },

  deleteSystem: async (id: string) => {
    set({ operationLoading: true, operationError: null });
    try {
      await fuzzyService.deleteSystem(id);
      set((state) => ({
        systems: state.systems.filter((s) => s.id !== id),
        selectedDetail:
          state.selectedDetail?.system.id === id ? null : state.selectedDetail,
        operationLoading: false,
      }));
    } catch (error) {
      set({ operationError: extractErrorMessage(error), operationLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  System CRUD
  // ────────────────────────────────────────

  createSystem: async (request: CreateFuzzySystemRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const created = await fuzzyService.createSystem(request);
      set((state) => ({
        systems: [created, ...state.systems],
        crudLoading: false,
      }));
      return created;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  updateSystem: async (id: string, request: UpdateFuzzySystemRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const updated = await fuzzyService.updateSystem(id, request);
      set((state) => ({
        systems: state.systems.map((s) => (s.id === id ? updated : s)),
        selectedDetail: state.selectedDetail?.system.id === id
          ? { ...state.selectedDetail, system: updated }
          : state.selectedDetail,
        crudLoading: false,
      }));
      return updated;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  updateSystemStatus: async (id: string, request: UpdateFuzzySystemStatusRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const updated = await fuzzyService.updateSystemStatus(id, request);
      set((state) => ({
        systems: state.systems.map((s) => (s.id === id ? updated : s)),
        selectedDetail: state.selectedDetail?.system.id === id
          ? { ...state.selectedDetail, system: updated }
          : state.selectedDetail,
        crudLoading: false,
      }));
      return updated;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Variable CRUD
  // ────────────────────────────────────────

  createVariable: async (request: CreateFuzzyVariableRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const created = await fuzzyService.createVariable(request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            variables: [...state.selectedDetail.variables, created],
          },
          crudLoading: false,
        };
      });
      return created;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  updateVariable: async (id: string, request: UpdateFuzzyVariableRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const updated = await fuzzyService.updateVariable(id, request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            variables: state.selectedDetail.variables.map((v) =>
              v.id === id ? updated : v
            ),
          },
          crudLoading: false,
        };
      });
      return updated;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  deleteVariable: async (id: string) => {
    set({ crudLoading: true, crudError: null });
    try {
      await fuzzyService.deleteVariable(id);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            variables: state.selectedDetail.variables.filter((v) => v.id !== id),
            terms: state.selectedDetail.terms.filter((t) => t.variableId !== id),
          },
          crudLoading: false,
        };
      });
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Term CRUD
  // ────────────────────────────────────────

  createTerm: async (request: CreateFuzzyTermRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const created = await fuzzyService.createTerm(request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            terms: [...state.selectedDetail.terms, created],
          },
          crudLoading: false,
        };
      });
      return created;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  updateTerm: async (id: string, request: UpdateFuzzyTermRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const updated = await fuzzyService.updateTerm(id, request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            terms: state.selectedDetail.terms.map((t) =>
              t.id === id ? updated : t
            ),
          },
          crudLoading: false,
        };
      });
      return updated;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  deleteTerm: async (id: string) => {
    set({ crudLoading: true, crudError: null });
    try {
      await fuzzyService.deleteTerm(id);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            terms: state.selectedDetail.terms.filter((t) => t.id !== id),
          },
          crudLoading: false,
        };
      });
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Rule CRUD
  // ────────────────────────────────────────

  createRule: async (request: CreateFuzzyRuleRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const created = await fuzzyService.createRule(request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            rules: [...state.selectedDetail.rules, created],
          },
          crudLoading: false,
        };
      });
      return created;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  updateRule: async (id: string, request: UpdateFuzzyRuleRequest) => {
    set({ crudLoading: true, crudError: null });
    try {
      const updated = await fuzzyService.updateRule(id, request);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            rules: state.selectedDetail.rules.map((r) =>
              r.id === id ? updated : r
            ),
          },
          crudLoading: false,
        };
      });
      return updated;
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  deleteRule: async (id: string) => {
    set({ crudLoading: true, crudError: null });
    try {
      await fuzzyService.deleteRule(id);
      set((state) => {
        if (!state.selectedDetail) return { crudLoading: false };
        return {
          selectedDetail: {
            ...state.selectedDetail,
            rules: state.selectedDetail.rules.filter((r) => r.id !== id),
          },
          crudLoading: false,
        };
      });
    } catch (error) {
      set({ crudError: extractErrorMessage(error), crudLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Export / Import
  // ────────────────────────────────────────

  exportSystem: async (id: string) => {
    set({ exportLoading: true, exportImportError: null });
    try {
      const data = await fuzzyService.exportSystem(id);
      set({ exportData: data, exportLoading: false });
      return data;
    } catch (error) {
      set({ exportImportError: extractErrorMessage(error), exportLoading: false });
      throw error;
    }
  },

  importSystem: async (exportData: FuzzySystemExport) => {
    set({ importLoading: true, exportImportError: null });
    try {
      const imported = await fuzzyService.importSystem(exportData);
      set((state) => ({
        systems: [imported, ...state.systems],
        importLoading: false,
      }));
      return imported;
    } catch (error) {
      set({ exportImportError: extractErrorMessage(error), importLoading: false });
      throw error;
    }
  },

  // ────────────────────────────────────────
  //  Simulation
  // ────────────────────────────────────────

  simulateSystem: async (id: string, request: SimulateFuzzySystemRequest) => {
    set({ simulationLoading: true, simulationError: null, simulationResult: null });
    try {
      const data = await fuzzyService.simulateSystem(id, request);
      set({ simulationResult: data, simulationLoading: false });
    } catch (error) {
      set({ simulationError: extractErrorMessage(error), simulationLoading: false });
    }
  },

  clearSimulation: () => {
    set({ simulationResult: null, simulationError: null });
  },

  // ────────────────────────────────────────
  //  Evaluation History (RF-F07)
  // ────────────────────────────────────────

  fetchEvaluations: async (params?: FuzzyEvaluationListParams) => {
    set({ evaluationsLoading: true, evaluationsError: null });
    try {
      // When filtering by system + date range, use the system-specific endpoint
      // which correctly applies both filters together (the generic endpoint
      // ignores date filters when systemId is present).
      const useSystemEndpoint =
        params?.systemId && (params.startDate || params.endDate);

      const data: FuzzyEvaluationsListResponse = useSystemEndpoint
        ? await fuzzyService.getEvaluationsBySystem(params!.systemId!, {
            startDate: params!.startDate,
            endDate: params!.endDate,
            page: params!.page,
            pageSize: params!.pageSize,
            sortOrder: params!.sortOrder,
          })
        : await fuzzyService.getEvaluations(params);

      set({
        evaluations: data.evaluations,
        evaluationsTotalCount: data.totalCount,
        evaluationsPage: data.page,
        evaluationsPageSize: data.pageSize,
        evaluationsTotalPages: data.totalPages,
        evaluationsHasNext: data.hasNext,
        evaluationsHasPrevious: data.hasPrevious,
        evaluationsLoading: false,
      });
    } catch (error) {
      set({ evaluationsError: extractErrorMessage(error), evaluationsLoading: false });
    }
  },

  fetchRecentEvaluations: async (
    hours: number = 24,
    systemId?: string,
    page: number = 1,
    pageSize: number = 20,
  ) => {
    set({ evaluationsLoading: true, evaluationsError: null });
    try {
      const data: FuzzyEvaluationsListResponse = await fuzzyService.getRecentEvaluations(
        hours, systemId, page, pageSize,
      );
      set({
        evaluations: data.evaluations,
        evaluationsTotalCount: data.totalCount,
        evaluationsPage: data.page,
        evaluationsPageSize: data.pageSize,
        evaluationsTotalPages: data.totalPages,
        evaluationsHasNext: data.hasNext,
        evaluationsHasPrevious: data.hasPrevious,
        evaluationsLoading: false,
      });
    } catch (error) {
      set({ evaluationsError: extractErrorMessage(error), evaluationsLoading: false });
    }
  },

  fetchEvaluationStats: async (systemId?: string, days: number = 7) => {
    set({ evaluationStatsLoading: true, evaluationStatsError: null });
    try {
      const data = await fuzzyService.getEvaluationStats(systemId, days);
      set({ evaluationStats: data, evaluationStatsLoading: false });
    } catch (error) {
      set({ evaluationStatsError: extractErrorMessage(error), evaluationStatsLoading: false });
    }
  },

  clearEvaluations: () => {
    set({
      evaluations: [],
      evaluationsTotalCount: 0,
      evaluationsPage: 1,
      evaluationsTotalPages: 0,
      evaluationsHasNext: false,
      evaluationsHasPrevious: false,
      evaluationsError: null,
      evaluationStats: null,
      evaluationStatsError: null,
    });
  },

  // ────────────────────────────────────────
  //  Utility
  // ────────────────────────────────────────

  clearErrors: () => {
    set({
      systemsError: null,
      detailError: null,
      simulationError: null,
      exportImportError: null,
      operationError: null,
      crudError: null,
      evaluationsError: null,
      evaluationStatsError: null,
    });
  },

  resetFuzzyStore: () => {
    set(initialState);
  },
}));
