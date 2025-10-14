# Dashboard - Panel de Control Hidropónico

Este directorio contiene todas las páginas del dashboard de la aplicación Hidro Espinaca, implementando un sistema completo de monitoreo y control para cultivos hidropónicos.

## Arquitectura del Dashboard

### Principios de Diseño

- **Modularidad**: Cada página maneja un aspecto específico del sistema
- **Consistencia**: Layout y patrones de UI uniformes
- **Responsividad**: Adaptación completa a dispositivos móviles y desktop
- **Tiempo real**: Actualización automática de datos críticos
- **Usabilidad**: Interfaz intuitiva para operadores de cultivo

### Estructura de Páginas

```
dashboard/
├── page.tsx                 # Dashboard principal - Resumen general
├── lecturas/               # Monitoreo de lecturas de sensores
│   └── page.tsx
├── actuadores-config/      # Configuración de actuadores
│   └── page.tsx
├── sensores-config/        # Configuración de sensores
│   └── page.tsx
├── variables-config/       # Configuración de variables
│   └── page.tsx
├── nueva-variable/         # Creación de nuevas variables
│   └── page.tsx
└── README.md              # Esta documentación
```

## Páginas del Dashboard

### 🏠 Dashboard Principal (`page.tsx`)

**Propósito**: Vista general del estado del sistema hidropónico

**Características**:
- **Métricas en tiempo real**: Temperatura, humedad, pH, luz, conductividad
- **Estado del sistema**: Componentes online/offline, alertas críticas
- **Navegación rápida**: Acceso directo a todas las secciones
- **Responsive design**: Adaptación automática a móvil/tablet/desktop

**Componentes principales**:
- `MetricsGrid`: Grid de métricas principales
- `SystemStatusCard`: Estado de componentes del sistema
- `QuickActions`: Acciones rápidas de navegación

**Datos mostrados**:
```typescript
interface DashboardData {
  metrics: MetricData[];        // Métricas actuales
  systemStatus: SystemComponent[]; // Estado de componentes
  alerts: Alert[];             // Alertas activas
  lastUpdate: string;          // Última actualización
}
```

**Actualización**: Cada 30 segundos automáticamente

### 📊 Lecturas de Sensores (`lecturas/page.tsx`)

**Propósito**: Monitoreo detallado de todas las lecturas de sensores

**Características**:
- **Resumen estadístico**: Media, mínimo, máximo por sensor
- **Lecturas individuales**: Listado cronológico detallado
- **Filtros avanzados**: Por fecha, hora, sensor, valor
- **Exportación**: Descarga de datos en CSV/Excel

**Secciones**:
1. **Resumen de últimos 10 minutos**
   - Tabla con estadísticas por sensor
   - Valores con unidades
   - Timestamp de última lectura

2. **Controles de filtro**
   - Selector de fecha
   - Selector de hora
   - Filtro por sensor
   - Búsqueda por valor

3. **Lecturas individuales**
   - Tabla paginada
   - Ordenamiento por fecha
   - Vista responsive con cards móviles

**Tipos de datos**:
```typescript
interface SensorSummary {
  sensor: string;           // Nombre del sensor
  media: number;           // Valor promedio
  minimo: number;          // Valor mínimo registrado
  maximo: number;          // Valor máximo registrado
  unidad: string;          // Unidad de medida
  ultimaLectura: string;   // Timestamp última lectura
}

interface IndividualReading {
  id: string;              // ID único de la lectura
  sensor: string;          // Nombre del sensor
  valor: number;           // Valor medido
  unidad: string;          // Unidad de medida
  fecha: string;           // Timestamp de la lectura
}
```

### ⚙️ Configuración de Actuadores (`actuadores-config/page.tsx`)

**Propósito**: Gestión y configuración de actuadores del sistema

**Características**:
- **Lista de actuadores**: Tabla con todos los actuadores configurados
- **Estados en tiempo real**: Activo, inactivo, error
- **Configuración individual**: Edición de parámetros por actuador
- **Control manual**: Activación/desactivación directa

