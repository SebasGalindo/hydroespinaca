# Utilities - Funciones Utilitarias

Este directorio contiene todas las funciones utilitarias y helpers de la aplicación HydroEspinaca, proporcionando funcionalidades comunes y reutilizables.

## Arquitectura de Utilidades

### Principios de Diseño

- **Funciones puras**: Sin efectos secundarios
- **Reutilización**: Funciones altamente reutilizables
- **Tipado fuerte**: TypeScript para mayor seguridad
- **Performance**: Optimizadas para rendimiento
- **Testing**: Completamente testeable
- **Tree-shaking**: Importación granular

### Categorías de Utilidades

```
utils/
├── formatting/             # Formateo de datos
│   ├── date.ts
│   ├── number.ts
│   ├── currency.ts
│   └── text.ts
├── validation/             # Validación de datos
│   ├── schemas.ts
│   ├── rules.ts
│   └── sanitization.ts
├── data/                   # Manipulación de datos
│   ├── array.ts
│   ├── object.ts
│   ├── transform.ts
│   └── filter.ts
├── dom/                    # Utilidades DOM
│   ├── events.ts
│   ├── scroll.ts
│   ├── focus.ts
│   └── clipboard.ts
├── api/                    # Utilidades API
│   ├── request.ts
│   ├── response.ts
│   ├── error.ts
│   └── cache.ts
├── math/                   # Cálculos matemáticos
│   ├── statistics.ts
│   ├── conversion.ts
│   └── interpolation.ts
├── constants/              # Constantes de la aplicación
│   ├── sensors.ts
│   ├── units.ts
│   ├── colors.ts
│   └── config.ts
└── README.md              # Esta documentación
```

## Utilidades de Formateo

### date.ts - Formateo de Fechas

**Propósito**: Funciones para formatear y manipular fechas

```typescript
/**
 * Formatea una fecha en formato legible
 */
export function formatDate(
  date: Date | string | number,
  format: 'short' | 'medium' | 'long' | 'full' = 'medium',
  locale: string = 'es-ES'
): string {
  const dateObj = new Date(date);
  
  const options: Intl.DateTimeFormatOptions = {
    short: { day: '2-digit', month: '2-digit', year: 'numeric' },
    medium: { day: '2-digit', month: 'short', year: 'numeric' },
    long: { day: '2-digit', month: 'long', year: 'numeric' },
    full: { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' }
  }[format];
  
  return new Intl.DateTimeFormat(locale, options).format(dateObj);
}

/**
 * Formatea una hora en formato legible
 */
export function formatTime(
  date: Date | string | number,
  format: '12h' | '24h' = '24h',
  showSeconds: boolean = false
): string {
  const dateObj = new Date(date);
  
  const options: Intl.DateTimeFormatOptions = {
    hour: '2-digit',
    minute: '2-digit',
    ...(showSeconds && { second: '2-digit' }),
    hour12: format === '12h'
  };
  
  return new Intl.DateTimeFormat('es-ES', options).format(dateObj);
}

/**
 * Formatea fecha y hora completa
 */
export function formatDateTime(
  date: Date | string | number,
  options: {
    dateFormat?: 'short' | 'medium' | 'long';
    timeFormat?: '12h' | '24h';
    showSeconds?: boolean;
    separator?: string;
  } = {}
): string {
  const {
    dateFormat = 'medium',
    timeFormat = '24h',
    showSeconds = false,
    separator = ' - '
  } = options;
  
  const formattedDate = formatDate(date, dateFormat);
  const formattedTime = formatTime(date, timeFormat, showSeconds);
  
  return `${formattedDate}${separator}${formattedTime}`;
}

/**
 * Calcula tiempo relativo (hace X minutos)
 */
export function formatRelativeTime(
  date: Date | string | number,
  locale: string = 'es-ES'
): string {
  const dateObj = new Date(date);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - dateObj.getTime()) / 1000);
  
  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
  
  if (diffInSeconds < 60) {
    return rtf.format(-diffInSeconds, 'second');
  } else if (diffInSeconds < 3600) {
    return rtf.format(-Math.floor(diffInSeconds / 60), 'minute');
  } else if (diffInSeconds < 86400) {
    return rtf.format(-Math.floor(diffInSeconds / 3600), 'hour');
  } else {
    return rtf.format(-Math.floor(diffInSeconds / 86400), 'day');
  }
}

/**
 * Obtiene el rango de fechas para filtros comunes
 */
export function getDateRange(
  range: 'today' | 'yesterday' | 'last7days' | 'last30days' | 'thisMonth' | 'lastMonth'
): { from: Date; to: Date } {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  
  switch (range) {
    case 'today':
      return {
        from: today,
        to: new Date(today.getTime() + 24 * 60 * 60 * 1000 - 1)
      };
      
    case 'yesterday':
      const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000);
      return {
        from: yesterday,
        to: new Date(yesterday.getTime() + 24 * 60 * 60 * 1000 - 1)
      };
      
    case 'last7days':
      return {
        from: new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000),
        to: now
      };
      
    case 'last30days':
      return {
        from: new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000),
        to: now
      };
      
    case 'thisMonth':
      return {
        from: new Date(now.getFullYear(), now.getMonth(), 1),
        to: now
      };
      
    case 'lastMonth':
      const lastMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const lastMonthEnd = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59);
      return {
        from: lastMonth,
        to: lastMonthEnd
      };
      
    default:
      return { from: today, to: now };
  }
}

// Ejemplos de uso
const now = new Date();
console.log(formatDate(now)); // "26 ene 2024"
console.log(formatTime(now)); // "14:30"
console.log(formatDateTime(now)); // "26 ene 2024 - 14:30"
console.log(formatRelativeTime(new Date(Date.now() - 300000))); // "hace 5 minutos"

const { from, to } = getDateRange('last7days');
console.log(`Desde: ${formatDate(from)} hasta: ${formatDate(to)}`);
```

