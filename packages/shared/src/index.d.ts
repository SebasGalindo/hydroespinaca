import * as zustand from 'zustand';
import React$1, { FormEvent } from 'react';

interface User$1 {
    id: string;
    email: string;
    name: string;
}
interface AuthState {
    user: User$1 | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: string | null;
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
    clearError: () => void;
}
declare const useAuthStore: zustand.UseBoundStore<zustand.StoreApi<AuthState>>;

interface SensorData {
    timestamp: string;
    temperature: number;
    humidity: number;
    ph: number;
    light: number;
    conductivity: number;
}
interface IndividualSensorData$1 {
    id?: string;
    idFisico: string;
    ubicacion: string;
    esp32Id: string;
    frecuenciaLectura: number;
    variablesAMedir: string[];
    unidadMedida: string;
    rangoMinimo: number;
    rangoMaximo: number;
    rangoOptimoMinimo: number;
    rangoOptimoMaximo: number;
    estado: 'activo' | 'inactivo';
    createdAt?: string;
    lastModified?: string;
}
interface MetricData {
    title: string;
    value: string;
    unit: string;
    status: 'optimal' | 'warning' | 'critical';
    trend: 'up' | 'down' | 'stable';
    change: string;
    iconType: 'temperature' | 'humidity' | 'ph' | 'light' | 'sun' | 'electric' | 'ruler' | 'water';
}
interface SystemComponent {
    name: string;
    status: 'online' | 'warning' | 'offline';
    lastUpdate: string;
    details?: string;
}
interface SensorState {
    sensorData: SensorData[];
    currentMetrics: MetricData[];
    systemComponents: SystemComponent[];
    individualSensors: IndividualSensorData$1[];
    timeRange: '1h' | '6h' | '24h' | '7d';
    isRealTime: boolean;
    loading: boolean;
    setSensorData: (data: SensorData[]) => void;
    addSensorDataPoint: (dataPoint: SensorData) => void;
    setTimeRange: (range: '1h' | '6h' | '24h' | '7d') => void;
    setIsRealTime: (isRealTime: boolean) => void;
    setLoading: (loading: boolean) => void;
    generateMockData: () => void;
    updateCurrentMetrics: () => void;
    initializeSystemComponents: () => void;
    updateSystemComponentStatus: (name: string, status: 'online' | 'warning' | 'offline', lastUpdate: string, details?: string) => void;
    addIndividualSensor: (sensor: IndividualSensorData$1) => void;
    updateIndividualSensor: (sensorId: string, sensor: IndividualSensorData$1) => void;
    removeIndividualSensor: (sensorId: string) => void;
    initializeIndividualSensors: () => void;
}
declare const useSensorStore: zustand.UseBoundStore<zustand.StoreApi<SensorState>>;

interface Alert$1 {
    id: string;
    type: 'info' | 'warning' | 'error' | 'success';
    title: string;
    message: string;
    timestamp: string;
    isRead: boolean;
    sensor?: string;
}
interface AlertState {
    alerts: Alert$1[];
    filter: 'all' | 'unread' | 'warning' | 'error';
    filteredAlerts: Alert$1[];
    unreadCount: number;
    addAlert: (alert: Omit<Alert$1, 'id'>) => void;
    markAsRead: (alertId: string) => void;
    markAllAsRead: () => void;
    removeAlert: (alertId: string) => void;
    clearAllAlerts: () => void;
    setFilter: (filter: 'all' | 'unread' | 'warning' | 'error') => void;
    initializeAlerts: () => void;
    getFilteredAlerts: () => Alert$1[];
    getUnreadCount: () => number;
}
declare const useAlertStore: zustand.UseBoundStore<zustand.StoreApi<AlertState>>;

