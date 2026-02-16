import { create } from 'zustand';
import { fuzzyService, FuzzyApiError } from '../api/fuzzyService';
import type {
  FuzzySystem,
  FuzzySystemDetail,
  CloneFuzzySystemRequest,
  SimulateFuzzySystemRequest,
  SimulateFuzzySystemResponse,
  FuzzySystemExport,
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
}

// ==================== Actions Interface ====================

interface FuzzyActions {
  // System list
  fetchSystems: () => Promise<void>;

  // System detail
  fetchSystemDetail: (id: string) => Promise<void>;
  clearSelectedDetail: () => void;

  // Advanced operations
  activateSystem: (id: string) => Promise<FuzzySystem>;
  cloneSystem: (id: string, request?: CloneFuzzySystemRequest) => Promise<FuzzySystem>;
  deleteSystem: (id: string) => Promise<void>;

  // Export / Import
  exportSystem: (id: string) => Promise<FuzzySystemExport>;
  importSystem: (exportData: FuzzySystemExport) => Promise<FuzzySystem>;

  // Simulation
  simulateSystem: (id: string, request: SimulateFuzzySystemRequest) => Promise<void>;
  clearSimulation: () => void;

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
  //  Utility
  // ────────────────────────────────────────

  clearErrors: () => {
    set({
      systemsError: null,
      detailError: null,
      simulationError: null,
      exportImportError: null,
      operationError: null,
    });
  },

  resetFuzzyStore: () => {
    set(initialState);
  },
}));