### number.ts - Formateo de Números

**Propósito**: Funciones para formatear números, porcentajes y valores numéricos

```typescript
/**
 * Formatea un número con separadores de miles
 */
export function formatNumber(
  value: number,
  options: {
    decimals?: number;
    locale?: string;
    notation?: 'standard' | 'scientific' | 'engineering' | 'compact';
  } = {}
): string {
  const { decimals = 2, locale = 'es-ES', notation = 'standard' } = options;
  
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
    notation
  }).format(value);
}

/**
 * Formatea un porcentaje
 */
export function formatPercentage(
  value: number,
  decimals: number = 1,
  locale: string = 'es-ES'
): string {
  return new Intl.NumberFormat(locale, {
    style: 'percent',
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  }).format(value / 100);
}

/**
 * Formatea un valor con unidad
 */
export function formatValueWithUnit(
  value: number,
  unit: string,
  decimals: number = 2
): string {
  const formattedValue = formatNumber(value, { decimals });
  return `${formattedValue} ${unit}`;
}

/**
 * Formatea un rango de valores
 */
export function formatRange(
  min: number,
  max: number,
  unit?: string,
  decimals: number = 2
): string {
  const formattedMin = formatNumber(min, { decimals });
  const formattedMax = formatNumber(max, { decimals });
  const unitSuffix = unit ? ` ${unit}` : '';
  
  return `${formattedMin} - ${formattedMax}${unitSuffix}`;
}

/**
 * Redondea un número a decimales específicos
 */
export function roundToDecimals(value: number, decimals: number = 2): number {
  const factor = Math.pow(10, decimals);
  return Math.round(value * factor) / factor;
}

/**
 * Clamp un valor entre min y max
 */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Convierte un valor a un rango específico
 */
export function mapRange(
  value: number,
  fromMin: number,
  fromMax: number,
  toMin: number,
  toMax: number
): number {
  const fromRange = fromMax - fromMin;
  const toRange = toMax - toMin;
  const scaledValue = (value - fromMin) / fromRange;
  return toMin + scaledValue * toRange;
}

// Ejemplos de uso
console.log(formatNumber(1234.567)); // "1.234,57"
console.log(formatPercentage(75.5)); // "75,5%"
console.log(formatValueWithUnit(24.5, '°C')); // "24,50 °C"
console.log(formatRange(20, 30, '°C')); // "20,00 - 30,00 °C"
console.log(roundToDecimals(3.14159, 2)); // 3.14
console.log(clamp(150, 0, 100)); // 100
console.log(mapRange(50, 0, 100, 0, 255)); // 127.5
```

## Utilidades de Validación

### schemas.ts - Esquemas de Validación

**Propósito**: Esquemas de validación para diferentes tipos de datos

