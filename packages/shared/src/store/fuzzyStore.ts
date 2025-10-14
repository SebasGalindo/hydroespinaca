import { create } from 'zustand';
import { 
  SimpleFuzzySystem, 
  SimpleFuzzyVariable, 
  SimpleFuzzyTerm, 
  SimpleFuzzyRule, 
  SimpleFuzzyRoutine,
  FuzzySystemStore 
} from '../types/fuzzyTypes';
import { 
  mockFuzzySystems, 
  mockFuzzyVariables, 
  mockFuzzyTerms, 
  mockFuzzyRules, 
  mockFuzzyRoutines 
} from '../api/fuzzyPlaceholders';

export const useFuzzyStore = create<FuzzySystemStore>((set, get) => ({
  // State
  fuzzySystems: [],
  fuzzyVariables: [],
  fuzzyTerms: [],
  fuzzyRules: [],
  fuzzyRoutines: [],
  loading: false,
  selectedSystem: null,

  // Actions
  setFuzzySystems: (systems: SimpleFuzzySystem[]) => 
    set({ fuzzySystems: systems }),

  setFuzzyVariables: (variables: SimpleFuzzyVariable[]) => 
    set({ fuzzyVariables: variables }),

  setFuzzyTerms: (terms: SimpleFuzzyTerm[]) => 
    set({ fuzzyTerms: terms }),

  setFuzzyRules: (rules: SimpleFuzzyRule[]) => 
    set({ fuzzyRules: rules }),

  setFuzzyRoutines: (routines: SimpleFuzzyRoutine[]) => 
    set({ fuzzyRoutines: routines }),

  setLoading: (loading: boolean) => 
    set({ loading }),

  // Initialize with mock data
  initializeFuzzyData: () => {
    set({
      fuzzySystems: mockFuzzySystems,
      fuzzyVariables: mockFuzzyVariables,
      fuzzyTerms: mockFuzzyTerms,
      fuzzyRules: mockFuzzyRules,
      fuzzyRoutines: mockFuzzyRoutines,
      loading: false
    });
  },

  // Add new entities
  addFuzzySystem: (system: SimpleFuzzySystem) => 
    set(state => ({ 
      fuzzySystems: [...state.fuzzySystems, system] 
    })),

  addFuzzyVariable: (variable: SimpleFuzzyVariable) => 
    set(state => ({ 
      fuzzyVariables: [...state.fuzzyVariables, variable] 
    })),

  addFuzzyTerm: (term: SimpleFuzzyTerm) => 
    set(state => ({ 
      fuzzyTerms: [...state.fuzzyTerms, term] 
    })),

  addFuzzyRule: (rule: SimpleFuzzyRule) => 
    set(state => ({ 
      fuzzyRules: [...state.fuzzyRules, rule] 
    })),

  addFuzzyRoutine: (routine: SimpleFuzzyRoutine) => 
    set(state => ({ 
      fuzzyRoutines: [...state.fuzzyRoutines, routine] 
    })),

  // Update entities
  updateFuzzySystem: (id: string, updates: Partial<SimpleFuzzySystem>) => 
    set(state => ({
      fuzzySystems: state.fuzzySystems.map(system => 
        system.id === id ? { ...system, ...updates } : system
      )
    })),

  updateFuzzyVariable: (id: string, updates: Partial<SimpleFuzzyVariable>) => 
    set(state => ({
      fuzzyVariables: state.fuzzyVariables.map(variable => 
        variable.id === id ? { ...variable, ...updates } : variable
      )
    })),

  updateFuzzyTerm: (id: string, updates: Partial<SimpleFuzzyTerm>) => 
    set(state => ({
      fuzzyTerms: state.fuzzyTerms.map(term => 
        term.id === id ? { ...term, ...updates } : term
      )
    })),

  updateFuzzyRule: (id: string, updates: Partial<SimpleFuzzyRule>) => 
    set(state => ({
      fuzzyRules: state.fuzzyRules.map(rule => 
        rule.id === id ? { ...rule, ...updates } : rule
      )
    })),

  updateFuzzyRoutine: (id: string, updates: Partial<SimpleFuzzyRoutine>) => 
    set(state => ({
      fuzzyRoutines: state.fuzzyRoutines.map(routine => 
        routine.id === id ? { ...routine, ...updates } : routine
      )
    })),

  // Remove entities
  removeFuzzySystem: (id: string) => 
    set(state => ({
      fuzzySystems: state.fuzzySystems.filter(system => system.id !== id)
    })),

  removeFuzzyVariable: (id: string) => 
    set(state => ({
      fuzzyVariables: state.fuzzyVariables.filter(variable => variable.id !== id)
    })),

  removeFuzzyTerm: (id: string) => 
    set(state => ({
      fuzzyTerms: state.fuzzyTerms.filter(term => term.id !== id)
    })),

  removeFuzzyRule: (id: string) => 
    set(state => ({
      fuzzyRules: state.fuzzyRules.filter(rule => rule.id !== id)
    })),

  removeFuzzyRoutine: (id: string) => 
    set(state => ({
      fuzzyRoutines: state.fuzzyRoutines.filter(routine => routine.id !== id)
    })),

  // Computed getters
  getFuzzySystemById: (id: string) => {
    const state = get();
    return state.fuzzySystems.find(system => system.id === id);
  },

  getFuzzyVariableById: (id: string) => {
    const state = get();
    return state.fuzzyVariables.find(variable => variable.id === id);
  },

  getFuzzyTermById: (id: string) => {
    const state = get();
    return state.fuzzyTerms.find(term => term.id === id);
  },

  getFuzzyRuleById: (id: string) => {
    const state = get();
    return state.fuzzyRules.find(rule => rule.id === id);
  },

  getFuzzyRoutineById: (id: string) => {
    const state = get();
    return state.fuzzyRoutines.find(routine => routine.id === id);
  },

  getActiveFuzzySystems: () => {
    const state = get();
    return state.fuzzySystems.filter(system => system.status === 'ACTIVE');
  },

  getInactiveFuzzySystems: () => {
    const state = get();
    return state.fuzzySystems.filter(system => system.status === 'INACTIVE');
  },

  getVariablesBySystemId: (systemId: string) => {
    const state = get();
    return state.fuzzyVariables.filter(variable => 
      variable.system_id === systemId
    );
  },

  getInputVariablesBySystemId: (systemId: string) => {
    const state = get();
    return state.fuzzyVariables.filter(variable => 
      variable.system_id === systemId && variable.variable_type === 'input'
    );
  },

  getOutputVariablesBySystemId: (systemId: string) => {
    const state = get();
    return state.fuzzyVariables.filter(variable => 
      variable.system_id === systemId && variable.variable_type === 'output'
    );
  },

  getTermsByVariableId: (variableId: string) => {
    const state = get();
    return state.fuzzyTerms.filter(term => term.variable_id === variableId);
  },

  getRulesBySystemId: (systemId: string) => {
    const state = get();
    return state.fuzzyRules.filter(rule => rule.system_id === systemId);
  },

  getRoutinesBySystemId: (systemId: string) => {
    const state = get();
    return state.fuzzyRoutines.filter(routine => routine.system_id === systemId);
  }
}));