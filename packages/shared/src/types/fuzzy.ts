// ==================== Fuzzy System Types ====================

/**
 * Status of a fuzzy system
 */
export type FuzzySystemStatus = 'DRAFT' | 'ACTIVE' | 'INACTIVE' | 'TESTING';

export const FUZZY_STATUS_LABELS: Record<FuzzySystemStatus, string> = {
  DRAFT: 'Borrador',
  ACTIVE: 'Activo',
  INACTIVE: 'Inactivo',
  TESTING: 'Pruebas',
};

export const FUZZY_STATUS_COLORS: Record<FuzzySystemStatus, string> = {
  DRAFT: 'gray',
  ACTIVE: 'green',
  INACTIVE: 'red',
  TESTING: 'yellow',
};

export const FUZZY_STATUS_ICONS: Record<FuzzySystemStatus, string> = {
  DRAFT: 'file-edit',
  ACTIVE: 'check-circle',
  INACTIVE: 'x-circle',
  TESTING: 'flask',
};

/**
 * Operators configuration for a fuzzy system
 */
export interface OperatorsConfig {
  andMethod: string;
  orMethod: string;
  notMethod: string;
}

/**
 * A complete fuzzy system
 */
export interface FuzzySystem {
  id: string;
  name: string;
  status: FuzzySystemStatus;
  defuzzificationMethod: string;
  operators: OperatorsConfig;
  inputVariableIds: string[];
  outputVariableIds: string[];
  ruleIds: string[];
  createdAt: string | null;
  updatedAt: string | null;
  createdBy: string | null;
}

// ==================== Fuzzy Variable Types ====================

export type VariableType = 'input' | 'output';
export type ActuatorType = 'PWM' | 'DIGITAL';

export const VARIABLE_TYPE_LABELS: Record<VariableType, string> = {
  input: 'Entrada',
  output: 'Salida',
};

export const VARIABLE_TYPE_ICONS: Record<VariableType, string> = {
  input: 'arrow-down',
  output: 'arrow-up',
};

/**
 * A fuzzy variable (input or output)
 */
export interface FuzzyVariable {
  id: string;
  name: string;
  description: string | null;
  variableType: VariableType;
  actuatorType: ActuatorType | null;
  defuzzificationThreshold: number;
  universeMin: number | null;
  universeMax: number | null;
  referenceCode: string | null;
  terms: string[];
  createdAt: string | null;
  updatedAt: string | null;
}

// ==================== Fuzzy Term Types ====================

/**
 * Supported membership function types (matches Python MembershipFunctionType enum values)
 */
export type MembershipFunctionType =
  | 'triangular'
  | 'trapezoidal'
  | 'gaussian'
  | 'sigmoid'
  | 'bell';

export const MEMBERSHIP_FUNCTION_LABELS: Record<string, string> = {
  triangular: 'Triangular',
  trapezoidal: 'Trapezoidal',
  gaussian: 'Gaussiana',
  sigmoid: 'Sigmoide',
  bell: 'Campana Generalizada',
};

/**
 * A membership function definition
 */
export interface MembershipFunction {
  functionType: MembershipFunctionType;
  parameters: number[];
  universeMin: number;
  universeMax: number;
}

/**
 * A fuzzy term (linguistic label) with its membership function
 */
export interface FuzzyTerm {
  id: string;
  variableId: string;
  label: string;
  membershipFunction: MembershipFunction;
  createdAt: string | null;
  updatedAt: string | null;
}

// ==================== Fuzzy Rule Types ====================

export type LogicalOperator = 'IS' | 'IS_NOT';
export type RuleConnector = 'AND' | 'OR';
export type AggregationMethod = 'max' | 'sum' | 'probabilistic_or';

/**
 * A condition in a fuzzy rule (IF variable IS/IS_NOT term)
 */
export interface RuleCondition {
  variableId: string;
  operator: LogicalOperator;
  value: string;
}

/**
 * A consequent (THEN part) of a Mamdani fuzzy rule
 */
export interface RuleConsequent {
  variableId: string;
  terms: string[];
  aggregationMethod: AggregationMethod;
}

/**
 * A complete fuzzy rule
 */
export interface FuzzyRule {
  id: string;
  name: string;
  systemId: string | null;
  description: string | null;
  conditions: RuleCondition[];
  connectors: RuleConnector[];
  consequents: RuleConsequent[];
  createdAt: string | null;
  ruleText: string | null;
}

// ==================== System Detail (Orchestrated) ====================

/**
 * Detailed view of a fuzzy system with all related entities.
 * Returned by the BFF orchestrated endpoint.
 */
export interface FuzzySystemDetail {
  system: FuzzySystem;
  variables: FuzzyVariable[];
  terms: FuzzyTerm[];
  rules: FuzzyRule[];
}

// ==================== Request / Response Types ====================

/**
 * Request to clone a fuzzy system
 */
export interface CloneFuzzySystemRequest {
  name?: string;
}

// ==================== CRUD Request Types ====================

/**
 * Request to create a new fuzzy system
 */
export interface CreateFuzzySystemRequest {
  name: string;
  defuzzificationMethod?: string;
  operators?: Partial<OperatorsConfig>;
}

/**
 * Request to update a fuzzy system
 */
