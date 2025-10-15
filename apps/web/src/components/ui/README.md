# UI Components - Componentes de Interfaz de Usuario

Este directorio contiene todos los componentes de UI reutilizables de la aplicación HydroEspinaca, implementando un sistema de diseño consistente y accesible.

## Arquitectura de Componentes UI

### Principios de Diseño

- **Reutilización**: Componentes altamente reutilizables y configurables
- **Consistencia**: Diseño visual y comportamental uniforme
- **Accesibilidad**: Cumplimiento con estándares WCAG 2.1
- **Responsividad**: Adaptación automática a todos los dispositivos
- **Composición**: Componentes que se combinan fácilmente
- **Tipado fuerte**: TypeScript para mayor seguridad

### Categorías de Componentes

```
ui/
├── forms/                  # Componentes de formularios
│   ├── Input.tsx
│   ├── Select.tsx
│   ├── Checkbox.tsx
│   ├── RadioGroup.tsx
│   ├── DatePicker.tsx
│   ├── TimePicker.tsx
│   └── FormField.tsx
├── data-display/           # Visualización de datos
│   ├── Table.tsx
│   ├── Card.tsx
│   ├── Badge.tsx
│   ├── Avatar.tsx
│   ├── Metric.tsx
│   └── StatusIndicator.tsx
├── feedback/               # Componentes de retroalimentación
│   ├── Alert.tsx
│   ├── Toast.tsx
│   ├── Modal.tsx
│   ├── Spinner.tsx
│   ├── Skeleton.tsx
│   └── ProgressBar.tsx
├── navigation/             # Navegación
│   ├── Button.tsx
│   ├── Link.tsx
│   ├── Breadcrumb.tsx
│   ├── Pagination.tsx
│   └── Tabs.tsx
├── layout/                 # Componentes de layout
│   ├── Container.tsx
│   ├── Grid.tsx
│   ├── Stack.tsx
│   ├── Divider.tsx
│   └── Section.tsx
└── README.md              # Esta documentación
```

## Componentes de Formularios

### Input - Campo de Entrada

**Propósito**: Campo de entrada de texto versátil y accesible

```typescript
interface InputProps {
  label?: string;              // Etiqueta del campo
  placeholder?: string;        // Texto de placeholder
  value?: string;             // Valor controlado
  defaultValue?: string;      // Valor por defecto
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  size?: 'sm' | 'md' | 'lg';  // Tamaño del campo
  variant?: 'default' | 'filled' | 'outlined';
  error?: string;             // Mensaje de error
  helperText?: string;        // Texto de ayuda
  required?: boolean;         // Campo requerido
  disabled?: boolean;         // Campo deshabilitado
  readOnly?: boolean;         // Solo lectura
  startIcon?: React.ReactNode; // Icono al inicio
  endIcon?: React.ReactNode;  // Icono al final
  onChange?: (value: string) => void;
  onBlur?: () => void;
  onFocus?: () => void;
}

// Ejemplo de uso
<Input
  label="Temperatura Mínima"
  type="number"
  placeholder="Ingrese temperatura"
  value={minTemp}
  onChange={setMinTemp}
  error={errors.minTemp}
  helperText="Temperatura mínima en grados Celsius"
  startIcon={<ThermometerIcon />}
  required
/>
```

**Características**:
- Validación en tiempo real
- Estados visuales (normal, error, disabled, focus)
- Soporte para iconos
- Accesibilidad completa (ARIA labels, keyboard navigation)
- Responsive design

### Select - Selector Desplegable

**Propósito**: Selector de opciones con búsqueda y múltiple selección

```typescript
interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
  icon?: React.ReactNode;
}

interface SelectProps {
  label?: string;
  placeholder?: string;
  options: SelectOption[];
  value?: string | number | (string | number)[];
  multiple?: boolean;         // Selección múltiple
  searchable?: boolean;       // Búsqueda de opciones
  clearable?: boolean;        // Opción de limpiar
  size?: 'sm' | 'md' | 'lg';
  error?: string;
  helperText?: string;
  required?: boolean;
  disabled?: boolean;
  loading?: boolean;          // Estado de carga
  onCreate?: (value: string) => void; // Crear nueva opción
  onChange?: (value: string | number | (string | number)[]) => void;
}

// Ejemplo de uso
<Select
  label="Tipo de Sensor"
  placeholder="Seleccione un tipo"
  options={[
    { value: 'temperature', label: 'Temperatura', icon: <ThermometerIcon /> },
    { value: 'humidity', label: 'Humedad', icon: <DropletIcon /> },
    { value: 'ph', label: 'pH', icon: <BeakerIcon /> }
  ]}
  value={sensorType}
  onChange={setSensorType}
  searchable
  required
/>
```

