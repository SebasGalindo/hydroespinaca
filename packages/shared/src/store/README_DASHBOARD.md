# Dashboard Store

## Descripción

El `dashboardStore` es un store de Zustand que gestiona los datos de las gráficas del dashboard. Simula la obtención de datos desde una API y los cachea para evitar llamadas innecesarias.

## Estructura de Datos

### Variables de Gráficas

El store incluye 6 variables predefinidas con valores base realistas:

| ID | Variable | Unidad | Valor Base | Rango | Color |
|----|----------|--------|------------|-------|-------|
| '1' | Temperatura | °C | 25 | ±10 | #FF6B6B |
| '2' | Humedad | % | 60 | ±30 | #4ECDC4 |
| '3' | Presión | hPa | 1013 | ±100 | #45B7D1 |
| '4' | pH | - | 7.2 | ±2 | #96CEB4 |
| '5' | Conductividad | µS/cm | 500 | ±200 | #F7DC6F |
| '6' | Oxígeno Disuelto | mg/L | 8.5 | ±4 | #BB8FCE |

### Tipos de Datos

```typescript
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
```

## Uso

### Importar el Store

```typescript
import { useDashboardStore, CHART_VARIABLES } from '@hidroespinaca/shared';
```

### Obtener Variables Disponibles

```typescript
const variables = Object.values(CHART_VARIABLES).map(v => ({
  id: v.id,
  name: v.name,
  unit: v.unit,
}));
```

### Cargar Datos de Series Temporales

```typescript
const {
  fetchTimeSeriesData,
  getTimeSeriesData,
  loading,
  error
} = useDashboardStore();

// Cargar datos (simula llamada a API)
useEffect(() => {
  fetchTimeSeriesData('1'); // Temperatura
}, []);

// Obtener datos del cache
const data = getTimeSeriesData('1');
```

### Cargar Datos de Gráfico de Dispersión

```typescript
const {
  fetchScatterData,
  getScatterData,
  loading,
  error
} = useDashboardStore();

// Cargar datos
useEffect(() => {
  fetchScatterData('1', '2'); // Temperatura vs Humedad
}, []);

// Obtener datos del cache
const data = getScatterData('1', '2');
```

## Generación de Datos

### Series Temporales

Los datos de series temporales se generan con:
- **Tendencia anual**: Patrón senoidal basado en el día del año
- **Patrón semanal**: Variación cíclica de 7 días
- **Ruido aleatorio**: Para simular variaciones naturales

```typescript
const value = baseValue + (trend + daily + noise) * range;
```

### Gráfico de Dispersión

Los datos de dispersión incluyen correlaciones realistas entre variables:

| Variables | Correlación | Interpretación |
|-----------|-------------|----------------|
| Temperatura - Humedad | -0.6 | Inversa moderada-fuerte |
| Temperatura - O₂ | -0.7 | Inversa fuerte |
| pH - O₂ | 0.5 | Directa moderada |
| Temperatura - Conductividad | 0.5 | Directa moderada |

## Reemplazo de API

Para conectar con una API real, reemplaza las funciones de generación:

```typescript
// En lugar de:
const data = generateTimeSeriesData(variableId, days);

// Usa:
const response = await fetch(`/api/timeseries/${variableId}?days=${days}`);
const data = await response.json();
```

## Cache

El store mantiene dos caches:
- `timeSeriesCache`: Map<variableId, data[]>
- `scatterCache`: Map<`${varX}-${varY}`, data[]>

Limpia el cache cuando sea necesario:

```typescript
const { clearCache } = useDashboardStore();
clearCache();
```

## Estado de Carga y Errores

```typescript
const { loading, error } = useDashboardStore();

if (loading) return <LoadingIndicator />;
if (error) return <ErrorMessage message={error} />;
```

## Beneficios

✅ **Centralización**: Todos los datos de gráficas en un solo lugar  
✅ **Caché**: Evita llamadas innecesarias a la API  
✅ **Consistencia**: Valores base y rangos estandarizados  
✅ **Realismo**: Datos generados con patrones y correlaciones realistas  
✅ **Tipado**: TypeScript completo para todos los datos  
✅ **Escalabilidad**: Fácil migración a API real
