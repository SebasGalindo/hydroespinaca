# Nueva Variable Manual - Componentes Atómicos

Esta carpeta contiene los componentes para la vista de "Nueva Variable Manual" siguiendo principios de diseño atómico.

## Estructura de Componentes

### 🧩 Componentes Atómicos

#### `FormField.tsx`
- **Propósito**: Componente base para todos los campos de entrada
- **Tipos soportados**: text, number, email, textarea, select
- **Características**:
  - Validación visual de errores
  - Texto de ayuda
  - Estados disabled/required
  - Estilos consistentes con focus states

#### `FormSection.tsx`
- **Propósito**: Contenedor para agrupar campos relacionados
- **Características**:
  - Separadores visuales entre secciones
  - Títulos de sección consistentes
  - Espaciado uniforme

#### `FormButtons.tsx`
- **Propósito**: Botones de acción del formulario
- **Características**:
  - Estados de loading
  - Responsive design
  - Iconos integrados
  - Accesibilidad completa

### 🔧 Componentes Moleculares

#### `VariableTypeSelector.tsx`
- **Propósito**: Selector visual de tipo de variable
- **Tipos disponibles**:
  - 🌡️ Ambiental (temperatura, humedad, luz)
  - 🧪 Nutricional (pH, nutrientes, conductividad)
  - 📏 Física (nivel de agua, flujo, presión)
  - 🌱 Biológica (crecimiento de plantas)

#### `RangeInputs.tsx`
- **Propósito**: Configuración de rangos de valores
- **Características**:
  - Rango general de medición
  - Rango óptimo para el cultivo
  - Visualización gráfica de rangos
  - Validación de valores coherentes

#### `AlertsConfiguration.tsx`
- **Propósito**: Configuración de alertas críticas
- **Características**:
  - Toggle para activar/desactivar alertas
  - Configuración de rangos críticos
  - Tipos de notificación
  - Frecuencia de alertas

### 🏗️ Componente Organismo

#### `NewVariableForm.tsx`
- **Propósito**: Formulario completo que integra todos los componentes
- **Características**:
  - Gestión de estado centralizada
  - Validación de formulario
  - Envío de datos
  - Responsive design completo

## Diseño Responsive

### Breakpoints
- **Mobile**: < 768px
- **Tablet**: 768px - 1024px
- **Desktop**: > 1024px

### Adaptaciones por Pantalla

#### Mobile (< 768px)
- Formulario en una sola columna
- Botones apilados verticalmente
- Espaciado reducido
- Texto optimizado para lectura móvil

#### Tablet (768px - 1024px)
- Campos en 2 columnas donde sea apropiado
- Botones en fila
- Espaciado intermedio

#### Desktop (> 1024px)
- Layout completo en 2 columnas
- Máximo ancho del formulario: 1024px
- Espaciado completo
- Visualizaciones gráficas expandidas

## Principios de Diseño

### ♻️ Reutilización
- Todos los componentes son reutilizables
- Props bien definidas y tipadas
- Estilos consistentes mediante clases Tailwind

### 🧹 Mantenibilidad
- Separación clara de responsabilidades
- Componentes pequeños y enfocados
- Documentación inline con TypeScript

### 🎨 Consistencia Visual
- Paleta de colores verde coherente con el diseño
- Tipografía Inter para toda la interfaz
- Espaciado basado en múltiplos de 4px
- Bordes redondeados consistentes

### ♿ Accesibilidad
- Labels apropiadas para todos los campos
- Estados de focus visibles
- Contraste de colores adecuado
- Navegación por teclado
- Textos de ayuda descriptivos

## Uso

```tsx
import NewVariableForm from '@/components/dashboard/nueva-variable/NewVariableForm';

export default function NewVariablePage() {
  return (
    <div className="container">
      <NewVariableForm />
    </div>
  );
}
```

## Extensibilidad

Para agregar nuevos tipos de variables:
1. Actualizar el array `variableTypes` en `VariableTypeSelector.tsx`
2. Agregar validaciones específicas si es necesario
3. Actualizar los tipos TypeScript correspondientes

Para nuevos tipos de campo:
1. Extender el tipo `type` en `FormField.tsx`
2. Agregar el caso correspondiente en `renderInput()`
3. Actualizar las props si es necesario