// Tipos adicionales para sensores individuales
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

// Extensión del SensorState para incluir funciones CRUD de sensores individuales
export interface ExtendedSensorState {
  individualSensors: IndividualSensorData[];
  addIndividualSensor: (sensor: IndividualSensorData) => void;
  updateIndividualSensor: (sensorId: string, sensor: IndividualSensorData) => void;
  removeIndividualSensor: (sensorId: string) => void;
  initializeIndividualSensors: () => void;
}