**Funcionalidades**:
- **CRUD completo**: Crear, leer, actualizar, eliminar actuadores
- **Validación**: Verificación de configuraciones
- **Historial**: Registro de cambios y activaciones
- **Agrupación**: Por tipo, ubicación, ESP32

**Tipos de actuadores**:
- `bomba`: Bombas de agua y nutrientes
- `valvula`: Válvulas de control de flujo
- `ventilador`: Ventilación y circulación de aire
- `luz`: Iluminación LED para cultivo

**Configuración por actuador**:
```typescript
interface ActuadorData {
  id: string;              // ID único
  name: string;            // Nombre descriptivo
  type: ActuatorType;      // Tipo de actuador
  location: string;        // Ubicación física
  pin: number;             // Pin de conexión GPIO
  esp32Id: string;         // ID del ESP32 asociado
  status: ActuatorStatus;  // Estado actual
  createdAt: string;       // Fecha de creación
  lastModified: string;    // Última modificación
}
```

### 🔧 Configuración de Sensores (`sensores-config/page.tsx`)

**Propósito**: Configuración y calibración de sensores del sistema

**Características**:
- **Gestión de sensores**: Lista completa con estados
- **Calibración**: Herramientas de calibración por sensor
- **Rangos de operación**: Configuración de valores óptimos
- **Frecuencia de lectura**: Configuración de intervalos

**Funcionalidades**:
- **Calibración automática**: Procesos guiados de calibración
- **Rangos personalizados**: Definición de valores mínimos/máximos
- **Alertas configurables**: Umbrales de advertencia y críticos
- **Historial de calibración**: Registro de calibraciones anteriores

**Tipos de sensores**:
- Temperatura ambiente
- Humedad relativa
- pH del agua
- Conductividad eléctrica
- Intensidad lumínica
- Nivel de agua

### 📋 Configuración de Variables (`variables-config/page.tsx`)

**Propósito**: Gestión de variables de monitoreo del sistema

**Características**:
- **Variables del sistema**: Lista de todas las variables monitoreadas
- **Categorización**: Por tipo (ambiental, nutricional, física, biológica)
- **Configuración de rangos**: Valores mínimos, máximos y óptimos
- **Estados**: Activa, inactiva, en mantenimiento

**Categorías de variables**:
1. **Ambientales**: Temperatura, humedad, luz
2. **Nutricionales**: pH, conductividad, nutrientes
3. **Físicas**: Nivel de agua, flujo, presión
4. **Biológicas**: Crecimiento, desarrollo de plantas

**Configuración por variable**:
```typescript
interface VariableData {
  id: string;              // ID único
  name: string;            // Nombre de la variable
  description: string;     // Descripción detallada
  unit: string;            // Unidad de medida
  type: VariableType;      // Tipo de variable
  dataType: DataType;      // Tipo de dato (number, boolean, string)
  minValue?: number;       // Valor mínimo
  maxValue?: number;       // Valor máximo
  isRequired: boolean;     // Si es requerida
  category: Category;      // Categoría
  status: Status;          // Estado actual
  createdAt: string;       // Fecha de creación
  lastModified: string;    // Última modificación
}
```

### ➕ Nueva Variable (`nueva-variable/page.tsx`)

**Propósito**: Creación de nuevas variables de monitoreo

**Características**:
- **Formulario guiado**: Proceso paso a paso
- **Validación en tiempo real**: Verificación de datos
- **Previsualización**: Vista previa de la configuración
- **Plantillas**: Variables predefinidas comunes

**Proceso de creación**:
1. **Información básica**: Nombre, descripción, unidad
2. **Tipo y categoría**: Clasificación de la variable
3. **Rangos de valores**: Mínimo, máximo, óptimo
4. **Configuración de alertas**: Umbrales críticos
5. **Revisión y confirmación**: Validación final

