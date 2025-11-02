# Custom Hooks - Hooks Personalizados

Este directorio contiene todos los hooks personalizados de React para la aplicación HydroEspinaca, proporcionando lógica reutilizable y gestión de estado especializada.

## Arquitectura de Hooks

### Principios de Diseño

- **Reutilización**: Lógica compartida entre componentes
- **Separación de responsabilidades**: Cada hook tiene un propósito específico
- **Composición**: Hooks que se pueden combinar fácilmente
- **Performance**: Optimización automática con memoización
- **Tipado fuerte**: TypeScript para mayor seguridad
- **Testing**: Hooks completamente testeable

### Categorías de Hooks

```
hooks/
├── data/                   # Hooks de gestión de datos
│   ├── useApi.ts
│   ├── useSensorData.ts
│   ├── useReadings.ts
│   ├── useActuators.ts
│   └── useVariables.ts
├── ui/                     # Hooks de interfaz de usuario
│   ├── useModal.ts
│   ├── useToast.ts
│   ├── useForm.ts
│   ├── useTable.ts
│   └── usePagination.ts
├── utils/                  # Hooks utilitarios
│   ├── useLocalStorage.ts
│   ├── useDebounce.ts
│   ├── useInterval.ts
│   ├── useMediaQuery.ts
│   └── useClickOutside.ts
├── auth/                   # Hooks de autenticación
│   ├── useAuth.ts
│   ├── usePermissions.ts
│   └── useSession.ts
└── README.md              # Esta documentación
```

## Hooks de Gestión de Datos

### useApi - Cliente API Universal

**Propósito**: Hook genérico para realizar peticiones HTTP con manejo de estado

```typescript
interface UseApiOptions<T> {
  immediate?: boolean;        // Ejecutar inmediatamente
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  retry?: number;            // Número de reintentos
  retryDelay?: number;       // Delay entre reintentos
  cache?: boolean;           // Cachear respuesta
  cacheKey?: string;         // Clave de cache personalizada
}

interface UseApiReturn<T> {
  data: T | null;
  loading: boolean;
  error: Error | null;
  execute: (...args: any[]) => Promise<T>;
  reset: () => void;
  retry: () => Promise<T>;
}

function useApi<T>(
  apiFunction: (...args: any[]) => Promise<T>,
  options?: UseApiOptions<T>
): UseApiReturn<T>

// Ejemplo de uso
const {
  data: sensors,
  loading: sensorsLoading,
  error: sensorsError,
  execute: fetchSensors,
  retry: retrySensors
} = useApi(api.getSensors, {
  immediate: true,
  cache: true,
  cacheKey: 'sensors-list',
  onError: (error) => {
    toast.show({
      type: 'error',
      message: 'Error al cargar sensores'
    });
  }
});

// Refrescar datos
const handleRefresh = () => {
  fetchSensors();
};

// Reintentar en caso de error
if (sensorsError) {
  return (
    <ErrorState
      message="Error al cargar sensores"
      onRetry={retrySensors}
    />
  );
}
```

**Características**:
- Manejo automático de estados (loading, error, data)
- Sistema de reintentos configurable
- Cache automático con invalidación
- Callbacks para éxito y error
- Cancelación automática de peticiones

### useSensorData - Datos de Sensores

**Propósito**: Hook especializado para gestionar datos de sensores con actualizaciones automáticas

```typescript
interface UseSensorDataOptions {
  autoRefresh?: boolean;      // Actualización automática
  refreshInterval?: number;   // Intervalo en ms
  sensorIds?: string[];      // Filtrar por IDs específicos
  includeHistory?: boolean;   // Incluir datos históricos
}

interface UseSensorDataReturn {
  sensors: SensorData[];
  currentReadings: Record<string, number>;
  lastUpdate: Date | null;
  isOnline: boolean;
  loading: boolean;
  error: Error | null;
  refreshSensors: () => Promise<void>;
  getSensorById: (id: string) => SensorData | undefined;
  getSensorReading: (sensorId: string) => number | null;
}

function useSensorData(options?: UseSensorDataOptions): UseSensorDataReturn

// Ejemplo de uso
const {
  sensors,
  currentReadings,
  lastUpdate,
  isOnline,
  loading,
  refreshSensors,
  getSensorReading
} = useSensorData({
  autoRefresh: true,
  refreshInterval: 30000, // 30 segundos
  sensorIds: ['temp-001', 'humidity-001'],
  includeHistory: false
});

// Obtener lectura específica
const currentTemp = getSensorReading('temp-001');

// Componente de estado
const StatusIndicator = () => (
  <div className={`status ${isOnline ? 'online' : 'offline'}`}>
    {isOnline ? 'Conectado' : 'Desconectado'}
    {lastUpdate && (
      <span className="last-update">
        Última actualización: {formatTime(lastUpdate)}
      </span>
    )}
  </div>
);
```