### FormField - Contenedor de Campo

**Propósito**: Wrapper que proporciona layout y validación consistente

```typescript
interface FormFieldProps {
  label?: string;
  error?: string;
  helperText?: string;
  required?: boolean;
  children: React.ReactNode;
  htmlFor?: string;           // ID del campo asociado
  layout?: 'vertical' | 'horizontal';
}

// Ejemplo de uso
<FormField
  label="Configuración de Rango"
  error={errors.range}
  helperText="Defina los valores mínimo y máximo"
  required
>
  <div className="flex gap-4">
    <Input
      placeholder="Mínimo"
      value={minValue}
      onChange={setMinValue}
    />
    <Input
      placeholder="Máximo"
      value={maxValue}
      onChange={setMaxValue}
    />
  </div>
</FormField>
```

## Componentes de Visualización de Datos

### Table - Tabla de Datos

**Propósito**: Tabla responsive con funcionalidades avanzadas

```typescript
interface TableColumn<T> {
  key: keyof T;
  title: string;
  width?: string | number;
  align?: 'left' | 'center' | 'right';
  sortable?: boolean;
  filterable?: boolean;
  render?: (value: any, record: T, index: number) => React.ReactNode;
}

interface TableProps<T> {
  columns: TableColumn<T>[];
  data: T[];
  loading?: boolean;
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    onChange: (page: number, pageSize: number) => void;
  };
  selection?: {
    selectedRowKeys: string[];
    onChange: (selectedRowKeys: string[], selectedRows: T[]) => void;
  };
  sorting?: {
    field: keyof T;
    direction: 'asc' | 'desc';
    onChange: (field: keyof T, direction: 'asc' | 'desc') => void;
  };
  responsive?: boolean;       // Modo responsive con cards
  mobileCardRender?: (record: T) => React.ReactNode;
  emptyText?: string;
  onRowClick?: (record: T) => void;
}

// Ejemplo de uso
const columns: TableColumn<SensorReading>[] = [
  {
    key: 'sensor',
    title: 'Sensor',
    sortable: true,
    filterable: true
  },
  {
    key: 'valor',
    title: 'Valor',
    align: 'right',
    render: (value, record) => `${value} ${record.unidad}`
  },
  {
    key: 'fecha',
    title: 'Fecha',
    sortable: true,
    render: (value) => formatDate(value)
  }
];

<Table
  columns={columns}
  data={readings}
  loading={isLoading}
  pagination={{
    current: currentPage,
    pageSize: 10,
    total: totalReadings,
    onChange: handlePageChange
  }}
  responsive
  mobileCardRender={(record) => (
    <ReadingCard reading={record} />
  )}
/>
```

**Características**:
- Ordenamiento por columnas
- Filtrado por columnas
- Paginación integrada
- Selección de filas
- Modo responsive automático
- Carga diferida
- Personalización completa de renderizado

### Card - Tarjeta de Contenido

**Propósito**: Contenedor versátil para agrupar información relacionada

```typescript
interface CardProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  cover?: React.ReactNode;    // Imagen o contenido de portada
  size?: 'sm' | 'md' | 'lg';
  variant?: 'default' | 'outlined' | 'elevated';
  hoverable?: boolean;        // Efecto hover
  loading?: boolean;
  className?: string;
  onClick?: () => void;
}

// Ejemplo de uso
<Card
  title="Sensor de Temperatura"
  subtitle="ESP32-001 • Activo"
  variant="elevated"
  hoverable
  actions={
    <div className="flex gap-2">
      <Button size="sm" variant="outline">Editar</Button>
      <Button size="sm" variant="primary">Ver Detalles</Button>
    </div>
  }
>
  <div className="space-y-4">
    <Metric
      label="Temperatura Actual"
      value="24.5°C"
      trend="up"
      change="+1.2°C"
    />
    <StatusIndicator
      status="online"
      label="Conectado"
    />
  </div>
</Card>
```

### Metric - Métrica Visual

**Propósito**: Visualización de métricas con tendencias y cambios

```typescript
interface MetricProps {
  label: string;
  value: string | number;
  unit?: string;
  trend?: 'up' | 'down' | 'stable';
  change?: string;
  changeType?: 'positive' | 'negative' | 'neutral';
  icon?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
  layout?: 'horizontal' | 'vertical';
  precision?: number;         // Decimales para números
}

// Ejemplo de uso
<Metric
  label="pH del Agua"
  value={6.8}
  unit="pH"
  trend="down"
  change="-0.2"
  changeType="negative"
  icon={<BeakerIcon />}
  precision={1}
/>
```