export interface UpdateFuzzySystemRequest {
  name?: string;
  defuzzificationMethod?: string;
  operators?: Partial<OperatorsConfig>;
}

/**
 * Request to change a fuzzy system's status
 */
export interface UpdateFuzzySystemStatusRequest {
  status: FuzzySystemStatus;
}

/**
 * Request to create a new fuzzy variable
 */
export interface CreateFuzzyVariableRequest {
  systemId: string;
  name: string;
  variableType: VariableType;
  description?: string;
  actuatorType?: ActuatorType;
  defuzzificationThreshold?: number;
  universeMin?: number;
  universeMax?: number;
  referenceCode?: string;
}

/**
 * Request to update a fuzzy variable
 */
export interface UpdateFuzzyVariableRequest {
  name?: string;
  description?: string;
  variableType?: VariableType;
  actuatorType?: ActuatorType;
  defuzzificationThreshold?: number;
  universeMin?: number;
  universeMax?: number;
  referenceCode?: string;
}

/**
 * Request to create a new fuzzy term
 */
export interface CreateFuzzyTermRequest {
  variableId: string;
  label: string;
  membershipFunction: MembershipFunction;
}

/**
 * Request to update a fuzzy term
 */
export interface UpdateFuzzyTermRequest {
  label?: string;
  membershipFunction?: MembershipFunction;
}

/**
 * Request to create a new fuzzy rule
 */
export interface CreateFuzzyRuleRequest {
  systemId: string;
  name: string;
  description?: string;
  conditions: RuleCondition[];
  connectors: RuleConnector[];
  consequents: RuleConsequent[];
}

/**
 * Request to update a fuzzy rule
 */
export interface UpdateFuzzyRuleRequest {
  name?: string;
  description?: string;
  conditions?: RuleCondition[];
  connectors?: RuleConnector[];
  consequents?: RuleConsequent[];
}

// ==================== Helper Constants ====================

/**
 * Number of parameters required for each membership function type
 */
export const MF_PARAM_COUNTS: Record<MembershipFunctionType, number> = {
  triangular: 3,
  trapezoidal: 4,
  gaussian: 2,
  sigmoid: 2,
  bell: 3,
};

/**
 * Human-readable parameter labels per membership function type
 */
export const MF_PARAM_LABELS: Record<MembershipFunctionType, string[]> = {
  triangular: ['Izquierda (a)', 'Centro (b)', 'Derecha (c)'],
  trapezoidal: ['Izquierda (a)', 'Izq-Centro (b)', 'Der-Centro (c)', 'Derecha (d)'],
  gaussian: ['Centro (c)', 'Sigma (σ)'],
  sigmoid: ['Centro (c)', 'Pendiente (a)'],
  bell: ['Ancho (a)', 'Pendiente (b)', 'Centro (c)'],
};

/**
 * Default defuzzification methods
 */
export const DEFUZZIFICATION_METHODS = [
  { value: 'centroid', label: 'Centroide' },
  { value: 'bisector', label: 'Bisector' },
  { value: 'mom', label: 'Media del Máximo (MOM)' },
  { value: 'lom', label: 'Más Grande del Máximo (LOM)' },
  { value: 'som', label: 'Más Pequeño del Máximo (SOM)' },
] as const;

/**
 * Operator method options
 */
export const AND_METHODS = [
  { value: 'min', label: 'Mínimo' },
  { value: 'prod', label: 'Producto' },
] as const;

export const OR_METHODS = [
  { value: 'max', label: 'Máximo' },
  { value: 'probor', label: 'OR Probabilístico' },
] as const;

export const AGGREGATION_METHODS = [
  { value: 'max', label: 'Máximo' },
  { value: 'sum', label: 'Suma' },
  { value: 'probabilistic_or', label: 'OR Probabilístico' },
] as const;

export const NOT_METHODS = [
  { value: 'complement', label: 'Complemento' },
] as const;

/**
 * A single simulation input
 */
export interface SimulateInput {
  referenceCode: string;
  value: number;
}

/**
 * Request to simulate a fuzzy system evaluation
 */
export interface SimulateFuzzySystemRequest {
  inputs: SimulateInput[];
}

/**
 * An output value produced by a rule activation during simulation
 */
export interface SimulateOutputValue {
  referenceCode: string;
  power: string | null;
  dutyCycle: number | null;
  duration: number;
}

/**
 * A rule activation result from simulation
 */
export interface SimulateRuleActivation {
  ruleId: string;
  ruleName: string;
  firingStrength: number;
  outputValues: SimulateOutputValue[];
}

/**
 * An output result from simulation
 */
export interface SimulateOutput {
  variableName: string;
  referenceCode: string;
  crispValue: number;
  actuatorType: string | null;
}

/**
 * Full response from fuzzy system simulation
 */
export interface SimulateFuzzySystemResponse {
  systemId: string;
  systemName: string;
  inputs: SimulateInput[];
  activatedRules: SimulateRuleActivation[];
  finalOutputs: SimulateOutput[];
  simulatedAt: string;
}

/**
 * Export format (portable JSON)
 */
export interface FuzzySystemExport {
  version: string;
  exportedAt: string;
  system: Record<string, unknown>;
  variables: Record<string, unknown>[];
  terms: Record<string, unknown>[];
  rules: Record<string, unknown>[];
}
