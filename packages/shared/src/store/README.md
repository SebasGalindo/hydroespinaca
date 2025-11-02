# Store Management - Gestión de Estado Global

Este directorio contiene todos los stores de Zustand para la gestión del estado global de la aplicación HydroEspinaca.

## Arquitectura de Estado

La aplicación utiliza **Zustand** como biblioteca de gestión de estado por su simplicidad, rendimiento y excelente integración con TypeScript.

### Principios de Diseño

- **Separación de responsabilidades**: Cada store maneja un dominio específico
- **Inmutabilidad**: Los estados se actualizan de forma inmutable
- **Tipado estricto**: Todas las interfaces están completamente tipadas
- **Acciones centralizadas**: Cada store expone acciones para modificar su estado

## Stores Disponibles

### 🔐 AuthStore (`authStore.ts`)
**Propósito**: Gestión de autenticación y sesión de usuario

**Estado**:
- `user`: Información del usuario autenticado
- `isAuthenticated`: Estado de autenticación
- `isLoading`: Estado de carga durante operaciones de auth

**Acciones**:
- `login(email, password)`: Iniciar sesión
- `logout()`: Cerrar sesión
- `register(userData)`: Registrar nuevo usuario
- `updateProfile(userData)`: Actualizar perfil de usuario

### 📊 SensorStore (`sensorStore.ts`)
**Propósito**: Gestión de datos de sensores y métricas del sistema

**Estado**:
- `sensorData`: Datos históricos de sensores
- `currentMetrics`: Métricas actuales del sistema
- `systemComponents`: Estado de componentes del sistema
- `isLoading`: Estado de carga

**Acciones**:
- `generateMockData()`: Generar datos de prueba
- `updateCurrentMetrics()`: Actualizar métricas
- `initializeSystemComponents()`: Inicializar componentes del sistema
- `updateSystemComponentStatus()`: Actualizar estado de componentes

### 📖 ReadingsStore (`readingsStore.ts`)
**Propósito**: Gestión de lecturas de sensores individuales y resúmenes

**Estado**:
- `sensorSummary`: Resumen estadístico de sensores
- `individualReadings`: Lecturas individuales de sensores

**Acciones**:
- `setSensorSummary(summary)`: Establecer resumen de sensores
- `setIndividualReadings(readings)`: Establecer lecturas individuales
- `addReading(reading)`: Agregar nueva lectura
- `initializeReadings()`: Inicializar con datos de prueba

### ⚙️ ActuatorStore (`actuatorStore.ts`)
**Propósito**: Gestión de actuadores del sistema hidropónico

**Estado**:
- `actuadores`: Lista de actuadores disponibles
- `isLoading`: Estado de carga

**Acciones**:
- `setActuadores(actuadores)`: Establecer lista de actuadores
- `addActuador(actuador)`: Agregar nuevo actuador
- `updateActuador(id, data)`: Actualizar actuador existente
- `deleteActuador(id)`: Eliminar actuador
- `initializeActuadores()`: Inicializar con datos de prueba

### 📋 VariableStore (`variableStore.ts`)
**Propósito**: Gestión de variables de monitoreo del sistema

**Estado**:
- `variables`: Lista de variables configuradas
- `isLoading`: Estado de carga

**Acciones**:
- `setVariables(variables)`: Establecer lista de variables
- `addVariable(variable)`: Agregar nueva variable
- `updateVariable(id, data)`: Actualizar variable existente
- `deleteVariable(id)`: Eliminar variable
- `initializeVariables()`: Inicializar con datos de prueba

### 🚨 AlertStore (`alertStore.ts`)
**Propósito**: Gestión de alertas y notificaciones del sistema

**Estado**:
- `alerts`: Lista de alertas activas
- `alertHistory`: Historial de alertas
- `isLoading`: Estado de carga

**Acciones**:
- `addAlert(alert)`: Agregar nueva alerta
- `markAsRead(id)`: Marcar alerta como leída
- `dismissAlert(id)`: Descartar alerta
- `clearAllAlerts()`: Limpiar todas las alertas

## Tipos de Datos

### Interfaces Principales

```typescript
// Usuario autenticado
interface User {
  id: string;
  email: string;
  name: string;
  role: 'admin' | 'user';
  avatar?: string;
}

// Datos de sensor
interface SensorData {
  timestamp: string;
  temperature: number;
  humidity: number;
  ph: number;
  light: number;
  conductivity: number;
}

// Métrica del sistema
interface MetricData {
  title: string;
  value: string;
  unit: string;
  status: 'optimal' | 'warning' | 'critical';
  trend: 'up' | 'down' | 'stable';
  change: string;
  iconType: string;
}

// Resumen de sensor
interface SensorSummary {
  sensor: string;
  media: number;
  minimo: number;
  maximo: number;
  unidad: string;
  ultimaLectura: string;
}

// Lectura individual
interface IndividualReading {
  id: string;
  sensor: string;
  valor: number;
  unidad: string;
  fecha: string;
}

// Datos de actuador
interface ActuadorData {
  id: string;
  name: string;
  type: 'bomba' | 'valvula' | 'ventilador' | 'luz';
  location: string;
  pin: number;
  esp32Id: string;
  status: 'activo' | 'inactivo' | 'error';
  createdAt: string;
  lastModified: string;
}

// Datos de variable
interface VariableData {
  id: string;
  name: string;
  description: string;
  unit: string;
  type: 'ambiental' | 'nutricional' | 'fisica' | 'biologica';
  dataType: 'number' | 'boolean' | 'string';
  minValue?: number;
  maxValue?: number;
  isRequired: boolean;
  category: 'sensor' | 'actuator' | 'calculated';
  status: 'active' | 'inactive';
  createdAt: string;
  lastModified: string;
}

// Alerta del sistema
interface Alert {
  id: string;
  type: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
  timestamp: string;
  isRead: boolean;
  source?: string;
}
```