### useReadings - Gestión de Lecturas Históricas

**Propósito**: Hook para gestionar lecturas históricas con filtrado y paginación

```typescript
interface UseReadingsFilters {
  sensorId?: string;
  dateFrom?: Date;
  dateTo?: Date;
  minValue?: number;
  maxValue?: number;
}

interface UseReadingsOptions {
  pageSize?: number;
  autoLoad?: boolean;
  realTime?: boolean;
}

interface UseReadingsReturn {
  readings: IndividualReading[];
  summary: SensorSummary[];
  pagination: {
    current: number;
    total: number;
    pageSize: number;
    hasNext: boolean;
    hasPrev: boolean;
  };
  filters: UseReadingsFilters;
  loading: boolean;
  error: Error | null;
  setFilters: (filters: Partial<UseReadingsFilters>) => void;
  setPage: (page: number) => void;
  exportData: (format: 'csv' | 'excel') => Promise<void>;
  refreshReadings: () => Promise<void>;
}

function useReadings(options?: UseReadingsOptions): UseReadingsReturn

// Ejemplo de uso
const {
  readings,
  summary,
  pagination,
  filters,
  loading,
  setFilters,
  setPage,
  exportData
} = useReadings({
  pageSize: 50,
  autoLoad: true,
  realTime: true
});

// Filtrar por sensor
const handleSensorFilter = (sensorId: string) => {
  setFilters({ sensorId });
};

// Filtrar por fecha
const handleDateFilter = (dateFrom: Date, dateTo: Date) => {
  setFilters({ dateFrom, dateTo });
};

// Exportar datos
const handleExport = async () => {
  await exportData('excel');
  toast.show({
    type: 'success',
    message: 'Datos exportados correctamente'
  });
};
```

## Hooks de Interfaz de Usuario

### useModal - Gestión de Modales

**Propósito**: Hook para gestionar el estado y comportamiento de modales

```typescript
interface UseModalOptions {
  closeOnEscape?: boolean;    // Cerrar con ESC
  closeOnOverlay?: boolean;   // Cerrar al hacer clic fuera
  preventScroll?: boolean;    // Prevenir scroll del body
  focusOnOpen?: boolean;      // Enfocar al abrir
}

interface UseModalReturn {
  isOpen: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
  modalProps: {
    open: boolean;
    onClose: () => void;
  };
}

function useModal(options?: UseModalOptions): UseModalReturn

// Ejemplo de uso
const deleteModal = useModal({
  closeOnEscape: true,
  closeOnOverlay: false,
  preventScroll: true
});

const editModal = useModal();

const SensorManagement = () => {
  const [selectedSensor, setSelectedSensor] = useState<SensorData | null>(null);
  
  const handleEdit = (sensor: SensorData) => {
    setSelectedSensor(sensor);
    editModal.open();
  };
  
  const handleDelete = (sensor: SensorData) => {
    setSelectedSensor(sensor);
    deleteModal.open();
  };
  
  return (
    <div>
      <SensorsList
        onEdit={handleEdit}
        onDelete={handleDelete}
      />
      
      <Modal {...editModal.modalProps}>
        <EditSensorForm
          sensor={selectedSensor}
          onSave={() => editModal.close()}
          onCancel={() => editModal.close()}
        />
      </Modal>
      
      <Modal {...deleteModal.modalProps}>
        <ConfirmDelete
          sensor={selectedSensor}
          onConfirm={() => deleteModal.close()}
          onCancel={() => deleteModal.close()}
        />
      </Modal>
    </div>
  );
};
```

