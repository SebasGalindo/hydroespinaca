// Tipos para la autenticación
export interface User {
  id: string;
  email: string;
  name: string;
  role?: 'admin' | 'user';
}

export interface AuthResponse {
  user: User;
  token: string;
}

// Tipos para los cultivos hidropónicos
export interface HydroponicCrop {
  id: string;
  name: string;
  type: string;
  startDate: string;
  status: 'active' | 'harvested' | 'failed';
  currentPhase: string;
  estimatedHarvestDate: string;
}

// Tipos para los sensores
export interface SensorReading {
  id: string;
  sensorId: string;
  sensorType: 'ph' | 'temperature' | 'humidity' | 'nutrient' | 'light';
  value: number;
  unit: string;
  timestamp: string;
  cropId?: string;
}

export interface Sensor {
  id: string;
  name: string;
  type: 'ph' | 'temperature' | 'humidity' | 'nutrient' | 'light';
  status: 'active' | 'inactive' | 'maintenance';
  lastReading?: SensorReading;
}

// Tipos para las alertas
export interface Alert {
  id: string;
  type: 'warning' | 'critical' | 'info';
  message: string;
  timestamp: string;
  read: boolean;
  sensorId?: string;
  cropId?: string;
}

// Tipos para las tareas
export interface Task {
  id: string;
  title: string;
  description: string;
  dueDate: string;
  status: 'pending' | 'in_progress' | 'completed';
  priority: 'low' | 'medium' | 'high';
  assignedTo?: string;
  cropId?: string;
}

// Tipos para el tema
export interface Theme {
  colors: Record<string, any>;
  typography: Record<string, any>;
  textStyles: Record<string, any>;
  spacing: Record<string, any>;
  borderRadius: Record<string, any>;
  elevation: Record<string, any>;
  isDark: boolean;
}

export interface IndividualReading {
  id: string;
  fecha: string;
  sensor: string;
  valor: number;
  unidad: string;
}

// Exportar tipos CRUD
export * from './crud';
export * from './sensorTypes';
export * from './fuzzyTypes';
export * from './systemStatus';
export * from './common';

// Exportar tipos de autenticación
export * from './auth';