```typescript
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

export type ValidationRule<T> = (value: T) => string | null;

/**
 * Validador genérico
 */
export function validate<T>(
  value: T,
  rules: ValidationRule<T>[]
): ValidationResult {
  const errors: string[] = [];
  
  for (const rule of rules) {
    const error = rule(value);
    if (error) {
      errors.push(error);
    }
  }
  
  return {
    isValid: errors.length === 0,
    errors
  };
}

/**
 * Reglas de validación comunes
 */
export const validationRules = {
  required: <T>(message: string = 'Este campo es requerido'): ValidationRule<T> => {
    return (value: T) => {
      if (value === null || value === undefined || value === '') {
        return message;
      }
      return null;
    };
  },
  
  minLength: (min: number, message?: string): ValidationRule<string> => {
    return (value: string) => {
      if (value && value.length < min) {
        return message || `Mínimo ${min} caracteres`;
      }
      return null;
    };
  },
  
  maxLength: (max: number, message?: string): ValidationRule<string> => {
    return (value: string) => {
      if (value && value.length > max) {
        return message || `Máximo ${max} caracteres`;
      }
      return null;
    };
  },
  
  email: (message: string = 'Email inválido'): ValidationRule<string> => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return (value: string) => {
      if (value && !emailRegex.test(value)) {
        return message;
      }
      return null;
    };
  },
  
  numeric: (message: string = 'Debe ser un número'): ValidationRule<string | number> => {
    return (value: string | number) => {
      if (value !== '' && value !== null && value !== undefined) {
        const num = typeof value === 'string' ? parseFloat(value) : value;
        if (isNaN(num)) {
          return message;
        }
      }
      return null;
    };
  },
  
  min: (min: number, message?: string): ValidationRule<number> => {
    return (value: number) => {
      if (value !== null && value !== undefined && value < min) {
        return message || `El valor mínimo es ${min}`;
      }
      return null;
    };
  },
  
  max: (max: number, message?: string): ValidationRule<number> => {
    return (value: number) => {
      if (value !== null && value !== undefined && value > max) {
        return message || `El valor máximo es ${max}`;
      }
      return null;
    };
  },
  
  range: (min: number, max: number, message?: string): ValidationRule<number> => {
    return (value: number) => {
      if (value !== null && value !== undefined) {
        if (value < min || value > max) {
          return message || `El valor debe estar entre ${min} y ${max}`;
        }
      }
      return null;
    };
  },
  
  pattern: (regex: RegExp, message: string): ValidationRule<string> => {
    return (value: string) => {
      if (value && !regex.test(value)) {
        return message;
      }
      return null;
    };
  }
};

/**
 * Esquemas de validación específicos
 */
export const sensorValidationSchema = {
  name: [
    validationRules.required('El nombre del sensor es requerido'),
    validationRules.minLength(3, 'El nombre debe tener al menos 3 caracteres'),
    validationRules.maxLength(50, 'El nombre no puede exceder 50 caracteres')
  ],
  
  type: [
    validationRules.required('El tipo de sensor es requerido')
  ],
  
  minValue: [
    validationRules.required('El valor mínimo es requerido'),
    validationRules.numeric('Debe ser un número válido')
  ],
  
  maxValue: [
    validationRules.required('El valor máximo es requerido'),
    validationRules.numeric('Debe ser un número válido')
  ],
  
  location: [
    validationRules.required('La ubicación es requerida'),
    validationRules.minLength(2, 'La ubicación debe tener al menos 2 caracteres')
  ]
};

export const userValidationSchema = {
  email: [
    validationRules.required('El email es requerido'),
    validationRules.email('Formato de email inválido')
  ],
  
  password: [
    validationRules.required('La contraseña es requerida'),
    validationRules.minLength(8, 'La contraseña debe tener al menos 8 caracteres'),
    validationRules.pattern(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
      'La contraseña debe contener al menos una mayúscula, una minúscula y un número'
    )
  ],
  
  name: [
    validationRules.required('El nombre es requerido'),
    validationRules.minLength(2, 'El nombre debe tener al menos 2 caracteres'),
    validationRules.maxLength(100, 'El nombre no puede exceder 100 caracteres')
  ]
};

// Ejemplo de uso
const sensorData = {
  name: 'Temp-001',
  type: 'temperature',
  minValue: 0,
  maxValue: 50,
  location: 'Invernadero A'
};

const nameValidation = validate(sensorData.name, sensorValidationSchema.name);
if (!nameValidation.isValid) {
  console.log('Errores en el nombre:', nameValidation.errors);
}
```

## Utilidades de Manipulación de Datos

### array.ts - Manipulación de Arrays

**Propósito**: Funciones utilitarias para trabajar con arrays

