import { create } from 'zustand';

export interface SensorData {
  timestamp: string;
  temperature: number;
  humidity: number;
  ph: number;
  light: number;
  conductivity: number;
}

export interface IndividualSensorData {
  id?: string;
  idFisico: string;
  ubicacion: string;
  esp32Id: string;
  frecuenciaLectura: number; // en segundos
  variablesAMedir: string[]; // IDs de las variables
  unidadMedida: string;
  rangoMinimo: number;
  rangoMaximo: number;
  rangoOptimoMinimo: number;
  rangoOptimoMaximo: number;
  estado: 'activo' | 'inactivo';
  createdAt?: string;
  lastModified?: string;
}

export interface MetricData {
  title: string;
  value: string;
  unit: string;
  status: 'optimal' | 'warning' | 'critical';
  trend: 'up' | 'down' | 'stable';
  change: string;
  iconType: 'temperature' | 'humidity' | 'ph' | 'light' | 'sun' | 'electric' | 'ruler' | 'water';
}

export interface SystemComponent {
  name: string;
  status: 'online' | 'warning' | 'offline';
  lastUpdate: string;
  details?: string;
}

export interface SensorState {
  // Data
  sensorData: SensorData[];
  currentMetrics: MetricData[];
  systemComponents: SystemComponent[];
  individualSensors: IndividualSensorData[];
  
  // UI State
  timeRange: '1h' | '6h' | '24h' | '7d';
  isRealTime: boolean;
  loading: boolean;
  
  // Actions
  setSensorData: (data: SensorData[]) => void;
  addSensorDataPoint: (dataPoint: SensorData) => void;
  setTimeRange: (range: '1h' | '6h' | '24h' | '7d') => void;
  setIsRealTime: (isRealTime: boolean) => void;
  setLoading: (loading: boolean) => void;
  generateMockData: () => void;
  updateCurrentMetrics: () => void;
  initializeSystemComponents: () => void;
  updateSystemComponentStatus: (name: string, status: 'online' | 'warning' | 'offline', lastUpdate: string, details?: string) => void;
  
  // Individual Sensors CRUD
  addIndividualSensor: (sensor: IndividualSensorData) => void;
  updateIndividualSensor: (sensorId: string, sensor: IndividualSensorData) => void;
  removeIndividualSensor: (sensorId: string) => void;
  initializeIndividualSensors: () => void;
}

