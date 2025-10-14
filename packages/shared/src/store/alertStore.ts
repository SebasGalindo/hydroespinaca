import { create } from 'zustand';

export interface Alert {
  id: string;
  type: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
  timestamp: string;
  isRead: boolean;
  sensor?: string;
}

interface AlertState {
  // Data
  alerts: Alert[];
  
  // UI State
  filter: 'all' | 'unread' | 'warning' | 'error';
  
  // Computed properties
  filteredAlerts: Alert[];
  unreadCount: number;
  
  // Actions
  addAlert: (alert: Omit<Alert, 'id'>) => void;
  markAsRead: (alertId: string) => void;
  markAllAsRead: () => void;
  removeAlert: (alertId: string) => void;
  clearAllAlerts: () => void;
  setFilter: (filter: 'all' | 'unread' | 'warning' | 'error') => void;
  initializeAlerts: () => void;
  
  // Computed functions
  getFilteredAlerts: () => Alert[];
  getUnreadCount: () => number;
}

export const useAlertStore = create<AlertState>((set, get) => ({
  // Initial state
  alerts: [],
  filter: 'all',
  
  // Computed properties
  get filteredAlerts() {
    const state = get();
    let filtered = state.alerts;
    
    switch (state.filter) {
      case 'unread':
        filtered = filtered.filter(alert => !alert.isRead);
        break;
      case 'warning':
        filtered = filtered.filter(alert => alert.type === 'warning');
        break;
      case 'error':
        filtered = filtered.filter(alert => alert.type === 'error');
        break;
      case 'all':
      default:
        // No filtering needed
        break;
    }
    
    return filtered;
  },
  
  get unreadCount() {
    const state = get();
    return state.alerts.filter(alert => !alert.isRead).length;
  },
  
  // Actions
  addAlert: (alertData) => {
    const newAlert: Alert = {
      ...alertData,
      id: Date.now().toString() + Math.random().toString(36).substr(2, 9)
    };
    
    set(state => ({
      alerts: [newAlert, ...state.alerts]
    }));
  },
  
  markAsRead: (alertId) => {
    set(state => ({
      alerts: state.alerts.map(alert => 
        alert.id === alertId ? { ...alert, isRead: true } : alert
      )
    }));
  },
  
  markAllAsRead: () => {
    set(state => ({
      alerts: state.alerts.map(alert => ({ ...alert, isRead: true }))
    }));
  },
  
  removeAlert: (alertId) => {
    set(state => ({
      alerts: state.alerts.filter(alert => alert.id !== alertId)
    }));
  },
  
  clearAllAlerts: () => {
    set({ alerts: [] });
  },
  
  setFilter: (filter) => {
    set({ filter });
  },
  
  initializeAlerts: () => {
    const initialAlerts: Alert[] = [
      {
        id: '1',
        type: 'warning',
        title: 'pH fuera de rango',
        message: 'El nivel de pH (6.2) está por debajo del rango óptimo (6.5-7.0)',
        timestamp: 'Hace 5 minutos',
        isRead: false,
        sensor: 'ESP32-002'
      },
      {
        id: '2',
        type: 'info',
        title: 'Calibración programada',
        message: 'Calibración automática del sensor de pH programada para mañana',
        timestamp: 'Hace 1 hora',
        isRead: false,
        sensor: 'ESP32-002'
      },
      {
        id: '3',
        type: 'success',
        title: 'Sistema estabilizado',
        message: 'Todos los parámetros han vuelto a niveles óptimos',
        timestamp: 'Hace 2 horas',
        isRead: true
      },
      {
        id: '4',
        type: 'info',
        title: 'Actualización disponible',
        message: 'Nueva versión del firmware disponible para ESP32-001',
        timestamp: 'Hace 3 horas',
        isRead: true,
        sensor: 'ESP32-001'
      }
    ];
    
    set({ alerts: initialAlerts });
  },
  
  // Computed functions
  getFilteredAlerts: () => {
    const { alerts, filter } = get();
    
    switch (filter) {
      case 'unread':
        return alerts.filter(alert => !alert.isRead);
      case 'warning':
        return alerts.filter(alert => alert.type === 'warning');
      case 'error':
        return alerts.filter(alert => alert.type === 'error');
      default:
        return alerts;
    }
  },
  
  getUnreadCount: () => {
    const { alerts } = get();
    return alerts.filter(alert => !alert.isRead).length;
  }
}));