## Componentes de Retroalimentación

### Alert - Alerta de Sistema

**Propósito**: Mostrar mensajes importantes al usuario

```typescript
interface AlertProps {
  type: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  message: string;
  closable?: boolean;
  actions?: React.ReactNode;
  icon?: React.ReactNode;
  onClose?: () => void;
}

// Ejemplo de uso
<Alert
  type="warning"
  title="Valor fuera de rango"
  message="La temperatura está por encima del rango óptimo (25°C)"
  closable
  actions={
    <Button size="sm" variant="outline">
      Ajustar Configuración
    </Button>
  }
  onClose={handleCloseAlert}
/>
```

### Modal - Ventana Modal

**Propósito**: Diálogo modal para acciones importantes

```typescript
interface ModalProps {
  open: boolean;
  title?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  closable?: boolean;
  maskClosable?: boolean;     // Cerrar al hacer clic fuera
  onClose?: () => void;
  onOk?: () => void;
  onCancel?: () => void;
}

// Ejemplo de uso
<Modal
  open={isModalOpen}
  title="Confirmar Eliminación"
  size="md"
  onClose={handleCloseModal}
  footer={
    <div className="flex gap-3 justify-end">
      <Button variant="outline" onClick={handleCancel}>
        Cancelar
      </Button>
      <Button variant="danger" onClick={handleConfirm}>
        Eliminar
      </Button>
    </div>
  }
>
  <p>¿Está seguro de que desea eliminar este sensor?</p>
  <p className="text-sm text-gray-600 mt-2">
    Esta acción no se puede deshacer.
  </p>
</Modal>
```

### Toast - Notificación Temporal

**Propósito**: Notificaciones no intrusivas que se auto-ocultan

```typescript
interface ToastProps {
  type: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  message: string;
  duration?: number;          // Duración en ms (0 = no auto-hide)
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
  closable?: boolean;
  onClose?: () => void;
}

// Sistema de Toast global
interface ToastManager {
  show: (toast: Omit<ToastProps, 'onClose'>) => string;
  hide: (id: string) => void;
  hideAll: () => void;
}

// Ejemplo de uso
const toast = useToast();

const handleSave = async () => {
  try {
    await saveSensor(sensorData);
    toast.show({
      type: 'success',
      title: 'Sensor guardado',
      message: 'El sensor se ha configurado correctamente',
      duration: 3000
    });
  } catch (error) {
    toast.show({
      type: 'error',
      title: 'Error al guardar',
      message: 'No se pudo guardar la configuración del sensor',
      duration: 5000
    });
  }
};
```

## Componentes de Navegación

### Button - Botón de Acción

**Propósito**: Botón versátil para todas las acciones de la aplicación

```typescript
interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  disabled?: boolean;
  loading?: boolean;
  icon?: React.ReactNode;
  iconPosition?: 'left' | 'right';
  fullWidth?: boolean;
  type?: 'button' | 'submit' | 'reset';
  onClick?: () => void;
}

// Ejemplo de uso
<Button
  variant="primary"
  size="md"
  icon={<SaveIcon />}
  loading={isSaving}
  onClick={handleSave}
>
  Guardar Configuración
</Button>

<Button
  variant="danger"
  size="sm"
  icon={<TrashIcon />}
  iconPosition="left"
  onClick={handleDelete}
>
  Eliminar
</Button>
```

**Variantes disponibles**:
- `primary`: Acción principal (azul)
- `secondary`: Acción secundaria (gris)
- `outline`: Botón con borde
- `ghost`: Botón transparente
- `danger`: Acciones destructivas (rojo)

### Pagination - Paginación

**Propósito**: Navegación entre páginas de datos

```typescript
interface PaginationProps {
  current: number;            // Página actual
  total: number;              // Total de elementos
  pageSize: number;           // Elementos por página
  showSizeChanger?: boolean;  // Selector de tamaño de página
  showQuickJumper?: boolean;  // Salto rápido a página
  showTotal?: boolean;        // Mostrar total de elementos
  onChange: (page: number, pageSize: number) => void;
}

// Ejemplo de uso
<Pagination
  current={currentPage}
  total={totalReadings}
  pageSize={pageSize}
  showSizeChanger
  showQuickJumper
  showTotal
  onChange={handlePageChange}
/>
```

## Componentes de Layout

### Container - Contenedor Principal

