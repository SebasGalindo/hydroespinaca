import { create } from 'zustand';

// Tipos de datos para las gráficas
export interface TimeSeriesDataPoint {
  value: number;
  timestamp: string;
  label?: string;
}

export interface ScatterDataPoint {
  value: number;
  value1: number;
  label?: string;
}

export interface ChartVariable {
  id: string;
  name: string;
  unit: string;
  color?: string;
  baseValue: number;
  range: number;
}

export interface TimeSeriesData {
  variableId: string;
  data: TimeSeriesDataPoint[];
}

export interface ScatterData {
  variableXId: string;
  variableYId: string;
  data: ScatterDataPoint[];
}

// Definición de variables disponibles para graficar
export const CHART_VARIABLES: Record<string, ChartVariable> = {
  '1': {
    id: '1',
    name: 'Temperatura',
    unit: '°C',
    color: '#FF6B6B',
    baseValue: 25,
    range: 10,
  },
  '2': {
    id: '2',
    name: 'Humedad',
    unit: '%',
    color: '#4ECDC4',
    baseValue: 60,
    range: 30,
  },
  '3': {
    id: '3',
    name: 'Presión',
    unit: 'hPa',
    color: '#45B7D1',
    baseValue: 1013,
    range: 100,
  },
  '4': {
    id: '4',
    name: 'pH',
    unit: '',
    color: '#96CEB4',
    baseValue: 7.2,
    range: 2,
  },
  '5': {
    id: '5',
    name: 'Conductividad',
    unit: 'µS/cm',
    color: '#F7DC6F',
    baseValue: 500,
    range: 200,
  },
  '6': {
    id: '6',
    name: 'Oxígeno Disuelto',
    unit: 'mg/L',
    color: '#BB8FCE',
    baseValue: 8.5,
    range: 4,
  },
};

interface DashboardState {
  // Data
  timeSeriesCache: Map<string, TimeSeriesDataPoint[]>;
  scatterCache: Map<string, ScatterDataPoint[]>;
  loading: boolean;
  error: string | null;

  // Actions
  fetchTimeSeriesData: (variableId: string, days?: number) => Promise<void>;
  fetchScatterData: (variableXId: string, variableYId: string, points?: number) => Promise<void>;
  clearCache: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;

  // Getters
  getTimeSeriesData: (variableId: string) => TimeSeriesDataPoint[] | null;
  getScatterData: (variableXId: string, variableYId: string) => ScatterDataPoint[] | null;
}

// Función helper para generar datos realistas de series temporales
// Usa una semilla basada en el variableId para generar datos determinísticos
const generateTimeSeriesData = (variableId: string, days: number = 30): TimeSeriesDataPoint[] => {
  const variable = CHART_VARIABLES[variableId];
  if (!variable) return [];

  const { baseValue, range } = variable;
  const data: TimeSeriesDataPoint[] = [];
  
  // Usar el ID de la variable como semilla para generar datos consistentes
  const seed = parseInt(variableId) || 1;

  for (let i = 0; i < days; i++) {
    const date = new Date();
    date.setDate(date.getDate() - (days - 1 - i));
    
    // Generar valor con patrón senoidal determinístico (sin Math.random)
    const dayOfYear = Math.floor((date.getTime() - new Date(date.getFullYear(), 0, 0).getTime()) / 86400000);
    const trend = Math.sin((dayOfYear / 365) * 2 * Math.PI) * 0.3; // Tendencia anual
    const daily = Math.sin((i / 7) * 2 * Math.PI) * 0.4; // Patrón semanal
    // Ruido pseudo-aleatorio determinístico basado en la semilla
    const noise = Math.sin((i + seed) * 123.456) * 0.3;
    
    const variation = (trend + daily + noise);
    const value = baseValue + variation * range;

    data.push({
      value: Math.round(value * 100) / 100,
      timestamp: date.toISOString(),
      label: date.toISOString().split('T')[0],
    });
  }

  return data;
};

