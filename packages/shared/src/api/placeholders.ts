// API Placeholder Functions
// These functions serve as placeholders for future API integration
// Replace hardcoded data with actual API calls when backend is ready

import { SensorSummary, IndividualReading } from '../store/readingsStore';
import { ActuadorData } from '../store/actuatorStore';
import { SensorData, MetricData } from '../store/sensorStore';
import { VariableData } from '../store/variableStore';

// ============================================================================
// READINGS API PLACEHOLDERS
// ============================================================================

/**
 * Fetch sensor summary data from API
 * @returns Promise<SensorSummary[]>
 */
export async function fetchSensorSummary(): Promise<SensorSummary[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/sensors/summary').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        {
          sensor: 'Temperatura',
          media: 25.5,
          minimo: 24.8,
          maximo: 26.2,
          unidad: '°C',
          ultimaLectura: '2024-01-26 10:01:10'
        },
        {
          sensor: 'Humedad',
          media: 60.2,
          minimo: 59.5,
          maximo: 61.0,
          unidad: '%',
          ultimaLectura: '2024-01-26 10:01:00'
        },
        {
          sensor: 'Intensidad Lumínica',
          media: 850,
          minimo: 820,
          maximo: 880,
          unidad: 'lux',
          ultimaLectura: '2024-01-26 10:01:00'
        },
        {
          sensor: 'Conductividad Eléctrica',
          media: 45,
          minimo: 44,
          maximo: 46,
          unidad: '%',
          ultimaLectura: '2024-01-26 10:00:10'
        }
      ]);
    }, 500);
  });
}

/**
 * Fetch individual readings from API
 * @returns Promise<IndividualReading[]>
 */
export async function fetchIndividualReadings(): Promise<IndividualReading[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/readings').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        { id: '1', fecha: '2024-01-26 10:00:00', sensor: 'Temperatura', valor: 25.2, unidad: '°C' },
        { id: '2', fecha: '2024-01-26 10:00:00', sensor: 'Humedad', valor: 60.1, unidad: '%' },
        { id: '3', fecha: '2024-01-26 10:00:00', sensor: 'Intensidad Lumínica', valor: 845, unidad: 'lux' },
        { id: '4', fecha: '2024-01-26 10:00:00', sensor: 'Conductividad Eléctrica', valor: 44, unidad: '%' },
        { id: '5', fecha: '2024-01-26 10:00:10', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
        { id: '6', fecha: '2024-01-26 10:00:10', sensor: 'Humedad', valor: 60.3, unidad: '%' },
        { id: '7', fecha: '2024-01-26 10:00:10', sensor: 'Intensidad Lumínica', valor: 855, unidad: 'lux' },
        { id: '8', fecha: '2024-01-26 10:00:10', sensor: 'Conductividad Eléctrica', valor: 46, unidad: '%' },
        { id: '9', fecha: '2024-01-26 10:00:20', sensor: 'Temperatura', valor: 25.5, unidad: '°C' },
        { id: '10', fecha: '2024-01-26 10:00:20', sensor: 'Humedad', valor: 60.5, unidad: '%' },
        { id: '11', fecha: '2024-01-26 10:00:30', sensor: 'Temperatura', valor: 25.4, unidad: '°C' },
        { id: '12', fecha: '2024-01-26 10:00:30', sensor: 'Humedad', valor: 60.2, unidad: '%' },
        { id: '13', fecha: '2024-01-26 10:00:30', sensor: 'Intensidad Lumínica', valor: 860, unidad: 'lux' },
        { id: '14', fecha: '2024-01-26 10:00:40', sensor: 'Temperatura', valor: 25.6, unidad: '°C' },
        { id: '15', fecha: '2024-01-26 10:00:40', sensor: 'Humedad', valor: 60.4, unidad: '%' },
        { id: '16', fecha: '2024-01-26 10:00:50', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
        { id: '17', fecha: '2024-01-26 10:01:00', sensor: 'Temperatura', valor: 25.7, unidad: '°C' },
        { id: '18', fecha: '2024-01-26 10:01:00', sensor: 'Humedad', valor: 60.6, unidad: '%' },
        { id: '19', fecha: '2024-01-26 10:01:00', sensor: 'Intensidad Lumínica', valor: 865, unidad: 'lux' },
        { id: '20', fecha: '2024-01-26 10:01:10', sensor: 'Temperatura', valor: 25.8, unidad: '°C' }
      ]);
    }, 500);
  });
}

