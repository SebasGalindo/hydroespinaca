# API Layer - Capa de Abstracción de API

Este directorio contiene la capa de abstracción para todas las comunicaciones con el backend de la aplicación Hidro Espinaca.

## Arquitectura de API

### Principios de Diseño

- **Separación de responsabilidades**: Cada módulo maneja un dominio específico
- **Abstracción**: Los componentes no conocen detalles de implementación de la API
- **Tipado estricto**: Todas las funciones están completamente tipadas
- **Manejo de errores**: Gestión consistente de errores en toda la aplicación
- **Interceptores**: Middleware para autenticación, logging y transformación

### Estado Actual: Placeholders

Actualmente, la aplicación utiliza **funciones placeholder** que simulan llamadas a la API real. Esto permite:

- Desarrollo frontend independiente del backend
- Testing con datos consistentes
- Prototipado rápido de funcionalidades
- Migración sencilla a API real

## Estructura de Archivos

```
api/
├── placeholders.ts          # Funciones placeholder actuales
├── types.ts                 # Tipos compartidos de API
├── interceptors.ts          # Middleware de HTTP (futuro)
├── endpoints.ts             # Configuración de endpoints (futuro)
└── README.md               # Esta documentación
```

## Funciones Placeholder Disponibles

### 📊 Readings API

#### `fetchSensorSummary(): Promise<SensorSummary[]>`
**Propósito**: Obtener resumen estadístico de sensores

**Retorna**:
```typescript
interface SensorSummary {
  sensor: string;           // Nombre del sensor
  media: number;           // Valor promedio
  minimo: number;          // Valor mínimo
  maximo: number;          // Valor máximo
  unidad: string;          // Unidad de medida
  ultimaLectura: string;   // Timestamp de última lectura
}
```

**Datos de ejemplo**:
- Temperatura: 25.5°C (24.8-26.2)
- Humedad: 60.2% (59.5-61.0)
- Intensidad Lumínica: 850 lux (820-880)
- Conductividad Eléctrica: 45% (44-46)

#### `fetchIndividualReadings(): Promise<IndividualReading[]>`
**Propósito**: Obtener lecturas individuales de sensores

**Retorna**:
```typescript
interface IndividualReading {
  id: string;              // ID único de la lectura
  sensor: string;          // Nombre del sensor
  valor: number;           // Valor medido
  unidad: string;          // Unidad de medida
  fecha: string;           // Timestamp de la lectura
}
```

**Características**:
- 20 lecturas de ejemplo
- Datos cronológicos
- Múltiples sensores
- Valores realistas

### ⚙️ Actuators API

#### `fetchActuators(): Promise<ActuadorData[]>`
**Propósito**: Obtener lista de actuadores del sistema

**Retorna**:
```typescript
interface ActuadorData {
  id: string;              // ID único del actuador
  name: string;            // Nombre descriptivo
  type: 'bomba' | 'valvula' | 'ventilador' | 'luz'; // Tipo de actuador
  location: string;        // Ubicación física
  pin: number;             // Pin de conexión
  esp32Id: string;         // ID del ESP32 asociado
  status: 'activo' | 'inactivo' | 'error'; // Estado actual
  createdAt: string;       // Fecha de creación
  lastModified: string;    // Última modificación
}
```

#### `updateActuator(id: string, data: Partial<ActuadorData>): Promise<ActuadorData>`
**Propósito**: Actualizar configuración de actuador

**Parámetros**:
- `id`: ID del actuador a actualizar
- `data`: Datos parciales a actualizar

**Retorna**: Actuador actualizado

### 📈 Sensors API

#### `fetchMetrics(): Promise<MetricData[]>`
**Propósito**: Obtener métricas actuales del sistema

**Retorna**:
```typescript
interface MetricData {
  title: string;           // Título de la métrica
  value: string;           // Valor actual (como string para formato)
  unit: string;            // Unidad de medida
  status: 'optimal' | 'warning' | 'critical'; // Estado de la métrica
  trend: 'up' | 'down' | 'stable'; // Tendencia
  change: string;          // Cambio desde última medición
  iconType: string;        // Tipo de icono a mostrar
}
```

**Métricas incluidas**:
- Temperatura ambiente
- Humedad relativa
- Nivel de pH
- Luz solar
- Conductividad eléctrica

