# Sensores Config - Configuración de Sensores

Este módulo implementa la funcionalidad de configuración y calibración de sensores para el sistema de hidroponía, siguiendo los principios de diseño atómico y buenas prácticas de desarrollo.

## Estructura de Componentes

### Componentes Atómicos

#### `FormSection.tsx`
- **Propósito**: Contenedor reutilizable para secciones del formulario
- **Props**: `title`, `subtitle?`, `children`
- **Uso**: Agrupa elementos relacionados con título y subtítulo opcionales

#### `RangeSlider.tsx`
- **Propósito**: Visualización gráfica de rangos de sensores
- **Props**: `minRange`, `maxRange`, `optimalMin`, `optimalMax`, `currentValue`, `unit`
- **Características**:
  - Barra visual con zona óptima destacada
  - Indicador de valor actual con estado (óptimo/fuera de rango)
  - Información contextual y leyenda

### Componentes Moleculares

#### `SensorCard.tsx`
- **Propósito**: Tarjeta individual para cada sensor
- **Props**: `sensor`, `onToggle`, `onCalibrate`, `onSelect`
- **Características**:
  - Estado visual del sensor (activo/inactivo)
  - Información de calibración y valor actual
  - Botones de acción (toggle, calibrar)
  - Iconos dinámicos según tipo de sensor

#### `ConfigButtons.tsx`
- **Propósito**: Botones de acción del formulario
- **Props**: `onSave`, `onReset`, `isLoading?`, `saveText?`, `resetText?`
- **Características**:
  - Estados de carga
  - Información contextual sobre las acciones
  - Diseño responsivo

### Componentes Organismos

#### `CalibrationPanel.tsx`
- **Propósito**: Panel completo de calibración de sensores
- **Props**: `sensor`, `onClose`, `onSave`
- **Características**:
  - Proceso de calibración en 3 puntos
  - Configuración de rangos (medición y óptimo)
  - Instrucciones dinámicas según tipo de sensor
  - Validación de datos

#### `SensorsConfigForm.tsx`
- **Propósito**: Formulario principal de configuración
- **Estado**: Maneja lista de sensores, sensor seleccionado, estado de carga
- **Funcionalidades**:
  - Gestión de estado de sensores
  - Selección y calibración
  - Guardado de configuraciones

## Tipos de Datos

```typescript
interface Sensor {
  id: string;
  name: string;
  type: 'temperature' | 'ph' | 'humidity' | 'light';
  isActive: boolean;
  currentValue: number;
  unit: string;
  lastCalibration: string;
  minRange: number;
  maxRange: number;
  optimalMin: number;
  optimalMax: number;
  calibrationPoints: CalibrationPoint[];
}

interface CalibrationPoint {
  reference: number;
  measured: number;
}
```

## Flujo de Calibración

1. **Selección de Sensor**: Usuario selecciona sensor desde la lista
2. **Configuración de Rangos**: Ajuste de rangos de medición y óptimos
3. **Calibración en 3 Puntos**: 
   - Punto bajo (25% del rango)
   - Punto medio (50% del rango)
   - Punto alto (75% del rango)
4. **Validación**: Verificación de datos ingresados
5. **Guardado**: Persistencia de configuración

## Características de UX/UI

### Responsividad
- Diseño adaptable para desktop y móvil
- Grid responsivo para tarjetas de sensores
- Botones y formularios optimizados para touch

### Accesibilidad
- Contraste adecuado en todos los estados
- Indicadores visuales claros para estados
- Textos descriptivos y ayudas contextuales

### Feedback Visual
- Estados de carga en botones
- Indicadores de estado de sensores
- Visualización gráfica de rangos
- Alertas y confirmaciones

## Integración

### Página Principal
- Ruta: `/dashboard/sensores-config`
- Layout: `DashboardHeader` + `SensorsConfigForm` + `BottomNavigation`

### Dependencias
- Iconos: `@/components/ui/icons/Icons`
- Estilos: Tailwind CSS
- Estado: React hooks (useState, useEffect)

## Escalabilidad

### Extensiones Futuras
- Soporte para nuevos tipos de sensores
- Calibración automática
- Histórico de calibraciones
- Exportación de configuraciones
- Notificaciones en tiempo real

### Mantenibilidad
- Componentes desacoplados y reutilizables
- Tipado estricto con TypeScript
- Documentación inline en código
- Estructura modular y escalable