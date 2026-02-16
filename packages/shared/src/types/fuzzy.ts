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
  aggregationMethod: string;
  defuzzificationMethod: string;
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
  | 'bell'
  | 'pi_shaped'
  | 's_shaped'
  | 'z_shaped'
  | 'linear'
  | 'constant';

export const MEMBERSHIP_FUNCTION_LABELS: Record<string, string> = {
  triangular: 'Triangular',
  trapezoidal: 'Trapezoidal',
  gaussian: 'Gaussiana',
  sigmoid: 'Sigmoide',
  bell: 'Campana Generalizada',
  pi_shaped: 'Forma Pi',
  s_shaped: 'Forma S',
  z_shaped: 'Forma Z',
  linear: 'Lineal',
  constant: 'Constante',
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
 * A rule activation result from simulation
 */
export interface SimulateRuleActivation {
  ruleId: string;
  ruleName: string;
  firingStrength: number;
  outputValues: Record<string, number>;
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
