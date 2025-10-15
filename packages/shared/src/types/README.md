# Types - Definiciones de Tipos TypeScript

Este directorio contiene todas las definiciones de tipos, interfaces y enums de TypeScript para la aplicación HydroEspinaca, proporcionando un sistema de tipos robusto y consistente.

## Arquitectura de Tipos

### Principios de Diseño

- **Type Safety**: Máxima seguridad de tipos
- **Reutilización**: Tipos composables y extensibles
- **Consistencia**: Nomenclatura y estructura uniforme
- **Documentación**: Tipos autodocumentados
- **Evolución**: Fácil mantenimiento y extensión
- **Performance**: Optimización en tiempo de compilación

### Estructura de Tipos

```
types/
├── api/                    # Tipos relacionados con API
│   ├── requests.ts
│   ├── responses.ts
│   ├── errors.ts
│   └── pagination.ts
├── entities/               # Entidades del dominio
│   ├── sensor.ts
│   ├── actuator.ts
│   ├── user.ts
│   ├── reading.ts
│   ├── variable.ts
│   └── alert.ts
├── ui/                     # Tipos de componentes UI
│   ├── components.ts
│   ├── forms.ts
│   ├── tables.ts
│   └── charts.ts
├── store/                  # Tipos de estado global
│   ├── auth.ts
│   ├── sensors.ts
│   ├── readings.ts
│   └── common.ts
├── utils/                  # Tipos utilitarios
│   ├── helpers.ts
│   ├── validators.ts
│   └── formatters.ts
├── global.d.ts            # Tipos globales
├── index.ts               # Exportaciones principales
└── README.md              # Esta documentación
```

## Tipos de Entidades del Dominio

### sensor.ts - Tipos de Sensores

**Propósito**: Definiciones de tipos para sensores y sus datos

```typescript
/**
 * Tipos de sensores disponibles en el sistema
 */
export enum SensorType {
  TEMPERATURE = 'temperature',
  HUMIDITY = 'humidity',
  PH = 'ph',
  CONDUCTIVITY = 'conductivity',
  LIGHT = 'light',
  WATER_LEVEL = 'water_level',
  DISSOLVED_OXYGEN = 'dissolved_oxygen',
  TURBIDITY = 'turbidity'
}

/**
 * Estados posibles de un sensor
 */
export enum SensorStatus {
  ONLINE = 'online',
  OFFLINE = 'offline',
  ERROR = 'error',
  MAINTENANCE = 'maintenance',
  CALIBRATING = 'calibrating'
}

/**
 * Categorías de sensores para organización
 */
export enum SensorCategory {
  ENVIRONMENTAL = 'environmental',
  NUTRITIONAL = 'nutritional',
  PHYSICAL = 'physical',
  BIOLOGICAL = 'biological'
}

/**
 * Configuración base de un sensor
 */
export interface SensorConfig {
  /** Nombre descriptivo del sensor */
  name: string;
  /** Unidad de medida */
  unit: string;
  /** Icono para la UI */
  icon: string;
  /** Color asociado en hex */
  color: string;
  /** Rango de valores válidos */
  validRange: {
    min: number;
    max: number;
  };
  /** Rango óptimo para la planta */
  optimalRange: {
    min: number;
    max: number;
  };
  /** Precisión decimal */
  precision: number;
  /** Categoría del sensor */
  category: SensorCategory;
  /** Frecuencia de lectura en segundos */
  readingInterval: number;
}

/**
 * Datos completos de un sensor
 */
export interface SensorData {
  /** Identificador único */
  id: string;
  /** Nombre del sensor */
  name: string;
  /** Tipo de sensor */
  type: SensorType;
  /** Estado actual */
  status: SensorStatus;
  /** Ubicación física */
  location: string;
  /** Descripción opcional */
  description?: string;
  /** Configuración del sensor */
  config: SensorConfig;
  /** Valor actual */
  currentValue: number;
  /** Timestamp de la última lectura */
  lastReading: string;
  /** Configuración de alertas */
  alertConfig: {
    enabled: boolean;
    minThreshold?: number;
    maxThreshold?: number;
    criticalMin?: number;
    criticalMax?: number;
  };
  /** Metadatos adicionales */
  metadata: {
    /** Fecha de instalación */
    installedAt: string;
    /** Última calibración */
    lastCalibration?: string;
    /** Próximo mantenimiento */
    nextMaintenance?: string;
    /** Número de serie */
    serialNumber?: string;
    /** Modelo del sensor */
    model?: string;
    /** Fabricante */
    manufacturer?: string;
  };
  /** Timestamps de auditoría */
  createdAt: string;
  updatedAt: string;
}

/**
 * Resumen estadístico de un sensor
 */
export interface SensorSummary {
  /** ID del sensor */
  sensorId: string;
  /** Nombre del sensor */
  sensor: string;
  /** Tipo de sensor */
  type: SensorType;
  /** Unidad de medida */
  unidad: string;
  /** Valor promedio */
  media: number;
  /** Valor mínimo */
  minimo: number;
  /** Valor máximo */
  maximo: number;
  /** Última lectura */
  ultimaLectura: string;
  /** Número total de lecturas */
  totalLecturas: number;
  /** Estado del sensor */
  status: SensorStatus;
}

/**
 * Configuración de calibración
 */
export interface SensorCalibration {
  /** ID del sensor */
  sensorId: string;
  /** Fecha de calibración */
  calibrationDate: string;
  /** Valores de referencia */
  referenceValues: {
    input: number;
    expected: number;
    measured: number;
  }[];
  /** Factor de corrección */
  correctionFactor: number;
  /** Offset de calibración */
  offset: number;
  /** Técnico que realizó la calibración */
  technician: string;
  /** Notas de calibración */
  notes?: string;
}

/**
 * Historial de mantenimiento
 */
export interface SensorMaintenance {
  /** ID único del mantenimiento */
  id: string;
  /** ID del sensor */
  sensorId: string;
  /** Tipo de mantenimiento */
  type: 'preventive' | 'corrective' | 'calibration' | 'replacement';
  /** Fecha del mantenimiento */
  date: string;
  /** Descripción del trabajo realizado */
  description: string;
  /** Técnico responsable */
  technician: string;
  /** Partes reemplazadas */
  partsReplaced?: string[];
  /** Costo del mantenimiento */
  cost?: number;
  /** Próximo mantenimiento programado */
  nextScheduled?: string;
  /** Estado del sensor después del mantenimiento */
  postMaintenanceStatus: SensorStatus;
}

/**
 * Filtros para consultas de sensores
 */
export interface SensorFilters {
  /** Filtrar por tipo */
  type?: SensorType[];
  /** Filtrar por estado */
  status?: SensorStatus[];
  /** Filtrar por categoría */
  category?: SensorCategory[];
  /** Filtrar por ubicación */
  location?: string[];
  /** Filtrar por rango de fechas */
  dateRange?: {
    from: string;
    to: string;
  };
  /** Búsqueda por texto */
  search?: string;
}

/**
 * Opciones de ordenamiento
 */
export interface SensorSortOptions {
  /** Campo por el cual ordenar */
  field: keyof SensorData;
  /** Dirección del ordenamiento */
  direction: 'asc' | 'desc';
}

/**
 * Resultado de consulta de sensores
 */
export interface SensorQueryResult {
  /** Sensores encontrados */
  sensors: SensorData[];
  /** Información de paginación */
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
  /** Filtros aplicados */
  appliedFilters: SensorFilters;
  /** Ordenamiento aplicado */
  sorting: SensorSortOptions;
}

// Tipos utilitarios para sensores
export type SensorId = string;
export type SensorName = string;
export type SensorValue = number;
export type SensorTimestamp = string;

// Tipos de eventos de sensores
export interface SensorEvent {
  id: string;
  sensorId: string;
  type: 'reading' | 'alert' | 'status_change' | 'calibration' | 'maintenance';
  timestamp: string;
  data: Record<string, any>;
  severity?: 'low' | 'medium' | 'high' | 'critical';
}

// Tipos para configuración de dashboard
export interface SensorDashboardConfig {
  /** Sensores visibles en el dashboard */
  visibleSensors: string[];
  /** Configuración de gráficos */
  chartConfig: {
    timeRange: '1h' | '6h' | '24h' | '7d' | '30d';
    refreshInterval: number;
    showTrends: boolean;
    showAlerts: boolean;
  };
  /** Configuración de alertas */
  alertConfig: {
    enableNotifications: boolean;
    soundEnabled: boolean;
    emailNotifications: boolean;
  };
}
```