```typescript
/**
 * Agrupa elementos de un array por una clave
 */
export function groupBy<T, K extends keyof T>(
  array: T[],
  key: K
): Record<string, T[]> {
  return array.reduce((groups, item) => {
    const groupKey = String(item[key]);
    if (!groups[groupKey]) {
      groups[groupKey] = [];
    }
    groups[groupKey].push(item);
    return groups;
  }, {} as Record<string, T[]>);
}

/**
 * Ordena un array por múltiples criterios
 */
export function sortBy<T>(
  array: T[],
  ...criteria: Array<{
    key: keyof T;
    direction?: 'asc' | 'desc';
    type?: 'string' | 'number' | 'date';
  }>
): T[] {
  return [...array].sort((a, b) => {
    for (const criterion of criteria) {
      const { key, direction = 'asc', type = 'string' } = criterion;
      
      let aValue = a[key];
      let bValue = b[key];
      
      // Convertir valores según el tipo
      if (type === 'number') {
        aValue = Number(aValue) as any;
        bValue = Number(bValue) as any;
      } else if (type === 'date') {
        aValue = new Date(aValue as any).getTime() as any;
        bValue = new Date(bValue as any).getTime() as any;
      }
      
      let comparison = 0;
      if (aValue < bValue) comparison = -1;
      if (aValue > bValue) comparison = 1;
      
      if (comparison !== 0) {
        return direction === 'desc' ? -comparison : comparison;
      }
    }
    return 0;
  });
}

/**
 * Filtra elementos únicos de un array
 */
export function unique<T>(array: T[]): T[];
export function unique<T, K>(array: T[], keySelector: (item: T) => K): T[];
export function unique<T, K>(
  array: T[],
  keySelector?: (item: T) => K
): T[] {
  if (!keySelector) {
    return [...new Set(array)];
  }
  
  const seen = new Set<K>();
  return array.filter(item => {
    const key = keySelector(item);
    if (seen.has(key)) {
      return false;
    }
    seen.add(key);
    return true;
  });
}

/**
 * Pagina un array
 */
export function paginate<T>(
  array: T[],
  page: number,
  pageSize: number
): {
  data: T[];
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrev: boolean;
  };
} {
  const total = array.length;
  const totalPages = Math.ceil(total / pageSize);
  const startIndex = (page - 1) * pageSize;
  const endIndex = startIndex + pageSize;
  const data = array.slice(startIndex, endIndex);
  
  return {
    data,
    pagination: {
      current: page,
      pageSize,
      total,
      totalPages,
      hasNext: page < totalPages,
      hasPrev: page > 1
    }
  };
}

/**
 * Encuentra diferencias entre dos arrays
 */
export function arrayDiff<T>(
  array1: T[],
  array2: T[],
  keySelector?: (item: T) => any
): {
  added: T[];
  removed: T[];
  common: T[];
} {
  const getKey = keySelector || ((item: T) => item);
  
  const set1 = new Map(array1.map(item => [getKey(item), item]));
  const set2 = new Map(array2.map(item => [getKey(item), item]));
  
  const added: T[] = [];
  const removed: T[] = [];
  const common: T[] = [];
  
  // Elementos en array2 pero no en array1 (añadidos)
  for (const [key, item] of set2) {
    if (!set1.has(key)) {
      added.push(item);
    } else {
      common.push(item);
    }
  }
  
  // Elementos en array1 pero no en array2 (removidos)
  for (const [key, item] of set1) {
    if (!set2.has(key)) {
      removed.push(item);
    }
  }
  
  return { added, removed, common };
}

/**
 * Convierte array en chunks de tamaño específico
 */
export function chunk<T>(array: T[], size: number): T[][] {
  const chunks: T[][] = [];
  for (let i = 0; i < array.length; i += size) {
    chunks.push(array.slice(i, i + size));
  }
  return chunks;
}

// Ejemplos de uso
const sensors = [
  { id: '1', name: 'Temp-001', type: 'temperature', value: 25.5, date: '2024-01-26' },
  { id: '2', name: 'Hum-001', type: 'humidity', value: 60.2, date: '2024-01-25' },
  { id: '3', name: 'Temp-002', type: 'temperature', value: 23.1, date: '2024-01-26' }
];

// Agrupar por tipo
const groupedByType = groupBy(sensors, 'type');
console.log(groupedByType);
// { temperature: [...], humidity: [...] }

// Ordenar por valor descendente
const sortedByValue = sortBy(sensors, {
  key: 'value',
  direction: 'desc',
  type: 'number'
});

// Obtener tipos únicos
const uniqueTypes = unique(sensors, sensor => sensor.type);

// Paginar resultados
const paginatedSensors = paginate(sensors, 1, 2);
console.log(paginatedSensors.data); // Primeros 2 elementos
console.log(paginatedSensors.pagination); // Info de paginación
```

### object.ts - Manipulación de Objetos

**Propósito**: Funciones utilitarias para trabajar con objetos

