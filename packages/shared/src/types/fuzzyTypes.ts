// Tipos para el sistema de lógica fuzzy

// Tipo base para ObjectId de MongoDB
export interface ObjectId {
  $oid: string;
}

// Tipo base para fechas de MongoDB
export interface MongoDate {
  $date: string;
}

// Operadores fuzzy
export interface FuzzyOperators {
  and: 'min' | 'product';
  or: 'max' | 'sum';
  not: 'complement';
}

// Sistema Fuzzy principal
export interface FuzzySystem {
  _id: ObjectId;
  name: string;
  status: 'ACTIVE' | 'INACTIVE';
  defuzzification_method: 'centroid' | 'bisector' | 'mom' | 'som' | 'lom';
  operators: FuzzyOperators;
  input_variable_ids: string[];
  output_variable_ids: string[];
  rule_ids: string[];
  created_at: MongoDate;
  updated_at: MongoDate;
  created_by: string | null;
}

// Variable Fuzzy
export interface FuzzyVariable {
  _id: ObjectId;
  name: string;
  description: string;
  variable_type: 'input' | 'output';
  device_id: string;
  terms: string[]; // IDs de los términos
  created_at: MongoDate;
  updated_at: MongoDate;
}

// Función de membresía
export interface MembershipFunction {
  function_type: 'triangular' | 'trapezoidal' | 'gaussian' | 'sigmoid';
  parameters: number[];
  universe_min: number;
  universe_max: number;
}

// Término fuzzy
export interface FuzzyTerm {
  _id: ObjectId;
  variable_id: string;
  label: string;
  membership_function: MembershipFunction;
  created_at: MongoDate;
  updated_at: MongoDate;
}

// Condición de regla fuzzy
export interface RuleCondition {
  variableId: string;
  operator: 'IS' | 'IS_NOT';
  value: string; // label del término
}

// Regla Fuzzy
export interface FuzzyRule {
  _id: ObjectId;
  name: string;
  system_id: string;
  description: string;
  conditions: RuleCondition[];
  connectors: ('AND' | 'OR')[]; // Conectores entre condiciones
  consequent: string; // ID de la rutina
  created_at: MongoDate;
}

// Paso de rutina fuzzy
export interface RoutineStep {
  step_id: number;
  condition: string;
  power_term_id: string;
  duration_term_id: string;
}

// Rutina Fuzzy
export interface FuzzyRoutine {
  _id: ObjectId;
  routine_name: string;
  created_at: MongoDate;
  steps: RoutineStep[];
}

// Tipos simplificados para el frontend (sin ObjectId y MongoDate)
export interface SimpleFuzzySystem {
  id: string;
  name: string;
  status: 'ACTIVE' | 'INACTIVE';
  defuzzification_method: string;
  operators: FuzzyOperators;
  input_variables: SimpleFuzzyVariable[];
  output_variables: SimpleFuzzyVariable[];
  rules: SimpleFuzzyRule[];
  created_at: string;
  updated_at: string;
  created_by: string | null;
}

export interface SimpleFuzzyVariable {
  id: string;
  system_id: string;
  name: string;
  description: string;
  variable_type: 'input' | 'output';
  device_id: string;
  created_at: string;
  updated_at: string;
}

export interface SimpleFuzzyTerm {
  id: string;
  variable_id: string;
  label: string;
  membership_function: MembershipFunction;
  created_at: string;
  updated_at: string;
}

export interface SimpleFuzzyRule {
  id: string;
  name: string;
  system_id: string;
  description: string;
  conditions: RuleCondition[];
  connectors: ('AND' | 'OR')[];
  routine: SimpleFuzzyRoutine;
  created_at: string;
}

export interface SimpleFuzzyRoutine {
  id: string;
  system_id: string;
  routine_name: string;
  created_at: string;
  steps: RoutineStep[];
}

// Tipos para el estado del store
export interface FuzzySystemState {
  fuzzySystems: SimpleFuzzySystem[];
  fuzzyVariables: SimpleFuzzyVariable[];
  fuzzyTerms: SimpleFuzzyTerm[];
  fuzzyRules: SimpleFuzzyRule[];
  fuzzyRoutines: SimpleFuzzyRoutine[];
  loading: boolean;
  selectedSystem: SimpleFuzzySystem | null;
}

export interface FuzzySystemActions {
  // Actions
  setFuzzySystems: (systems: SimpleFuzzySystem[]) => void;
  setFuzzyVariables: (variables: SimpleFuzzyVariable[]) => void;
  setFuzzyTerms: (terms: SimpleFuzzyTerm[]) => void;
  setFuzzyRules: (rules: SimpleFuzzyRule[]) => void;
  setFuzzyRoutines: (routines: SimpleFuzzyRoutine[]) => void;
  setLoading: (loading: boolean) => void;
  initializeFuzzyData: () => void;

  // Add new entities
  addFuzzySystem: (system: SimpleFuzzySystem) => void;
  addFuzzyVariable: (variable: SimpleFuzzyVariable) => void;
  addFuzzyTerm: (term: SimpleFuzzyTerm) => void;
  addFuzzyRule: (rule: SimpleFuzzyRule) => void;
  addFuzzyRoutine: (routine: SimpleFuzzyRoutine) => void;

  // Update entities
  updateFuzzySystem: (id: string, updates: Partial<SimpleFuzzySystem>) => void;
  updateFuzzyVariable: (id: string, updates: Partial<SimpleFuzzyVariable>) => void;
  updateFuzzyTerm: (id: string, updates: Partial<SimpleFuzzyTerm>) => void;
  updateFuzzyRule: (id: string, updates: Partial<SimpleFuzzyRule>) => void;
  updateFuzzyRoutine: (id: string, updates: Partial<SimpleFuzzyRoutine>) => void;

  // Remove entities
  removeFuzzySystem: (id: string) => void;
  removeFuzzyVariable: (id: string) => void;
  removeFuzzyTerm: (id: string) => void;
  removeFuzzyRule: (id: string) => void;
  removeFuzzyRoutine: (id: string) => void;

  // Computed getters
  getFuzzySystemById: (id: string) => SimpleFuzzySystem | undefined;
  getFuzzyVariableById: (id: string) => SimpleFuzzyVariable | undefined;
  getFuzzyTermById: (id: string) => SimpleFuzzyTerm | undefined;
  getFuzzyRuleById: (id: string) => SimpleFuzzyRule | undefined;
  getFuzzyRoutineById: (id: string) => SimpleFuzzyRoutine | undefined;
  getActiveFuzzySystems: () => SimpleFuzzySystem[];
  getInactiveFuzzySystems: () => SimpleFuzzySystem[];
  getVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
  getInputVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
  getOutputVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
  getTermsByVariableId: (variableId: string) => SimpleFuzzyTerm[];
  getRulesBySystemId: (systemId: string) => SimpleFuzzyRule[];
  getRoutinesBySystemId: (systemId: string) => SimpleFuzzyRoutine[];
}

export interface FuzzySystemStore extends FuzzySystemState, FuzzySystemActions {}