export const useSensorStore = create<SensorState>((set, get) => ({
  // Initial state
  sensorData: [],
  currentMetrics: [],
  systemComponents: [],
  individualSensors: [],
  timeRange: '24h',
  isRealTime: true,
  loading: true,
  
  // Actions
  setSensorData: (data) => {
    set({ sensorData: data });
    get().updateCurrentMetrics();
  },
  
  addSensorDataPoint: (dataPoint) => {
    const { sensorData, timeRange } = get();
    const updated = [...sensorData, dataPoint];
    
    // Mantener solo los últimos datos según el rango de tiempo
    const maxPoints = timeRange === '1h' ? 60 : timeRange === '6h' ? 360 : timeRange === '24h' ? 1440 : 10080;
    const newData = updated.slice(-maxPoints);
    
    set({ sensorData: newData });
    get().updateCurrentMetrics();
  },
  
  setTimeRange: (range) => {
    set({ timeRange: range });
    get().generateMockData();
  },
  
  setIsRealTime: (isRealTime) => set({ isRealTime }),
  
  setLoading: (loading) => set({ loading }),
  
  generateMockData: () => {
    const { timeRange } = get();
    set({ loading: true });
    
    // Simular carga de datos
    setTimeout(() => {
      const data: SensorData[] = [];
      const now = new Date();
      const intervals = timeRange === '1h' ? 60 : timeRange === '6h' ? 360 : timeRange === '24h' ? 1440 : 10080;
      const step = timeRange === '1h' ? 1 : timeRange === '6h' ? 6 : timeRange === '24h' ? 24 : 168;
      
      for (let i = intervals; i >= 0; i -= step) {
        const timestamp = new Date(now.getTime() - i * 60000);
        data.push({
          timestamp: timestamp.toISOString(),
          temperature: 24 + Math.sin(i / 100) * 2 + Math.random() * 0.5,
          humidity: 65 + Math.cos(i / 80) * 10 + Math.random() * 2,
          ph: 6.5 + Math.sin(i / 120) * 0.3 + Math.random() * 0.1,
          light: 800 + Math.sin(i / 60) * 200 + Math.random() * 50,
          conductivity: 1.2 + Math.sin(i / 90) * 0.2 + Math.random() * 0.05
        });
      }
      
      get().setSensorData(data);
      set({ loading: false });
    }, 1000);
  },
  
  updateCurrentMetrics: () => {
    const { sensorData } = get();
    
    if (sensorData.length === 0) {
      set({ currentMetrics: [] });
      return;
    }
    
    const latest = sensorData[sensorData.length - 1];
    
    if (!latest) {
      set({ currentMetrics: [] });
      return;
    }
    
    const metrics: MetricData[] = [
      {
        title: 'Temperatura',
        value: latest.temperature.toFixed(1),
        unit: '°C',
        status: latest.temperature >= 18 && latest.temperature <= 25 ? 'optimal' : 
                latest.temperature >= 15 && latest.temperature <= 30 ? 'warning' : 'critical',
        trend: 'stable',
        change: '+0.2°C',
        iconType: 'temperature'
      },
      {
        title: 'Humedad',
        value: latest.humidity.toFixed(1),
        unit: '%',
        status: latest.humidity >= 60 && latest.humidity <= 80 ? 'optimal' : 
                latest.humidity >= 50 && latest.humidity <= 90 ? 'warning' : 'critical',
        trend: 'up',
        change: '+1.5%',
        iconType: 'humidity'
      },
      {
        title: 'pH',
        value: latest.ph.toFixed(2),
        unit: 'pH',
        status: latest.ph >= 5.5 && latest.ph <= 6.5 ? 'optimal' : 
                latest.ph >= 5.0 && latest.ph <= 7.0 ? 'warning' : 'critical',
        trend: 'down',
        change: '-0.1',
        iconType: 'ph'
      },
      {
        title: 'Luz',
        value: Math.round(latest.light).toString(),
        unit: 'lux',
        status: latest.light >= 200 && latest.light <= 400 ? 'optimal' : 
                latest.light >= 100 && latest.light <= 500 ? 'warning' : 'critical',
        trend: 'up',
        change: '+50 lux',
        iconType: 'light'
      },
      {
        title: 'Conductividad',
        value: latest.conductivity.toFixed(2),
        unit: 'mS/cm',
        status: latest.conductivity >= 1.2 && latest.conductivity <= 2.0 ? 'optimal' : 
                latest.conductivity >= 1.0 && latest.conductivity <= 2.5 ? 'warning' : 'critical',
        trend: 'stable',
        change: '0.00',
        iconType: 'electric'
      }
    ];
    
    set({ currentMetrics: metrics });
  },
  
  initializeSystemComponents: () => {
    const components: SystemComponent[] = [
      {
        name: 'Sensor de Temperatura',
        status: 'online',
        lastUpdate: 'Hace 30 segundos',
        details: 'ESP32-001'
      },
      {
        name: 'Sensor de Humedad',
        status: 'online',
        lastUpdate: 'Hace 30 segundos',
        details: 'ESP32-001'
      },
      {
        name: 'Sensor de pH',
        status: 'warning',
        lastUpdate: 'Hace 2 minutos',
        details: 'ESP32-002 - Calibración requerida'
      },
      {
        name: 'Sensor de Luz',
        status: 'online',
        lastUpdate: 'Hace 30 segundos',
        details: 'ESP32-003'
      },
      {
        name: 'Sensor de Conductividad',
        status: 'online',
        lastUpdate: 'Hace 30 segundos',
        details: 'ESP32-002'
      },
      {
        name: 'Conectividad WiFi',
        status: 'online',
        lastUpdate: 'Conectado',
        details: 'Señal: -45 dBm'
      }
    ];
    
    set({ systemComponents: components });
  },
  
  updateSystemComponentStatus: (name, status, lastUpdate, details) => {
    const { systemComponents } = get();
    const updated = systemComponents.map(component => 
      component.name === name 
        ? { ...component, status, lastUpdate, ...(details !== undefined && { details }) }
        : component
    );
    set({ systemComponents: updated });
  },

  // Individual Sensors CRUD
  addIndividualSensor: (sensor) => {
    const { individualSensors } = get();
    const newSensor: IndividualSensorData = {
      ...sensor,
      id: sensor.id || `SENSOR-${Date.now()}`,
      createdAt: sensor.createdAt || new Date().toISOString(),
      lastModified: new Date().toISOString(),
    };
    set({ individualSensors: [...individualSensors, newSensor] });
  },

  updateIndividualSensor: (sensorId, sensor) => {
    const { individualSensors } = get();
    const updated = individualSensors.map(s => 
      s.id === sensorId 
        ? { ...s, ...sensor, id: sensorId, lastModified: new Date().toISOString() }
        : s
    );
    set({ individualSensors: updated });
  },

  removeIndividualSensor: (sensorId) => {
    const { individualSensors } = get();
    const updated = individualSensors.filter(s => s.id !== sensorId);
    set({ individualSensors: updated });
  },

  initializeIndividualSensors: () => {
    const sensors: IndividualSensorData[] = [
      {
        id: 'SENSOR-001',
        idFisico: 'TEMP-001',
        ubicacion: 'Zona A - Cultivo Principal',
        esp32Id: 'ESP32-001',
        frecuenciaLectura: 60,
        variablesAMedir: ['VAR-001'], // Temperatura
        unidadMedida: '°C',
        rangoMinimo: 0,
        rangoMaximo: 50,
        rangoOptimoMinimo: 18,
        rangoOptimoMaximo: 25,
        estado: 'activo',
        createdAt: new Date().toISOString(),
        lastModified: new Date().toISOString(),
      },
      {
        id: 'SENSOR-002',
        idFisico: 'HUM-001',
        ubicacion: 'Zona A - Cultivo Principal',
        esp32Id: 'ESP32-001',
        frecuenciaLectura: 60,
        variablesAMedir: ['VAR-002'], // Humedad
        unidadMedida: '%',
        rangoMinimo: 0,
        rangoMaximo: 100,
        rangoOptimoMinimo: 60,
        rangoOptimoMaximo: 80,
        estado: 'activo',
        createdAt: new Date().toISOString(),
        lastModified: new Date().toISOString(),
      },
    ];
    
    set({ individualSensors: sensors });
  }
}));