```typescript
/**
 * Deep clone de un objeto
 */
export function deepClone<T>(obj: T): T {
  if (obj === null || typeof obj !== 'object') {
    return obj;
  }
  
  if (obj instanceof Date) {
    return new Date(obj.getTime()) as any;
  }
  
  if (obj instanceof Array) {
    return obj.map(item => deepClone(item)) as any;
  }
  
  if (typeof obj === 'object') {
    const cloned = {} as any;
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        cloned[key] = deepClone(obj[key]);
      }
    }
    return cloned;
  }
  
  return obj;
}

/**
 * Merge profundo de objetos
 */
export function deepMerge<T extends Record<string, any>>(
  target: T,
  ...sources: Partial<T>[]
): T {
  if (!sources.length) return target;
  const source = sources.shift();
  
  if (isObject(target) && isObject(source)) {
    for (const key in source) {
      if (isObject(source[key])) {
        if (!target[key]) Object.assign(target, { [key]: {} });
        deepMerge(target[key], source[key]);
      } else {
        Object.assign(target, { [key]: source[key] });
      }
    }
  }
  
  return deepMerge(target, ...sources);
}

function isObject(item: any): boolean {
  return item && typeof item === 'object' && !Array.isArray(item);
}

/**
 * Obtiene un valor anidado de un objeto usando dot notation
 */
export function get<T>(
  obj: any,
  path: string,
  defaultValue?: T
): T | undefined {
  const keys = path.split('.');
  let result = obj;
  
  for (const key of keys) {
    if (result === null || result === undefined) {
      return defaultValue;
    }
    result = result[key];
  }
  
  return result !== undefined ? result : defaultValue;
}

/**
 * Establece un valor anidado en un objeto usando dot notation
 */
export function set<T extends Record<string, any>>(
  obj: T,
  path: string,
  value: any
): T {
  const keys = path.split('.');
  const lastKey = keys.pop()!;
  let current = obj;
  
  for (const key of keys) {
    if (!(key in current) || typeof current[key] !== 'object') {
      current[key] = {};
    }
    current = current[key];
  }
  
  current[lastKey] = value;
  return obj;
}

/**
 * Omite propiedades específicas de un objeto
 */
export function omit<T extends Record<string, any>, K extends keyof T>(
  obj: T,
  ...keys: K[]
): Omit<T, K> {
  const result = { ...obj };
  for (const key of keys) {
    delete result[key];
  }
  return result;
}

/**
 * Selecciona solo propiedades específicas de un objeto
 */
export function pick<T extends Record<string, any>, K extends keyof T>(
  obj: T,
  ...keys: K[]
): Pick<T, K> {
  const result = {} as Pick<T, K>;
  for (const key of keys) {
    if (key in obj) {
      result[key] = obj[key];
    }
  }
  return result;
}

/**
 * Verifica si un objeto está vacío
 */
export function isEmpty(obj: any): boolean {
  if (obj === null || obj === undefined) return true;
  if (typeof obj === 'string' || Array.isArray(obj)) return obj.length === 0;
  if (typeof obj === 'object') return Object.keys(obj).length === 0;
  return false;
}

/**
 * Compara dos objetos profundamente
 */
export function isEqual(a: any, b: any): boolean {
  if (a === b) return true;
  
  if (a === null || b === null) return false;
  if (a === undefined || b === undefined) return false;
  
  if (typeof a !== typeof b) return false;
  
  if (typeof a === 'object') {
    if (Array.isArray(a) !== Array.isArray(b)) return false;
    
    const keysA = Object.keys(a);
    const keysB = Object.keys(b);
    
    if (keysA.length !== keysB.length) return false;
    
    for (const key of keysA) {
      if (!keysB.includes(key)) return false;
      if (!isEqual(a[key], b[key])) return false;
    }
    
    return true;
  }
  
  return false;
}

// Ejemplos de uso
const sensorConfig = {
  sensor: {
    name: 'Temp-001',
    settings: {
      minValue: 0,
      maxValue: 50,
      unit: '°C'
    }
  },
  alerts: {
    enabled: true,
    thresholds: [20, 30]
  }
};

// Obtener valor anidado
const minValue = get(sensorConfig, 'sensor.settings.minValue'); // 0
const nonExistent = get(sensorConfig, 'sensor.settings.nonExistent', 'default'); // 'default'

// Establecer valor anidado
const updated = deepClone(sensorConfig);
set(updated, 'sensor.settings.calibration', true);

// Omitir propiedades
const withoutAlerts = omit(sensorConfig, 'alerts');

// Seleccionar propiedades
const sensorOnly = pick(sensorConfig, 'sensor');

// Comparar objetos
const isConfigEqual = isEqual(sensorConfig, updated); // false
```

## Utilidades Matemáticas

### statistics.ts - Cálculos Estadísticos

**Propósito**: Funciones para cálculos estadísticos de datos de sensores

