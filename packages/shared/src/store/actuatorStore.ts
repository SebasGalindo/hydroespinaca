import { create } from 'zustand';

// ⭐ PASO 1: Simplificar la interfaz a los campos esenciales.
export interface ActuadorData {
  id: string; // ID lógico (ej: ACT-001)
  name: string;
  type: 'pump' | 'valve' | 'fan' | 'heater' | 'light' | 'motor';
  location: string;
  pin: number;
  esp32Id: string; // ID del microcontrolador físico
  status: 'active' | 'inactive' | 'error' | 'maintenance';
  createdAt: string; // Fecha de creación
  lastModified: string; // Fecha de última modificación
}

interface ActuatorState {
  actuadores: ActuadorData[];
  addActuador: (actuador: Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'>) => void;
  updateActuador: (id: string, updates: Partial<Omit<ActuadorData, 'id' | 'createdAt'>>) => void;
  removeActuador: (id: string) => void;
  initializeActuadores: () => void; // Para cargar datos iniciales si es necesario
}

export const useActuatorStore = create<ActuatorState>((set, get) => ({
  // Estado inicial
  actuadores: [],

  // Acciones
  addActuador: (actuadorData) => {
    const newActuador: ActuadorData = {
      // Usamos el tipo Omit para asegurar que solo pasamos los campos del formulario
      ...(actuadorData as Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'>),
      id: 'ACT-' + Date.now().toString().slice(-6),
      createdAt: new Date().toISOString().split('T')[0],
      lastModified: new Date().toISOString().split('T')[0],
    };

    set(state => ({
      actuadores: [...state.actuadores, newActuador],
    }));
  },

  updateActuador: (id, updates) => {
    set(state => ({
      actuadores: state.actuadores.map(actuador =>
        actuador.id === id
          ? { ...actuador, ...updates, lastModified: new Date().toISOString().split('T')[0] }
          : actuador
      ),
    }));
  },

  removeActuador: (id) => {
    set(state => ({
      actuadores: state.actuadores.filter(actuador => actuador.id !== id),
    }));
  },
  
  // ⭐ PASO 2: Simplificar los datos de ejemplo para que coincidan.
  initializeActuadores: () => {
    const initialActuadores: ActuadorData[] = [
      {
        id: 'ACT-001',
        name: 'Bomba de Riego Principal',
        type: 'pump',
        location: 'Zona A - Sector 1',
        pin: 12,
        esp32Id: 'ESP32-001',
        status: 'active',
        createdAt: '2023-12-01',
        lastModified: '2024-01-20',
      },
      {
        id: 'ACT-002',
        name: 'Ventilador Extractor Norte',
        type: 'fan',
        location: 'Zona B - Ventilación',
        pin: 14,
        esp32Id: 'ESP32-002',
        status: 'inactive',
        createdAt: '2023-11-15',
        lastModified: '2024-01-18',
      },
    ];
    set({ actuadores: initialActuadores });
  },
}));