## Uso de los Stores

### Importación
```typescript
import { 
  useAuthStore, 
  useSensorStore, 
  useReadingsStore,
  useActuatorStore,
  useVariableStore,
  useAlertStore 
} from '@/shared/store';
```

### Ejemplo de Uso en Componente
```typescript
function DashboardComponent() {
  // Obtener estado y acciones
  const { user, isAuthenticated } = useAuthStore();
  const { currentMetrics, generateMockData } = useSensorStore();
  const { sensorSummary, initializeReadings } = useReadingsStore();
  
  // Usar en efectos
  useEffect(() => {
    if (isAuthenticated) {
      generateMockData();
      initializeReadings();
    }
  }, [isAuthenticated]);
  
  return (
    <div>
      <h1>Bienvenido, {user?.name}</h1>
      {/* Renderizar métricas */}
    </div>
  );
}
```

## Inicialización de Stores

La aplicación utiliza el componente `StoreInitializer` para inicializar todos los stores con datos de prueba al cargar la aplicación.

```typescript
// En layout.tsx o _app.tsx
import { StoreInitializer } from '@/components/store/StoreInitializer';

function RootLayout({ children }) {
  return (
    <html>
      <body>
        <StoreInitializer />
        {children}
      </body>
    </html>
  );
}
```

## Persistencia de Datos

### Estado Actual
Actualmente, los stores utilizan datos en memoria que se reinician al recargar la página.

### Futuras Mejoras
- **LocalStorage**: Para persistir configuraciones de usuario
- **API Integration**: Reemplazar datos mock con llamadas reales a la API
- **Middleware de persistencia**: Usar `zustand/middleware` para persistencia automática

## Patrones de Desarrollo

### Convenciones de Nomenclatura
- **Estados**: Sustantivos descriptivos (`user`, `sensors`, `alerts`)
- **Acciones**: Verbos en infinitivo (`login`, `addSensor`, `updateMetrics`)
- **Selectores**: Prefijo `get` o `is` (`getActiveAlerts`, `isLoading`)

### Manejo de Estados Asíncronos
```typescript
// Patrón estándar para operaciones asíncronas
const loginAction = async (email: string, password: string) => {
  set({ isLoading: true });
  try {
    const user = await authAPI.login(email, password);
    set({ user, isAuthenticated: true, isLoading: false });
  } catch (error) {
    set({ isLoading: false });
    // Manejar error
  }
};
```

### Optimización de Rendimiento
- **Selectores específicos**: Evitar re-renders innecesarios
- **Shallow comparison**: Usar `shallow` de Zustand cuando sea necesario
- **Memoización**: Combinar con `useMemo` y `useCallback` en componentes

## Testing

### Estrategia de Testing
- **Unit tests**: Para acciones individuales de stores
- **Integration tests**: Para flujos completos de estado
- **Mock stores**: Para testing de componentes

### Ejemplo de Test
```typescript
import { renderHook, act } from '@testing-library/react';
import { useAuthStore } from './authStore';

test('should login user successfully', async () => {
  const { result } = renderHook(() => useAuthStore());
  
  await act(async () => {
    await result.current.login('test@example.com', 'password');
  });
  
  expect(result.current.isAuthenticated).toBe(true);
  expect(result.current.user).toBeDefined();
});
```

## Migración a API Real

Cuando se implemente el backend real:

1. **Reemplazar funciones mock** en cada store
2. **Implementar manejo de errores** robusto
3. **Agregar estados de carga** granulares
4. **Implementar retry logic** para operaciones críticas
5. **Agregar validación de datos** del servidor

## Contribución

Para agregar un nuevo store:

1. Crear archivo en `src/shared/store/`
2. Definir interfaces TypeScript
3. Implementar store con Zustand
4. Exportar desde `index.ts`
5. Agregar documentación
6. Escribir tests unitarios

## Recursos Adicionales

- [Documentación de Zustand](https://github.com/pmndrs/zustand)
- [Patrones de Estado en React](https://kentcdodds.com/blog/application-state-management-with-react)
- [TypeScript con Zustand](https://github.com/pmndrs/zustand#typescript)