```typescript
/**
 * Calcula estadísticas básicas de un array de números
 */
export function calculateStats(values: number[]): {
  count: number;
  sum: number;
  mean: number;
  median: number;
  mode: number[];
  min: number;
  max: number;
  range: number;
  variance: number;
  standardDeviation: number;
  q1: number;
  q3: number;
  iqr: number;
} {
  if (values.length === 0) {
    throw new Error('Array no puede estar vacío');
  }
  
  const sorted = [...values].sort((a, b) => a - b);
  const count = values.length;
  const sum = values.reduce((acc, val) => acc + val, 0);
  const mean = sum / count;
  
  // Mediana
  const median = count % 2 === 0
    ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2
    : sorted[Math.floor(count / 2)];
  
  // Moda
  const frequency: Record<number, number> = {};
  values.forEach(val => {
    frequency[val] = (frequency[val] || 0) + 1;
  });
  const maxFreq = Math.max(...Object.values(frequency));
  const mode = Object.keys(frequency)
    .filter(key => frequency[Number(key)] === maxFreq)
    .map(Number);
  
  // Min, Max, Rango
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min;
  
  // Varianza y Desviación Estándar
  const variance = values.reduce((acc, val) => acc + Math.pow(val - mean, 2), 0) / count;
  const standardDeviation = Math.sqrt(variance);
  
  // Cuartiles
  const q1Index = Math.floor(count * 0.25);
  const q3Index = Math.floor(count * 0.75);
  const q1 = sorted[q1Index];
  const q3 = sorted[q3Index];
  const iqr = q3 - q1;
  
  return {
    count,
    sum,
    mean,
    median,
    mode,
    min,
    max,
    range,
    variance,
    standardDeviation,
    q1,
    q3,
    iqr
  };
}

/**
 * Detecta valores atípicos usando el método IQR
 */
export function detectOutliers(
  values: number[],
  multiplier: number = 1.5
): {
  outliers: number[];
  lowerBound: number;
  upperBound: number;
  cleanData: number[];
} {
  const stats = calculateStats(values);
  const lowerBound = stats.q1 - multiplier * stats.iqr;
  const upperBound = stats.q3 + multiplier * stats.iqr;
  
  const outliers: number[] = [];
  const cleanData: number[] = [];
  
  values.forEach(value => {
    if (value < lowerBound || value > upperBound) {
      outliers.push(value);
    } else {
      cleanData.push(value);
    }
  });
  
  return {
    outliers,
    lowerBound,
    upperBound,
    cleanData
  };
}

/**
 * Calcula media móvil
 */
export function movingAverage(
  values: number[],
  windowSize: number
): number[] {
  if (windowSize > values.length) {
    throw new Error('Tamaño de ventana mayor que el array');
  }
  
  const result: number[] = [];
  
  for (let i = 0; i <= values.length - windowSize; i++) {
    const window = values.slice(i, i + windowSize);
    const average = window.reduce((sum, val) => sum + val, 0) / windowSize;
    result.push(average);
  }
  
  return result;
}

/**
 * Calcula correlación entre dos series de datos
 */
export function correlation(x: number[], y: number[]): number {
  if (x.length !== y.length) {
    throw new Error('Los arrays deben tener la misma longitud');
  }
  
  const n = x.length;
  const meanX = x.reduce((sum, val) => sum + val, 0) / n;
  const meanY = y.reduce((sum, val) => sum + val, 0) / n;
  
  let numerator = 0;
  let sumXSquared = 0;
  let sumYSquared = 0;
  
  for (let i = 0; i < n; i++) {
    const deltaX = x[i] - meanX;
    const deltaY = y[i] - meanY;
    
    numerator += deltaX * deltaY;
    sumXSquared += deltaX * deltaX;
    sumYSquared += deltaY * deltaY;
  }
  
  const denominator = Math.sqrt(sumXSquared * sumYSquared);
  
  return denominator === 0 ? 0 : numerator / denominator;
}

/**
 * Normaliza valores a un rango específico
 */
export function normalize(
  values: number[],
  targetMin: number = 0,
  targetMax: number = 1
): number[] {
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min;
  
  if (range === 0) {
    return values.map(() => targetMin);
  }
  
  const targetRange = targetMax - targetMin;
  
  return values.map(value => {
    const normalized = (value - min) / range;
    return targetMin + normalized * targetRange;
  });
}

// Ejemplos de uso
const temperatureReadings = [22.5, 23.1, 24.2, 23.8, 25.1, 24.7, 23.9, 24.3, 22.8, 23.5];

// Calcular estadísticas
const stats = calculateStats(temperatureReadings);
console.log(`Media: ${stats.mean.toFixed(2)}°C`);
console.log(`Desviación estándar: ${stats.standardDeviation.toFixed(2)}°C`);

// Detectar valores atípicos
const outlierAnalysis = detectOutliers(temperatureReadings);
console.log(`Valores atípicos: ${outlierAnalysis.outliers}`);

// Media móvil de 3 períodos
const movingAvg = movingAverage(temperatureReadings, 3);
console.log(`Media móvil: ${movingAvg.map(v => v.toFixed(2))}`);

// Normalizar valores
const normalizedTemps = normalize(temperatureReadings, 0, 100);
console.log(`Temperaturas normalizadas: ${normalizedTemps.map(v => v.toFixed(2))}`);
```

## Constantes de la Aplicación

### sensors.ts - Constantes de Sensores

**Propósito**: Definiciones y configuraciones de tipos de sensores