### reading.ts - Tipos de Lecturas

**Propósito**: Definiciones para lecturas de sensores y datos históricos

```typescript
/**
 * Lectura individual de un sensor
 */
export interface IndividualReading {
  /** ID único de la lectura */
  id: string;
  /** ID del sensor */
  sensorId: string;
  /** Nombre del sensor */
  sensor: string;
  /** Tipo de sensor */
  type: SensorType;
  /** Valor medido */
  valor: number;
  /** Unidad de medida */
  unidad: string;
  /** Timestamp de la lectura */
  fecha: string;
  /** Calidad de la lectura */
  quality: ReadingQuality;
  /** Metadatos adicionales */
  metadata?: {
    /** Temperatura ambiente durante la lectura */
    ambientTemp?: number;
    /** Humedad ambiente */
    ambientHumidity?: number;
    /** Voltaje del sensor */
    voltage?: number;
    /** Señal de ruido */
    noise?: number;
  };
}

/**
 * Calidad de una lectura
 */
export enum ReadingQuality {
  EXCELLENT = 'excellent',
  GOOD = 'good',
  FAIR = 'fair',
  POOR = 'poor',
  INVALID = 'invalid'
}

/**
 * Lectura agregada (estadísticas por período)
 */
export interface AggregatedReading {
  /** ID del sensor */
  sensorId: string;
  /** Período de agregación */
  period: string;
  /** Tipo de agregación */
  aggregationType: 'hourly' | 'daily' | 'weekly' | 'monthly';
  /** Estadísticas del período */
  statistics: {
    count: number;
    mean: number;
    median: number;
    min: number;
    max: number;
    standardDeviation: number;
    variance: number;
  };
  /** Timestamp del inicio del período */
  periodStart: string;
  /** Timestamp del fin del período */
  periodEnd: string;
}

/**
 * Batch de lecturas para inserción masiva
 */
export interface ReadingBatch {
  /** ID del batch */
  batchId: string;
  /** Lecturas en el batch */
  readings: Omit<IndividualReading, 'id'>[];
  /** Timestamp de creación del batch */
  createdAt: string;
  /** Estado del procesamiento */
  status: 'pending' | 'processing' | 'completed' | 'failed';
  /** Errores durante el procesamiento */
  errors?: string[];
}

/**
 * Configuración de exportación de lecturas
 */
export interface ReadingExportConfig {
  /** Sensores a incluir */
  sensorIds: string[];
  /** Rango de fechas */
  dateRange: {
    from: string;
    to: string;
  };
  /** Formato de exportación */
  format: 'csv' | 'json' | 'xlsx' | 'pdf';
  /** Incluir metadatos */
  includeMetadata: boolean;
  /** Agregación de datos */
  aggregation?: {
    enabled: boolean;
    interval: 'minute' | 'hour' | 'day';
    functions: ('mean' | 'min' | 'max' | 'count')[];
  };
  /** Filtros adicionales */
  filters?: {
    qualityThreshold?: ReadingQuality;
    valueRange?: {
      min: number;
      max: number;
    };
  };
}

/**
 * Resultado de análisis de tendencias
 */
export interface TrendAnalysis {
  /** ID del sensor */
  sensorId: string;
  /** Período analizado */
  period: {
    from: string;
    to: string;
  };
  /** Tendencia general */
  trend: 'increasing' | 'decreasing' | 'stable' | 'volatile';
  /** Pendiente de la tendencia */
  slope: number;
  /** Coeficiente de correlación */
  correlation: number;
  /** Puntos de cambio detectados */
  changePoints: {
    timestamp: string;
    value: number;
    significance: number;
  }[];
  /** Predicciones futuras */
  predictions?: {
    timestamp: string;
    predictedValue: number;
    confidence: number;
  }[];
}

/**
 * Configuración de alertas basadas en lecturas
 */
export interface ReadingAlertConfig {
  /** ID del sensor */
  sensorId: string;
  /** Tipo de alerta */
  type: 'threshold' | 'trend' | 'anomaly' | 'missing_data';
  /** Configuración específica por tipo */
  config: {
    threshold?: {
      min?: number;
      max?: number;
      duration?: number; // minutos
    };
    trend?: {
      direction: 'increasing' | 'decreasing';
      rate: number;
      duration: number;
    };
    anomaly?: {
      sensitivity: 'low' | 'medium' | 'high';
      windowSize: number;
    };
    missingData?: {
      maxInterval: number; // minutos
    };
  };
  /** Acciones a tomar */
  actions: {
    notification: boolean;
    email: boolean;
    sms: boolean;
    webhook?: string;
  };
  /** Estado de la alerta */
  enabled: boolean;
}

/**
 * Filtros para consultas de lecturas
 */
export interface ReadingFilters {
  /** IDs de sensores */
  sensorIds?: string[];
  /** Rango de fechas */
  dateRange?: {
    from: string;
    to: string;
  };
  /** Rango de valores */
  valueRange?: {
    min: number;
    max: number;
  };
  /** Calidad mínima */
  minQuality?: ReadingQuality;
  /** Tipos de sensores */
  sensorTypes?: SensorType[];
}

/**
 * Opciones de consulta de lecturas
 */
export interface ReadingQueryOptions {
  /** Filtros a aplicar */
  filters?: ReadingFilters;
  /** Ordenamiento */
  sort?: {
    field: 'fecha' | 'valor' | 'sensor';
    direction: 'asc' | 'desc';
  };
  /** Paginación */
  pagination?: {
    page: number;
    pageSize: number;
  };
  /** Incluir metadatos */
  includeMetadata?: boolean;
}

/**
 * Resultado de consulta de lecturas
 */
export interface ReadingQueryResult {
  /** Lecturas encontradas */
  readings: IndividualReading[];
  /** Información de paginación */
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
  /** Estadísticas del conjunto de datos */
  statistics?: {
    count: number;
    mean: number;
    min: number;
    max: number;
    latest: string;
    oldest: string;
  };
}
```

### actuator.ts - Tipos de Actuadores

**Propósito**: Definiciones para actuadores y dispositivos de control