### useToast - Sistema de Notificaciones

**Propósito**: Hook para gestionar notificaciones toast globales

```typescript
interface ToastOptions {
  type: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  message: string;
  duration?: number;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
  closable?: boolean;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface UseToastReturn {
  show: (options: ToastOptions) => string;
  hide: (id: string) => void;
  hideAll: () => void;
  update: (id: string, options: Partial<ToastOptions>) => void;
}

function useToast(): UseToastReturn

// Ejemplo de uso
const toast = useToast();

const SensorForm = () => {
  const handleSave = async (data: SensorData) => {
    try {
      await saveSensor(data);
      
      toast.show({
        type: 'success',
        title: 'Sensor guardado',
        message: 'La configuración se ha guardado correctamente',
        duration: 3000
      });
    } catch (error) {
      const toastId = toast.show({
        type: 'error',
        title: 'Error al guardar',
        message: 'No se pudo guardar la configuración',
        duration: 0, // No auto-hide
        action: {
          label: 'Reintentar',
          onClick: () => {
            toast.hide(toastId);
            handleSave(data);
          }
        }
      });
    }
  };
};
```

### useForm - Gestión de Formularios

**Propósito**: Hook para gestionar estado y validación de formularios

```typescript
interface UseFormOptions<T> {
  initialValues: T;
  validationSchema?: ValidationSchema<T>;
  onSubmit: (values: T) => Promise<void> | void;
  validateOnChange?: boolean;
  validateOnBlur?: boolean;
}

interface UseFormReturn<T> {
  values: T;
  errors: Partial<Record<keyof T, string>>;
  touched: Partial<Record<keyof T, boolean>>;
  isSubmitting: boolean;
  isValid: boolean;
  isDirty: boolean;
  setValue: (field: keyof T, value: any) => void;
  setError: (field: keyof T, error: string) => void;
  setTouched: (field: keyof T, touched: boolean) => void;
  handleChange: (field: keyof T) => (value: any) => void;
  handleBlur: (field: keyof T) => () => void;
  handleSubmit: (e?: React.FormEvent) => Promise<void>;
  reset: () => void;
  validate: () => Promise<boolean>;
}

function useForm<T>(options: UseFormOptions<T>): UseFormReturn<T>

// Ejemplo de uso
interface SensorFormData {
  name: string;
  type: string;
  location: string;
  minValue: number;
  maxValue: number;
}

const validationSchema = {
  name: (value: string) => {
    if (!value) return 'El nombre es requerido';
    if (value.length < 3) return 'Mínimo 3 caracteres';
    return null;
  },
  type: (value: string) => {
    if (!value) return 'El tipo es requerido';
    return null;
  },
  minValue: (value: number, values: SensorFormData) => {
    if (value >= values.maxValue) {
      return 'El valor mínimo debe ser menor al máximo';
    }
    return null;
  }
};

const SensorForm = ({ sensor, onSave }: SensorFormProps) => {
  const form = useForm<SensorFormData>({
    initialValues: {
      name: sensor?.name || '',
      type: sensor?.type || '',
      location: sensor?.location || '',
      minValue: sensor?.minValue || 0,
      maxValue: sensor?.maxValue || 100
    },
    validationSchema,
    onSubmit: async (values) => {
      await onSave(values);
      toast.show({
        type: 'success',
        message: 'Sensor guardado correctamente'
      });
    },
    validateOnChange: true
  });
  
  return (
    <form onSubmit={form.handleSubmit}>
      <Input
        label="Nombre"
        value={form.values.name}
        onChange={form.handleChange('name')}
        onBlur={form.handleBlur('name')}
        error={form.touched.name ? form.errors.name : undefined}
        required
      />
      
      <Select
        label="Tipo"
        value={form.values.type}
        onChange={form.handleChange('type')}
        options={sensorTypeOptions}
        error={form.touched.type ? form.errors.type : undefined}
        required
      />
      
      <div className="flex gap-4">
        <Input
          label="Valor Mínimo"
          type="number"
          value={form.values.minValue}
          onChange={form.handleChange('minValue')}
          error={form.touched.minValue ? form.errors.minValue : undefined}
        />
        <Input
          label="Valor Máximo"
          type="number"
          value={form.values.maxValue}
          onChange={form.handleChange('maxValue')}
          error={form.touched.maxValue ? form.errors.maxValue : undefined}
        />
      </div>
      
      <div className="flex gap-3 justify-end">
        <Button type="button" variant="outline">
          Cancelar
        </Button>
        <Button
          type="submit"
          loading={form.isSubmitting}
          disabled={!form.isValid}
        >
          Guardar
        </Button>
      </div>
    </form>
  );
};
```

