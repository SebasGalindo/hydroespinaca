# Biblioteca de Utilidades (lib)

Este directorio contiene utilidades, configuraciones y helpers fundamentales para el funcionamiento de la aplicación de monitoreo hidropónico.

## Arquitectura

### Principios de Diseño

- **Modularidad**: Cada utilidad tiene una responsabilidad específica
- **Reutilización**: Funciones diseñadas para uso en múltiples contextos
- **Performance**: Optimizadas para operaciones frecuentes
- **Type Safety**: Completamente tipadas con TypeScript
- **Tree Shaking**: Exportaciones específicas para optimización de bundle

### Estructura del Directorio

```
src/lib/
├── utils.ts              # Utilidades generales
├── validations.ts        # Esquemas de validación con Zod
├── constants.ts          # Constantes de la aplicación
├── formatters.ts         # Funciones de formateo
├── auth.ts              # Utilidades de autenticación
├── api.ts               # Cliente y configuración de API
├── storage.ts           # Gestión de almacenamiento local
├── websocket.ts         # Cliente WebSocket
├── charts.ts            # Configuraciones para gráficos
├── notifications.ts     # Sistema de notificaciones
└── README.md           # Esta documentación
```

## Utilidades Principales

### utils.ts - Utilidades Generales

**Propósito**: Funciones helper comunes utilizadas en toda la aplicación

```typescript
/**
 * Combina clases CSS de manera condicional
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

/**
 * Genera un ID único
 */
export function generateId(prefix?: string): string {
  const id = Math.random().toString(36).substr(2, 9);
  return prefix ? `${prefix}_${id}` : id;
}

/**
 * Debounce para optimizar llamadas frecuentes
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
}

/**
 * Throttle para limitar frecuencia de ejecución
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle: boolean;
  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => (inThrottle = false), limit);
    }
  };
}

/**
 * Copia texto al portapapeles
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch (error) {
    console.error('Error copying to clipboard:', error);
    return false;
  }
}

/**
 * Descarga datos como archivo
 */
export function downloadAsFile(
  data: string,
  filename: string,
  type: string = 'text/plain'
): void {
  const blob = new Blob([data], { type });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Calcula estadísticas básicas de un array de números
 */
export function calculateStats(values: number[]): {
  mean: number;
  median: number;
  min: number;
  max: number;
  std: number;
} {
  if (values.length === 0) {
    return { mean: 0, median: 0, min: 0, max: 0, std: 0 };
  }

  const sorted = [...values].sort((a, b) => a - b);
  const mean = values.reduce((sum, val) => sum + val, 0) / values.length;
  const median = sorted.length % 2 === 0
    ? (sorted[sorted.length / 2 - 1] + sorted[sorted.length / 2]) / 2
    : sorted[Math.floor(sorted.length / 2)];
  const min = sorted[0];
  const max = sorted[sorted.length - 1];
  const variance = values.reduce((sum, val) => sum + Math.pow(val - mean, 2), 0) / values.length;
  const std = Math.sqrt(variance);

  return { mean, median, min, max, std };
}
```

### validations.ts - Esquemas de Validación

**Propósito**: Esquemas Zod para validación de datos