```typescript
/**
 * Tipos de actuadores disponibles
 */
export enum ActuatorType {
  PUMP = 'pump',
  VALVE = 'valve',
  FAN = 'fan',
  HEATER = 'heater',
  COOLER = 'cooler',
  LIGHT = 'light',
  MOTOR = 'motor',
  RELAY = 'relay'
}

/**
 * Estados de un actuador
 */
export enum ActuatorStatus {
  ACTIVE = 'active',
  INACTIVE = 'inactive',
  ERROR = 'error',
  MAINTENANCE = 'maintenance',
  MANUAL = 'manual'
}

/**
 * Modos de operación
 */
export enum ActuatorMode {
  MANUAL = 'manual',
  AUTOMATIC = 'automatic',
  SCHEDULED = 'scheduled',
  SENSOR_TRIGGERED = 'sensor_triggered'
}

/**
 * Datos completos de un actuador
 */
export interface ActuadorData {
  /** Identificador único */
  id: string;
  /** Nombre del actuador */
  name: string;
  /** Tipo de actuador */
  type: ActuatorType;
  /** Estado actual */
  status: ActuatorStatus;
  /** Modo de operación */
  mode: ActuatorMode;
  /** Pin de conexión */
  pin: number;
  /** Ubicación física */
  location: string;
  /** Descripción */
  description?: string;
  /** Configuración específica */
  config: {
    /** Voltaje de operación */
    voltage: number;
    /** Corriente máxima */
    maxCurrent: number;
    /** Potencia nominal */
    power: number;
    /** Tiempo mínimo entre activaciones */
    minCycleTime: number;
    /** Tiempo máximo de operación continua */
    maxRunTime: number;
  };
  /** Estado de activación */
  isActive: boolean;
  /** Valor actual (para actuadores variables) */
  currentValue?: number;
  /** Rango de valores (para actuadores variables) */
  valueRange?: {
    min: number;
    max: number;
    unit: string;
  };
  /** Configuración de automatización */
  automation?: {
    enabled: boolean;
    triggers: ActuatorTrigger[];
    schedule?: ActuatorSchedule[];
  };
  /** Timestamps */
  createdAt: string;
  lastModified: string;
  lastActivated?: string;
}

/**
 * Trigger para automatización
 */
export interface ActuatorTrigger {
  /** ID único del trigger */
  id: string;
  /** Nombre descriptivo */
  name: string;
  /** Sensor que dispara la acción */
  sensorId: string;
  /** Condición del trigger */
  condition: {
    operator: 'gt' | 'lt' | 'eq' | 'gte' | 'lte' | 'between';
    value: number;
    value2?: number; // Para 'between'
  };
  /** Acción a ejecutar */
  action: {
    type: 'activate' | 'deactivate' | 'set_value';
    value?: number;
    duration?: number; // minutos
  };
  /** Delay antes de ejecutar */
  delay?: number; // segundos
  /** Cooldown después de ejecutar */
  cooldown?: number; // minutos
  /** Estado del trigger */
  enabled: boolean;
}

/**
 * Programación temporal
 */
export interface ActuatorSchedule {
  /** ID único de la programación */
  id: string;
  /** Nombre descriptivo */
  name: string;
  /** Días de la semana (0=domingo, 6=sábado) */
  daysOfWeek: number[];
  /** Hora de inicio (HH:mm) */
  startTime: string;
  /** Duración en minutos */
  duration: number;
  /** Acción a ejecutar */
  action: {
    type: 'activate' | 'deactivate' | 'set_value';
    value?: number;
  };
  /** Estado de la programación */
  enabled: boolean;
  /** Fecha de inicio de vigencia */
  validFrom?: string;
  /** Fecha de fin de vigencia */
  validTo?: string;
}

/**
 * Historial de activaciones
 */
export interface ActuatorActivationLog {
  /** ID único del log */
  id: string;
  /** ID del actuador */
  actuatorId: string;
  /** Timestamp de la activación */
  timestamp: string;
  /** Tipo de acción */
  action: 'activated' | 'deactivated' | 'value_changed';
  /** Valor anterior */
  previousValue?: number;
  /** Nuevo valor */
  newValue?: number;
  /** Duración de la activación */
  duration?: number;
  /** Origen de la acción */
  source: 'manual' | 'automatic' | 'scheduled' | 'trigger';
  /** ID del trigger o schedule que causó la acción */
  sourceId?: string;
  /** Usuario que ejecutó la acción (si es manual) */
  userId?: string;
  /** Notas adicionales */
  notes?: string;
}

/**
 * Estadísticas de uso de actuador
 */
export interface ActuatorUsageStats {
  /** ID del actuador */
  actuatorId: string;
  /** Período de las estadísticas */
  period: {
    from: string;
    to: string;
  };
  /** Tiempo total activo (minutos) */
  totalActiveTime: number;
  /** Número de activaciones */
  activationCount: number;
  /** Tiempo promedio por activación */
  averageActivationTime: number;
  /** Consumo energético estimado (kWh) */
  estimatedEnergyConsumption: number;
  /** Distribución por fuente */
  sourceDistribution: {
    manual: number;
    automatic: number;
    scheduled: number;
    trigger: number;
  };
  /** Eficiencia (activaciones exitosas / total) */
  efficiency: number;
}

/**
 * Comando para controlar actuador
 */
export interface ActuatorCommand {
  /** ID del actuador */
  actuatorId: string;
  /** Tipo de comando */
  command: 'activate' | 'deactivate' | 'set_value' | 'toggle';
  /** Valor para comandos set_value */
  value?: number;
  /** Duración del comando (minutos) */
  duration?: number;
  /** Prioridad del comando */
  priority: 'low' | 'normal' | 'high' | 'emergency';
  /** Usuario que ejecuta el comando */
  userId: string;
  /** Notas del comando */
  notes?: string;
  /** Timestamp de creación */
  createdAt: string;
}

/**
 * Resultado de ejecución de comando
 */
export interface ActuatorCommandResult {
  /** ID del comando */
  commandId: string;
  /** Estado de ejecución */
  status: 'pending' | 'executing' | 'completed' | 'failed' | 'cancelled';
  /** Timestamp de inicio */
  startedAt?: string;
  /** Timestamp de finalización */
  completedAt?: string;
  /** Mensaje de error si falló */
  error?: string;
  /** Valor resultante */
  resultValue?: number;
}
```

### user.ts - Tipos de Usuario

**Propósito**: Definiciones para usuarios y autenticación