// ============================================================================
// ACTUATORS API PLACEHOLDERS
// ============================================================================

/**
 * Fetch actuators data from API
 * @returns Promise<ActuadorData[]>
 */
export async function fetchActuators(): Promise<ActuadorData[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/actuators').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        {
          id: '1',
          name: 'Bomba de Agua Principal',
          type: 'pump',
          location: 'Tanque Principal',
          pin: 2,
          esp32Id: 'ESP32_001',
          status: 'active',
          createdAt: '2024-01-15',
          lastModified: '2024-01-20'
        },
        {
          id: '2',
          name: 'Ventilador de Circulación',
          type: 'fan',
          location: 'Área de Cultivo',
          pin: 3,
          esp32Id: 'ESP32_001',
          status: 'active',
          createdAt: '2024-01-16',
          lastModified: '2024-01-18'
        },
        {
          id: '3',
          name: 'LED de Crecimiento',
          type: 'light',
          location: 'Área de Cultivo',
          pin: 4,
          esp32Id: 'ESP32_002',
          status: 'inactive',
          createdAt: '2024-01-10',
          lastModified: '2024-01-15'
        }
      ]);
    }, 500);
  });
}

/**
 * Create new actuator via API
 * @param actuator ActuadorData
 * @returns Promise<ActuadorData>
 */
export async function createActuator(actuator: Omit<ActuadorData, 'id'>): Promise<ActuadorData> {
  // TODO: Replace with actual API call
  // return await fetch('/api/actuators', {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify(actuator)
  // }).then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        id: Date.now().toString(),
        ...actuator
      });
    }, 500);
  });
}

/**
 * Update actuator via API
 * @param id string
 * @param actuator Partial<ActuadorData>
 * @returns Promise<ActuadorData>
 */
export async function updateActuator(id: string, actuator: Partial<ActuadorData>): Promise<ActuadorData> {
  // TODO: Replace with actual API call
  // return await fetch(`/api/actuators/${id}`, {
  //   method: 'PUT',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify(actuator)
  // }).then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        id,
        name: 'Actuador Actualizado',
        type: 'pump',
        location: 'Ubicación Actualizada',
        pin: 1,
        esp32Id: 'ESP32_001',
        status: 'active',
        createdAt: '2024-01-01',
        lastModified: new Date().toISOString().split('T')[0],
        ...actuator
      });
    }, 500);
  });
}

/**
 * Delete actuator via API
 * @param id string
 * @returns Promise<void>
 */
export async function deleteActuator(id: string): Promise<void> {
  // TODO: Replace with actual API call
  // return await fetch(`/api/actuators/${id}`, { method: 'DELETE' });
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve();
    }, 500);
  });
}

// ============================================================================
// SENSORS API PLACEHOLDERS
// ============================================================================

/**
 * Fetch sensor data for monitoring dashboard
 * @returns Promise<SensorData[]>
 */
export async function fetchSensorData(): Promise<SensorData[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/sensors/data').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      const now = new Date();
      const data: SensorData[] = [];
      
      for (let i = 0; i < 60; i++) {
        const timestamp = new Date(now.getTime() - i * 60000); // 1 minute intervals
        data.push({
          timestamp: timestamp.toISOString(),
          temperature: 24 + Math.random() * 4, // 24-28°C
          humidity: 60 + Math.random() * 20, // 60-80%
          ph: 6.0 + Math.random() * 1.5, // 6.0-7.5
          light: 800 + Math.random() * 200, // 800-1000 lux
          conductivity: 1.0 + Math.random() * 0.5 // 1.0-1.5 mS/cm
        });
      }
      
      resolve(data.reverse());
    }, 500);
  });
}