### useTable - Gestión de Tablas

**Propósito**: Hook para gestionar estado de tablas con ordenamiento, filtrado y paginación

```typescript
interface UseTableOptions<T> {
  data: T[];
  columns: TableColumn<T>[];
  pageSize?: number;
  sortable?: boolean;
  filterable?: boolean;
  selectable?: boolean;
}

interface UseTableReturn<T> {
  displayData: T[];
  pagination: {
    current: number;
    total: number;
    pageSize: number;
    totalPages: number;
  };
  sorting: {
    field: keyof T | null;
    direction: 'asc' | 'desc';
  };
  filters: Record<string, any>;
  selection: {
    selectedRows: T[];
    selectedRowKeys: string[];
    isAllSelected: boolean;
    isIndeterminate: boolean;
  };
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  setSorting: (field: keyof T, direction: 'asc' | 'desc') => void;
  setFilter: (field: string, value: any) => void;
  clearFilters: () => void;
  selectRow: (row: T) => void;
  selectAll: () => void;
  clearSelection: () => void;
}

function useTable<T>(options: UseTableOptions<T>): UseTableReturn<T>

// Ejemplo de uso
const SensorsTable = () => {
  const { data: sensors, loading } = useSensorData();
  
  const table = useTable({
    data: sensors || [],
    columns: sensorColumns,
    pageSize: 10,
    sortable: true,
    filterable: true,
    selectable: true
  });
  
  const handleBulkDelete = async () => {
    if (table.selection.selectedRows.length === 0) return;
    
    const confirmed = await confirm(
      `¿Eliminar ${table.selection.selectedRows.length} sensores?`
    );
    
    if (confirmed) {
      await deleteSensors(table.selection.selectedRowKeys);
      table.clearSelection();
    }
  };
  
  return (
    <div>
      <div className="flex justify-between mb-4">
        <div className="flex gap-2">
          <Input
            placeholder="Buscar sensores..."
            onChange={(value) => table.setFilter('search', value)}
          />
          <Select
            placeholder="Filtrar por tipo"
            options={sensorTypeOptions}
            onChange={(value) => table.setFilter('type', value)}
          />
        </div>
        
        <div className="flex gap-2">
          {table.selection.selectedRows.length > 0 && (
            <Button
              variant="danger"
              onClick={handleBulkDelete}
            >
              Eliminar ({table.selection.selectedRows.length})
            </Button>
          )}
          <Button variant="primary">
            Agregar Sensor
          </Button>
        </div>
      </div>
      
      <Table
        columns={sensorColumns}
        data={table.displayData}
        loading={loading}
        pagination={table.pagination}
        sorting={table.sorting}
        selection={table.selection}
        onPageChange={table.setPage}
        onSort={table.setSorting}
        onSelectRow={table.selectRow}
        onSelectAll={table.selectAll}
      />
    </div>
  );
};
```

## Hooks Utilitarios

### useLocalStorage - Persistencia Local

**Propósito**: Hook para gestionar datos en localStorage con sincronización

