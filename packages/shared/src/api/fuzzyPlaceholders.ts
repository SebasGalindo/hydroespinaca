import type { 
  SimpleFuzzySystem, 
  SimpleFuzzyVariable, 
  SimpleFuzzyTerm, 
  SimpleFuzzyRule, 
  SimpleFuzzyRoutine 
} from '../types/fuzzyTypes';

// Términos para temperatura
const temperatureTerms: SimpleFuzzyTerm[] = [
  {
    id: 'temp-low',
    variable_id: 'temp-input',
    label: 'baja',
    membership_function: {
      function_type: 'triangular',
      parameters: [10, 15, 20],
      universe_min: 10,
      universe_max: 35
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'temp-medium',
    variable_id: 'temp-input',
    label: 'media',
    membership_function: {
      function_type: 'triangular',
      parameters: [18, 23, 28],
      universe_min: 10,
      universe_max: 35
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'temp-high',
    variable_id: 'temp-input',
    label: 'alta',
    membership_function: {
      function_type: 'triangular',
      parameters: [25, 30, 35],
      universe_min: 10,
      universe_max: 35
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Términos para humedad
const humidityTerms: SimpleFuzzyTerm[] = [
  {
    id: 'hum-low',
    variable_id: 'humidity-input',
    label: 'baja',
    membership_function: {
      function_type: 'triangular',
      parameters: [30, 40, 50],
      universe_min: 30,
      universe_max: 90
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'hum-medium',
    variable_id: 'humidity-input',
    label: 'media',
    membership_function: {
      function_type: 'triangular',
      parameters: [50, 65, 80],
      universe_min: 30,
      universe_max: 90
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'hum-high',
    variable_id: 'humidity-input',
    label: 'alta',
    membership_function: {
      function_type: 'triangular',
      parameters: [70, 80, 90],
      universe_min: 30,
      universe_max: 90
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Términos para duración de riego
const irrigationTerms: SimpleFuzzyTerm[] = [
  {
    id: 'irrig-short',
    variable_id: 'irrigation-output',
    label: 'corta',
    membership_function: {
      function_type: 'triangular',
      parameters: [1, 3, 5],
      universe_min: 0,
      universe_max: 15
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'irrig-medium',
    variable_id: 'irrigation-output',
    label: 'media',
    membership_function: {
      function_type: 'triangular',
      parameters: [4, 7, 10],
      universe_min: 0,
      universe_max: 15
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'irrig-long',
    variable_id: 'irrigation-output',
    label: 'larga',
    membership_function: {
      function_type: 'triangular',
      parameters: [8, 12, 15],
      universe_min: 0,
      universe_max: 15
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Términos para potencia del ventilador
const fanPowerTerms: SimpleFuzzyTerm[] = [
  {
    id: 'fan-low',
    variable_id: 'fan-output',
    label: 'baja',
    membership_function: {
      function_type: 'triangular',
      parameters: [0, 25, 50],
      universe_min: 0,
      universe_max: 100
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'fan-medium',
    variable_id: 'fan-output',
    label: 'media',
    membership_function: {
      function_type: 'triangular',
      parameters: [30, 50, 70],
      universe_min: 0,
      universe_max: 100
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'fan-high',
    variable_id: 'fan-output',
    label: 'alta',
    membership_function: {
      function_type: 'triangular',
      parameters: [60, 80, 100],
      universe_min: 0,
      universe_max: 100
    },
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Variables de entrada
const inputVariables: SimpleFuzzyVariable[] = [
  {
    id: 'temp-input',
    system_id: 'hydro-control-system',
    name: 'Temperatura',
    description: 'Temperatura ambiente en grados Celsius',
    variable_type: 'input',
    device_id: 'sensor-temp-001',
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'humidity-input',
    system_id: 'hydro-control-system',
    name: 'Humedad',
    description: 'Humedad relativa del ambiente en porcentaje',
    variable_type: 'input',
    device_id: 'sensor-hum-001',
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Variables de salida
const outputVariables: SimpleFuzzyVariable[] = [
  {
    id: 'irrigation-output',
    system_id: 'hydro-control-system',
    name: 'Duración de Riego',
    description: 'Duración del riego en minutos',
    variable_type: 'output',
    device_id: 'actuator-pump-001',
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'fan-output',
    system_id: 'hydro-control-system',
    name: 'Potencia del Ventilador',
    description: 'Potencia del ventilador en porcentaje',
    variable_type: 'output',
    device_id: 'actuator-fan-001',
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z'
  }
];

// Rutinas
const routines: SimpleFuzzyRoutine[] = [
  {
    id: 'routine-irrigation-short',
    system_id: 'hydro-control-system',
    routine_name: 'Riego Corto',
    created_at: '2024-01-15T10:00:00Z',
    steps: [
      {
        step_id: 0,
        condition: 'Actuador: bomba de agua',
        power_term_id: 'irrig-short',
        duration_term_id: 'irrig-short'
      }
    ]
  },
  {
    id: 'routine-irrigation-medium',
    system_id: 'hydro-control-system',
    routine_name: 'Riego Medio',
    created_at: '2024-01-15T10:00:00Z',
    steps: [
      {
        step_id: 0,
        condition: 'Actuador: bomba de agua',
        power_term_id: 'irrig-medium',
        duration_term_id: 'irrig-medium'
      }
    ]
  },
  {
    id: 'routine-fan-cooling',
    system_id: 'hydro-control-system',
    routine_name: 'Enfriamiento con Ventilador',
    created_at: '2024-01-15T10:00:00Z',
    steps: [
      {
        step_id: 0,
        condition: 'Actuador: ventilador',
        power_term_id: 'fan-high',
        duration_term_id: 'irrig-medium'
      }
    ]
  }
];

// Reglas fuzzy
const rules: SimpleFuzzyRule[] = [
  {
    id: 'rule-temp-high-hum-low',
    name: 'Temperatura alta y humedad baja => Riego medio',
    system_id: 'hydro-control-system',
    description: 'Si la temperatura es alta y la humedad es baja, entonces aplicar riego medio',
    conditions: [
      {
        variableId: 'temp-input',
        operator: 'IS',
        value: 'alta'
      },
      {
        variableId: 'humidity-input',
        operator: 'IS',
        value: 'baja'
      }
    ],
    connectors: ['AND'],
    routine: routines[1], // Riego Medio
    created_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'rule-temp-high',
    name: 'Temperatura alta => Ventilador',
    system_id: 'hydro-control-system',
    description: 'Si la temperatura es alta, entonces activar ventilador',
    conditions: [
      {
        variableId: 'temp-input',
        operator: 'IS',
        value: 'alta'
      }
    ],
    connectors: [],
    routine: routines[2], // Enfriamiento con Ventilador
    created_at: '2024-01-15T10:00:00Z'
  },
  {
    id: 'rule-hum-low',
    name: 'Humedad baja => Riego corto',
    system_id: 'hydro-control-system',
    description: 'Si la humedad es baja, entonces aplicar riego corto',
    conditions: [
      {
        variableId: 'humidity-input',
        operator: 'IS',
        value: 'baja'
      }
    ],
    connectors: [],
    routine: routines[0], // Riego Corto
    created_at: '2024-01-15T10:00:00Z'
  }
];

// Sistema fuzzy principal
export const mockFuzzySystems: SimpleFuzzySystem[] = [
  {
    id: 'hydro-control-system',
    name: 'Sistema de Control Hidropónico',
    status: 'ACTIVE',
    defuzzification_method: 'centroid',
    operators: {
      and: 'min',
      or: 'max',
      not: 'complement'
    },
    input_variables: inputVariables,
    output_variables: outputVariables,
    rules: rules,
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z',
    created_by: null
  },
  {
    id: 'climate-control-system',
    name: 'Sistema de Control de Clima',
    status: 'INACTIVE',
    defuzzification_method: 'centroid',
    operators: {
      and: 'min',
      or: 'max',
      not: 'complement'
    },
    input_variables: [inputVariables[0]], // Solo temperatura
    output_variables: [outputVariables[1]], // Solo ventilador
    rules: [rules[1]], // Solo regla de ventilador
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z',
    created_by: null
  },
  {
    id: 'pressure-monitoring-system',
    name: 'Monitorización de Presión',
    status: 'INACTIVE',
    defuzzification_method: 'centroid',
    operators: {
      and: 'min',
      or: 'max',
      not: 'complement'
    },
    input_variables: [],
    output_variables: [],
    rules: [],
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z',
    created_by: null
  },
  {
    id: 'smart-lighting-system',
    name: 'Sistema de Iluminación Inteligente',
    status: 'INACTIVE',
    defuzzification_method: 'centroid',
    operators: {
      and: 'min',
      or: 'max',
      not: 'complement'
    },
    input_variables: [],
    output_variables: [],
    rules: [],
    created_at: '2024-01-15T10:00:00Z',
    updated_at: '2024-01-15T10:00:00Z',
    created_by: null
  }
];

// Exportar todos los datos de ejemplo
export const mockFuzzyVariables = [...inputVariables, ...outputVariables];
export const mockFuzzyTerms = [...temperatureTerms, ...humidityTerms, ...irrigationTerms, ...fanPowerTerms];
export const mockFuzzyRules = rules;
export const mockFuzzyRoutines = routines;