// Función helper para generar datos de dispersión con correlación
// IMPORTANTE: Usa los mismos datos de series temporales para mantener consistencia
const generateScatterData = (
  variableXId: string,
  variableYId: string,
  points: number = 30
): ScatterDataPoint[] => {
  const variableX = CHART_VARIABLES[variableXId];
  const variableY = CHART_VARIABLES[variableYId];
  
  if (!variableX || !variableY) return [];

  // Generar datos de series temporales para ambas variables
  // Esto asegura que los datos sean consistentes con las gráficas de serie de tiempo
  const dataX = generateTimeSeriesData(variableXId, points);
  const dataY = generateTimeSeriesData(variableYId, points);

  // Definir correlaciones realistas entre variables
  const correlations: Record<string, Record<string, number>> = {
    '1': { '2': -0.6, '3': -0.2, '4': 0.3, '5': 0.5, '6': -0.7 }, // Temperatura
    '2': { '1': -0.6, '3': 0.1, '4': -0.2, '5': -0.2, '6': -0.4 }, // Humedad
    '3': { '1': -0.2, '2': 0.1, '4': 0.1, '5': 0.2, '6': 0.3 }, // Presión
    '4': { '1': 0.3, '2': -0.2, '3': 0.1, '5': 0.4, '6': 0.5 }, // pH
    '5': { '1': 0.5, '2': -0.2, '3': 0.2, '4': 0.4, '6': -0.3 }, // Conductividad
    '6': { '1': -0.7, '2': -0.4, '3': 0.3, '4': 0.5, '5': -0.3 }, // Oxígeno
  };

  const correlation = correlations[variableXId]?.[variableYId] || 
                      correlations[variableYId]?.[variableXId] || 
                      0;

  const { baseValue: baseX, range: rangeX } = variableX;
  const { baseValue: baseY, range: rangeY } = variableY;

  const data: ScatterDataPoint[] = [];

  for (let i = 0; i < Math.min(dataX.length, dataY.length); i++) {
    // Usar los valores de las series temporales
    let x = dataX[i].value;
    let y = dataY[i].value;

    // Aplicar correlación ajustando el valor Y en función de X
    if (correlation !== 0) {
      const seed = (parseInt(variableXId) + parseInt(variableYId)) * i;
      const noise = Math.sin(seed * 78.91) * 0.2; // Ruido determinístico pequeño
      
      // Ajustar Y para que esté correlacionado con X
      const xNormalized = (x - baseX) / rangeX;
      const correlationEffect = correlation * xNormalized * rangeY;
      const noiseEffect = Math.sqrt(Math.max(0, 1 - correlation * correlation)) * noise * rangeY;
      
      y = baseY + correlationEffect + noiseEffect;
    }

    data.push({
      value: Math.round(x * 100) / 100,
      value1: Math.round(y * 100) / 100,
      label: `P${i + 1}`,
    });
  }

  return data;
};

export const useDashboardStore = create<DashboardState>((set, get) => ({
  // Initial state
  timeSeriesCache: new Map(),
  scatterCache: new Map(),
  loading: false,
  error: null,

  // Actions
  fetchTimeSeriesData: async (variableId: string, days: number = 30) => {
    set({ loading: true, error: null });

    try {
      // Simular llamada a API con delay
      await new Promise(resolve => setTimeout(resolve, 300));

      // En producción, aquí harías: const response = await fetch(`/api/timeseries/${variableId}?days=${days}`)
      const data = generateTimeSeriesData(variableId, days);

      set(state => {
        const newCache = new Map(state.timeSeriesCache);
        newCache.set(variableId, data);
        return { timeSeriesCache: newCache, loading: false };
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Error al cargar datos de series temporales',
        loading: false 
      });
    }
  },

  fetchScatterData: async (variableXId: string, variableYId: string, points: number = 30) => {
    set({ loading: true, error: null });

    try {
      // Simular llamada a API con delay
      await new Promise(resolve => setTimeout(resolve, 300));

      // En producción: const response = await fetch(`/api/scatter/${variableXId}/${variableYId}?points=${points}`)
      const data = generateScatterData(variableXId, variableYId, points);

      const cacheKey = `${variableXId}-${variableYId}`;
      set(state => {
        const newCache = new Map(state.scatterCache);
        newCache.set(cacheKey, data);
        return { scatterCache: newCache, loading: false };
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Error al cargar datos de dispersión',
        loading: false 
      });
    }
  },

  clearCache: () => {
    set({ timeSeriesCache: new Map(), scatterCache: new Map() });
  },

  setLoading: (loading: boolean) => {
    set({ loading });
  },

  setError: (error: string | null) => {
    set({ error });
  },

  // Getters
  getTimeSeriesData: (variableId: string) => {
    return get().timeSeriesCache.get(variableId) || null;
  },

  getScatterData: (variableXId: string, variableYId: string) => {
    const cacheKey = `${variableXId}-${variableYId}`;
    return get().scatterCache.get(cacheKey) || null;
  },
}));