```typescript
/**
 * Roles de usuario en el sistema
 */
export enum UserRole {
  ADMIN = 'admin',
  OPERATOR = 'operator',
  VIEWER = 'viewer',
  TECHNICIAN = 'technician'
}

/**
 * Permisos específicos del sistema
 */
export enum Permission {
  // Sensores
  SENSORS_VIEW = 'sensors:view',
  SENSORS_CREATE = 'sensors:create',
  SENSORS_EDIT = 'sensors:edit',
  SENSORS_DELETE = 'sensors:delete',
  SENSORS_CALIBRATE = 'sensors:calibrate',
  
  // Actuadores
  ACTUATORS_VIEW = 'actuators:view',
  ACTUATORS_CREATE = 'actuators:create',
  ACTUATORS_EDIT = 'actuators:edit',
  ACTUATORS_DELETE = 'actuators:delete',
  ACTUATORS_CONTROL = 'actuators:control',
  
  // Lecturas
  READINGS_VIEW = 'readings:view',
  READINGS_EXPORT = 'readings:export',
  READINGS_DELETE = 'readings:delete',
  
  // Usuarios
  USERS_VIEW = 'users:view',
  USERS_CREATE = 'users:create',
  USERS_EDIT = 'users:edit',
  USERS_DELETE = 'users:delete',
  
  // Sistema
  SYSTEM_CONFIG = 'system:config',
  SYSTEM_LOGS = 'system:logs',
  SYSTEM_BACKUP = 'system:backup'
}

/**
 * Datos completos del usuario
 */
export interface User {
  /** ID único del usuario */
  id: string;
  /** Email (usado como username) */
  email: string;
  /** Nombre completo */
  name: string;
  /** Rol principal */
  role: UserRole;
  /** Permisos específicos */
  permissions: Permission[];
  /** Avatar/foto de perfil */
  avatar?: string;
  /** Teléfono */
  phone?: string;
  /** Departamento/área */
  department?: string;
  /** Estado de la cuenta */
  status: 'active' | 'inactive' | 'suspended';
  /** Configuraciones personales */
  preferences: UserPreferences;
  /** Metadata de la cuenta */
  metadata: {
    /** Último login */
    lastLogin?: string;
    /** Fecha de creación */
    createdAt: string;
    /** Última actualización */
    updatedAt: string;
    /** Intentos de login fallidos */
    failedLoginAttempts: number;
    /** Fecha de bloqueo temporal */
    lockedUntil?: string;
    /** Requiere cambio de contraseña */
    requiresPasswordChange: boolean;
  };
}

/**
 * Preferencias del usuario
 */
export interface UserPreferences {
  /** Idioma preferido */
  language: 'es' | 'en';
  /** Zona horaria */
  timezone: string;
  /** Formato de fecha */
  dateFormat: 'DD/MM/YYYY' | 'MM/DD/YYYY' | 'YYYY-MM-DD';
  /** Formato de hora */
  timeFormat: '12h' | '24h';
  /** Tema de la interfaz */
  theme: 'light' | 'dark' | 'auto';
  /** Configuración de notificaciones */
  notifications: {
    email: boolean;
    push: boolean;
    sms: boolean;
    alerts: {
      critical: boolean;
      warning: boolean;
      info: boolean;
    };
  };
  /** Configuración del dashboard */
  dashboard: {
    /** Widgets visibles */
    visibleWidgets: string[];
    /** Orden de los widgets */
    widgetOrder: string[];
    /** Configuración de gráficos */
    chartSettings: {
      defaultTimeRange: '1h' | '6h' | '24h' | '7d' | '30d';
      refreshInterval: number;
      showAnimations: boolean;
    };
  };
}

/**
 * Datos de autenticación
 */
export interface AuthData {
  /** Token de acceso */
  accessToken: string;
  /** Token de refresco */
  refreshToken: string;
  /** Tipo de token */
  tokenType: 'Bearer';
  /** Tiempo de expiración en segundos */
  expiresIn: number;
  /** Timestamp de expiración */
  expiresAt: string;
  /** Alcance de permisos */
  scope: string[];
}

/**
 * Credenciales de login
 */
export interface LoginCredentials {
  /** Email del usuario */
  email: string;
  /** Contraseña */
  password: string;
  /** Recordar sesión */
  rememberMe?: boolean;
  /** Token de 2FA si está habilitado */
  twoFactorToken?: string;
}

/**
 * Datos para registro de usuario
 */
export interface RegisterData {
  /** Email */
  email: string;
  /** Contraseña */
  password: string;
  /** Confirmación de contraseña */
  confirmPassword: string;
  /** Nombre completo */
  name: string;
  /** Teléfono opcional */
  phone?: string;
  /** Departamento */
  department?: string;
  /** Términos y condiciones aceptados */
  acceptTerms: boolean;
}

/**
 * Datos para cambio de contraseña
 */
export interface ChangePasswordData {
  /** Contraseña actual */
  currentPassword: string;
  /** Nueva contraseña */
  newPassword: string;
  /** Confirmación de nueva contraseña */
  confirmNewPassword: string;
}

/**
 * Datos para recuperación de contraseña
 */
export interface PasswordResetData {
  /** Email del usuario */
  email: string;
}

/**
 * Datos para confirmar reset de contraseña
 */
export interface PasswordResetConfirmData {
  /** Token de reset */
  token: string;
  /** Nueva contraseña */
  newPassword: string;
  /** Confirmación de nueva contraseña */
  confirmNewPassword: string;
}

/**
 * Sesión activa del usuario
 */
export interface UserSession {
  /** ID de la sesión */
  sessionId: string;
  /** ID del usuario */
  userId: string;
  /** IP de origen */
  ipAddress: string;
  /** User agent */
  userAgent: string;
  /** Timestamp de inicio */
  startedAt: string;
  /** Última actividad */
  lastActivity: string;
  /** Ubicación geográfica */
  location?: {
    country: string;
    city: string;
    coordinates?: {
      lat: number;
      lng: number;
    };
  };
  /** Estado de la sesión */
  status: 'active' | 'expired' | 'terminated';
}

/**
 * Log de actividad del usuario
 */
export interface UserActivityLog {
  /** ID único del log */
  id: string;
  /** ID del usuario */
  userId: string;
  /** Tipo de actividad */
  action: string;
  /** Recurso afectado */
  resource?: string;
  /** ID del recurso */
  resourceId?: string;
  /** Detalles adicionales */
  details?: Record<string, any>;
  /** IP de origen */
  ipAddress: string;
  /** User agent */
  userAgent: string;
  /** Timestamp */
  timestamp: string;
  /** Resultado de la acción */
  result: 'success' | 'failure' | 'partial';
}
```

## Tipos de API

### api/responses.ts - Respuestas de API

**Propósito**: Tipos estándar para respuestas de la API

```typescript
/**
 * Respuesta base de la API
 */
export interface ApiResponse<T = any> {
  /** Datos de la respuesta */
  data: T;
  /** Mensaje descriptivo */
  message: string;
  /** Código de estado */
  status: number;
  /** Timestamp de la respuesta */
  timestamp: string;
  /** ID de la petición para tracking */
  requestId: string;
}

/**
 * Respuesta de error de la API
 */
export interface ApiErrorResponse {
  /** Código de error */
  error: {
    code: string;
    message: string;
    details?: Record<string, any>;
  };
  /** Código de estado HTTP */
  status: number;
  /** Timestamp del error */
  timestamp: string;
  /** ID de la petición */
  requestId: string;
  /** Stack trace (solo en desarrollo) */
  stack?: string;
}

/**
 * Respuesta paginada
 */
export interface PaginatedResponse<T> {
  /** Datos de la página actual */
  data: T[];
  /** Información de paginación */
  pagination: {
    /** Página actual */
    current: number;
    /** Tamaño de página */
    pageSize: number;
    /** Total de elementos */
    total: number;
    /** Total de páginas */
    totalPages: number;
    /** Hay página siguiente */
    hasNext: boolean;
    /** Hay página anterior */
    hasPrev: boolean;
  };
  /** Metadatos adicionales */
  meta?: {
    /** Filtros aplicados */
    filters?: Record<string, any>;
    /** Ordenamiento aplicado */
    sort?: {
      field: string;
      direction: 'asc' | 'desc';
    };
    /** Tiempo de consulta en ms */
    queryTime?: number;
  };
}

/**
 * Respuesta de operación batch
 */
export interface BatchOperationResponse<T> {
  /** Operaciones exitosas */
  successful: T[];
  /** Operaciones fallidas */
  failed: {
    item: T;
    error: string;
  }[];
  /** Resumen de la operación */
  summary: {
    total: number;
    successful: number;
    failed: number;
    duration: number;
  };
}

/**
 * Respuesta de validación
 */
export interface ValidationResponse {
  /** Es válido */
  isValid: boolean;
  /** Errores de validación */
  errors: {
    field: string;
    message: string;
    code: string;
  }[];
  /** Advertencias */
  warnings?: {
    field: string;
    message: string;
  }[];
}

/**
 * Respuesta de estado del sistema
 */
export interface SystemStatusResponse {
  /** Estado general */
  status: 'healthy' | 'degraded' | 'down';
  /** Timestamp de la verificación */
  timestamp: string;
  /** Versión del sistema */
  version: string;
  /** Tiempo de actividad */
  uptime: number;
  /** Componentes del sistema */
  components: {
    name: string;
    status: 'healthy' | 'degraded' | 'down';
    responseTime?: number;
    lastCheck: string;
    details?: Record<string, any>;
  }[];
  /** Métricas del sistema */
  metrics: {
    cpu: number;
    memory: number;
    disk: number;
    network: {
      inbound: number;
      outbound: number;
    };
  };
}
```

