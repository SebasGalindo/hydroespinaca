# Components - Arquitectura de Componentes

Este directorio contiene todos los componentes reutilizables de la aplicación HydroEspinaca, organizados siguiendo los principios de **Atomic Design** y las mejores prácticas de React.

## Arquitectura de Componentes

### Principios de Diseño

- **Atomic Design**: Organización jerárquica desde átomos hasta organismos
- **Composición sobre herencia**: Componentes pequeños y componibles
- **Single Responsibility**: Cada componente tiene una responsabilidad específica
- **Props drilling mínimo**: Uso de context y stores para estado global
- **Tipado estricto**: Todas las props están completamente tipadas

### Estructura de Directorios

```
components/
├── auth/                    # Componentes de autenticación
├── dashboard/               # Componentes específicos del dashboard
│   ├── actuadores-config/   # Configuración de actuadores
│   ├── lecturas/           # Lecturas de sensores
│   ├── monitoreo/          # Monitoreo
│   ├── nueva-variable/     # Creación de variables
│   ├── sensores-config/    # Configuración de sensores
│   └── variables-config/   # Configuración de variables
├── layout/                 # Componentes de layout y navegación
├── store/                  # Componentes relacionados con estado
└── ui/                     # Componentes de interfaz reutilizables
    ├── icons/              # Iconos del sistema
    └── [componentes base]  # Botones, inputs, modales, etc.
```

## Categorías de Componentes

### 🧩 Componentes Atómicos (UI)

Componentes básicos y reutilizables que forman la base del sistema de diseño.

#### `Button.tsx`
**Propósito**: Botón base del sistema
**Props**:
- `variant`: 'primary' | 'secondary' | 'danger' | 'ghost'
- `size`: 'sm' | 'md' | 'lg'
- `disabled`: boolean
- `loading`: boolean
- `icon`: ReactNode

#### `Input.tsx`
**Propósito**: Campo de entrada base
**Props**:
- `type`: 'text' | 'email' | 'password' | 'number'
- `placeholder`: string
- `error`: string
- `disabled`: boolean
- `required`: boolean

#### `Badge.tsx`
**Propósito**: Etiquetas de estado y categorización
**Props**:
- `variant`: 'success' | 'warning' | 'error' | 'info'
- `size`: 'sm' | 'md' | 'lg'
- `children`: ReactNode

#### `Table.tsx`
**Propósito**: Tabla responsive con funcionalidades avanzadas
**Props**:
- `columns`: TableColumn[]
- `data`: unknown[]
- `responsive`: boolean
- `mobileCardRender`: función para vista móvil
- `emptyMessage`: string

### 🔧 Componentes Moleculares

Combinaciones de componentes atómicos que forman unidades funcionales.

#### `FilterControls.tsx`
**Propósito**: Controles de filtrado para tablas y listas
**Características**:
- Filtros por fecha, tiempo, sensor
- Búsqueda por texto
- Estado de filtros persistente
- Responsive design

#### `MetricCard.tsx`
**Propósito**: Tarjeta para mostrar métricas del sistema
**Características**:
- Valor principal con unidad
- Indicador de tendencia
- Estado visual (óptimo/advertencia/crítico)
- Iconos dinámicos

#### `SensorCard.tsx`
**Propósito**: Tarjeta individual de sensor
**Características**:
- Estado del sensor (activo/inactivo)
- Información de calibración
- Botones de acción
- Visualización de rangos

### 🏗️ Componentes Organismos

Componentes complejos que combinan múltiples moléculas y átomos.

#### `SummaryTable.tsx`
**Propósito**: Tabla de resumen de sensores
**Características**:
- Datos estadísticos (media, mínimo, máximo)
- Vista responsive con cards móviles
- Integración con store de lecturas
- Formato automático de unidades

#### `ReadingsTable.tsx`
**Propósito**: Tabla de lecturas individuales
**Características**:
- Listado cronológico de lecturas
- Filtrado y búsqueda
- Paginación automática
- Export de datos

#### `SystemStatusCard.tsx`
**Propósito**: Estado general del sistema
**Características**:
- Resumen de componentes
- Indicadores de conectividad
- Alertas críticas
- Acciones rápidas

### 📱 Componentes de Layout

#### `MainLayout.tsx`
**Propósito**: Layout principal de la aplicación
**Características**:
- Header responsive
- Navegación lateral
- Footer informativo
- Gestión de autenticación

#### `PageLayout.tsx`
**Propósito**: Layout para páginas internas
**Props**:
- `title`: string
- `subtitle`: string
- `maxWidth`: 'sm' | 'md' | 'lg' | 'xl' | 'full'
- `children`: ReactNode

#### `DashboardHeader.tsx`
**Propósito**: Header específico del dashboard
**Características**:
- Breadcrumbs dinámicos
- Acciones contextuales
- Notificaciones
- Perfil de usuario

### 🔐 Componentes de Autenticación

#### `LoginForm.tsx`
**Propósito**: Formulario de inicio de sesión
**Características**:
- Validación
- Estados de carga
- Manejo de errores
- Recordar sesión

#### `ProtectedRoute.tsx`
**Propósito**: Protección de rutas autenticadas
**Características**:
- Verificación de autenticación
- Redirección automática
- Estados de carga
- Manejo de permisos

## Patrones de Desarrollo

