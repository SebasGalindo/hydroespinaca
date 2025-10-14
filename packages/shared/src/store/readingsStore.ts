import { create } from 'zustand';

export interface SensorSummary {
  sensor: string;
  media: number;
  minimo: number;
  maximo: number;
  unidad: string;
  ultimaLectura: string;
}

export interface IndividualReading {
  id: string;
  sensor: string;
  valor: number;
  unidad: string;
  fecha: string;
}interface ReadingsState {
  // Data
  sensorSummary: SensorSummary[];
  individualReadings: IndividualReading[];
  
  // Actions
  setSensorSummary: (summary: SensorSummary[]) => void;
  setIndividualReadings: (readings: IndividualReading[]) => void;
  addReading: (reading: IndividualReading) => void;
  initializeReadings: () => void;
}

export const useReadingsStore = create<ReadingsState>((set, get) => ({
  // Initial state
  sensorSummary: [],
  individualReadings: [],
  
  // Actions
  setSensorSummary: (summary) => {
    set({ sensorSummary: summary });
  },
  
  setIndividualReadings: (readings) => {
    set({ individualReadings: readings });
  },
  
  addReading: (reading) => {
    set(state => ({
      individualReadings: [reading, ...state.individualReadings]
    }));
  },
  
  initializeReadings: () => {
    const initialSummary: SensorSummary[] = [
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
    ];
    
    const initialReadings: IndividualReading[] = [
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
    ];
    
    set({ 
      sensorSummary: initialSummary,
      individualReadings: initialReadings
    });
  }
}));