## Tipos de UI

### ui/components.ts - Tipos de Componentes

**Propósito**: Tipos para props y estados de componentes UI

```typescript
/**
 * Props base para todos los componentes
 */
export interface BaseComponentProps {
  /** Clase CSS adicional */
  className?: string;
  /** ID del elemento */
  id?: string;
  /** Datos de test */
  'data-testid'?: string;
  /** Elementos hijos */
  children?: React.ReactNode;
}

/**
 * Tamaños estándar para componentes
 */
export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Variantes de color para componentes
 */
export type ComponentVariant = 
  | 'primary' 
  | 'secondary' 
  | 'success' 
  | 'warning' 
  | 'error' 
  | 'info';

/**
 * Props para botones
 */
export interface ButtonProps extends BaseComponentProps {
  /** Variante del botón */
  variant?: ComponentVariant;
  /** Tamaño del botón */
  size?: ComponentSize;
  /** Estado deshabilitado */
  disabled?: boolean;
  /** Estado de carga */
  loading?: boolean;
  /** Icono del botón */
  icon?: string;
  /** Posición del icono */
  iconPosition?: 'left' | 'right';
  /** Tipo de botón */
  type?: 'button' | 'submit' | 'reset';
  /** Función onClick */
  onClick?: (event: React.MouseEvent<HTMLButtonElement>) => void;
}

/**
 * Props para inputs
 */
export interface InputProps extends BaseComponentProps {
  /** Tipo de input */
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  /** Valor del input */
  value?: string | number;
  /** Valor por defecto */
  defaultValue?: string | number;
  /** Placeholder */
  placeholder?: string;
  /** Estado deshabilitado */
  disabled?: boolean;
  /** Solo lectura */
  readOnly?: boolean;
  /** Requerido */
  required?: boolean;
  /** Tamaño del input */
  size?: ComponentSize;
  /** Estado de error */
  error?: boolean;
  /** Mensaje de error */
  errorMessage?: string;
  /** Texto de ayuda */
  helpText?: string;
  /** Icono del input */
  icon?: string;
  /** Posición del icono */
  iconPosition?: 'left' | 'right';
  /** Función onChange */
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  /** Función onBlur */
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  /** Función onFocus */
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
}

/**
 * Props para modales
 */
export interface ModalProps extends BaseComponentProps {
  /** Estado abierto/cerrado */
  isOpen: boolean;
  /** Función para cerrar */
  onClose: () => void;
  /** Título del modal */
  title?: string;
  /** Tamaño del modal */
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  /** Cerrar al hacer click fuera */
  closeOnOverlayClick?: boolean;
  /** Cerrar con tecla Escape */
  closeOnEscape?: boolean;
  /** Mostrar botón de cerrar */
  showCloseButton?: boolean;
  /** Footer del modal */
  footer?: React.ReactNode;
}

/**
 * Props para tablas
 */
export interface TableProps<T = any> extends BaseComponentProps {
  /** Datos de la tabla */
  data: T[];
  /** Definición de columnas */
  columns: TableColumn<T>[];
  /** Estado de carga */
  loading?: boolean;
  /** Mensaje cuando no hay datos */
  emptyMessage?: string;
  /** Paginación */
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    onChange: (page: number, pageSize: number) => void;
  };
  /** Ordenamiento */
  sorting?: {
    field: keyof T;
    direction: 'asc' | 'desc';
    onChange: (field: keyof T, direction: 'asc' | 'desc') => void;
  };
  /** Selección de filas */
  selection?: {
    selectedRows: string[];
    onSelectionChange: (selectedRows: string[]) => void;
    getRowId: (row: T) => string;
  };
  /** Acciones de fila */
  rowActions?: TableRowAction<T>[];
}

/**
 * Definición de columna de tabla
 */
export interface TableColumn<T = any> {
  /** Clave de la columna */
  key: keyof T | string;
  /** Título de la columna */
  title: string;
  /** Ancho de la columna */
  width?: string | number;
  /** Alineación del contenido */
  align?: 'left' | 'center' | 'right';
  /** Función de renderizado personalizada */
  render?: (value: any, row: T, index: number) => React.ReactNode;
  /** Es ordenable */
  sortable?: boolean;
  /** Es filtrable */
  filterable?: boolean;
  /** Tipo de filtro */
  filterType?: 'text' | 'select' | 'date' | 'number';
  /** Opciones de filtro (para select) */
  filterOptions?: { label: string; value: any }[];
}

/**
 * Acción de fila de tabla
 */
export interface TableRowAction<T = any> {
  /** Clave única de la acción */
  key: string;
  /** Etiqueta de la acción */
  label: string;
  /** Icono de la acción */
  icon?: string;
  /** Variante de color */
  variant?: ComponentVariant;
  /** Función de la acción */
  onClick: (row: T, index: number) => void;
  /** Condición para mostrar la acción */
  condition?: (row: T) => boolean;
}

/**
 * Props para gráficos
 */
export interface ChartProps extends BaseComponentProps {
  /** Datos del gráfico */
  data: any[];
  /** Tipo de gráfico */
  type: 'line' | 'bar' | 'area' | 'pie' | 'doughnut' | 'scatter';
  /** Configuración del gráfico */
  config?: {
    /** Mostrar leyenda */
    showLegend?: boolean;
    /** Posición de la leyenda */
    legendPosition?: 'top' | 'bottom' | 'left' | 'right';
    /** Mostrar grid */
    showGrid?: boolean;
    /** Mostrar tooltips */
    showTooltips?: boolean;
    /** Animaciones */
    animations?: boolean;
    /** Colores personalizados */
    colors?: string[];
  };
  /** Altura del gráfico */
  height?: number;
  /** Estado de carga */
  loading?: boolean;
}

/**
 * Props para alertas/notificaciones
 */
export interface AlertProps extends BaseComponentProps {
  /** Tipo de alerta */
  type: 'success' | 'warning' | 'error' | 'info';
  /** Título de la alerta */
  title?: string;
  /** Mensaje de la alerta */
  message: string;
  /** Es dismissible */
  dismissible?: boolean;
  /** Función onDismiss */
  onDismiss?: () => void;
  /** Auto-dismiss después de X ms */
  autoDismiss?: number;
  /** Mostrar icono */
  showIcon?: boolean;
  /** Acciones adicionales */
  actions?: {
    label: string;
    onClick: () => void;
    variant?: ComponentVariant;
  }[];
}
```