```typescript
import { z } from 'zod';

/**
 * Esquema para datos de sensor
 */
export const sensorSchema = z.object({
  id: z.string().min(1, 'ID es requerido'),
  name: z.string().min(1, 'Nombre es requerido').max(100, 'Nombre muy largo'),
  type: z.enum(['temperature', 'humidity', 'ph', 'ec', 'light', 'water_level']),
  location: z.string().min(1, 'Ubicación es requerida'),
  description: z.string().optional(),
  minThreshold: z.number().min(-100).max(1000),
  maxThreshold: z.number().min(-100).max(1000),
  alertsEnabled: z.boolean().default(true)
}).refine(data => data.minThreshold < data.maxThreshold, {
  message: 'El umbral mínimo debe ser menor que el máximo',
  path: ['maxThreshold']
});

/**
 * Esquema para lectura de sensor
 */
export const readingSchema = z.object({
  sensorId: z.string().min(1),
  value: z.number().finite(),
  timestamp: z.string().datetime(),
  quality: z.enum(['good', 'warning', 'error']).default('good')
});

/**
 * Esquema para usuario
 */
export const userSchema = z.object({
  email: z.string().email('Email inválido'),
  password: z.string().min(8, 'Contraseña debe tener al menos 8 caracteres'),
  name: z.string().min(1, 'Nombre es requerido'),
  role: z.enum(['admin', 'operator', 'viewer']).default('viewer')
});

/**
 * Esquema para configuración de actuador
 */
export const actuatorSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1, 'Nombre es requerido'),
  type: z.enum(['pump', 'valve', 'fan', 'heater', 'light']),
  location: z.string().min(1, 'Ubicación es requerida'),
  autoMode: z.boolean().default(false),
  schedule: z.array(z.object({
    startTime: z.string().regex(/^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/),
    endTime: z.string().regex(/^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/),
    days: z.array(z.number().min(0).max(6))
  })).optional()
});

/**
 * Esquema para variable del sistema
 */
export const variableSchema = z.object({
  name: z.string().min(1, 'Nombre es requerido'),
  type: z.enum(['number', 'string', 'boolean']),
  value: z.union([z.number(), z.string(), z.boolean()]),
  description: z.string().optional(),
  category: z.string().min(1, 'Categoría es requerida'),
  unit: z.string().optional(),
  validRange: z.object({
    min: z.number(),
    max: z.number()
  }).optional()
});

/**
 * Funciones de validación
 */
export const validators = {
  sensor: (data: unknown) => sensorSchema.parse(data),
  reading: (data: unknown) => readingSchema.parse(data),
  user: (data: unknown) => userSchema.parse(data),
  actuator: (data: unknown) => actuatorSchema.parse(data),
  variable: (data: unknown) => variableSchema.parse(data),
  
  // Validaciones parciales para formularios
  sensorPartial: (data: unknown) => sensorSchema.partial().parse(data),
  userPartial: (data: unknown) => userSchema.partial().parse(data)
};
```

### constants.ts - Constantes de la Aplicación

**Propósito**: Valores constantes utilizados en toda la aplicación

```typescript
/**
 * Configuración de sensores
 */
export const SENSOR_TYPES = {
  TEMPERATURE: {
    id: 'temperature',
    name: 'Temperatura',
    unit: '°C',
    icon: 'thermometer',
    color: '#ef4444',
    validRange: { min: -10, max: 50 },
    optimalRange: { min: 18, max: 28 }
  },
  HUMIDITY: {
    id: 'humidity',
    name: 'Humedad',
    unit: '%',
    icon: 'droplets',
    color: '#3b82f6',
    validRange: { min: 0, max: 100 },
    optimalRange: { min: 60, max: 80 }
  },
  PH: {
    id: 'ph',
    name: 'pH',
    unit: 'pH',
    icon: 'beaker',
    color: '#10b981',
    validRange: { min: 0, max: 14 },
    optimalRange: { min: 5.5, max: 6.5 }
  },
  EC: {
    id: 'ec',
    name: 'Conductividad',
    unit: 'mS/cm',
    icon: 'zap',
    color: '#f59e0b',
    validRange: { min: 0, max: 5 },
    optimalRange: { min: 1.2, max: 2.0 }
  },
  LIGHT: {
    id: 'light',
    name: 'Luz',
    unit: 'lux',
    icon: 'sun',
    color: '#eab308',
    validRange: { min: 0, max: 100000 },
    optimalRange: { min: 20000, max: 40000 }
  },
  WATER_LEVEL: {
    id: 'water_level',
    name: 'Nivel de Agua',
    unit: 'cm',
    icon: 'waves',
    color: '#06b6d4',
    validRange: { min: 0, max: 100 },
    optimalRange: { min: 20, max: 80 }
  }
} as const;

/**
 * Estados del sistema
 */
export const SYSTEM_STATUS = {
  ONLINE: { id: 'online', name: 'En Línea', color: '#10b981' },
  OFFLINE: { id: 'offline', name: 'Desconectado', color: '#ef4444' },
  WARNING: { id: 'warning', name: 'Advertencia', color: '#f59e0b' },
  MAINTENANCE: { id: 'maintenance', name: 'Mantenimiento', color: '#6b7280' }
} as const;

/**
 * Configuración de API
 */
export const API_CONFIG = {
  BASE_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001',
  WS_URL: process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:3001',
  TIMEOUT: 10000,
  RETRY_ATTEMPTS: 3,
  RETRY_DELAY: 1000
} as const;

/**
 * Configuración de almacenamiento
 */
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  USER_PREFERENCES: 'user_preferences',
  DASHBOARD_LAYOUT: 'dashboard_layout',
  SENSOR_FILTERS: 'sensor_filters',
  CHART_SETTINGS: 'chart_settings'
} as const;

/**
 * Intervalos de actualización (en milisegundos)
 */
export const UPDATE_INTERVALS = {
  REAL_TIME: 1000,
  FAST: 5000,
  NORMAL: 30000,
  SLOW: 60000
} as const;

/**
 * Límites de la aplicación
 */
export const LIMITS = {
  MAX_SENSORS: 50,
  MAX_READINGS_PER_REQUEST: 1000,
  MAX_FILE_SIZE: 5 * 1024 * 1024, // 5MB
  MAX_CHART_POINTS: 500
} as const;

/**
 * Configuración de notificaciones
 */
export const NOTIFICATION_TYPES = {
  SUCCESS: { id: 'success', duration: 3000 },
  ERROR: { id: 'error', duration: 5000 },
  WARNING: { id: 'warning', duration: 4000 },
  INFO: { id: 'info', duration: 3000 }
} as const;
```