**Componentes del formulario**:
- `VariableTypeSelector`: Selector visual de tipos
- `RangeInputs`: Configuración de rangos
- `AlertsConfiguration`: Configuración de alertas
- `FormValidation`: Validación en tiempo real

## Patrones de Desarrollo

### Estructura de Página Estándar

```typescript
'use client';

import React, { useState, useEffect } from 'react';
import { useStore } from '@/shared/store';
import PageLayout from '@/components/layout/PageLayout';
import Section from '@/components/ui/Section';

export default function DashboardPage() {
  // Estado local
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Store global
  const { data, actions } = useStore();
  
  // Efectos
  useEffect(() => {
    // Inicialización
  }, []);
  
  // Handlers
  const handleAction = () => {
    // Lógica de acción
  };
  
  return (
    <PageLayout 
      title="Título de la Página"
      subtitle="Descripción de la funcionalidad"
      maxWidth="xl"
    >
      <Section title="Sección 1" spacing="md">
        {/* Contenido */}
      </Section>
      
      <Section title="Sección 2" spacing="md">
        {/* Contenido */}
      </Section>
    </PageLayout>
  );
}
```

### Manejo de Estado

```typescript
// Estado local para UI
const [selectedDate, setSelectedDate] = useState('');
const [filters, setFilters] = useState<FilterState>({});
const [isLoading, setIsLoading] = useState(false);

// Estado global desde stores
const { sensorData, updateSensor } = useSensorStore();
const { alerts, addAlert } = useAlertStore();

// Efectos para sincronización
useEffect(() => {
  const interval = setInterval(() => {
    // Actualización automática
  }, 30000);
  
  return () => clearInterval(interval);
}, []);
```

### Responsive Design

```typescript
// Grid responsive estándar
const ResponsiveGrid = ({ children }) => (
  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
    {children}
  </div>
);

// Tabla responsive con cards móviles
const ResponsiveTable = ({ data, columns }) => (
  <Table 
    columns={columns}
    data={data}
    responsive
    mobileCardRender={(item) => (
      <MobileCard item={item} />
    )}
  />
);
```

## Navegación y Routing

### Estructura de Rutas

```
/dashboard                   # Dashboard principal
/dashboard/lecturas         # Lecturas de sensores
/dashboard/actuadores-config # Configuración de actuadores
/dashboard/sensores-config  # Configuración de sensores
/dashboard/variables-config # Configuración de variables
/dashboard/nueva-variable   # Nueva variable
```

### Navegación Programática

```typescript
import { useRouter } from 'next/navigation';

const NavigationExample = () => {
  const router = useRouter();
  
  const navigateToReadings = () => {
    router.push('/dashboard/lecturas');
  };
  
  const navigateWithParams = () => {
    router.push('/dashboard/lecturas?sensor=temperature&date=2024-01-26');
  };
};
```

## Seguridad y Autenticación

### Protección de Rutas

```typescript
// Middleware de autenticación
export default function DashboardLayout({ children }) {
  const { isAuthenticated, user } = useAuthStore();
  
  if (!isAuthenticated) {
    redirect('/login');
  }
  
  return (
    <div className="dashboard-layout">
      <DashboardHeader user={user} />
      <main>{children}</main>
      <BottomNavigation />
    </div>
  );
}
```

### Permisos por Rol

```typescript
const ProtectedAction = ({ requiredRole, children }) => {
  const { user } = useAuthStore();
  
  if (user?.role !== requiredRole) {
    return <UnauthorizedMessage />;
  }
  
  return children;
};

// Uso
<ProtectedAction requiredRole="admin">
  <DeleteButton onDelete={handleDelete} />
</ProtectedAction>
```

## Performance y Optimización

### Lazy Loading

```typescript
// Carga diferida de componentes pesados
const HeavyChart = lazy(() => import('@/components/charts/HeavyChart'));

const DashboardPage = () => (
  <Suspense fallback={<ChartSkeleton />}>
    <HeavyChart data={chartData} />
  </Suspense>
);
```