```typescript
export const SENSOR_TYPES = {
  TEMPERATURE: 'temperature',
  HUMIDITY: 'humidity',
  PH: 'ph',
  CONDUCTIVITY: 'conductivity',
  LIGHT: 'light',
  WATER_LEVEL: 'water_level'
} as const;

export type SensorType = typeof SENSOR_TYPES[keyof typeof SENSOR_TYPES];

export const SENSOR_CONFIGS: Record<SensorType, {
  name: string;
  unit: string;
  icon: string;
  color: string;
  defaultRange: { min: number; max: number };
  optimalRange: { min: number; max: number };
  precision: number;
  category: string;
}> = {
  [SENSOR_TYPES.TEMPERATURE]: {
    name: 'Temperatura',
    unit: '°C',
    icon: 'thermometer',
    color: '#ef4444',
    defaultRange: { min: -10, max: 50 },
    optimalRange: { min: 18, max: 28 },
    precision: 1,
    category: 'Ambiental'
  },
  [SENSOR_TYPES.HUMIDITY]: {
    name: 'Humedad',
    unit: '%',
    icon: 'droplet',
    color: '#3b82f6',
    defaultRange: { min: 0, max: 100 },
    optimalRange: { min: 60, max: 80 },
    precision: 1,
    category: 'Ambiental'
  },
  [SENSOR_TYPES.PH]: {
    name: 'pH',
    unit: 'pH',
    icon: 'beaker',
    color: '#10b981',
    defaultRange: { min: 0, max: 14 },
    optimalRange: { min: 5.5, max: 6.5 },
    precision: 2,
    category: 'Nutricional'
  },
  [SENSOR_TYPES.CONDUCTIVITY]: {
    name: 'Conductividad',
    unit: 'EC',
    icon: 'zap',
    color: '#f59e0b',
    defaultRange: { min: 0, max: 5 },
    optimalRange: { min: 1.2, max: 2.0 },
    precision: 2,
    category: 'Nutricional'
  },
  [SENSOR_TYPES.LIGHT]: {
    name: 'Luz',
    unit: 'lux',
    icon: 'sun',
    color: '#eab308',
    defaultRange: { min: 0, max: 100000 },
    optimalRange: { min: 20000, max: 40000 },
    precision: 0,
    category: 'Ambiental'
  },
  [SENSOR_TYPES.WATER_LEVEL]: {
    name: 'Nivel de Agua',
    unit: 'cm',
    icon: 'waves',
    color: '#06b6d4',
    defaultRange: { min: 0, max: 100 },
    optimalRange: { min: 80, max: 95 },
    precision: 1,
    category: 'Físico'
  }
};

export const SENSOR_CATEGORIES = {
  ENVIRONMENTAL: 'Ambiental',
  NUTRITIONAL: 'Nutricional',
  PHYSICAL: 'Físico',
  BIOLOGICAL: 'Biológico'
} as const;

export const SENSOR_STATUS = {
  ONLINE: 'online',
  OFFLINE: 'offline',
  ERROR: 'error',
  MAINTENANCE: 'maintenance'
} as const;

export type SensorStatus = typeof SENSOR_STATUS[keyof typeof SENSOR_STATUS];

export const SENSOR_STATUS_CONFIGS: Record<SensorStatus, {
  label: string;
  color: string;
  icon: string;
}> = {
  [SENSOR_STATUS.ONLINE]: {
    label: 'En línea',
    color: '#10b981',
    icon: 'check-circle'
  },
  [SENSOR_STATUS.OFFLINE]: {
    label: 'Desconectado',
    color: '#6b7280',
    icon: 'x-circle'
  },
  [SENSOR_STATUS.ERROR]: {
    label: 'Error',
    color: '#ef4444',
    icon: 'alert-circle'
  },
  [SENSOR_STATUS.MAINTENANCE]: {
    label: 'Mantenimiento',
    color: '#f59e0b',
    icon: 'tool'
  }
};

// Funciones helper
export function getSensorConfig(type: SensorType) {
  return SENSOR_CONFIGS[type];
}

export function getSensorStatusConfig(status: SensorStatus) {
  return SENSOR_STATUS_CONFIGS[status];
}

export function isValueInOptimalRange(type: SensorType, value: number): boolean {
  const config = getSensorConfig(type);
  return value >= config.optimalRange.min && value <= config.optimalRange.max;
}

export function formatSensorValue(type: SensorType, value: number): string {
  const config = getSensorConfig(type);
  return `${value.toFixed(config.precision)} ${config.unit}`;
}
```

## Testing de Utilidades

### Ejemplo de Tests