```typescript
interface UseLocalStorageOptions<T> {
  defaultValue: T;
  serializer?: {
    read: (value: string) => T;
    write: (value: T) => string;
  };
}

interface UseLocalStorageReturn<T> {
  value: T;
  setValue: (value: T | ((prev: T) => T)) => void;
  removeValue: () => void;
}

function useLocalStorage<T>(
  key: string,
  options: UseLocalStorageOptions<T>
): UseLocalStorageReturn<T>

// Ejemplo de uso
const UserPreferences = () => {
  const {
    value: preferences,
    setValue: setPreferences
  } = useLocalStorage('user-preferences', {
    defaultValue: {
      theme: 'light',
      language: 'es',
      autoRefresh: true,
      refreshInterval: 30000
    }
  });
  
  const updateTheme = (theme: 'light' | 'dark') => {
    setPreferences(prev => ({ ...prev, theme }));
  };
  
  const updateRefreshInterval = (interval: number) => {
    setPreferences(prev => ({ ...prev, refreshInterval: interval }));
  };
  
  return (
    <div>
      <Select
        label="Tema"
        value={preferences.theme}
        onChange={updateTheme}
        options={[
          { value: 'light', label: 'Claro' },
          { value: 'dark', label: 'Oscuro' }
        ]}
      />
      
      <Input
        label="Intervalo de actualización (ms)"
        type="number"
        value={preferences.refreshInterval}
        onChange={updateRefreshInterval}
      />
    </div>
  );
};
```

### useDebounce - Debounce de Valores

**Propósito**: Hook para debounce de valores y funciones

```typescript
function useDebounce<T>(value: T, delay: number): T
function useDebounce<T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T

// Ejemplo de uso - Debounce de valor
const SearchInput = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearchTerm = useDebounce(searchTerm, 300);
  
  const { data: results, loading } = useApi(
    () => searchSensors(debouncedSearchTerm),
    { immediate: false }
  );
  
  useEffect(() => {
    if (debouncedSearchTerm) {
      searchSensors(debouncedSearchTerm);
    }
  }, [debouncedSearchTerm]);
  
  return (
    <div>
      <Input
        placeholder="Buscar sensores..."
        value={searchTerm}
        onChange={setSearchTerm}
      />
      {loading && <Spinner />}
      <SearchResults results={results} />
    </div>
  );
};

// Ejemplo de uso - Debounce de función
const AutoSaveForm = () => {
  const [formData, setFormData] = useState({});
  
  const debouncedSave = useDebounce(async (data) => {
    await saveFormData(data);
    toast.show({
      type: 'success',
      message: 'Guardado automáticamente'
    });
  }, 1000);
  
  useEffect(() => {
    if (Object.keys(formData).length > 0) {
      debouncedSave(formData);
    }
  }, [formData, debouncedSave]);
  
  return (
    <form>
      <Input
        value={formData.name}
        onChange={(value) => setFormData(prev => ({ ...prev, name: value }))}
      />
      {/* Más campos */}
    </form>
  );
};
```

### useInterval - Intervalos Automáticos

**Propósito**: Hook para gestionar intervalos con limpieza automática

```typescript
interface UseIntervalOptions {
  immediate?: boolean;        // Ejecutar inmediatamente
  enabled?: boolean;          // Habilitar/deshabilitar
}

function useInterval(
  callback: () => void,
  delay: number | null,
  options?: UseIntervalOptions
): {
  start: () => void;
  stop: () => void;
  toggle: () => void;
  isRunning: boolean;
}

// Ejemplo de uso
const RealTimeMonitor = () => {
  const [isMonitoring, setIsMonitoring] = useState(true);
  const { data: sensorData, refreshSensors } = useSensorData();
  
  const interval = useInterval(
    refreshSensors,
    30000, // 30 segundos
    {
      immediate: true,
      enabled: isMonitoring
    }
  );
  
  const toggleMonitoring = () => {
    setIsMonitoring(!isMonitoring);
    interval.toggle();
  };
  
  return (
    <div>
      <div className="flex justify-between items-center">
        <h2>Monitor</h2>
        <Button
          variant={isMonitoring ? 'danger' : 'primary'}
          onClick={toggleMonitoring}
        >
          {isMonitoring ? 'Pausar' : 'Iniciar'} Monitoreo
        </Button>
      </div>
      
      <div className="status-indicator">
        <div className={`dot ${interval.isRunning ? 'active' : 'inactive'}`} />
        {interval.isRunning ? 'Monitoreando' : 'Pausado'}
      </div>
      
      <SensorGrid sensors={sensorData} />
    </div>
  );
};
```

### useMediaQuery - Responsive Queries

**Propósito**: Hook para detectar media queries y responsive breakpoints