### Memoización

```typescript
// Memoización de cálculos costosos
const processedData = useMemo(() => {
  return rawData.map(item => expensiveCalculation(item));
}, [rawData]);

// Memoización de componentes
const MemoizedTable = React.memo(Table);
```

### Virtualización

```typescript
// Para listas muy grandes
import { FixedSizeList as List } from 'react-window';

const VirtualizedReadingsList = ({ readings }) => (
  <List
    height={600}
    itemCount={readings.length}
    itemSize={60}
    itemData={readings}
  >
    {ReadingRow}
  </List>
);
```

## Testing

### Tests de Páginas

```typescript
import { render, screen, waitFor } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import DashboardPage from './page';

// Mock del router
jest.mock('next/navigation');
const mockPush = jest.fn();
(useRouter as jest.Mock).mockReturnValue({ push: mockPush });

describe('Dashboard Page', () => {
  test('renders dashboard metrics', async () => {
    render(<DashboardPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Temperatura')).toBeInTheDocument();
      expect(screen.getByText('Humedad')).toBeInTheDocument();
    });
  });
  
  test('navigates to readings page', () => {
    render(<DashboardPage />);
    
    const readingsButton = screen.getByText('Ver Lecturas');
    fireEvent.click(readingsButton);
    
    expect(mockPush).toHaveBeenCalledWith('/dashboard/lecturas');
  });
});
```

### Tests de Integración

```typescript
// Test de flujo completo
test('complete sensor configuration flow', async () => {
  render(<SensorsConfigPage />);
  
  // Agregar nuevo sensor
  fireEvent.click(screen.getByText('Agregar Sensor'));
  
  // Llenar formulario
  fireEvent.change(screen.getByLabelText('Nombre'), {
    target: { value: 'Sensor de Prueba' }
  });
  
  // Guardar
  fireEvent.click(screen.getByText('Guardar'));
  
  // Verificar que aparece en la lista
  await waitFor(() => {
    expect(screen.getByText('Sensor de Prueba')).toBeInTheDocument();
  });
});
```

## Deployment y Build

### Optimización de Build

```typescript
// next.config.js
module.exports = {
  // Optimizaciones específicas del dashboard
  experimental: {
    optimizeCss: true,
    optimizeServerReact: true,
  },
  
  // Compresión de imágenes
  images: {
    formats: ['image/webp', 'image/avif'],
    minimumCacheTTL: 60,
  },
  
  // Bundle analyzer
  webpack: (config, { isServer }) => {
    if (!isServer) {
      config.resolve.fallback.fs = false;
    }
    return config;
  },
};
```

### Variables de Entorno

```bash
# .env.local
NEXT_PUBLIC_API_URL=https://api.hidroespinaca.com
NEXT_PUBLIC_WS_URL=wss://ws.hidroespinaca.com
NEXT_PUBLIC_REFRESH_INTERVAL=30000
NEXT_PUBLIC_ENABLE_MOCK_DATA=false
```

## Contribución

### Agregar Nueva Página

1. **Crear directorio** en `/dashboard/nueva-pagina/`
2. **Implementar page.tsx** siguiendo patrones establecidos
3. **Agregar componentes** específicos si es necesario
4. **Actualizar navegación** en layout y menús
5. **Escribir tests** unitarios e integración
6. **Documentar** funcionalidad y uso

### Code Review Checklist

- [ ] Tipado TypeScript completo
- [ ] Responsive design implementado
- [ ] Accesibilidad verificada
- [ ] Performance optimizada
- [ ] Tests escritos y pasando
- [ ] Documentación actualizada
- [ ] Patrones de diseño seguidos
- [ ] Manejo de errores robusto

## Recursos

- [Next.js App Router](https://nextjs.org/docs/app)
- [React Hooks](https://react.dev/reference/react)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [Zustand State Management](https://github.com/pmndrs/zustand)
- [Testing Library](https://testing-library.com/docs/react-testing-library/intro/)