## Tipos Utilitarios

### utils/helpers.ts - Tipos Helper

**Propósito**: Tipos utilitarios y helpers de TypeScript

```typescript
/**
 * Hace todas las propiedades opcionales excepto las especificadas
 */
export type PartialExcept<T, K extends keyof T> = Partial<T> & Pick<T, K>;

/**
 * Hace todas las propiedades requeridas excepto las especificadas
 */
export type RequiredExcept<T, K extends keyof T> = Required<T> & Partial<Pick<T, K>>;

/**
 * Extrae el tipo de los elementos de un array
 */
export type ArrayElement<T> = T extends (infer U)[] ? U : never;

/**
 * Extrae el tipo de retorno de una función
 */
export type ReturnTypeOf<T> = T extends (...args: any[]) => infer R ? R : never;

/**
 * Extrae el tipo de los parámetros de una función
 */
export type ParametersOf<T> = T extends (...args: infer P) => any ? P : never;

/**
 * Hace un tipo profundamente parcial
 */
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

/**
 * Hace un tipo profundamente requerido
 */
export type DeepRequired<T> = {
  [P in keyof T]-?: T[P] extends object ? DeepRequired<T[P]> : T[P];
};

/**
 * Tipo para valores que pueden ser null o undefined
 */
export type Nullable<T> = T | null;
export type Optional<T> = T | undefined;
export type Maybe<T> = T | null | undefined;

/**
 * Tipo para IDs
 */
export type ID = string | number;

/**
 * Tipo para timestamps
 */
export type Timestamp = string | number | Date;

/**
 * Tipo para coordenadas geográficas
 */
export interface Coordinates {
  lat: number;
  lng: number;
}

/**
 * Tipo para rangos de valores
 */
export interface Range<T = number> {
  min: T;
  max: T;
}

/**
 * Tipo para pares clave-valor
 */
export interface KeyValuePair<K = string, V = any> {
  key: K;
  value: V;
}

/**
 * Tipo para opciones de select
 */
export interface SelectOption<T = any> {
  label: string;
  value: T;
  disabled?: boolean;
  group?: string;
}

/**
 * Tipo para resultados de operaciones
 */
export type OperationResult<T = any, E = Error> = 
  | { success: true; data: T }
  | { success: false; error: E };

/**
 * Tipo para estados de carga
 */
export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

/**
 * Tipo para estados de operación asíncrona
 */
export interface AsyncState<T = any, E = Error> {
  data: T | null;
  loading: boolean;
  error: E | null;
  lastUpdated: Timestamp | null;
}

/**
 * Tipo para configuración de paginación
 */
export interface PaginationConfig {
  page: number;
  pageSize: number;
  total?: number;
}

/**
 * Tipo para configuración de ordenamiento
 */
export interface SortConfig<T = any> {
  field: keyof T;
  direction: 'asc' | 'desc';
}

/**
 * Tipo para configuración de filtros
 */
export interface FilterConfig<T = any> {
  field: keyof T;
  operator: 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte' | 'contains' | 'startsWith' | 'endsWith';
  value: any;
}

/**
 * Tipo para metadatos de consulta
 */
export interface QueryMetadata {
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
  queryTime?: number;
}

/**
 * Tipo para configuración de tema
 */
export interface ThemeConfig {
  mode: 'light' | 'dark';
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  backgroundColor: string;
  textColor: string;
  borderColor: string;
}

/**
 * Tipo para configuración de notificaciones
 */
export interface NotificationConfig {
  type: 'success' | 'warning' | 'error' | 'info';
  title?: string;
  message: string;
  duration?: number;
  persistent?: boolean;
  actions?: {
    label: string;
    action: () => void;
  }[];
}

/**
 * Tipo para eventos del sistema
 */
export interface SystemEvent<T = any> {
  id: string;
  type: string;
  source: string;
  timestamp: Timestamp;
  data: T;
  severity: 'low' | 'medium' | 'high' | 'critical';
  acknowledged?: boolean;
}

/**
 * Tipo para configuración de validación
 */
export interface ValidationRule<T = any> {
  field: keyof T;
  required?: boolean;
  type?: 'string' | 'number' | 'boolean' | 'date' | 'email' | 'url';
  min?: number;
  max?: number;
  pattern?: RegExp;
  custom?: (value: any) => string | null;
}

/**
 * Tipo para resultado de validación
 */
export interface ValidationResult {
  isValid: boolean;
  errors: {
    field: string;
    message: string;
  }[];
}

/**
 * Tipo para configuración de exportación
 */
export interface ExportConfig {
  format: 'csv' | 'xlsx' | 'json' | 'pdf';
  filename?: string;
  includeHeaders?: boolean;
  dateRange?: {
    from: Timestamp;
    to: Timestamp;
  };
  filters?: FilterConfig[];
  columns?: string[];
}

/**
 * Tipo para configuración de importación
 */
export interface ImportConfig {
  format: 'csv' | 'xlsx' | 'json';
  hasHeaders?: boolean;
  delimiter?: string;
  encoding?: string;
  mapping?: Record<string, string>;
  validation?: ValidationRule[];
}

/**
 * Tipo para resultado de importación
 */
export interface ImportResult<T = any> {
  successful: T[];
  failed: {
    row: number;
    data: any;
    errors: string[];
  }[];
  summary: {
    total: number;
    successful: number;
    failed: number;
    duration: number;
  };
}
```

## Tipos Globales

### global.d.ts - Declaraciones Globales

**Propósito**: Tipos y declaraciones globales de la aplicación