### formatters.ts - Funciones de Formateo

**Propósito**: Formateo consistente de datos para la UI

```typescript
/**
 * Formatea números con precisión específica
 */
export function formatNumber(
  value: number,
  precision: number = 2,
  locale: string = 'es-MX'
): string {
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: precision,
    maximumFractionDigits: precision
  }).format(value);
}

/**
 * Formatea valores de sensores con unidad
 */
export function formatSensorValue(
  value: number,
  unit: string,
  precision: number = 1
): string {
  return `${formatNumber(value, precision)} ${unit}`;
}

/**
 * Formatea fechas de manera relativa
 */
export function formatRelativeTime(
  date: string | Date,
  locale: string = 'es'
): string {
  const now = new Date();
  const target = new Date(date);
  const diffMs = now.getTime() - target.getTime();
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMinutes < 1) return 'Ahora';
  if (diffMinutes < 60) return `Hace ${diffMinutes} min`;
  if (diffHours < 24) return `Hace ${diffHours} h`;
  if (diffDays < 7) return `Hace ${diffDays} días`;
  
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(target);
}

/**
 * Formatea fechas absolutas
 */
export function formatDateTime(
  date: string | Date,
  options: Intl.DateTimeFormatOptions = {},
  locale: string = 'es-MX'
): string {
  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    ...options
  };
  
  return new Intl.DateTimeFormat(locale, defaultOptions).format(new Date(date));
}

/**
 * Formatea tamaños de archivo
 */
export function formatFileSize(bytes: number): string {
  const units = ['B', 'KB', 'MB', 'GB'];
  let size = bytes;
  let unitIndex = 0;
  
  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex++;
  }
  
  return `${formatNumber(size, unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
}

/**
 * Formatea porcentajes
 */
export function formatPercentage(
  value: number,
  total: number,
  precision: number = 1
): string {
  const percentage = (value / total) * 100;
  return `${formatNumber(percentage, precision)}%`;
}

/**
 * Formatea duración en milisegundos
 */
export function formatDuration(ms: number): string {
  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);
  
  if (days > 0) return `${days}d ${hours % 24}h`;
  if (hours > 0) return `${hours}h ${minutes % 60}m`;
  if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
  return `${seconds}s`;
}

/**
 * Trunca texto con elipsis
 */
export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength - 3) + '...';
}

/**
 * Capitaliza primera letra
 */
export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
}

/**
 * Convierte a formato de título
 */
export function toTitleCase(text: string): string {
  return text
    .split(' ')
    .map(word => capitalize(word))
    .join(' ');
}
```

### auth.ts - Utilidades de Autenticación

**Propósito**: Gestión de autenticación y autorización

```typescript
import { jwtDecode } from 'jwt-decode';
import type { User, UserRole, Permission } from '@/shared/types';

/**
 * Interfaz para token JWT
 */