**Propósito**: Contenedor responsive con máximo ancho

```typescript
interface ContainerProps {
  children: React.ReactNode;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | 'full';
  padding?: 'none' | 'sm' | 'md' | 'lg';
  centered?: boolean;
  className?: string;
}

// Ejemplo de uso
<Container maxWidth="xl" padding="lg" centered>
  <h1>Dashboard Principal</h1>
  <Grid cols={3} gap="md">
    <MetricCard />
    <MetricCard />
    <MetricCard />
  </Grid>
</Container>
```

### Grid - Sistema de Grillas

**Propósito**: Layout de grilla responsive y flexible

```typescript
interface GridProps {
  children: React.ReactNode;
  cols?: number | {
    xs?: number;
    sm?: number;
    md?: number;
    lg?: number;
    xl?: number;
  };
  gap?: 'none' | 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  align?: 'start' | 'center' | 'end' | 'stretch';
  justify?: 'start' | 'center' | 'end' | 'between' | 'around';
}

// Ejemplo de uso
<Grid
  cols={{ xs: 1, sm: 2, lg: 3, xl: 4 }}
  gap="md"
  align="stretch"
>
  <SensorCard />
  <SensorCard />
  <SensorCard />
  <SensorCard />
</Grid>
```

### Section - Sección de Contenido

**Propósito**: Sección semántica con título y espaciado consistente

```typescript
interface SectionProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  spacing?: 'none' | 'sm' | 'md' | 'lg';
  divider?: boolean;          // Línea divisoria
}

// Ejemplo de uso
<Section
  title="Configuración de Sensores"
  subtitle="Gestione los sensores del sistema hidropónico"
  spacing="lg"
  divider
  actions={
    <Button variant="primary" icon={<PlusIcon />}>
      Agregar Sensor
    </Button>
  }
>
  <SensorsTable sensors={sensors} />
</Section>
```

## Sistema de Temas y Estilos

### Tokens de Diseño

```typescript
// Colores del sistema
const colors = {
  primary: {
    50: '#eff6ff',
    500: '#3b82f6',
    900: '#1e3a8a'
  },
  success: {
    50: '#f0fdf4',
    500: '#22c55e',
    900: '#14532d'
  },
  warning: {
    50: '#fffbeb',
    500: '#f59e0b',
    900: '#78350f'
  },
  error: {
    50: '#fef2f2',
    500: '#ef4444',
    900: '#7f1d1d'
  }
};

// Espaciado
const spacing = {
  xs: '0.5rem',   // 8px
  sm: '0.75rem',  // 12px
  md: '1rem',     // 16px
  lg: '1.5rem',   // 24px
  xl: '2rem'      // 32px
};

// Tipografía
const typography = {
  xs: '0.75rem',    // 12px
  sm: '0.875rem',   // 14px
  base: '1rem',     // 16px
  lg: '1.125rem',   // 18px
  xl: '1.25rem',    // 20px
  '2xl': '1.5rem'   // 24px
};
```

### Clases Utilitarias Personalizadas

```css
/* Utilidades para componentes */
.component-focus {
  @apply focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2;
}

.component-transition {
  @apply transition-all duration-200 ease-in-out;
}

.component-shadow {
  @apply shadow-sm hover:shadow-md;
}

/* Estados de componentes */
.component-disabled {
  @apply opacity-50 cursor-not-allowed;
}

.component-loading {
  @apply opacity-75 pointer-events-none;
}
```

## Accesibilidad

### Estándares Implementados

- **WCAG 2.1 AA**: Cumplimiento completo
- **Navegación por teclado**: Todos los componentes son navegables
- **Screen readers**: Soporte completo con ARIA labels
- **Contraste**: Ratios de contraste mínimo 4.5:1
- **Focus management**: Indicadores visuales claros

### Ejemplos de Implementación

```typescript
// Input accesible
<Input
  id="sensor-name"
  label="Nombre del Sensor"
  aria-describedby="sensor-name-help"
  aria-required="true"
  aria-invalid={!!error}
/>
<div id="sensor-name-help" className="sr-only">
  Ingrese un nombre descriptivo para el sensor
</div>

// Button accesible
<Button
  aria-label="Eliminar sensor de temperatura"
  onClick={handleDelete}
>
  <TrashIcon aria-hidden="true" />
</Button>

// Modal accesible
<Modal
  open={isOpen}
  onClose={handleClose}
  aria-labelledby="modal-title"
  aria-describedby="modal-description"
>
  <h2 id="modal-title">Confirmar Acción</h2>
  <p id="modal-description">Esta acción no se puede deshacer</p>
</Modal>
```