### 📋 Variables API

#### `fetchVariables(): Promise<VariableData[]>`
**Propósito**: Obtener variables de monitoreo configuradas

**Retorna**:
```typescript
interface VariableData {
  id: string;              // ID único de la variable
  name: string;            // Nombre de la variable
  description: string;     // Descripción detallada
  unit: string;            // Unidad de medida
  type: 'ambiental' | 'nutricional' | 'fisica' | 'biologica'; // Tipo
  dataType: 'number' | 'boolean' | 'string'; // Tipo de dato
  minValue?: number;       // Valor mínimo (opcional)
  maxValue?: number;       // Valor máximo (opcional)
  isRequired: boolean;     // Si es requerida
  category: 'sensor' | 'actuator' | 'calculated'; // Categoría
  status: 'active' | 'inactive'; // Estado
  createdAt: string;       // Fecha de creación
  lastModified: string;    // Última modificación
}
```

#### `updateVariable(id: string, data: Partial<VariableData>): Promise<VariableData>`
**Propósito**: Actualizar configuración de variable

**Parámetros**:
- `id`: ID de la variable a actualizar
- `data`: Datos parciales a actualizar

**Retorna**: Variable actualizada

## Uso en Componentes

### Importación
```typescript
import {
  fetchSensorSummary,
  fetchIndividualReadings,
  fetchActuators,
  updateActuator,
  fetchMetrics,
  fetchVariables,
  updateVariable
} from '@/shared/api/placeholders';
```

### Ejemplo de Uso
```typescript
const SensorDashboard = () => {
  const [metrics, setMetrics] = useState<MetricData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  useEffect(() => {
    const loadMetrics = async () => {
      try {
        setLoading(true);
        const data = await fetchMetrics();
        setMetrics(data);
      } catch (err) {
        setError('Error al cargar métricas');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    
    loadMetrics();
  }, []);
  
  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} />;
  
  return (
    <div>
      {metrics.map(metric => (
        <MetricCard key={metric.title} metric={metric} />
      ))}
    </div>
  );
};
```

### Manejo de Errores
```typescript
const updateActuatorStatus = async (id: string, status: string) => {
  try {
    const updatedActuator = await updateActuator(id, { status });
    // Actualizar estado local
    setActuators(prev => 
      prev.map(act => act.id === id ? updatedActuator : act)
    );
    // Mostrar notificación de éxito
    showNotification('Actuador actualizado correctamente', 'success');
  } catch (error) {
    // Manejar error
    console.error('Error updating actuator:', error);
    showNotification('Error al actualizar actuador', 'error');
  }
};
```

## Migración a API Real

### Pasos para la Migración

1. **Configurar cliente HTTP**
```typescript
// api/client.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para autenticación
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
```

2. **Reemplazar funciones placeholder**
```typescript
// Antes (placeholder)
export const fetchMetrics = async (): Promise<MetricData[]> => {
  // Datos mock
  return mockMetrics;
};

// Después (API real)
export const fetchMetrics = async (): Promise<MetricData[]> => {
  const response = await apiClient.get('/api/metrics');
  return response.data;
};
```

3. **Agregar manejo de errores robusto**
```typescript
export const fetchMetrics = async (): Promise<MetricData[]> => {
  try {
    const response = await apiClient.get('/api/metrics');
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      throw new APIError(
        error.response?.data?.message || 'Error al obtener métricas',
        error.response?.status || 500
      );
    }
    throw error;
  }
};
```

4. **Implementar retry logic**
```typescript
const retryRequest = async <T>(
  fn: () => Promise<T>,
  retries: number = 3
): Promise<T> => {
  try {
    return await fn();
  } catch (error) {
    if (retries > 0 && shouldRetry(error)) {
      await delay(1000); // Esperar 1 segundo
      return retryRequest(fn, retries - 1);
    }
    throw error;
  }
};
```

### Configuración de Endpoints
```typescript
// api/endpoints.ts
export const ENDPOINTS = {
  // Readings
  SENSOR_SUMMARY: '/api/readings/summary',
  INDIVIDUAL_READINGS: '/api/readings/individual',
  
  // Actuators
  ACTUATORS: '/api/actuators',
  ACTUATOR_BY_ID: (id: string) => `/api/actuators/${id}`,
  
  // Sensors
  METRICS: '/api/sensors/metrics',
  SENSOR_DATA: '/api/sensors/data',
  
  // Variables
  VARIABLES: '/api/variables',
  VARIABLE_BY_ID: (id: string) => `/api/variables/${id}`,
  
  // Auth
  LOGIN: '/api/auth/login',
  LOGOUT: '/api/auth/logout',
  REFRESH: '/api/auth/refresh',
} as const;
```