### Convenciones de Nomenclatura

- **Componentes**: PascalCase (`UserProfile`, `SensorCard`)
- **Props**: camelCase (`isLoading`, `onSubmit`)
- **Archivos**: PascalCase con extensión `.tsx`
- **Directorios**: kebab-case (`nueva-variable`, `sensores-config`)

### Estructura de Componente Estándar

```typescript
'use client'; // Si usa hooks del cliente

import React from 'react';
import { ComponentProps } from './types'; // Si es complejo

/**
 * Descripción del componente
 * @param props - Descripción de las props
 * @returns JSX.Element
 */
interface ComponentNameProps {
  // Props tipadas
  title: string;
  onAction?: () => void;
  children?: React.ReactNode;
}

const ComponentName: React.FC<ComponentNameProps> = ({
  title,
  onAction,
  children
}) => {
  // Hooks
  // Estado local
  // Efectos
  // Handlers
  // Render helpers
  
  return (
    <div className="component-container">
      {/* JSX */}
    </div>
  );
};

export default ComponentName;
```

### Manejo de Props

```typescript
// Props opcionales con valores por defecto
interface ButtonProps {
  variant?: 'primary' | 'secondary';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
}

const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  disabled = false,
  ...props
}) => {
  // Implementación
};
```

### Composición de Componentes

```typescript
// Componente padre que compone otros
const DashboardPage = () => {
  return (
    <PageLayout title="Dashboard" subtitle="Monitoreo del sistema">
      <Section title="Métricas">
        <MetricsGrid />
      </Section>
      
      <Section title="Estado del Sistema">
        <SystemStatusCard />
      </Section>
    </PageLayout>
  );
};
```

## Estilos y Theming

### Sistema de Diseño

La aplicación utiliza **Tailwind CSS** con un sistema de diseño personalizado:

```css
/* Variables CSS personalizadas */
@theme {
  --color-hidro-green-primary: #16a34a;
  --color-hidro-green-light: #dcfce7;
  --color-hidro-green-dark: #15803d;
  /* ... más variables */
}
```

### Clases Utilitarias Personalizadas

```css
.hidro-card {
  @apply bg-white rounded-lg shadow-sm border border-gray-200 p-6;
}

.hidro-button {
  @apply px-4 py-2 rounded-md font-medium transition-all duration-200;
}

.hidro-gradient {
  background: linear-gradient(135deg, theme(--color-hidro-green-bg) 0%, theme(--color-hidro-green-bg-light) 100%);
}
```

### Responsive Design

```typescript
// Breakpoints estándar
const breakpoints = {
  sm: '640px',   // Mobile
  md: '768px',   // Tablet
  lg: '1024px',  // Desktop
  xl: '1280px'   // Large Desktop
};

// Uso en componentes
const ResponsiveComponent = () => (
  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
    {/* Contenido responsive */}
  </div>
);
```

## Accesibilidad

### Principios WCAG

- **Perceptible**: Contraste adecuado, texto alternativo
- **Operable**: Navegación por teclado, tiempo suficiente
- **Comprensible**: Texto claro, comportamiento predecible
- **Robusto**: Compatible con tecnologías asistivas

### Implementación

```typescript
// Ejemplo de componente accesible
const AccessibleButton = ({ children, onClick, ...props }) => (
  <button
    onClick={onClick}
    aria-label={props['aria-label']}
    role="button"
    tabIndex={0}
    className="focus:outline-none focus:ring-2 focus:ring-green-500"
    {...props}
  >
    {children}
  </button>
);
```

## Testing

### Estrategia de Testing

- **Unit Tests**: Componentes individuales
- **Integration Tests**: Flujos de usuario
- **Visual Tests**: Consistencia de UI
- **Accessibility Tests**: Cumplimiento WCAG

### Ejemplo de Test

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import Button from './Button';

describe('Button Component', () => {
  test('renders with correct text', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });
  
  test('calls onClick when clicked', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Click me</Button>);
    
    fireEvent.click(screen.getByText('Click me'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });
});
```

## Performance

### Optimizaciones

- **React.memo**: Para componentes puros
- **useMemo**: Para cálculos costosos
- **useCallback**: Para funciones estables
- **Lazy loading**: Para componentes grandes

```typescript
// Ejemplo de optimización
const ExpensiveComponent = React.memo(({ data }) => {
  const processedData = useMemo(() => {
    return data.map(item => expensiveCalculation(item));
  }, [data]);
  
  return <div>{/* Render */}</div>;
});
```

## Contribución

### Agregar Nuevo Componente

1. **Crear archivo** en la carpeta apropiada
2. **Definir interfaces** TypeScript
3. **Implementar componente** siguiendo patrones
4. **Agregar estilos** con Tailwind
5. **Escribir tests** unitarios
6. **Documentar props** y uso
7. **Exportar** desde index apropiado

### Code Review Checklist

- [ ] Tipado TypeScript completo
- [ ] Props documentadas
- [ ] Accesibilidad implementada
- [ ] Responsive design
- [ ] Tests unitarios
- [ ] Performance optimizada
- [ ] Consistencia con sistema de diseño

## Recursos

- [React Documentation](https://react.dev/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Atomic Design](https://bradfrost.com/blog/post/atomic-web-design/)
- [WCAG Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Testing Library](https://testing-library.com/)