## Testing de Componentes

### Tests Unitarios

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { Button } from './Button';

describe('Button Component', () => {
  test('renders with correct text', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument();
  });
  
  test('calls onClick when clicked', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Click me</Button>);
    
    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });
  
  test('shows loading state', () => {
    render(<Button loading>Loading</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('aria-disabled', 'true');
  });
  
  test('is accessible', async () => {
    const { container } = render(<Button>Accessible Button</Button>);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

### Tests de Integración

```typescript
// Test de formulario completo
test('form submission with validation', async () => {
  const handleSubmit = jest.fn();
  
  render(
    <form onSubmit={handleSubmit}>
      <Input
        label="Nombre"
        name="name"
        required
      />
      <Select
        label="Tipo"
        name="type"
        options={typeOptions}
        required
      />
      <Button type="submit">Guardar</Button>
    </form>
  );
  
  // Intentar enviar sin llenar campos
  fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));
  
  // Verificar mensajes de error
  await waitFor(() => {
    expect(screen.getByText('Este campo es requerido')).toBeInTheDocument();
  });
  
  // Llenar campos y enviar
  fireEvent.change(screen.getByLabelText('Nombre'), {
    target: { value: 'Sensor de Prueba' }
  });
  
  fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));
  
  await waitFor(() => {
    expect(handleSubmit).toHaveBeenCalled();
  });
});
```

## Performance

### Optimizaciones Implementadas

```typescript
// Memoización de componentes pesados
const ExpensiveComponent = React.memo(({ data }) => {
  const processedData = useMemo(() => {
    return data.map(item => expensiveCalculation(item));
  }, [data]);
  
  return <div>{/* Renderizado */}</div>;
});

// Lazy loading de componentes
const HeavyModal = lazy(() => import('./HeavyModal'));

const ComponentWithModal = () => {
  const [showModal, setShowModal] = useState(false);
  
  return (
    <div>
      <Button onClick={() => setShowModal(true)}>Abrir Modal</Button>
      {showModal && (
        <Suspense fallback={<Spinner />}>
          <HeavyModal onClose={() => setShowModal(false)} />
        </Suspense>
      )}
    </div>
  );
};

// Virtualización para listas grandes
import { FixedSizeList as List } from 'react-window';

const VirtualizedTable = ({ items }) => (
  <List
    height={400}
    itemCount={items.length}
    itemSize={50}
    itemData={items}
  >
    {({ index, style, data }) => (
      <div style={style}>
        <TableRow item={data[index]} />
      </div>
    )}
  </List>
);
```

### Bundle Size Optimization

```typescript
// Tree shaking - importar solo lo necesario
import { Button } from '@/components/ui/Button';
// En lugar de:
// import { Button } from '@/components/ui';

// Code splitting por ruta
const DashboardPage = lazy(() => import('./pages/Dashboard'));
const SettingsPage = lazy(() => import('./pages/Settings'));

// Dynamic imports para funcionalidades opcionales
const loadChartLibrary = async () => {
  const { Chart } = await import('chart.js');
  return Chart;
};
```

## Contribución

### Agregar Nuevo Componente

1. **Crear archivo** en la categoría apropiada
2. **Implementar interfaz** TypeScript completa
3. **Seguir patrones** de diseño establecidos
4. **Implementar accesibilidad** completa
5. **Escribir tests** unitarios
6. **Documentar** props y uso
7. **Agregar a Storybook** si aplica

### Template de Componente

```typescript
import React, { forwardRef } from 'react';
import { cn } from '@/lib/utils';

export interface ComponentNameProps {
  // Props interface
  children?: React.ReactNode;
  className?: string;
}

export const ComponentName = forwardRef<
  HTMLDivElement,
  ComponentNameProps
>(({ children, className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        // Base classes
        'component-base-classes',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
});

ComponentName.displayName = 'ComponentName';
```

### Code Review Checklist

- [ ] TypeScript interfaces completas
- [ ] Accesibilidad implementada (ARIA, keyboard)
- [ ] Responsive design
- [ ] Tests unitarios escritos
- [ ] Documentación actualizada
- [ ] Performance optimizada
- [ ] Consistencia con design system
- [ ] Manejo de errores robusto

## Recursos

- [Tailwind CSS](https://tailwindcss.com/docs)
- [Headless UI](https://headlessui.com/)
- [React Hook Form](https://react-hook-form.com/)
- [Testing Library](https://testing-library.com/)
- [Storybook](https://storybook.js.org/)
- [WCAG Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)