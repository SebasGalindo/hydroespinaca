import { create } from 'zustand';
import type { Variable } from '../types/crud';

export interface VariableData extends Omit<Variable, 'title' | 'subtitle' | 'identifier' | 'fields' | 'modifiedDate'> {
  name: string;
  status: 'active' | 'inactive' | 'deprecated';
  createdAt: string;
  lastModified: string;
}

interface VariableState {
  // Data
  variables: VariableData[];
  
  // UI State
  loading: boolean;
  
  // Actions
  setVariables: (variables: VariableData[]) => void;
  addVariable: (variable: Omit<VariableData, 'id' | 'createdAt' | 'lastModified'>) => void;
  updateVariable: (id: string, updates: Partial<VariableData>) => void;
  removeVariable: (id: string) => void;
  setLoading: (loading: boolean) => void;
  initializeVariables: () => void;
  
  // Computed
  getVariableById: (id: string) => VariableData | undefined;
  getVariablesByType: (type: VariableData['type']) => VariableData[];
  getVariablesByCategory: (category: VariableData['category']) => VariableData[];
  getActiveVariables: () => VariableData[];
  getRequiredVariables: () => VariableData[];
}

export const useVariableStore = create<VariableState>((set, get) => ({
  // Initial state
  variables: [],
  loading: false,
  
  // Actions
  setVariables: (variables) => {
    set({ variables });
  },
  
  addVariable: (variableData) => {
    const newVariable: VariableData = {
      ...variableData,
      id: variableData.name.toLowerCase().replace(/\s+/g, '-') + '-' + Date.now().toString().slice(-4),
      createdAt: new Date().toISOString().split('T')[0],
      lastModified: new Date().toISOString().split('T')[0]
    };
    
    set(state => ({
      variables: [...state.variables, newVariable]
    }));
  },
  
  updateVariable: (id, updates) => {
    set(state => ({
      variables: state.variables.map(variable => 
        variable.id === id 
          ? { ...variable, ...updates, lastModified: new Date().toISOString().split('T')[0] }
          : variable
      )
    }));
  },
  
  removeVariable: (id) => {
    set(state => ({
      variables: state.variables.filter(variable => variable.id !== id)
    }));
  },
  
  setLoading: (loading) => {
    set({ loading });
  },
  
  initializeVariables: () => {
    const initialVariables: VariableData[] = [
      {
        id: 'temp-ambiente',
        name: 'Temperatura Ambiente',
        description: 'Temperatura del aire en el invernadero',
        unit: '°C',
        type: 'input',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 50,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-15',
        lastModified: '2024-01-18'
      },
      {
        id: 'humedad-relativa',
        name: 'Humedad Relativa',
        description: 'Porcentaje de humedad en el ambiente',
        unit: '%',
        type: 'input',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 100,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-15',
        lastModified: '2024-01-16'
      },
      {
        id: 'ph-agua',
        name: 'pH del Agua',
        description: 'Nivel de acidez del agua de riego',
        unit: 'pH',
        type: 'input',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 14,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-10',
        lastModified: '2024-01-20'
      },
      {
        id: 'riego-activo',
        name: 'Sistema de Riego',
        description: 'Estado del sistema de riego automático',
        unit: 'bool',
        type: 'output',
        dataType: 'boolean',
        isRequired: false,
        category: 'control',
        status: 'active',
        createdAt: '2024-01-12',
        lastModified: '2024-01-19'
      },
      {
        id: 'indice-crecimiento',
        name: 'Índice de Crecimiento',
        description: 'Índice calculado basado en condiciones ambientales',
        unit: 'índice',
        type: 'calculated',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 100,
        isRequired: false,
        category: 'system',
        status: 'active',
        createdAt: '2024-01-08',
        lastModified: '2024-01-17'
      },
      {
        id: 'conductividad-electrica',
        name: 'Conductividad Eléctrica',
        description: 'Medición de la conductividad del agua',
        unit: 'mS/cm',
        type: 'input',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 5,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-05',
        lastModified: '2024-01-15'
      },
      {
        id: 'intensidad-luminica',
        name: 'Intensidad Lumínica',
        description: 'Medición de la intensidad de luz en el invernadero',
        unit: 'lux',
        type: 'input',
        dataType: 'numeric',
        minValue: 0,
        maxValue: 2000,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-03',
        lastModified: '2024-01-14'
      }
    ];
    
    set({ variables: initialVariables });
  },
  
  // Computed functions
  getVariableById: (id) => {
    const { variables } = get();
    return variables.find(variable => variable.id === id);
  },
  
  getVariablesByType: (type) => {
    const { variables } = get();
    return variables.filter(variable => variable.type === type);
  },
  
  getVariablesByCategory: (category) => {
    const { variables } = get();
    return variables.filter(variable => variable.category === category);
  },
  
  getActiveVariables: () => {
    const { variables } = get();
    return variables.filter(variable => variable.status === 'active');
  },
  
  getRequiredVariables: () => {
    const { variables } = get();
    return variables.filter(variable => variable.isRequired);
  }
}));