```typescript
function useMediaQuery(query: string): boolean

// Breakpoints predefinidos
const breakpoints = {
  sm: '(min-width: 640px)',
  md: '(min-width: 768px)',
  lg: '(min-width: 1024px)',
  xl: '(min-width: 1280px)',
  '2xl': '(min-width: 1536px)'
};

function useBreakpoint() {
  return {
    isSm: useMediaQuery(breakpoints.sm),
    isMd: useMediaQuery(breakpoints.md),
    isLg: useMediaQuery(breakpoints.lg),
    isXl: useMediaQuery(breakpoints.xl),
    is2Xl: useMediaQuery(breakpoints['2xl'])
  };
}

// Ejemplo de uso
const ResponsiveDashboard = () => {
  const { isMd, isLg } = useBreakpoint();
  const isDesktop = useMediaQuery('(min-width: 1024px)');
  const isMobile = useMediaQuery('(max-width: 767px)');
  
  const getGridCols = () => {
    if (isLg) return 4;
    if (isMd) return 2;
    return 1;
  };
  
  return (
    <div>
      {isMobile && <MobileNavigation />}
      {isDesktop && <DesktopSidebar />}
      
      <Grid cols={getGridCols()} gap="md">
        <MetricCard />
        <MetricCard />
        <MetricCard />
        <MetricCard />
      </Grid>
      
      {isMobile ? (
        <MobileTable data={sensorData} />
      ) : (
        <DesktopTable data={sensorData} />
      )}
    </div>
  );
};
```

## Hooks de Autenticación

### useAuth - Gestión de Autenticación

**Propósito**: Hook para gestionar estado de autenticación y sesión

```typescript
interface UseAuthReturn {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  register: (userData: RegisterData) => Promise<void>;
  updateProfile: (data: Partial<User>) => Promise<void>;
  refreshToken: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
  hasRole: (role: string) => boolean;
}

function useAuth(): UseAuthReturn

// Ejemplo de uso
const LoginForm = () => {
  const { login, isLoading } = useAuth();
  const router = useRouter();
  
  const form = useForm({
    initialValues: {
      email: '',
      password: ''
    },
    onSubmit: async (values) => {
      try {
        await login(values);
        router.push('/dashboard');
      } catch (error) {
        toast.show({
          type: 'error',
          message: 'Credenciales inválidas'
        });
      }
    }
  });
  
  return (
    <form onSubmit={form.handleSubmit}>
      <Input
        label="Email"
        type="email"
        value={form.values.email}
        onChange={form.handleChange('email')}
        required
      />
      
      <Input
        label="Contraseña"
        type="password"
        value={form.values.password}
        onChange={form.handleChange('password')}
        required
      />
      
      <Button
        type="submit"
        loading={isLoading}
        fullWidth
      >
        Iniciar Sesión
      </Button>
    </form>
  );
};

// Componente protegido
const ProtectedComponent = () => {
  const { isAuthenticated, hasPermission } = useAuth();
  
  if (!isAuthenticated) {
    return <LoginPrompt />;
  }
  
  if (!hasPermission('sensors.manage')) {
    return <UnauthorizedMessage />;
  }
  
  return <SensorManagement />;
};
```

## Testing de Hooks

### Setup de Testing

```typescript
import { renderHook, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Wrapper para hooks que usan contexto
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });
  
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

// Test de hook simple
describe('useLocalStorage', () => {
  test('should initialize with default value', () => {
    const { result } = renderHook(() =>
      useLocalStorage('test-key', { defaultValue: 'default' })
    );
    
    expect(result.current.value).toBe('default');
  });
  
  test('should update value', () => {
    const { result } = renderHook(() =>
      useLocalStorage('test-key', { defaultValue: 'default' })
    );
    
    act(() => {
      result.current.setValue('new value');
    });
    
    expect(result.current.value).toBe('new value');
  });
});

// Test de hook con API
describe('useApi', () => {
  const mockApiFunction = jest.fn();
  
  beforeEach(() => {
    mockApiFunction.mockClear();
  });
  
  test('should handle successful API call', async () => {
    const mockData = { id: 1, name: 'Test' };
    mockApiFunction.mockResolvedValue(mockData);
    
    const { result } = renderHook(() =>
      useApi(mockApiFunction, { immediate: true }),
      { wrapper: createWrapper() }
    );
    
    expect(result.current.loading).toBe(true);
    
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
      expect(result.current.data).toEqual(mockData);
      expect(result.current.error).toBeNull();
    });
  });
  
  test('should handle API error', async () => {
    const mockError = new Error('API Error');
    mockApiFunction.mockRejectedValue(mockError);
    
    const { result } = renderHook(() =>
      useApi(mockApiFunction, { immediate: true }),
      { wrapper: createWrapper() }
    );
    
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
      expect(result.current.data).toBeNull();
      expect(result.current.error).toEqual(mockError);
    });
  });
});
```