```typescript
import { formatDate, formatNumber, calculateStats, groupBy } from '../utils';

describe('Date Utilities', () => {
  test('formatDate should format date correctly', () => {
    const date = new Date('2024-01-26T14:30:00');
    expect(formatDate(date, 'short')).toBe('26/01/2024');
    expect(formatDate(date, 'medium')).toBe('26 ene 2024');
  });
  
  test('formatRelativeTime should return correct relative time', () => {
    const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
    expect(formatRelativeTime(fiveMinutesAgo)).toBe('hace 5 minutos');
  });
});

describe('Number Utilities', () => {
  test('formatNumber should format with correct decimals', () => {
    expect(formatNumber(1234.567, { decimals: 2 })).toBe('1.234,57');
    expect(formatNumber(1234.567, { decimals: 0 })).toBe('1.235');
  });
  
  test('formatPercentage should format percentage correctly', () => {
    expect(formatPercentage(75.5)).toBe('75,5%');
    expect(formatPercentage(100)).toBe('100,0%');
  });
});

describe('Statistics Utilities', () => {
  const testData = [1, 2, 3, 4, 5];
  
  test('calculateStats should return correct statistics', () => {
    const stats = calculateStats(testData);
    expect(stats.mean).toBe(3);
    expect(stats.median).toBe(3);
    expect(stats.min).toBe(1);
    expect(stats.max).toBe(5);
  });
  
  test('movingAverage should calculate correctly', () => {
    const result = movingAverage(testData, 3);
    expect(result).toEqual([2, 3, 4]);
  });
});

describe('Array Utilities', () => {
  const testArray = [
    { id: 1, type: 'A', value: 10 },
    { id: 2, type: 'B', value: 20 },
    { id: 3, type: 'A', value: 30 }
  ];
  
  test('groupBy should group correctly', () => {
    const grouped = groupBy(testArray, 'type');
    expect(grouped.A).toHaveLength(2);
    expect(grouped.B).toHaveLength(1);
  });
  
  test('sortBy should sort correctly', () => {
    const sorted = sortBy(testArray, {
      key: 'value',
      direction: 'desc',
      type: 'number'
    });
    expect(sorted[0].value).toBe(30);
    expect(sorted[2].value).toBe(10);
  });
});
```

## Performance y Optimización

### Memoización de Funciones Costosas

```typescript
// Cache para funciones costosas
const memoCache = new Map<string, any>();

export function memoize<T extends (...args: any[]) => any>(
  fn: T,
  keyGenerator?: (...args: Parameters<T>) => string
): T {
  return ((...args: Parameters<T>) => {
    const key = keyGenerator ? keyGenerator(...args) : JSON.stringify(args);
    
    if (memoCache.has(key)) {
      return memoCache.get(key);
    }
    
    const result = fn(...args);
    memoCache.set(key, result);
    
    return result;
  }) as T;
}

// Ejemplo de uso
const expensiveCalculation = memoize((data: number[]) => {
  console.log('Calculando estadísticas...');
  return calculateStats(data);
});

// Debounce para funciones que se ejecutan frecuentemente
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): T {
  let timeout: NodeJS.Timeout;
  
  return ((...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  }) as T;
}

// Throttle para limitar frecuencia de ejecución
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): T {
  let inThrottle: boolean;
  
  return ((...args: Parameters<T>) => {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => inThrottle = false, limit);
    }
  }) as T;
}
```

## Contribución

### Agregar Nueva Utilidad

1. **Identificar categoría**: ¿Dónde encaja mejor?
2. **Implementar función**: Pura, tipada, documentada
3. **Escribir tests**: Cobertura completa
4. **Optimizar**: Performance y memory usage
5. **Documentar**: JSDoc y ejemplos
6. **Exportar**: Agregar a index.ts

### Template de Utilidad

```typescript
/**
 * Descripción de la función
 * 
 * @param param1 - Descripción del parámetro
 * @param param2 - Descripción del parámetro
 * @returns Descripción del retorno
 * 
 * @example
 * ```typescript
 * const result = myUtility('input', { option: true });
 * console.log(result); // Expected output
 * ```
 */
export function myUtility(
  param1: string,
  param2: { option: boolean } = { option: false }
): string {
  // Validación de entrada
  if (!param1) {
    throw new Error('param1 es requerido');
  }
  
  // Lógica de la función
  const result = param2.option ? param1.toUpperCase() : param1.toLowerCase();
  
  return result;
}
```

### Code Review Checklist

- [ ] Función pura (sin efectos secundarios)
- [ ] TypeScript tipado completo
- [ ] Validación de parámetros
- [ ] Manejo de casos edge
- [ ] Tests unitarios completos
- [ ] Documentación JSDoc
- [ ] Ejemplos de uso
- [ ] Performance optimizada

## Recursos

- [Lodash Documentation](https://lodash.com/docs/)
- [Ramda Documentation](https://ramdajs.com/docs/)
- [Date-fns Documentation](https://date-fns.org/docs/)
- [TypeScript Utility Types](https://www.typescriptlang.org/docs/handbook/utility-types.html)
- [JavaScript Performance Best Practices](https://developer.mozilla.org/en-US/docs/Web/Performance)