## Tipos de Datos

### Respuestas de API
```typescript
// Respuesta estándar de la API
interface APIResponse<T> {
  data: T;
  message: string;
  status: 'success' | 'error';
  timestamp: string;
}

// Error de API
interface APIError {
  message: string;
  code: string;
  details?: unknown;
  timestamp: string;
}

// Respuesta paginada
interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}
```

### Parámetros de Consulta
```typescript
// Filtros para lecturas
interface ReadingsFilters {
  startDate?: string;
  endDate?: string;
  sensorType?: string;
  limit?: number;
  offset?: number;
}

// Filtros para actuadores
interface ActuatorFilters {
  type?: ActuadorData['type'];
  status?: ActuadorData['status'];
  location?: string;
}
```

## Testing

### Mock de Funciones API
```typescript
// __mocks__/api.ts
export const mockFetchMetrics = jest.fn().mockResolvedValue([
  {
    title: 'Temperatura',
    value: '25.5',
    unit: '°C',
    status: 'optimal',
    trend: 'stable',
    change: '0.0',
    iconType: 'temperature'
  }
]);

export const mockUpdateActuator = jest.fn().mockImplementation(
  (id, data) => Promise.resolve({ id, ...data })
);
```

### Tests de Integración
```typescript
import { fetchMetrics } from '../placeholders';

describe('API Placeholders', () => {
  test('fetchMetrics returns expected data structure', async () => {
    const metrics = await fetchMetrics();
    
    expect(Array.isArray(metrics)).toBe(true);
    expect(metrics.length).toBeGreaterThan(0);
    
    metrics.forEach(metric => {
      expect(metric).toHaveProperty('title');
      expect(metric).toHaveProperty('value');
      expect(metric).toHaveProperty('status');
    });
  });
});
```

## Performance

### Optimizaciones

- **Caching**: Implementar cache de respuestas
- **Debouncing**: Para búsquedas y filtros
- **Pagination**: Para listas grandes
- **Compression**: Gzip en respuestas

```typescript
// Cache simple con TTL
const cache = new Map<string, { data: unknown; expires: number }>();

const getCachedData = <T>(key: string): T | null => {
  const cached = cache.get(key);
  if (cached && cached.expires > Date.now()) {
    return cached.data as T;
  }
  cache.delete(key);
  return null;
};

const setCachedData = <T>(key: string, data: T, ttl: number = 300000) => {
  cache.set(key, {
    data,
    expires: Date.now() + ttl
  });
};
```

## Seguridad

### Mejores Prácticas

- **Validación de entrada**: Sanitizar todos los inputs
- **Autenticación**: JWT tokens con refresh
- **Autorización**: Verificar permisos en cada endpoint
- **Rate limiting**: Prevenir abuso de API
- **HTTPS**: Todas las comunicaciones encriptadas

```typescript
// Validación de entrada
const validateActuatorData = (data: Partial<ActuadorData>): boolean => {
  if (data.name && data.name.length < 3) {
    throw new Error('Nombre debe tener al menos 3 caracteres');
  }
  if (data.pin && (data.pin < 0 || data.pin > 40)) {
    throw new Error('Pin debe estar entre 0 y 40');
  }
  return true;
};
```

## Contribución

### Agregar Nueva Función API

1. **Definir tipos** TypeScript
2. **Implementar función placeholder**
3. **Agregar documentación**
4. **Escribir tests**
5. **Actualizar este README**

### Code Review Checklist

- [ ] Tipado TypeScript completo
- [ ] Manejo de errores implementado
- [ ] Validación de entrada
- [ ] Tests unitarios
- [ ] Documentación actualizada
- [ ] Performance considerada
- [ ] Seguridad verificada

## Recursos

- [Axios Documentation](https://axios-http.com/)
- [REST API Best Practices](https://restfulapi.net/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Jest Testing Framework](https://jestjs.io/)