```typescript
/**
 * Declaraciones globales para la aplicación HydroEspinaca
 */

// Extensiones de tipos globales
declare global {
  /**
   * Variables de entorno
   */
  namespace NodeJS {
    interface ProcessEnv {
      NODE_ENV: 'development' | 'production' | 'test';
      NEXT_PUBLIC_API_URL: string;
      NEXT_PUBLIC_WS_URL: string;
      NEXT_PUBLIC_APP_NAME: string;
      NEXT_PUBLIC_APP_VERSION: string;
      DATABASE_URL: string;
      JWT_SECRET: string;
      SMTP_HOST: string;
      SMTP_PORT: string;
      SMTP_USER: string;
      SMTP_PASS: string;
    }
  }

  /**
   * Extensiones de Window para APIs del navegador
   */
  interface Window {
    /** API de notificaciones push */
    webkitNotifications?: any;
    /** API de geolocalización */
    navigator: Navigator & {
      geolocation: Geolocation;
    };
    /** Service Worker */
    serviceWorker?: ServiceWorkerContainer;
    /** Analytics */
    gtag?: (...args: any[]) => void;
    /** Configuración de la aplicación */
    __APP_CONFIG__?: {
      apiUrl: string;
      wsUrl: string;
      version: string;
      buildTime: string;
    };
  }

  /**
   * Módulos CSS
   */
  declare module '*.module.css' {
    const classes: { [key: string]: string };
    export default classes;
  }

  declare module '*.module.scss' {
    const classes: { [key: string]: string };
    export default classes;
  }

  /**
   * Archivos de imagen
   */
  declare module '*.svg' {
    const content: React.FunctionComponent<React.SVGAttributes<SVGElement>>;
    export default content;
  }

  declare module '*.png' {
    const content: string;
    export default content;
  }

  declare module '*.jpg' {
    const content: string;
    export default content;
  }

  declare module '*.jpeg' {
    const content: string;
    export default content;
  }

  declare module '*.gif' {
    const content: string;
    export default content;
  }

  declare module '*.webp' {
    const content: string;
    export default content;
  }

  /**
   * Archivos de datos
   */
  declare module '*.json' {
    const content: any;
    export default content;
  }
}

/**
 * Tipos para bibliotecas externas sin tipos
 */
declare module 'some-library-without-types' {
  export function someFunction(param: string): string;
}

/**
 * Constantes globales de la aplicación
 */
export const APP_CONSTANTS = {
  /** Nombre de la aplicación */
  APP_NAME: 'HydroEspinaca',
  /** Versión de la aplicación */
  VERSION: '1.0.0',
  /** Configuración de API */
  API: {
    BASE_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001',
    TIMEOUT: 30000,
    RETRY_ATTEMPTS: 3
  },
  /** Configuración de WebSocket */
  WEBSOCKET: {
    URL: process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:3001/ws',
    RECONNECT_INTERVAL: 5000,
    MAX_RECONNECT_ATTEMPTS: 10
  },
  /** Configuración de almacenamiento local */
  STORAGE: {
    PREFIX: 'hidro_espinaca_',
    KEYS: {
      AUTH_TOKEN: 'auth_token',
      USER_PREFERENCES: 'user_preferences',
      DASHBOARD_CONFIG: 'dashboard_config'
    }
  },
  /** Configuración de paginación */
  PAGINATION: {
    DEFAULT_PAGE_SIZE: 20,
    MAX_PAGE_SIZE: 100,
    PAGE_SIZE_OPTIONS: [10, 20, 50, 100]
  },
  /** Configuración de fechas */
  DATE_FORMATS: {
    DISPLAY: 'DD/MM/YYYY',
    DISPLAY_WITH_TIME: 'DD/MM/YYYY HH:mm',
    API: 'YYYY-MM-DD',
    API_WITH_TIME: 'YYYY-MM-DDTHH:mm:ss.SSSZ'
  },
  /** Configuración de validación */
  VALIDATION: {
    PASSWORD_MIN_LENGTH: 8,
    EMAIL_REGEX: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    PHONE_REGEX: /^[+]?[1-9]?[0-9]{7,15}$/
  },
  /** Configuración de archivos */
  FILES: {
    MAX_SIZE: 10 * 1024 * 1024, // 10MB
    ALLOWED_TYPES: ['image/jpeg', 'image/png', 'image/gif', 'application/pdf'],
    UPLOAD_CHUNK_SIZE: 1024 * 1024 // 1MB
  }
} as const;

/**
 * Tipos derivados de constantes
 */
export type AppConstant = typeof APP_CONSTANTS;
export type ApiConfig = typeof APP_CONSTANTS.API;
export type StorageKey = typeof APP_CONSTANTS.STORAGE.KEYS[keyof typeof APP_CONSTANTS.STORAGE.KEYS];

/**
 * Configuración de entorno
 */
export interface EnvironmentConfig {
  /** Entorno actual */
  NODE_ENV: 'development' | 'production' | 'test';
  /** URL base de la API */
  API_URL: string;
  /** URL del WebSocket */
  WS_URL: string;
  /** Configuración de base de datos */
  DATABASE_URL: string;
  /** Configuración de autenticación */
  JWT_SECRET: string;
  /** Configuración de email */
  SMTP_CONFIG: {
    host: string;
    port: number;
    user: string;
    pass: string;
  };
}

export {};
```

## Exportaciones Principales

### index.ts - Exportaciones Centralizadas

**Propósito**: Punto de entrada único para todos los tipos

```typescript
// Entidades del dominio
export * from './entities/sensor';
export * from './entities/actuator';
export * from './entities/user';
export * from './entities/reading';
export * from './entities/variable';
export * from './entities/alert';

// Tipos de API
export * from './api/requests';
export * from './api/responses';
export * from './api/errors';
export * from './api/pagination';

// Tipos de UI
export * from './ui/components';
export * from './ui/forms';
export * from './ui/tables';
export * from './ui/charts';

// Tipos de store
export * from './store/auth';
export * from './store/sensors';
export * from './store/readings';
export * from './store/common';

// Tipos utilitarios
export * from './utils/helpers';
export * from './utils/validators';
export * from './utils/formatters';

// Tipos globales
export * from './global';
```

## Patrones de Uso

### Composición de Tipos

```typescript
// Combinar tipos para crear nuevos tipos específicos
type SensorWithReadings = SensorData & {
  recentReadings: IndividualReading[];
  statistics: SensorSummary;
};

// Extender tipos base
interface ExtendedUser extends User {
  lastLoginDevice: string;
  securitySettings: {
    twoFactorEnabled: boolean;
    trustedDevices: string[];
  };
}

// Crear tipos condicionales
type ApiResponseType<T> = T extends 'error' 
  ? ApiErrorResponse 
  : ApiResponse<T>;
```

### Validación de Tipos en Runtime

```typescript
// Type guards para validación en runtime
export function isSensorData(obj: any): obj is SensorData {
  return (
    typeof obj === 'object' &&
    typeof obj.id === 'string' &&
    typeof obj.name === 'string' &&
    Object.values(SensorType).includes(obj.type) &&
    Object.values(SensorStatus).includes(obj.status)
  );
}

export function isValidReading(obj: any): obj is IndividualReading {
  return (
    typeof obj === 'object' &&
    typeof obj.id === 'string' &&
    typeof obj.sensorId === 'string' &&
    typeof obj.valor === 'number' &&
    typeof obj.fecha === 'string'
  );
}

// Función helper para validar arrays
export function validateArray<T>(
  array: unknown[],
  validator: (item: unknown) => item is T
): T[] {
  return array.filter(validator);
}
```

### Transformación de Tipos

```typescript
// Mappers para transformar entre tipos
export const sensorMappers = {
  // De API a tipo interno
  fromApi: (apiData: any): SensorData => ({
    id: apiData.id,
    name: apiData.name,
    type: apiData.type as SensorType,
    status: apiData.status as SensorStatus,
    location: apiData.location,
    config: {
      name: apiData.config.name,
      unit: apiData.config.unit,
      icon: apiData.config.icon,
      color: apiData.config.color,
      validRange: apiData.config.validRange,
      optimalRange: apiData.config.optimalRange,
      precision: apiData.config.precision,
      category: apiData.config.category as SensorCategory,
      readingInterval: apiData.config.readingInterval
    },
    currentValue: apiData.currentValue,
    lastReading: apiData.lastReading,
    alertConfig: apiData.alertConfig,
    metadata: apiData.metadata,
    createdAt: apiData.createdAt,
    updatedAt: apiData.updatedAt
  }),

  // De tipo interno a API
  toApi: (sensorData: SensorData): any => ({
    id: sensorData.id,
    name: sensorData.name,
    type: sensorData.type,
    status: sensorData.status,
    location: sensorData.location,
    config: sensorData.config,
    currentValue: sensorData.currentValue,
    lastReading: sensorData.lastReading,
    alertConfig: sensorData.alertConfig,
    metadata: sensorData.metadata
  }),

  // Para formularios
  toFormData: (sensorData: SensorData): SensorFormData => ({
    name: sensorData.name,
    type: sensorData.type,
    location: sensorData.location,
    description: sensorData.description || '',
    minThreshold: sensorData.alertConfig.minThreshold || 0,
    maxThreshold: sensorData.alertConfig.maxThreshold || 100,
    alertsEnabled: sensorData.alertConfig.enabled
  })
};
```