## Performance y Optimización

### Memoización de Hooks

```typescript
// Hook optimizado con memoización
const useOptimizedSensorData = (sensorIds: string[]) => {
  // Memoizar la lista de IDs para evitar re-renders innecesarios
  const memoizedSensorIds = useMemo(() => sensorIds, [sensorIds.join(',')]);
  
  // Memoizar la función de fetch
  const fetchSensors = useCallback(async () => {
    return await api.getSensors(memoizedSensorIds);
  }, [memoizedSensorIds]);
  
  // Usar el hook de API con la función memoizada
  return useApi(fetchSensors, {
    immediate: true,
    cache: true,
    cacheKey: `sensors-${memoizedSensorIds.join('-')}`
  });
};

// Hook con cleanup automático
const useWebSocket = (url: string) => {
  const [socket, setSocket] = useState<WebSocket | null>(null);
  const [data, setData] = useState(null);
  
  useEffect(() => {
    const ws = new WebSocket(url);
    
    ws.onmessage = (event) => {
      setData(JSON.parse(event.data));
    };
    
    setSocket(ws);
    
    // Cleanup automático
    return () => {
      ws.close();
    };
  }, [url]);
  
  const sendMessage = useCallback((message: any) => {
    if (socket?.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify(message));
    }
  }, [socket]);
  
  return { data, sendMessage, isConnected: socket?.readyState === WebSocket.OPEN };
};
```

## Contribución

### Crear Nuevo Hook

1. **Identificar necesidad**: ¿Es lógica reutilizable?
2. **Definir interfaz**: TypeScript interfaces claras
3. **Implementar hook**: Siguiendo patrones establecidos
4. **Escribir tests**: Cobertura completa
5. **Documentar**: Ejemplos de uso y API
6. **Optimizar**: Performance y memory leaks

### Template de Hook

```typescript
import { useState, useEffect, useCallback } from 'react';

// Interfaces
interface UseCustomHookOptions {
  // Opciones del hook
}

interface UseCustomHookReturn {
  // Valor de retorno
}

/**
 * Hook personalizado para [descripción]
 * 
 * @param options - Opciones de configuración
 * @returns Objeto con estado y funciones
 * 
 * @example
 * ```typescript
 * const { data, loading, error } = useCustomHook({
 *   option1: 'value1'
 * });
 * ```
 */
export function useCustomHook(
  options: UseCustomHookOptions
): UseCustomHookReturn {
  // Estado interno
  const [state, setState] = useState();
  
  // Efectos
  useEffect(() => {
    // Lógica del hook
  }, []);
  
  // Funciones memoizadas
  const memoizedFunction = useCallback(() => {
    // Lógica
  }, []);
  
  // Cleanup
  useEffect(() => {
    return () => {
      // Cleanup
    };
  }, []);
  
  return {
    // Retorno del hook
  };
}
```

### Code Review Checklist

- [ ] TypeScript interfaces completas
- [ ] Memoización apropiada
- [ ] Cleanup de efectos
- [ ] Tests unitarios
- [ ] Documentación con ejemplos
- [ ] Performance optimizada
- [ ] Manejo de errores
- [ ] Compatibilidad con SSR

## Recursos

- [React Hooks Documentation](https://react.dev/reference/react)
- [Custom Hooks Best Practices](https://react.dev/learn/reusing-logic-with-custom-hooks)
- [Testing Library React Hooks](https://react-hooks-testing-library.com/)
- [usehooks-ts](https://usehooks-ts.com/)
- [React Hook Form](https://react-hook-form.com/)