/**
 * Fetch metrics data for dashboard
 * @returns Promise<MetricData[]>
 */
export async function fetchMetrics(): Promise<MetricData[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/metrics').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        {
          title: 'Temperatura',
          value: '25.2',
          unit: '°C',
          status: 'optimal',
          trend: 'up',
          change: '+0.5',
          iconType: 'temperature'
        },
        {
          title: 'Humedad',
          value: '68.5',
          unit: '%',
          status: 'optimal',
          trend: 'stable',
          change: '+0.1',
          iconType: 'humidity'
        },
        {
          title: 'pH',
          value: '6.8',
          unit: '',
          status: 'warning',
          trend: 'down',
          change: '-0.2',
          iconType: 'ph'
        },
        {
          title: 'Luz',
          value: '850',
          unit: 'lux',
          status: 'optimal',
          trend: 'up',
          change: '+25',
          iconType: 'sun'
        }
      ]);
    }, 500);
  });
}

// ============================================================================
// VARIABLES API PLACEHOLDERS
// ============================================================================

/**
 * Fetch variables configuration from API
 * @returns Promise<VariableData[]>
 */
export async function fetchVariables(): Promise<VariableData[]> {
  // TODO: Replace with actual API call
  // return await fetch('/api/variables').then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        {
          id: '1',
          name: 'Temperatura Óptima',
          description: 'Rango de temperatura ideal para el crecimiento de espinacas hidropónicas',
          unit: '°C',
          type: 'input',
          dataType: 'numeric',
          minValue: 22,
          maxValue: 26,
          isRequired: true,
          category: 'environmental',
          status: 'active',
          createdAt: '2024-01-15',
          lastModified: '2024-01-20'
        },
        {
          id: '2',
          name: 'Humedad Relativa',
          description: 'Nivel de humedad óptimo para prevenir enfermedades y promover crecimiento',
          unit: '%',
          type: 'input',
          dataType: 'numeric',
          minValue: 65,
          maxValue: 75,
          isRequired: true,
          category: 'environmental',
          status: 'active',
          createdAt: '2024-01-16',
          lastModified: '2024-01-18'
        },
        {
          id: '3',
          name: 'pH del Agua',
          description: 'Rango de pH ideal para la absorción de nutrientes',
          unit: '',
          type: 'input',
          dataType: 'numeric',
          minValue: 6.0,
          maxValue: 7.0,
          isRequired: true,
          category: 'environmental',
          status: 'active',
          createdAt: '2024-01-10',
          lastModified: '2024-01-15'
        }
      ]);
    }, 500);
  });
}

/**
 * Create new variable via API
 * @param variable VariableData
 * @returns Promise<VariableData>
 */
export async function createVariable(variable: Omit<VariableData, 'id'>): Promise<VariableData> {
  // TODO: Replace with actual API call
  // return await fetch('/api/variables', {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify(variable)
  // }).then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        id: Date.now().toString(),
        ...variable
      });
    }, 500);
  });
}

/**
 * Update variable via API
 * @param id string
 * @param variable Partial<VariableData>
 * @returns Promise<VariableData>
 */
export async function updateVariable(id: string, variable: Partial<VariableData>): Promise<VariableData> {
  // TODO: Replace with actual API call
  // return await fetch(`/api/variables/${id}`, {
  //   method: 'PUT',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify(variable)
  // }).then(res => res.json());
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        id,
        name: 'Variable Actualizada',
        description: 'Descripción actualizada',
        unit: '°C',
        type: 'input',
        dataType: 'numeric',
        minValue: 20,
        maxValue: 30,
        isRequired: true,
        category: 'environmental',
        status: 'active',
        createdAt: '2024-01-01',
        lastModified: new Date().toISOString().split('T')[0],
        ...variable
      });
    }, 500);
  });
}

/**
 * Delete variable via API
 * @param id string
 * @returns Promise<void>
 */
export async function deleteVariable(id: string): Promise<void> {
  // TODO: Replace with actual API call
  // return await fetch(`/api/variables/${id}`, { method: 'DELETE' });
  
  // Placeholder implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve();
    }, 500);
  });
}