## Testing de Tipos

### Ejemplos de Tests de Tipos

```typescript
import { expectType, expectError } from 'tsd';
import { SensorData, SensorType, SensorStatus } from '../types';

// Test de tipos correctos
expectType<SensorData>({
  id: '1',
  name: 'Sensor 1',
  type: SensorType.TEMPERATURE,
  status: SensorStatus.ONLINE,
  location: 'Invernadero A',
  config: {
    name: 'Temperatura',
    unit: '°C',
    icon: 'thermometer',
    color: '#ef4444',
    validRange: { min: -10, max: 50 },
    optimalRange: { min: 18, max: 28 },
    precision: 1,
    category: SensorCategory.ENVIRONMENTAL,
    readingInterval: 60
  },
  currentValue: 25.5,
  lastReading: '2024-01-26T14:30:00Z',
  alertConfig: {
    enabled: true,
    minThreshold: 15,
    maxThreshold: 35
  },
  metadata: {
    installedAt: '2024-01-01T00:00:00Z',
    serialNumber: 'TEMP001'
  },
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-26T14:30:00Z'
});

// Test de errores de tipos
expectError<SensorData>({
  id: 1, // Error: debe ser string
  name: 'Sensor 1',
  type: 'invalid_type', // Error: tipo inválido
  status: SensorStatus.ONLINE
});
```

### Utilities para Testing

```typescript
// Factory functions para testing
export const createMockSensor = (overrides: Partial<SensorData> = {}): SensorData => ({
  id: 'mock-sensor-1',
  name: 'Mock Sensor',
  type: SensorType.TEMPERATURE,
  status: SensorStatus.ONLINE,
  location: 'Test Location',
  config: {
    name: 'Temperatura',
    unit: '°C',
    icon: 'thermometer',
    color: '#ef4444',
    validRange: { min: 0, max: 50 },
    optimalRange: { min: 18, max: 28 },
    precision: 1,
    category: SensorCategory.ENVIRONMENTAL,
    readingInterval: 60
  },
  currentValue: 25,
  lastReading: new Date().toISOString(),
  alertConfig: {
    enabled: true
  },
  metadata: {
    installedAt: new Date().toISOString()
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides
});

export const createMockReading = (overrides: Partial<IndividualReading> = {}): IndividualReading => ({
  id: 'mock-reading-1',
  sensorId: 'mock-sensor-1',
  sensor: 'Mock Sensor',
  type: SensorType.TEMPERATURE,
  valor: 25.5,
  unidad: '°C',
  fecha: new Date().toISOString(),
  quality: ReadingQuality.GOOD,
  ...overrides
});

export const createMockUser = (overrides: Partial<User> = {}): User => ({
  id: 'mock-user-1',
  email: 'test@example.com',
  name: 'Test User',
  role: UserRole.OPERATOR,
  permissions: [Permission.SENSORS_VIEW, Permission.READINGS_VIEW],
  status: 'active',
  preferences: {
    language: 'es',
    timezone: 'America/Mexico_City',
    dateFormat: 'DD/MM/YYYY',
    timeFormat: '24h',
    theme: 'light',
    notifications: {
      email: true,
      push: true,
      sms: false,
      alerts: {
        critical: true,
        warning: true,
        info: false
      }
    },
    dashboard: {
      visibleWidgets: ['sensors', 'readings', 'alerts'],
      widgetOrder: ['sensors', 'readings', 'alerts'],
      chartSettings: {
        defaultTimeRange: '24h',
        refreshInterval: 30000,
        showAnimations: true
      }
    }
  },
  metadata: {
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    failedLoginAttempts: 0,
    requiresPasswordChange: false
  },
  ...overrides
});
```

## Performance y Optimización

### Tipos Lazy y Dinámicos

```typescript
// Tipos lazy para componentes grandes
export type LazyComponentProps<T = {}> = T & {
  loading?: boolean;
  error?: Error;
  retry?: () => void;
};

// Tipos para datos paginados
export interface PaginatedData<T> {
  items: T[];
  hasMore: boolean;
  loadMore: () => Promise<void>;
  loading: boolean;
}

// Tipos para cache
export interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
  key: string;
}

export interface CacheManager<T> {
  get: (key: string) => T | null;
  set: (key: string, data: T, ttl?: number) => void;
  invalidate: (key: string) => void;
  clear: () => void;
}
```

### Optimización de Bundle

```typescript
// Re-exportaciones específicas para tree-shaking
export type { SensorData, SensorType, SensorStatus } from './entities/sensor';
export type { IndividualReading, ReadingQuality } from './entities/reading';
export type { User, UserRole, Permission } from './entities/user';

// Tipos condicionales para reducir bundle
export type ProductionTypes = NODE_ENV extends 'production' 
  ? Pick<SystemEvent, 'id' | 'type' | 'timestamp'>
  : SystemEvent;
```

## Migración y Versionado

### Estrategias de Migración

```typescript
// Tipos versionados
export namespace V1 {
  export interface SensorData {
    id: string;
    name: string;
    type: string;
    value: number;
  }
}

export namespace V2 {
  export interface SensorData {
    id: string;
    name: string;
    type: SensorType;
    currentValue: number;
    status: SensorStatus;
    location: string;
  }
}

// Funciones de migración
export const migrations = {
  v1ToV2: (v1Data: V1.SensorData): V2.SensorData => ({
    id: v1Data.id,
    name: v1Data.name,
    type: v1Data.type as SensorType,
    currentValue: v1Data.value,
    status: SensorStatus.ONLINE,
    location: 'Unknown'
  })
};
```

## Contribución

### Agregar Nuevos Tipos

1. **Identificar categoría**: ¿Entidad, API, UI, Store, Utility?
2. **Crear archivo**: En la carpeta correspondiente
3. **Documentar**: JSDoc completo con ejemplos
4. **Exportar**: Agregar a index.ts
5. **Testear**: Crear tests de tipos
6. **Validar**: Type guards si es necesario

### Template de Tipo

```typescript
/**
 * Descripción del tipo/interfaz
 * 
 * @example
 * ```typescript
 * const example: MyType = {
 *   property: 'value'
 * };
 * ```
 */
export interface MyType {
  /** Descripción de la propiedad */
  property: string;
  /** Propiedad opcional */
  optionalProperty?: number;
}

/**
 * Enum relacionado
 */
export enum MyEnum {
  VALUE_ONE = 'value_one',
  VALUE_TWO = 'value_two'
}

/**
 * Type guard para validación
 */
export function isMyType(obj: any): obj is MyType {
  return (
    typeof obj === 'object' &&
    typeof obj.property === 'string'
  );
}
```

### Code Review Checklist

- [ ] Tipos bien documentados con JSDoc
- [ ] Ejemplos de uso incluidos
- [ ] Nomenclatura consistente
- [ ] Exportaciones agregadas a index.ts
- [ ] Type guards implementados si es necesario
- [ ] Tests de tipos creados
- [ ] Compatibilidad con tipos existentes
- [ ] Performance considerada

## Recursos

- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Utility Types](https://www.typescriptlang.org/docs/handbook/utility-types.html)
- [Advanced Types](https://www.typescriptlang.org/docs/handbook/2/types-from-types.html)
- [Type Guards](https://www.typescriptlang.org/docs/handbook/2/narrowing.html)
- [Conditional Types](https://www.typescriptlang.org/docs/handbook/2/conditional-types.html)