interface JWTPayload {
  sub: string;
  email: string;
  name: string;
  role: UserRole;
  permissions: Permission[];
  exp: number;
  iat: number;
}

/**
 * Verifica si un token JWT es válido
 */
export function isTokenValid(token: string): boolean {
  try {
    const decoded = jwtDecode<JWTPayload>(token);
    const now = Date.now() / 1000;
    return decoded.exp > now;
  } catch {
    return false;
  }
}

/**
 * Extrae información del usuario del token
 */
export function getUserFromToken(token: string): User | null {
  try {
    const decoded = jwtDecode<JWTPayload>(token);
    return {
      id: decoded.sub,
      email: decoded.email,
      name: decoded.name,
      role: decoded.role,
      permissions: decoded.permissions,
      status: 'active'
    };
  } catch {
    return null;
  }
}

/**
 * Verifica si el usuario tiene un permiso específico
 */
export function hasPermission(
  user: User | null,
  permission: Permission
): boolean {
  if (!user) return false;
  return user.permissions.includes(permission);
}

/**
 * Verifica si el usuario tiene uno de varios permisos
 */
export function hasAnyPermission(
  user: User | null,
  permissions: Permission[]
): boolean {
  if (!user) return false;
  return permissions.some(permission => user.permissions.includes(permission));
}

/**
 * Verifica si el usuario tiene todos los permisos especificados
 */
export function hasAllPermissions(
  user: User | null,
  permissions: Permission[]
): boolean {
  if (!user) return false;
  return permissions.every(permission => user.permissions.includes(permission));
}

/**
 * Verifica si el usuario tiene un rol específico
 */
export function hasRole(user: User | null, role: UserRole): boolean {
  if (!user) return false;
  return user.role === role;
}

/**
 * Verifica si el usuario es administrador
 */
export function isAdmin(user: User | null): boolean {
  return hasRole(user, 'admin');
}

/**
 * Genera un hash seguro para contraseñas (solo para demo)
 */
export async function hashPassword(password: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(password);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return Array.from(new Uint8Array(hash))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
}

/**
 * Genera un token de sesión temporal
 */
export function generateSessionToken(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Calcula tiempo restante del token
 */
export function getTokenTimeRemaining(token: string): number {
  try {
    const decoded = jwtDecode<JWTPayload>(token);
    const now = Date.now() / 1000;
    return Math.max(0, decoded.exp - now);
  } catch {
    return 0;
  }
}
```

## Configuraciones Avanzadas

### api.ts - Cliente de API

**Propósito**: Cliente HTTP configurado para la API

```typescript
import { API_CONFIG } from './constants';

/**
 * Cliente HTTP base
 */
class ApiClient {
  private baseURL: string;
  private timeout: number;
  private retryAttempts: number;
  private retryDelay: number;

  constructor() {
    this.baseURL = API_CONFIG.BASE_URL;
    this.timeout = API_CONFIG.TIMEOUT;
    this.retryAttempts = API_CONFIG.RETRY_ATTEMPTS;
    this.retryDelay = API_CONFIG.RETRY_DELAY;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    attempt: number = 1
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
          ...options.headers
        }
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (attempt < this.retryAttempts) {
        await new Promise(resolve => setTimeout(resolve, this.retryDelay));
        return this.request<T>(endpoint, options, attempt + 1);
      }
      
      throw error;
    }
  }

  async get<T>(endpoint: string, headers?: Record<string, string>): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET', headers });
  }

  async post<T>(
    endpoint: string,
    data?: any,
    headers?: Record<string, string>
  ): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
      headers
    });
  }

  async put<T>(
    endpoint: string,
    data?: any,
    headers?: Record<string, string>
  ): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
      headers
    });
  }

  async delete<T>(
    endpoint: string,
    headers?: Record<string, string>
  ): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE', headers });
  }
}

export const apiClient = new ApiClient();
```

### storage.ts - Gestión de Almacenamiento

**Propósito**: Abstracción para localStorage con tipado

```typescript
import { STORAGE_KEYS } from './constants';

/**
 * Interfaz para el almacenamiento tipado
 */