interface ActuadorData {
    id: string;
    name: string;
    type: 'pump' | 'valve' | 'fan' | 'heater' | 'light' | 'motor';
    location: string;
    pin: number;
    esp32Id: string;
    status: 'active' | 'inactive' | 'error' | 'maintenance';
    createdAt: string;
    lastModified: string;
}
interface ActuatorState {
    actuadores: ActuadorData[];
    addActuador: (actuador: Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'>) => void;
    updateActuador: (id: string, updates: Partial<Omit<ActuadorData, 'id' | 'createdAt'>>) => void;
    removeActuador: (id: string) => void;
    initializeActuadores: () => void;
}
declare const useActuatorStore: zustand.UseBoundStore<zustand.StoreApi<ActuatorState>>;

interface IconProps {
    size?: number;
    color?: string;
    className?: string;
    style?: any;
    testID?: string;
}
export type IconName = 'home' | 'settings' | 'user' | 'close' | 'menu' | 'search' | 'plus' | 'minus' | 'check' | 'arrow-left' | 'arrow-right' | 'heart' | 'star' | 'bell' | 'mail' | 'phone' | 'camera' | 'edit' | 'delete' | 'save' | 'refresh' | 'download' | 'upload' | 'lock' | 'unlock' | 'eye' | 'eye-off' | 'calendar' | 'clock' | 'location' | 'wifi' | 'battery' | 'power' | 'warning' | 'info' | 'error' | 'success' | 'chevron-back' | 'chevron-forward' | 'play-skip-back' | 'play-skip-forward' | 'chart' | 'book' | 'trending-up' | 'brain' | 'temperature' | 'light' | 'humidity' | 'ph' | 'sun' | 'electric' | 'ruler' | 'water';
interface IconComponent {
    (props: IconProps): React.ReactElement;
}

type EntityStatus = 'active' | 'inactive' | 'deprecated';
type FieldType = 'text' | 'badge' | 'number' | 'date';
type ActionType = 'edit' | 'delete' | 'view' | 'custom';
type ActionVariant = 'primary' | 'secondary' | 'danger';
interface FieldConfig {
    key: string;
    label: string;
    type?: FieldType;
    variant?: 'default' | 'info' | 'success' | 'warning' | 'danger';
}
interface InfoField {
    key?: string;
    label: string;
    value: string;
    type?: FieldType;
    variant?: 'default' | 'info' | 'success' | 'warning' | 'danger';
}
interface EntityAction {
    id: string;
    type?: string;
    label: string;
    variant?: 'primary' | 'secondary' | 'danger';
    icon?: IconName;
    disabled?: boolean;
    onClick: () => void;
}
interface BaseEntity {
    id: string;
    title: string;
    subtitle?: string;
    identifier?: string;
    description?: string;
    status: EntityStatus;
    modifiedDate: string;
    fields: InfoField[];
}
interface EntityConfig {
    entityType: 'variable' | 'sensor' | 'actuator' | 'custom';
    titleField?: string;
    subtitleField?: string;
    technicalIdField?: string;
    statusField?: string;
    lastModifiedField?: string;
    infoFields?: FieldConfig[];
    showIdentifier?: boolean;
    showDescription?: boolean;
    showModifiedDate?: boolean;
    customFields?: string[];
    defaultActions?: EntityAction[];
}
interface PaginationProps {
    currentPage: number;
    totalPages: number;
    totalItems: number;
    itemsPerPage: number;
    loading?: boolean;
    onPageChange: (page: number) => void;
    onItemsPerPageChange?: (itemsPerPage: number) => void;
}
interface CrudListProps<T extends BaseEntity> {
    title?: string;
    subtitle?: string;
    data: T[];
    config: EntityConfig;
    pagination?: PaginationProps;
    loading?: boolean;
    error?: string;
    emptyMessage?: string;
    onRefresh?: () => void;
    refreshing?: boolean;
    getActions?: (item: T) => EntityAction[];
    onItemPress?: (item: T) => void;
}
interface Variable extends BaseEntity {
    unit: string;
    type: 'input' | 'output' | 'calculated';
    dataType: 'numeric' | 'boolean' | 'text';
    minValue?: number;
    maxValue?: number;
    isRequired: boolean;
    category: 'environmental' | 'control' | 'system' | 'user';
}
interface Actuator extends BaseEntity {
    type: 'pump' | 'valve' | 'fan' | 'heater' | 'other';
    location: string;
    powerRating?: string;
    controlType: 'manual' | 'automatic';
}
interface DisabledTextProps {
    textDisabled?: string;
}

interface VariableData extends Omit<Variable, 'title' | 'subtitle' | 'identifier' | 'fields' | 'modifiedDate'> {
    name: string;
    status: 'active' | 'inactive' | 'deprecated';
    createdAt: string;
    lastModified: string;
}
interface VariableState {
    variables: VariableData[];
    loading: boolean;
    setVariables: (variables: VariableData[]) => void;
    addVariable: (variable: Omit<VariableData, 'id' | 'createdAt' | 'lastModified'>) => void;
    updateVariable: (id: string, updates: Partial<VariableData>) => void;
    removeVariable: (id: string) => void;
    setLoading: (loading: boolean) => void;
    initializeVariables: () => void;
    getVariableById: (id: string) => VariableData | undefined;
    getVariablesByType: (type: VariableData['type']) => VariableData[];
    getVariablesByCategory: (category: VariableData['category']) => VariableData[];
    getActiveVariables: () => VariableData[];
    getRequiredVariables: () => VariableData[];
}
declare const useVariableStore: zustand.UseBoundStore<zustand.StoreApi<VariableState>>;

interface SensorSummary {
    sensor: string;
    media: number;
    minimo: number;
    maximo: number;
    unidad: string;
    ultimaLectura: string;
}
interface IndividualReading {
    id: string;
    sensor: string;
    valor: number;
    unidad: string;
    fecha: string;
}
interface ReadingsState {
    sensorSummary: SensorSummary[];
    individualReadings: IndividualReading[];
    setSensorSummary: (summary: SensorSummary[]) => void;
    setIndividualReadings: (readings: IndividualReading[]) => void;
    addReading: (reading: IndividualReading) => void;
    initializeReadings: () => void;
}
declare const useReadingsStore: zustand.UseBoundStore<zustand.StoreApi<ReadingsState>>;

interface ObjectId {
    $oid: string;
}
interface MongoDate {
    $date: string;
}
interface FuzzyOperators {
    and: 'min' | 'product';
    or: 'max' | 'sum';
    not: 'complement';
}
interface FuzzySystem {
    _id: ObjectId;
    name: string;
    status: 'ACTIVE' | 'INACTIVE';
    defuzzification_method: 'centroid' | 'bisector' | 'mom' | 'som' | 'lom';
    operators: FuzzyOperators;
    input_variable_ids: string[];
    output_variable_ids: string[];
    rule_ids: string[];
    created_at: MongoDate;
    updated_at: MongoDate;
    created_by: string | null;
}
interface FuzzyVariable {
    _id: ObjectId;
    name: string;
    description: string;
    variable_type: 'input' | 'output';
    device_id: string;
    terms: string[];
    created_at: MongoDate;
    updated_at: MongoDate;
}
interface MembershipFunction {
    function_type: 'triangular' | 'trapezoidal' | 'gaussian' | 'sigmoid';
    parameters: number[];
    universe_min: number;
    universe_max: number;
}
interface FuzzyTerm {
    _id: ObjectId;
    variable_id: string;
    label: string;
    membership_function: MembershipFunction;
    created_at: MongoDate;
    updated_at: MongoDate;
}
interface RuleCondition {
    variableId: string;
    operator: 'IS' | 'IS_NOT';
    value: string;
}
interface FuzzyRule {
    _id: ObjectId;
    name: string;
    system_id: string;
    description: string;
    conditions: RuleCondition[];
    connectors: ('AND' | 'OR')[];
    consequent: string;
    created_at: MongoDate;
}
interface RoutineStep {
    step_id: number;
    condition: string;
    power_term_id: string;
    duration_term_id: string;
}
interface FuzzyRoutine {
    _id: ObjectId;
    routine_name: string;
    created_at: MongoDate;
    steps: RoutineStep[];
}
interface SimpleFuzzySystem {
    id: string;
    name: string;
    status: 'ACTIVE' | 'INACTIVE';
    defuzzification_method: string;
    operators: FuzzyOperators;
    input_variables: SimpleFuzzyVariable[];
    output_variables: SimpleFuzzyVariable[];
    rules: SimpleFuzzyRule[];
    created_at: string;
    updated_at: string;
    created_by: string | null;
}
interface SimpleFuzzyVariable {
    id: string;
    system_id: string;
    name: string;
    description: string;
    variable_type: 'input' | 'output';
    device_id: string;
    created_at: string;
    updated_at: string;
}
interface SimpleFuzzyTerm {
    id: string;
    variable_id: string;
    label: string;
    membership_function: MembershipFunction;
    created_at: string;
    updated_at: string;
}
interface SimpleFuzzyRule {
    id: string;
    name: string;
    system_id: string;
    description: string;
    conditions: RuleCondition[];
    connectors: ('AND' | 'OR')[];
    routine: SimpleFuzzyRoutine;
    created_at: string;
}
interface SimpleFuzzyRoutine {
    id: string;
    system_id: string;
    routine_name: string;
    created_at: string;
    steps: RoutineStep[];
}
interface FuzzySystemState {
    fuzzySystems: SimpleFuzzySystem[];
    fuzzyVariables: SimpleFuzzyVariable[];
    fuzzyTerms: SimpleFuzzyTerm[];
    fuzzyRules: SimpleFuzzyRule[];
    fuzzyRoutines: SimpleFuzzyRoutine[];
    loading: boolean;
    selectedSystem: SimpleFuzzySystem | null;
}
interface FuzzySystemActions {
    setFuzzySystems: (systems: SimpleFuzzySystem[]) => void;
    setFuzzyVariables: (variables: SimpleFuzzyVariable[]) => void;
    setFuzzyTerms: (terms: SimpleFuzzyTerm[]) => void;
    setFuzzyRules: (rules: SimpleFuzzyRule[]) => void;
    setFuzzyRoutines: (routines: SimpleFuzzyRoutine[]) => void;
    setLoading: (loading: boolean) => void;
    initializeFuzzyData: () => void;
    addFuzzySystem: (system: SimpleFuzzySystem) => void;
    addFuzzyVariable: (variable: SimpleFuzzyVariable) => void;
    addFuzzyTerm: (term: SimpleFuzzyTerm) => void;
    addFuzzyRule: (rule: SimpleFuzzyRule) => void;
    addFuzzyRoutine: (routine: SimpleFuzzyRoutine) => void;
    updateFuzzySystem: (id: string, updates: Partial<SimpleFuzzySystem>) => void;
    updateFuzzyVariable: (id: string, updates: Partial<SimpleFuzzyVariable>) => void;
    updateFuzzyTerm: (id: string, updates: Partial<SimpleFuzzyTerm>) => void;
    updateFuzzyRule: (id: string, updates: Partial<SimpleFuzzyRule>) => void;
    updateFuzzyRoutine: (id: string, updates: Partial<SimpleFuzzyRoutine>) => void;
    removeFuzzySystem: (id: string) => void;
    removeFuzzyVariable: (id: string) => void;
    removeFuzzyTerm: (id: string) => void;
    removeFuzzyRule: (id: string) => void;
    removeFuzzyRoutine: (id: string) => void;
    getFuzzySystemById: (id: string) => SimpleFuzzySystem | undefined;
    getFuzzyVariableById: (id: string) => SimpleFuzzyVariable | undefined;
    getFuzzyTermById: (id: string) => SimpleFuzzyTerm | undefined;
    getFuzzyRuleById: (id: string) => SimpleFuzzyRule | undefined;
    getFuzzyRoutineById: (id: string) => SimpleFuzzyRoutine | undefined;
    getActiveFuzzySystems: () => SimpleFuzzySystem[];
    getInactiveFuzzySystems: () => SimpleFuzzySystem[];
    getVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
    getInputVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
    getOutputVariablesBySystemId: (systemId: string) => SimpleFuzzyVariable[];
    getTermsByVariableId: (variableId: string) => SimpleFuzzyTerm[];
    getRulesBySystemId: (systemId: string) => SimpleFuzzyRule[];
    getRoutinesBySystemId: (systemId: string) => SimpleFuzzyRoutine[];
}
interface FuzzySystemStore extends FuzzySystemState, FuzzySystemActions {
}

declare const useFuzzyStore: zustand.UseBoundStore<zustand.StoreApi<FuzzySystemStore>>;

interface TimeSeriesDataPoint {
    value: number;
    timestamp: string;
    label?: string;
}
interface ScatterDataPoint {
    value: number;
    value1: number;
    label?: string;
}
interface ChartVariable {
    id: string;
    name: string;
    unit: string;
    color?: string;
    baseValue: number;
    range: number;
}
declare const CHART_VARIABLES: Record<string, ChartVariable>;
interface DashboardState {
    timeSeriesCache: Map<string, TimeSeriesDataPoint[]>;
    scatterCache: Map<string, ScatterDataPoint[]>;
    loading: boolean;
    error: string | null;
    fetchTimeSeriesData: (variableId: string, days?: number) => Promise<void>;
    fetchScatterData: (variableXId: string, variableYId: string, points?: number) => Promise<void>;
    clearCache: () => void;
    setLoading: (loading: boolean) => void;
    setError: (error: string | null) => void;
    getTimeSeriesData: (variableId: string) => TimeSeriesDataPoint[] | null;
    getScatterData: (variableXId: string, variableYId: string) => ScatterDataPoint[] | null;
}
declare const useDashboardStore: zustand.UseBoundStore<zustand.StoreApi<DashboardState>>;

interface IndividualSensorData {
    id?: string;
    idFisico: string;
    ubicacion: string;
    esp32Id: string;
    frecuenciaLectura: number;
    variablesAMedir: string[];
    unidadMedida: string;
    rangoMinimo: number;
    rangoMaximo: number;
    rangoOptimoMinimo: number;
    rangoOptimoMaximo: number;
    estado: 'activo' | 'inactivo';
    createdAt?: string;
    lastModified?: string;
}
interface ExtendedSensorState {
    individualSensors: IndividualSensorData[];
    addIndividualSensor: (sensor: IndividualSensorData) => void;
    updateIndividualSensor: (sensorId: string, sensor: IndividualSensorData) => void;
    removeIndividualSensor: (sensorId: string) => void;
    initializeIndividualSensors: () => void;
}

interface User {
    id: string;
    email: string;
    name: string;
    role?: 'admin' | 'user';
}
interface AuthResponse {
    user: User;
    token: string;
}
interface HydroponicCrop {
    id: string;
    name: string;
    type: string;
    startDate: string;
    status: 'active' | 'harvested' | 'failed';
    currentPhase: string;
    estimatedHarvestDate: string;
}
interface SensorReading {
    id: string;
    sensorId: string;
    sensorType: 'ph' | 'temperature' | 'humidity' | 'nutrient' | 'light';
    value: number;
    unit: string;
    timestamp: string;
    cropId?: string;
}
interface Sensor {
    id: string;
    name: string;
    type: 'ph' | 'temperature' | 'humidity' | 'nutrient' | 'light';
    status: 'active' | 'inactive' | 'maintenance';
    lastReading?: SensorReading;
}
interface Alert {
    id: string;
    type: 'warning' | 'critical' | 'info';
    message: string;
    timestamp: string;
    read: boolean;
    sensorId?: string;
    cropId?: string;
}
interface Task {
    id: string;
    title: string;
    description: string;
    dueDate: string;
    status: 'pending' | 'in_progress' | 'completed';
    priority: 'low' | 'medium' | 'high';
    assignedTo?: string;
    cropId?: string;
}
interface Theme {
    colors: Record<string, any>;
    typography: Record<string, any>;
    textStyles: Record<string, any>;
    spacing: Record<string, any>;
    borderRadius: Record<string, any>;
    elevation: Record<string, any>;
    isDark: boolean;
}

interface LoginFormState {
    email: string;
    password: string;
}
interface UseLoginFormReturn {
    formState: LoginFormState;
    isLoading: boolean;
    error: string | null;
    handleChange: (e: React.ChangeEvent<HTMLInputElement> | {
        target: {
            name: string;
            value: string;
        };
    }) => void;
    handleSubmit: (e: FormEvent | {
        preventDefault: () => void;
    }) => Promise<void>;
    clearError: () => void;
}
declare const useLoginForm: () => UseLoginFormReturn;

/**
 * Formatea una fecha en formato legible
 * @param dateString - String de fecha ISO
 * @returns Fecha formateada
 */
declare const formatDate: (dateString: string) => string;
/**
 * Formatea un valor de sensor con su unidad
 * @param value - Valor numérico
 * @param unit - Unidad de medida
 * @returns Valor formateado con unidad
 */
declare const formatSensorValue: (value: number, unit: string) => string;
/**
 * Trunca un texto a una longitud máxima
 * @param text - Texto a truncar
 * @param maxLength - Longitud máxima
 * @returns Texto truncado
 */
declare const truncateText: (text: string, maxLength: number) => string;
/**
 * Formatea un número como moneda
 * @param amount - Cantidad
 * @param currency - Código de moneda
 * @returns Cantidad formateada como moneda
 */
declare const formatCurrency: (amount: number, currency?: string) => string;

/**
 * Valida un correo electrónico
 * @param email - Correo electrónico a validar
 * @returns true si el correo es válido, false en caso contrario
 */
declare const isValidEmail: (email: string) => boolean;
/**
 * Valida una contraseña según criterios de seguridad
 * @param password - Contraseña a validar
 * @returns Objeto con resultado y mensaje de error si aplica
 */
declare const validatePassword: (password: string) => {
    isValid: boolean;
    message?: string;
};
/**
 * Valida un valor de sensor según su tipo
 * @param value - Valor del sensor
 * @param sensorType - Tipo de sensor
 * @returns true si el valor es válido para ese tipo de sensor, false en caso contrario
 */
declare const isValidSensorValue: (value: number, sensorType: "ph" | "temperature" | "humidity" | "nutrient" | "light") => boolean;
/**
 * Valida si una fecha es futura
 * @param dateString - Fecha en formato string
 * @returns true si la fecha es futura, false en caso contrario
 */
declare const isFutureDate: (dateString: string) => boolean;
/**
 * Valida las credenciales de login
 * @param email - Correo electrónico del usuario
 * @param password - Contraseña del usuario
 * @returns Objeto con resultado de validación y mensaje de error si aplica
 */
declare const validateLoginCredentials: (email: string, password: string) => {
    isValid: boolean;
    message?: string;
};

declare const colors: {
    primary: {
        50: string;
        100: string;
        500: string;
        600: string;
        700: string;
        800: string;
        900: string;
    };
    gray: {
        50: string;
        100: string;
        200: string;
        300: string;
        400: string;
        500: string;
        600: string;
        700: string;
        800: string;
        900: string;
    };
    success: string;
    warning: string;
    error: string;
    info: string;
    hidro: {
        primary: string;
        light: string;
        bg: string;
        bgLight: string;
        bgPale: string;
        hover: string;
    };
    white: string;
    black: string;
    transparent: string;
    overlay: string;
};
declare const semanticColors: {
    textPrimary: string;
    textSecondary: string;
    textMuted: string;
    textInverse: string;
    textPlaceholder: string;
    textDisabled: string;
    background: string;
    backgroundSecondary: string;
    backgroundMuted: string;
    backgroundPrimary: string;
    surface: string;
    surfaceElevated: string;
    border: string;
    borderMuted: string;
    borderFocus: string;
    borderDisabled: string;
    primary: string;
    successText: string;
    successBg: string;
    warningText: string;
    warningBg: string;
    errorText: string;
    errorBg: string;
    infoText: string;
    infoBg: string;
    destructiveText: string;
    destructiveBg: string;
};
type ColorToken = keyof typeof colors;
type SemanticColorToken = keyof typeof semanticColors;

declare const typography: {
    fontFamily: {
        primary: string;
        mono: string;
        icons: string;
    };
    fontSize: {
        xs: number;
        sm: number;
        base: number;
        md: number;
        lg: number;
        xl: number;
        '2xl': number;
        '3xl': number;
        '4xl': number;
        '5xl': number;
    };
    fontWeight: {
        normal: string;
        medium: string;
        semibold: string;
        bold: string;
        extrabold: string;
    };
    lineHeight: {
        tight: number;
        normal: number;
        relaxed: number;
        loose: number;
    };
    letterSpacing: {
        tighter: string;
        tight: string;
        normal: string;
        wide: string;
        wider: string;
        widest: string;
    };
};
declare const textStyles: {
    h1: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    h2: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    h3: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    h4: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    h5: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    h6: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    body: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    bodyLarge: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    bodySmall: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    label: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    labelLarge: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    caption: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    button: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    buttonSmall: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    buttonLarge: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
    navItem: {
        fontSize: number;
        fontWeight: string;
        lineHeight: number;
    };
};
type TypographyToken = keyof typeof typography;
type TextStyleToken = keyof typeof textStyles;

declare const spacing: {
    xs: number;
    sm: number;
    md: number;
    lg: number;
    xl: number;
    '2xl': number;
    '3xl': number;
    '4xl': number;
    '5xl': number;
    '6xl': number;
    '7xl': number;
    '8xl': number;
};
declare const componentSpacing: {
    buttonPadding: {
        sm: {
            horizontal: number;
            vertical: number;
        };
        md: {
            horizontal: number;
            vertical: number;
        };
        lg: {
            horizontal: number;
            vertical: number;
        };
    };
    cardPadding: {
        sm: number;
        md: number;
        lg: number;
    };
    formSpacing: {
        fieldGap: number;
        labelGap: number;
        sectionGap: number;
    };
    listSpacing: {
        itemGap: number;
        sectionGap: number;
    };
    navigationSpacing: {
        tabPadding: number;
        tabGap: number;
        headerPadding: number;
    };
};
declare const borderRadius: {
    none: number;
    sm: number;
    md: number;
    lg: number;
    xl: number;
    '2xl': number;
    '3xl': number;
    full: number;
};
declare const elevation: {
    none: {
        shadowColor: string;
        shadowOffset: {
            width: number;
            height: number;
        };
        shadowOpacity: number;
        shadowRadius: number;
        elevation: number;
    };
    sm: {
        shadowColor: string;
        shadowOffset: {
            width: number;
            height: number;
        };
        shadowOpacity: number;
        shadowRadius: number;
        elevation: number;
    };
    md: {
        shadowColor: string;
        shadowOffset: {
            width: number;
            height: number;
        };
        shadowOpacity: number;
        shadowRadius: number;
        elevation: number;
    };
    lg: {
        shadowColor: string;
        shadowOffset: {
            width: number;
            height: number;
        };
        shadowOpacity: number;
        shadowRadius: number;
        elevation: number;
    };
    xl: {
        shadowColor: string;
        shadowOffset: {
            width: number;
            height: number;
        };
        shadowOpacity: number;
        shadowRadius: number;
        elevation: number;
    };
};
declare const zIndex: {
    base: number;
    dropdown: number;
    sticky: number;
    fixed: number;
    modal: number;
    popover: number;
    tooltip: number;
    toast: number;
};
type SpacingToken = keyof typeof spacing;
type BorderRadiusToken = keyof typeof borderRadius;
type ElevationToken = keyof typeof elevation;
type ZIndexToken = keyof typeof zIndex;

declare const baseTokens: {
    colors: {
        primary: string;
        secondary: string;
        success: string;
        warning: string;
        error: string;
        info: string;
        background: string;
        surface: string;
        text: string;
    };
    spacing: {
        xs: number;
        sm: number;
        md: number;
        lg: number;
        xl: number;
    };
    typography: {
        fontFamily: string;
        fontSize: {
            sm: number;
            md: number;
            lg: number;
            xl: number;
        };
    };
    borderRadius: {
        sm: number;
        md: number;
        lg: number;
    };
};
type BaseTokens = typeof baseTokens;

/**
 * Fetch sensor summary data from API
 * @returns Promise<SensorSummary[]>
 */
declare function fetchSensorSummary(): Promise<SensorSummary[]>;
/**
 * Fetch individual readings from API
 * @returns Promise<IndividualReading[]>
 */
declare function fetchIndividualReadings(): Promise<IndividualReading[]>;
/**
 * Fetch actuators data from API
 * @returns Promise<ActuadorData[]>
 */
declare function fetchActuators(): Promise<ActuadorData[]>;
/**
 * Create new actuator via API
 * @param actuator ActuadorData
 * @returns Promise<ActuadorData>
 */
declare function createActuator(actuator: Omit<ActuadorData, 'id'>): Promise<ActuadorData>;
/**
 * Update actuator via API
 * @param id string
 * @param actuator Partial<ActuadorData>
 * @returns Promise<ActuadorData>
 */
declare function updateActuator(id: string, actuator: Partial<ActuadorData>): Promise<ActuadorData>;
/**
 * Delete actuator via API
 * @param id string
 * @returns Promise<void>
 */
declare function deleteActuator(id: string): Promise<void>;
/**
 * Fetch sensor data for monitoring dashboard
 * @returns Promise<SensorData[]>
 */
declare function fetchSensorData(): Promise<SensorData[]>;
/**
 * Fetch metrics data for dashboard
 * @returns Promise<MetricData[]>
 */
declare function fetchMetrics(): Promise<MetricData[]>;
/**
 * Fetch variables configuration from API
 * @returns Promise<VariableData[]>
 */
declare function fetchVariables(): Promise<VariableData[]>;
/**
 * Create new variable via API
 * @param variable VariableData
 * @returns Promise<VariableData>
 */
declare function createVariable(variable: Omit<VariableData, 'id'>): Promise<VariableData>;
/**
 * Update variable via API
 * @param id string
 * @param variable Partial<VariableData>
 * @returns Promise<VariableData>
 */
declare function updateVariable(id: string, variable: Partial<VariableData>): Promise<VariableData>;
/**
 * Delete variable via API
 * @param id string
 * @returns Promise<void>
 */
declare function deleteVariable(id: string): Promise<void>;

interface WelcomeMessageProps {
    userName?: string;
    appName?: string;
}
declare const getWelcomeMessage: ({ userName, appName }?: WelcomeMessageProps) => string;
declare const getAppInfo: () => {
    name: string;
    version: string;
    description: string;
};

interface BaseIconProps {
    name: IconName;
    size?: number;
    color?: string;
    stroke?: string | undefined;
    strokeWidth?: number;
    fill?: string | undefined;
    platform?: 'web' | 'mobile';
    className?: string;
    style?: any;
    testID?: string;
}
declare const Icon: React$1.FC<BaseIconProps>;

declare const HomeIcon: React$1.FC<IconProps>;
declare const SettingsIcon: React$1.FC<IconProps>;
declare const UserIcon: React$1.FC<IconProps>;
declare const CloseIcon: React$1.FC<IconProps>;
declare const MenuIcon: React$1.FC<IconProps>;
declare const SearchIcon: React$1.FC<IconProps>;
declare const PlusIcon: React$1.FC<IconProps>;
declare const MinusIcon: React$1.FC<IconProps>;
declare const CheckIcon: React$1.FC<IconProps>;
declare const ArrowLeftIcon: React$1.FC<IconProps>;
declare const ArrowRightIcon: React$1.FC<IconProps>;
declare const HeartIcon: React$1.FC<IconProps>;
declare const StarIcon: React$1.FC<IconProps>;
declare const BellIcon: React$1.FC<IconProps>;
declare const MailIcon: React$1.FC<IconProps>;
declare const PhoneIcon: React$1.FC<IconProps>;
declare const CameraIcon: React$1.FC<IconProps>;
declare const EditIcon: React$1.FC<IconProps>;
declare const DeleteIcon: React$1.FC<IconProps>;
declare const SaveIcon: React$1.FC<IconProps>;
declare const RefreshIcon: React$1.FC<IconProps>;
declare const DownloadIcon: React$1.FC<IconProps>;
declare const UploadIcon: React$1.FC<IconProps>;
declare const LockIcon: React$1.FC<IconProps>;
declare const UnlockIcon: React$1.FC<IconProps>;
declare const EyeIcon: React$1.FC<IconProps>;
declare const EyeOffIcon: React$1.FC<IconProps>;
declare const CalendarIcon: React$1.FC<IconProps>;
declare const ClockIcon: React$1.FC<IconProps>;
declare const LocationIcon: React$1.FC<IconProps>;
declare const WifiIcon: React$1.FC<IconProps>;
declare const BatteryIcon: React$1.FC<IconProps>;
declare const PowerIcon: React$1.FC<IconProps>;
declare const WarningIcon: React$1.FC<IconProps>;
declare const InfoIcon: React$1.FC<IconProps>;
declare const ErrorIcon: React$1.FC<IconProps>;
declare const SuccessIcon: React$1.FC<IconProps>;
declare const ChevronBackIcon: React$1.FC<IconProps>;
declare const ChevronForwardIcon: React$1.FC<IconProps>;
declare const PlaySkipBackIcon: React$1.FC<IconProps>;
declare const PlaySkipForwardIcon: React$1.FC<IconProps>;
declare const HumidityIcon: React$1.FC<IconProps>;
declare const PhIcon: React$1.FC<IconProps>;
declare const SunIcon: React$1.FC<IconProps>;
declare const ElectricIcon: React$1.FC<IconProps>;
declare const LightIcon: React$1.FC<IconProps>;
declare const RulerIcon: React$1.FC<IconProps>;
declare const WaterIcon: React$1.FC<IconProps>;

declare const svgPaths: {
    readonly home: "M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M9 22V12h6v10";
    readonly settings: "M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z";
    readonly user: "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z";
    readonly close: "M18 6L6 18 M6 6l12 12";
    readonly menu: "M3 12h18 M3 6h18 M3 18h18";
    readonly search: "M21 21l-6-6m2-5a7 7 0 1 1-14 0 7 7 0 0 1 14 0z";
    readonly plus: "M12 5v14 M5 12h14";
    readonly minus: "M5 12h14";
    readonly check: "M20 6L9 17l-5-5";
    readonly 'arrow-left': "M19 12H5 M12 19l-7-7 7-7";
    readonly 'arrow-right': "M5 12h14 M12 5l7 7-7 7";
    readonly heart: "M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z";
    readonly star: "M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z";
    readonly bell: "M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9 M13.73 21a2 2 0 0 1-3.46 0";
    readonly mail: "M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z M22 6l-10 7L2 6";
    readonly phone: "M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z";
    readonly camera: "M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z M12 17a4 4 0 1 0 0-8 4 4 0 0 0 0 8z";
    readonly edit: "M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7 M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z";
    readonly delete: "M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6";
    readonly save: "M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z M17 21v-8H7v8 M7 3v5h8";
    readonly refresh: "M23 4v6h-6 M1 20v-6h6 M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15";
    readonly download: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M7 10l5 5 5-5 M12 15V3";
    readonly upload: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M17 8l-5-5-5 5 M12 3v12";
    readonly lock: "M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 10 0v4";
    readonly unlock: "M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 9.9-1";
    readonly eye: "M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z M12 16a4 4 0 1 0 0-8 4 4 0 0 0 0 8z";
    readonly 'eye-off': "M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24 M1 1l22 22";
    readonly calendar: "M19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM7 10h5v5H7z";
    readonly clock: "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 6v6l4 2";
    readonly location: "M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z M12 13a3 3 0 1 0 0-6 3 3 0 0 0 0 6z";
    readonly wifi: "M5 12.55a11 11 0 0 1 14.08 0 M1.42 9a16 16 0 0 1 21.16 0 M8.53 16.11a6 6 0 0 1 6.95 0 M12 20h.01";
    readonly battery: "M1 6v12h5l2 2h8l2-2h5V6H1zm4 10V8h14v8H5z M23 10v4";
    readonly power: "M12 2v10 M18.4 6.6a9 9 0 1 1-12.77.04";
    readonly warning: "M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z M12 9v4 M12 17h.01";
    readonly info: "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 8v4 M12 16h.01";
    readonly error: "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M15 9l-6 6 M9 9l6 6";
    readonly success: "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M9 12l2 2 4-4";
    readonly 'chevron-back': "M15.75 19.5L8.25 12l7.5-7.5";
    readonly 'chevron-forward': "M8.25 4.5l7.5 7.5-7.5 7.5";
    readonly 'play-skip-back': "M5.25 5.25v13.5m7.5-13.5v13.5L21 12l-8.25-6.75z";
    readonly 'play-skip-forward': "M18.75 18.75v-13.5m-7.5 13.5v-13.5L3 12l8.25 6.75z";
    readonly chart: "M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z";
    readonly book: "M4 19.5A2.5 2.5 0 0 1 6.5 17H20M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z";
    readonly 'trending-up': "M23 6 13.5 15.5 8.5 10.5 1 18M17 6h6v6";
    readonly brain: "M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96.44 2.5 2.5 0 0 1-2.96-3.08 3 3 0 0 1-.34-5.58 2.5 2.5 0 0 1 1.32-4.24 2.5 2.5 0 0 1 1.98-3A2.5 2.5 0 0 1 9.5 2ZM14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96.44 2.5 2.5 0 0 0 2.96-3.08 3 3 0 0 0 .34-5.58 2.5 2.5 0 0 0-1.32-4.24 2.5 2.5 0 0 0-1.98-3A2.5 2.5 0 0 0 14.5 2Z";
    readonly temperature: "M14 14.76V3.5a2.5 2.5 0 0 0-5 0v11.26a4.5 4.5 0 1 0 5 0zM12 17a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z";
    readonly humidity: "M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0L12 2.69z M9 10a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M15 16a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M10.5 8.5l4 4";
    readonly ph: "M10 2h4a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z M9 19h6 M12 19v2 M10 4h4v8a2 2 0 0 1-2 2 2 2 0 0 1-2-2V4z M11 6h2v2h-2V6z M11 9h2v1h-2V9z";
    readonly sun: "M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42M12 17a5 5 0 1 0 0-10 5 5 0 0 0 0 10z";
    readonly electric: "M13 2L3 14h9l-1 8 10-12h-9l1-8z";
    readonly light: "M9 2h6l2 2v1a9 9 0 1 1-10 0V4l2-2z M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10z M8 1h8v1H8V1z";
    readonly ruler: "M21.71 2.29a1 1 0 0 0-1.42 0L2.29 20.29a1 1 0 0 0 0 1.42 1 1 0 0 0 1.42 0L21.71 3.71a1 1 0 0 0 0-1.42zM7 7l1.5 1.5L7 10l-1.5-1.5L7 7zM10 10l1.5 1.5L10 13l-1.5-1.5L10 10zM13 13l1.5 1.5L13 16l-1.5-1.5L13 13z";
    readonly water: "M6 4h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M4 14h16 M6 16h2v2H6v-2z M10 16h2v2h-2v-2z M14 16h2v2h-2v-2z M7 12h1v1H7v-1z M9 11h1v1H9v-1z M11 12h1v1h-1v-1z M13 11h1v1h-1v-1z M15 12h1v1h-1v-1z";
};

interface NavigationItem {
    id: string;
    href: string;
    label: string;
    iconName: IconName;
    order: number;
    isMainTab?: boolean;
}
declare const navigationConfig: NavigationItem[];
declare const getMainTabs: () => NavigationItem[];
declare const getNavigationItem: (id: string) => NavigationItem | undefined;

interface DrawerMenuItem {
    id: string;
    label: string;
    iconName: IconName;
    href?: string;
    children?: DrawerMenuItem[];
    status?: 'connected' | 'disconnected' | null;
    isExpandable?: boolean;
}
declare const drawerMenuConfig: DrawerMenuItem[];
declare const getDrawerMenuItem: (id: string) => DrawerMenuItem | undefined;
declare const getExpandableItems: () => DrawerMenuItem[];
declare const getItemsWithStatus: () => DrawerMenuItem[];

export { ArrowLeftIcon, ArrowRightIcon, BatteryIcon, BellIcon, CHART_VARIABLES, CalendarIcon, CameraIcon, CheckIcon, ChevronBackIcon, ChevronForwardIcon, ClockIcon, CloseIcon, DeleteIcon, DownloadIcon, EditIcon, ElectricIcon, ErrorIcon, EyeIcon, EyeOffIcon, HeartIcon, HomeIcon, HumidityIcon, Icon, InfoIcon, LightIcon, LocationIcon, LockIcon, MailIcon, MenuIcon, MinusIcon, PhIcon, PhoneIcon, PlaySkipBackIcon, PlaySkipForwardIcon, PlusIcon, PowerIcon, RefreshIcon, RulerIcon, SaveIcon, SearchIcon, SettingsIcon, StarIcon, SuccessIcon, SunIcon, UnlockIcon, UploadIcon, UserIcon, WarningIcon, WaterIcon, WifiIcon, baseTokens, borderRadius, colors, componentSpacing, createActuator, createVariable, deleteActuator, deleteVariable, drawerMenuConfig, elevation, fetchActuators, fetchIndividualReadings, fetchMetrics, fetchSensorData, fetchSensorSummary, fetchVariables, formatCurrency, formatDate, formatSensorValue, getAppInfo, getDrawerMenuItem, getExpandableItems, getItemsWithStatus, getMainTabs, getNavigationItem, getWelcomeMessage, isFutureDate, isValidEmail, isValidSensorValue, navigationConfig, semanticColors, spacing, svgPaths, textStyles, truncateText, typography, updateActuator, updateVariable, useActuatorStore, useAlertStore, useAuthStore, useDashboardStore, useFuzzyStore, useLoginForm, useReadingsStore, useSensorStore, useVariableStore, validateLoginCredentials, validatePassword, zIndex };
export type { ActionType, ActionVariant, ActuadorData, Actuator, Alert, AuthResponse, BaseEntity, BaseTokens, BorderRadiusToken, ChartVariable, ColorToken, CrudListProps, DisabledTextProps, DrawerMenuItem, ElevationToken, EntityAction, EntityConfig, EntityStatus, ExtendedSensorState, FieldConfig, FieldType, FuzzyOperators, FuzzyRoutine, FuzzyRule, FuzzySystem, FuzzySystemActions, FuzzySystemState, FuzzySystemStore, FuzzyTerm, FuzzyVariable, HydroponicCrop, IconComponent, IconName, IconProps, IndividualReading, IndividualSensorData$1 as IndividualSensorData, InfoField, MembershipFunction, MetricData, MongoDate, NavigationItem, ObjectId, PaginationProps, RoutineStep, RuleCondition, ScatterDataPoint, SemanticColorToken, Sensor, SensorData, SensorReading, SensorState, SensorSummary, SimpleFuzzyRoutine, SimpleFuzzyRule, SimpleFuzzySystem, SimpleFuzzyTerm, SimpleFuzzyVariable, SpacingToken, SystemComponent, Task, TextStyleToken, Theme, TimeSeriesDataPoint, TypographyToken, User, Variable, VariableData, WelcomeMessageProps, ZIndexToken };