interface StorageData {
  [STORAGE_KEYS.AUTH_TOKEN]: string;
  [STORAGE_KEYS.USER_PREFERENCES]: {
    theme: 'light' | 'dark';
    language: string;
    notifications: boolean;
  };
  [STORAGE_KEYS.DASHBOARD_LAYOUT]: {
    widgets: string[];
    layout: any[];
  };
  [STORAGE_KEYS.SENSOR_FILTERS]: {
    types: string[];
    locations: string[];
    status: string[];
  };
  [STORAGE_KEYS.CHART_SETTINGS]: {
    timeRange: string;
    refreshInterval: number;
    showAnimations: boolean;
  };
}

/**
 * Clase para gestión de almacenamiento local
 */
class Storage {
  /**
   * Obtiene un valor del almacenamiento
   */
  get<K extends keyof StorageData>(
    key: K
  ): StorageData[K] | null {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : null;
    } catch (error) {
      console.error(`Error reading from storage (${key}):`, error);
      return null;
    }
  }

  /**
   * Guarda un valor en el almacenamiento
   */
  set<K extends keyof StorageData>(
    key: K,
    value: StorageData[K]
  ): void {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.error(`Error writing to storage (${key}):`, error);
    }
  }

  /**
   * Elimina un valor del almacenamiento
   */
  remove<K extends keyof StorageData>(key: K): void {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error(`Error removing from storage (${key}):`, error);
    }
  }

  /**
   * Limpia todo el almacenamiento
   */
  clear(): void {
    try {
      localStorage.clear();
    } catch (error) {
      console.error('Error clearing storage:', error);
    }
  }

  /**
   * Verifica si una clave existe
   */
  has<K extends keyof StorageData>(key: K): boolean {
    return localStorage.getItem(key) !== null;
  }

  /**
   * Obtiene todas las claves
   */
  keys(): string[] {
    return Object.keys(localStorage);
  }

  /**
   * Obtiene el tamaño usado en bytes (aproximado)
   */
  getSize(): number {
    let total = 0;
    for (const key in localStorage) {
      if (localStorage.hasOwnProperty(key)) {
        total += localStorage[key].length + key.length;
      }
    }
    return total;
  }
}

export const storage = new Storage();
```

## Testing y Calidad

### Estrategias de Testing

```typescript
// Ejemplo de test para utilidades
import { describe, it, expect, vi } from 'vitest';
import { debounce, throttle, calculateStats } from './utils';

describe('Utils', () => {
  describe('debounce', () => {
    it('should delay function execution', async () => {
      const fn = vi.fn();
      const debouncedFn = debounce(fn, 100);
      
      debouncedFn();
      debouncedFn();
      debouncedFn();
      
      expect(fn).not.toHaveBeenCalled();
      
      await new Promise(resolve => setTimeout(resolve, 150));
      expect(fn).toHaveBeenCalledTimes(1);
    });
  });

  describe('calculateStats', () => {
    it('should calculate correct statistics', () => {
      const values = [1, 2, 3, 4, 5];
      const stats = calculateStats(values);
      
      expect(stats.mean).toBe(3);
      expect(stats.median).toBe(3);
      expect(stats.min).toBe(1);
      expect(stats.max).toBe(5);
    });
  });
});
```

### Performance y Optimización

- **Memoización**: Funciones costosas memoizadas
- **Lazy Loading**: Carga diferida de utilidades pesadas
- **Tree Shaking**: Exportaciones específicas
- **Bundle Splitting**: Separación por funcionalidad

## Contribución

### Agregar Nueva Utilidad

1. **Identificar propósito**: ¿Qué problema resuelve?
2. **Crear función**: Con tipado completo
3. **Documentar**: JSDoc con ejemplos
4. **Testear**: Casos de uso y edge cases
5. **Exportar**: Agregar a exports
6. **Optimizar**: Performance y bundle size

### Code Review Checklist

- [ ] Función bien documentada
- [ ] Tipado completo
- [ ] Tests incluidos
- [ ] Performance considerada
- [ ] Compatibilidad con SSR
- [ ] Manejo de errores
- [ ] Exportación correcta

## Recursos

- [Zod Documentation](https://zod.dev/)
- [Web APIs](https://developer.mozilla.org/en-US/docs/Web/API)
- [TypeScript Utilities](https://www.typescriptlang.org/docs/handbook/utility-types.html)
- [Performance Best Practices](https://web.dev/performance/)