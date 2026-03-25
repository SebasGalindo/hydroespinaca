# Plan: CRUD Completo de Sistemas Fuzzy desde la Web

Fecha: 2026-02-15  
Objetivo: Permitir al usuario **crear, editar y eliminar** sistemas fuzzy completos (sistema + variables + términos + reglas) desde la interfaz web, con apoyo gráfico en tiempo real para funciones de membresía.

---

## 0) Análisis de Estado Actual

### Lo que YA existe y funciona

| Capa | Existe | Detalle |
|------|--------|---------|
| **fuzzy-service** | ✅ CRUD completo | Create/Read/Update/Delete para System, Variable, Term, Rule. 5 endpoints especiales (activate, clone, export, import, simulate) |
| **BFF** | 🟡 Solo lectura + operaciones | FuzzyController con 9 endpoints (list, detail, delete, activate, clone, export, import, simulate). **NO tiene create/update para ninguna entidad** |
| **Shared TS** | 🟡 Solo lectura + operaciones | FuzzyApiService con 9 métodos de lectura/operación. **NO tiene tipos ni métodos para create/update** |
| **Frontend** | 🟡 Solo visualización + operaciones | RoutineListPage + RoutineDetailPage. **NO tiene formularios de creación/edición** |

### Lo que FALTA implementar

| Capa | Trabajo |
|------|---------|
| **fuzzy-service** | ⚠️ Correcciones menores: auth faltante en controllers de Variable/Term/Rule, mismatch en endpoint de consequents |
| **BFF** | 🔴 14 endpoints nuevos: create/update System + CRUD Variable + CRUD Term + CRUD Rule + update status |
| **Shared TS** | 🔴 8 request types + 14 API methods + 14 store actions |
| **Frontend** | 🔴 ~15 componentes nuevos: formularios, builders, editores con gráfica en vivo |

---

## 1) Arquitectura del Flujo CRUD

```
Frontend (Forms)
  ↓ store action
Zustand Store (useFuzzyStore)
  ↓ api call
FuzzyApiService (authFetch)
  ↓ HTTP
BFF FuzzyController (.NET 9)
  ↓ FuzzyServiceClient (HttpClient)
fuzzy-service (Python/FastAPI)
  ↓ CQRS (Medyator)
MongoDB
```

### Patrón de formularios (consistente con BI)

```
Modal (isOpen, onClose, title)
  └─ FormField[] (type, label, value, onChange, error)
  └─ ActionButtons (onPrimary=submit, onSecondary=cancel)
```

Flujo de datos:
1. Form local `useState` para campos y errores
2. `validate()` manual (mismo patrón que BI)
3. `onSubmit(request)` → padre llama store action
4. Store → API Service → BFF → fuzzy-service
5. SweetAlert2 para confirmaciones y éxito/error

---

## 2) Modelo de Datos (referencia rápida)

### FuzzySystem
| Campo | Tipo | Create | Update | Notas |
|-------|------|--------|--------|-------|
| name | string (1-100) | ✅ Requerido | ✅ Opcional | Único |
| status | enum | ❌ (auto DRAFT) | ✅ PATCH | DRAFT/ACTIVE/INACTIVE/TESTING |
| defuzzification_method | enum | ✅ Opcional (default centroid) | ✅ Opcional | centroid/bisector/mom/som/lom/weighted_average |
| operators.and_method | string | ✅ Opcional (default min) | ✅ Opcional | min/prod |
| operators.or_method | string | ✅ Opcional (default max) | ✅ Opcional | max/sum/probor |
| operators.aggregation_method | string | ✅ Opcional | ✅ Opcional | max/sum/probor |
| operators.defuzzification_method | string | ✅ Opcional | ✅ Opcional | centroid/bisector/mom/som/lom |

### FuzzyVariable
| Campo | Tipo | Create | Update | Notas |
|-------|------|--------|--------|-------|
| name | string (1-100) | ✅ Requerido | ✅ Opcional | |
| description | string (0-500) | ✅ Opcional | ✅ Opcional | |
| variable_type | "input"/"output" | ✅ Requerido | ❌ No editable | |
| actuator_type | "PWM"/"DIGITAL" | ✅ Req. si output | ✅ Opcional | Prohibido en input |
| defuzzification_threshold | float (0-100) | ✅ Opcional (default 50) | ✅ Opcional | Solo para DIGITAL |
| universe_min | float | ✅ Opcional | ✅ Opcional | |
| universe_max | float | ✅ Opcional | ✅ Opcional | > universe_min |
| reference_code | string | ✅ Opcional | ✅ Opcional | Código sensor/actuador |

### FuzzyTerm
| Campo | Tipo | Create | Update | Notas |
|-------|------|--------|--------|-------|
| variable_id | ObjectId | ✅ Requerido | ❌ | Referencia a variable padre |
| label | string (1-30) | ✅ Requerido | ✅ Opcional | Ej: "baja", "media", "alta" |
| membership_function.function_type | enum | ✅ Requerido | ✅ Opcional | triangular/trapezoidal/gaussian/sigmoid/bell/pi_shaped/s_shaped/z_shaped/linear/constant |
| membership_function.parameters | float[] | ✅ Requerido | ✅ Opcional | Conteo depende del tipo |
| membership_function.universe_min | float | ✅ Requerido | ✅ Opcional | |
| membership_function.universe_max | float | ✅ Requerido | ✅ Opcional | |

### Parámetros por tipo de MF
| Tipo | Params | Descripción |
|------|--------|-------------|
| triangular | [a, b, c] | a < b < c |
| trapezoidal | [a, b, c, d] | a < b ≤ c < d |
| gaussian | [mean, sigma] | media, desv. estándar |
| sigmoid | [a, c] | pendiente, centro |
| bell | [a, b, c] | ancho, pendiente, centro |
| pi_shaped | [a, b, c, d] | 4 params |
| s_shaped | [a, b] | 2 params |
| z_shaped | [a, b] | 2 params |
| linear | [a, b] | pendiente, intercepto |
| constant | [c] | valor constante |

### FuzzyRule
| Campo | Tipo | Create | Update | Notas |
|-------|------|--------|--------|-------|
| name | string (1-100) | ✅ Requerido | ✅ Opcional | |
| system_id | ObjectId | ✅ Requerido | ❌ | |
| description | string | ✅ Opcional | ✅ Opcional | |
| conditions | Condition[] | ✅ Min 1 | ✅ Opcional | variableId + operator + value (term label) |
| connectors | ("AND"/"OR")[] | ✅ | ✅ Opcional | count = conditions - 1 |
| consequents | Consequent[] | ✅ Min 1 | ✅ Opcional | variable_id + terms[] + aggregation_method |

---

## 3) Plan de Implementación por Fases

### Fase 1: Backend — Correcciones fuzzy-service + BFF endpoints (Día 1)

> **Objetivo:** Tener todos los endpoints CRUD disponibles y seguros desde el BFF.

#### 1.1 fuzzy-service — Correcciones (1h)

| # | Tarea | Detalle |
|---|-------|---------|
| 1.1.1 | ~~Agregar auth a controllers de Variable/Term/Rule~~ | ⚠️ Estos controllers NO tienen auth — se decide si agregar o confiar en que el BFF ya autentica. **Decisión: se confía en el BFF + red Docker interna.** No se modifica el fuzzy-service para no romper la integración. |
| 1.1.2 | Verificar mismatch en `update_consequents` | El controller pasa `consequents` como query param (list), pero el command espera un string. Verificar y corregir si es necesario. |
| 1.1.3 | Verificar endpoint `add_condition` | Usa query params en vez de body para un POST. Evaluar si es problema para el BFF client. |

> **Nota:** Los endpoints CRUD del fuzzy-service ya existen y funcionan. El trabajo principal es en el BFF.

#### 1.2 BFF — Request DTOs nuevos (1h)

Crear en `BffService.Domain/DTOs/Fuzzy/`:

| DTO | Campos |
|-----|--------|
| `CreateFuzzySystemRequest` | Name (req), DefuzzificationMethod?, Operators? |
| `UpdateFuzzySystemRequest` | Name?, DefuzzificationMethod?, Operators?, InputVariableIds?, OutputVariableIds?, RuleIds? |
| `UpdateFuzzySystemStatusRequest` | Status (req) |
| `CreateFuzzyVariableRequest` | Name (req), VariableType (req), SystemId (req), Description?, ActuatorType?, DefuzzificationThreshold?, UniverseMin?, UniverseMax?, ReferenceCode? |
| `UpdateFuzzyVariableRequest` | Name?, Description?, ActuatorType?, DefuzzificationThreshold?, UniverseMin?, UniverseMax?, ReferenceCode? |
| `CreateFuzzyTermRequest` | VariableId (req), Label (req), MembershipFunction (req: FunctionType + Parameters + UniverseMin + UniverseMax) |
| `UpdateFuzzyTermRequest` | Label?, MembershipFunction? |
| `CreateFuzzyRuleRequest` | Name (req), SystemId (req), Description?, Conditions (req, min 1), Connectors, Consequents (req, min 1) |
| `UpdateFuzzyRuleRequest` | Name?, Description?, Conditions?, Connectors?, Consequents? |

#### 1.3 BFF — IFuzzyServiceClient + FuzzyServiceClient (2h)

Agregar métodos al interface y su implementación:

| Método | HTTP | Upstream |
|--------|------|----------|
| `CreateSystemAsync(request, token)` | POST | `/api/fuzzy-systems` |
| `UpdateSystemAsync(id, request, token)` | PUT | `/api/fuzzy-systems/{id}` |
| `UpdateSystemStatusAsync(id, request, token)` | PATCH | `/api/fuzzy-systems/{id}/status` |
| `CreateVariableAsync(request, token)` | POST | `/api/fuzzy-variables` |
| `UpdateVariableAsync(id, request, token)` | PUT | `/api/fuzzy-variables/{id}` |
| `DeleteVariableAsync(id, token)` | DELETE | `/api/fuzzy-variables/{id}` |
| `CreateTermAsync(request, token)` | POST | `/api/fuzzy-terms` |
| `UpdateTermAsync(id, request, token)` | PUT | `/api/fuzzy-terms/{id}` |
| `DeleteTermAsync(id, token)` | DELETE | `/api/fuzzy-terms/{id}` |
| `CreateRuleAsync(request, token)` | POST | `/api/fuzzy-rules` |
| `UpdateRuleAsync(id, request, token)` | PUT | `/api/fuzzy-rules/{id}` |
| `DeleteRuleAsync(id, token)` | DELETE | `/api/fuzzy-rules/{id}` |

#### 1.4 BFF — FuzzyController endpoints nuevos (2h)

| Método | Ruta BFF | Acción |
|--------|----------|--------|
| POST | `/fuzzy/systems` | Crear sistema |
| PUT | `/fuzzy/systems/{id}` | Actualizar sistema |
| PATCH | `/fuzzy/systems/{id}/status` | Cambiar status |
| POST | `/fuzzy/variables` | Crear variable |
| PUT | `/fuzzy/variables/{id}` | Actualizar variable |
| DELETE | `/fuzzy/variables/{id}` | Eliminar variable |
| POST | `/fuzzy/terms` | Crear término |
| PUT | `/fuzzy/terms/{id}` | Actualizar término |
| DELETE | `/fuzzy/terms/{id}` | Eliminar término |
| POST | `/fuzzy/rules` | Crear regla |
| PUT | `/fuzzy/rules/{id}` | Actualizar regla |
| DELETE | `/fuzzy/rules/{id}` | Eliminar regla |

#### 1.5 BFF — Documentación XML (0.5h)

Agregar `<summary>` a todos los nuevos DTOs, métodos y endpoints.

#### 1.6 BFF — Build verification (0.5h)

`dotnet build` exitoso para BffService.

---

### Fase 2: Shared TS Package — Tipos + API + Store (Día 2)

> **Objetivo:** Tipos TypeScript, métodos de API y store actions para todo el CRUD.

#### 2.1 Tipos nuevos en `types/fuzzy.ts` (1h)

| Tipo | Campos |
|------|--------|
| `CreateFuzzySystemRequest` | name, defuzzificationMethod?, operators? |
| `UpdateFuzzySystemRequest` | name?, defuzzificationMethod?, operators?, inputVariableIds?, outputVariableIds?, ruleIds? |
| `UpdateFuzzySystemStatusRequest` | status: FuzzySystemStatus |
| `CreateFuzzyVariableRequest` | name, variableType, systemId, description?, actuatorType?, defuzzificationThreshold?, universeMin?, universeMax?, referenceCode? |
| `UpdateFuzzyVariableRequest` | name?, description?, actuatorType?, defuzzificationThreshold?, universeMin?, universeMax?, referenceCode? |
| `CreateFuzzyTermRequest` | variableId, label, membershipFunction: { functionType, parameters, universeMin, universeMax } |
| `UpdateFuzzyTermRequest` | label?, membershipFunction? |
| `CreateFuzzyRuleRequest` | name, systemId, description?, conditions, connectors, consequents |
| `UpdateFuzzyRuleRequest` | name?, description?, conditions?, connectors?, consequents? |

Helpers adicionales:
| Tipo | Propósito |
|------|-----------|
| `MF_PARAM_COUNTS` | Record<MembershipFunctionType, number> — para validación |
| `MF_PARAM_LABELS` | Record<MembershipFunctionType, string[]> — labels para los inputs del formulario |
| `DEFUZZIFICATION_METHODS` | { value, label }[] — opciones para select |
| `AND_METHODS` / `OR_METHODS` / `AGGREGATION_METHODS` | { value, label }[] — opciones para selects de operadores |

#### 2.2 API methods en `api/fuzzyService.ts` (1.5h)

| Método | Endpoint BFF |
|--------|-------------|
| `createSystem(req)` | POST `/fuzzy/systems` |
| `updateSystem(id, req)` | PUT `/fuzzy/systems/{id}` |
| `updateSystemStatus(id, req)` | PATCH `/fuzzy/systems/{id}/status` |
| `createVariable(req)` | POST `/fuzzy/variables` |
| `updateVariable(id, req)` | PUT `/fuzzy/variables/{id}` |
| `deleteVariable(id)` | DELETE `/fuzzy/variables/{id}` |
| `createTerm(req)` | POST `/fuzzy/terms` |
| `updateTerm(id, req)` | PUT `/fuzzy/terms/{id}` |
| `deleteTerm(id)` | DELETE `/fuzzy/terms/{id}` |
| `createRule(req)` | POST `/fuzzy/rules` |
| `updateRule(id, req)` | PUT `/fuzzy/rules/{id}` |
| `deleteRule(id)` | DELETE `/fuzzy/rules/{id}` |

#### 2.3 Store actions en `store/fuzzyStore.ts` (2h)

Nuevo estado:
```
// CRUD operation states
crudLoading: boolean
crudError: string | null
```

Nuevas acciones:
```
// System CRUD
createSystem(req) → crea sistema, refetches systems
updateSystem(id, req) → actualiza, refetches detail si es el actual
updateSystemStatus(id, req) → cambia status, refetches
// Variable CRUD
createVariable(req) → crea, refetches detail
updateVariable(id, req) → actualiza, refetches detail
deleteVariable(id) → elimina, refetches detail
// Term CRUD
createTerm(req) → crea, refetches detail
updateTerm(id, req) → actualiza, refetches detail
deleteTerm(id) → elimina, refetches detail
// Rule CRUD
createRule(req) → crea, refetches detail
updateRule(id, req) → actualiza, refetches detail
deleteRule(id) → elimina, refetches detail
```

Patrón: cada acción de mutación → API call → `fetchSystemDetail(systemId)` para refrescar todo el detalle orquestado.

#### 2.4 Barrel exports + build verification (0.5h)

Actualizar index.ts, index.web.ts. `tsc --noEmit` exitoso.

---

### Fase 3: Frontend — Componentes base + Crear Sistema (Día 3)

> **Objetivo:** Crear los componentes reutilizables base y el formulario de creación de sistema.

#### 3.1 Atoms/Molecules nuevos reutilizables (2h)

| Componente | Tipo | Propósito |
|------------|------|-----------|
| `Tabs` | Atom | Componente reutilizable (actualmente copy-pasted). Props: `tabs: {id, label, icon?}[]`, `activeTab`, `onChange` |
| `TextArea` | Atom | Agregar type `'textarea'` a `FormField` existente (o crear standalone) |
| `NumberSlider` | Molecule | Input numérico con slider + campo editable. Props: `min, max, step, value, onChange, label` |
| `ConfirmDialog` | Molecule | Wrapper de SweetAlert2 para confirmaciones destructivas reutilizable |
| `StepIndicator` | Atom | Indicador de paso actual en flujos multi-step. Props: `steps: string[], currentStep: number` |

#### 3.2 Formulario: `CreateSystemForm` (3h)

Ubicación: `components/fuzzy/forms/CreateSystemForm.tsx`

| Sección | Campos |
|---------|--------|
| **Info básica** | Nombre (text, requerido) |
| **Defuzzificación** | Método (select: centroid/bisector/mom/som/lom/weighted_average) |
| **Operadores** | AND method (select: min/prod), OR method (select: max/sum/probor), Aggregation (select: max/sum/probor) |

Props:
```ts
interface CreateSystemFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (req: CreateFuzzySystemRequest) => Promise<void>;
  isLoading?: boolean;
}
```

Patrón: Modal + FormField + ActionButtons (consistente con BI forms).

#### 3.3 Formulario: `EditSystemForm` (2h)

Ubicación: `components/fuzzy/forms/EditSystemForm.tsx`

Igual que Create pero:
- Pre-populated con datos del sistema existente
- Agregar opción de cambiar status (DRAFT ↔ INACTIVE ↔ TESTING)
- Props incluyen `system: FuzzySystemDto`

#### 3.4 Integración con `RoutineListPage` (1h)

- Agregar botón "**+ Nuevo Sistema**" en el header
- Agregar botón "**Editar**" en `SystemActionButtons`
- Conectar formularios con store actions
- SweetAlert2 para feedback

#### 3.5 Integración con `RoutineDetailPage` (1h)

- Agregar botón "**Editar Sistema**" en `SystemInfoHeader`
- Agregar selector de cambio de status
- Conectar con store

#### 3.6 Build verification (1h)

---

### Fase 4: Frontend — CRUD de Variables (Día 4)

> **Objetivo:** Crear, editar y eliminar variables desde el detalle del sistema.

#### 4.1 Formulario: `CreateVariableForm` (3h)

Ubicación: `components/fuzzy/forms/CreateVariableForm.tsx`

| Sección | Campos |
|---------|--------|
| **Info básica** | Nombre (text), Descripción (textarea) |
| **Tipo** | Variable Type (select: input/output) — condiciona campos siguientes |
| **Actuador** (si output) | Actuator Type (select: PWM/DIGITAL), Defuzzification Threshold (number, solo DIGITAL) |
| **Universo** | Universe Min (number), Universe Max (number) |
| **Referencia** | Reference Code (text) — código del sensor o actuador |

Lógica condicional:
- Si `variable_type = "input"` → ocultar Actuator Type y Threshold
- Si `variable_type = "output"` → mostrar Actuator Type (requerido)
- Si `actuator_type = "DIGITAL"` → mostrar Threshold slider (0-100, default 50)

#### 4.2 Formulario: `EditVariableForm` (2h)

- Pre-populated, `variable_type` no editable (disabled)
- Props incluyen `variable: FuzzyVariableDto`

#### 4.3 Componente: `VariableCard` (2h)

Ubicación: `components/fuzzy/VariableCard.tsx`

Card individual para una variable con:
- Header: nombre, badge input/output, reference_code
- Body: universo (min-max), actuator type, threshold
- Conteo de términos
- Botones de acción: Editar, Eliminar, Ver Términos
- Preview mini de la MembershipFunctionChart (read-only)

#### 4.4 Refactorizar `VariableSection` (2h)

Agregar funcionalidad CRUD:
- Botón "+ Nueva Variable" arriba de la lista
- Cada variable renderizada como `VariableCard` con acciones
- Integrar `CreateVariableForm` y `EditVariableForm` como modals
- SweetAlert2 para confirmar eliminación
- Conectar con store actions

#### 4.5 Build verification (1h)

---

### Fase 5: Frontend — CRUD de Términos con Gráfica en Vivo (Día 5)

> **Objetivo:** Crear y editar términos lingüísticos con visualización en tiempo real de las funciones de membresía. Esta es la fase más compleja.

#### 5.1 Componente: `MembershipFunctionEditor` (4h) ⭐ CLAVE

Ubicación: `components/fuzzy/forms/MembershipFunctionEditor.tsx`

Este es el componente más importante y complejo. Permite configurar visualmente una función de membresía.

| Sección | Contenido |
|---------|-----------|
| **Tipo de MF** | Select con todos los tipos (triangular, trapezoidal, gaussian, etc.) |
| **Parámetros** | Inputs numéricos dinámicos según el tipo seleccionado. Labels descriptivos por tipo (ej: triangular → "Punto izquierdo (a)", "Pico (b)", "Punto derecho (c)") |
| **Universo** | Universe Min / Max (heredados de la variable padre, editables solo si no hay variable) |
| **Preview** | MembershipFunctionChart en tiempo real mostrando SOLO esta función |

Props:
```ts
interface MembershipFunctionEditorProps {
  value: MembershipFunctionDto;
  onChange: (mf: MembershipFunctionDto) => void;
  universeMin: number;
  universeMax: number;
  previewColor?: string;
  errors?: Record<string, string>;
}
```

Comportamiento:
- Al cambiar el tipo de MF → resetear parámetros a defaults lógicos (distribuidos uniformemente en el universo)
- Al cambiar cualquier parámetro → actualizar la gráfica en tiempo real (debounce 100ms)
- Validación en vivo: parámetros dentro del universo, orden correcto (ej: a < b < c para triangular)

**Defaults por tipo:**
| Tipo | Default params (para universo [0, 100]) |
|------|----------------------------------------|
| triangular | [25, 50, 75] |
| trapezoidal | [20, 40, 60, 80] |
| gaussian | [50, 15] |
| sigmoid | [0.1, 50] |
| bell | [20, 2, 50] |

#### 5.2 Formulario: `CreateTermForm` (3h)

Ubicación: `components/fuzzy/forms/CreateTermForm.tsx`

| Sección | Campos |
|---------|--------|
| **Label** | Nombre del término (text, 1-30 chars). Ej: "baja", "media", "alta" |
| **Función de membresía** | `MembershipFunctionEditor` (embebido) |

Props:
```ts
interface CreateTermFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (req: CreateFuzzyTermRequest) => Promise<void>;
  variableId: string;
  universeMin: number;
  universeMax: number;
  existingTerms: FuzzyTermDto[];  // para mostrar en la gráfica de contexto
  isLoading?: boolean;
}
```

#### 5.3 Formulario: `EditTermForm` (1h)

- Pre-populated con datos del término existente
- `MembershipFunctionEditor` pre-cargado
- Props incluyen `term: FuzzyTermDto`

#### 5.4 Componente: `TermsEditor` (3h) ⭐ CLAVE

Ubicación: `components/fuzzy/TermsEditor.tsx`

Panel completo para gestionar los términos de UNA variable. Muestra:

```
┌─────────────────────────────────────────────────────┐
│  Términos de "Temperatura Ambiente"                  │
│  [+ Nuevo Término]                                   │
├─────────────────────────────────────────────────────┤
│  ┌──────── Gráfica Combinada ────────┐              │
│  │  MembershipFunctionChart           │              │
│  │  (todos los términos juntos)       │              │
│  │  ← actualización en tiempo real →  │              │
│  └────────────────────────────────────┘              │
│                                                      │
│  ┌─ baja ─────┐ ┌─ media ────┐ ┌─ alta ─────┐      │
│  │ triangular  │ │ triangular │ │ triangular  │      │
│  │ [0,25,50]   │ │ [25,50,75] │ │ [50,75,100] │      │
│  │ [Editar][✕] │ │ [Editar][✕]│ │ [Editar][✕] │      │
│  └─────────────┘ └────────────┘ └─────────────┘      │
└─────────────────────────────────────────────────────┘
```

Funcionalidades:
- Gráfica `MembershipFunctionChart` con TODOS los términos actuales (la gráfica existente ya hace esto)
- Lista/grid de términos como cards pequeñas con info resumida
- Botón "+ Nuevo Término" → abre `CreateTermForm`
- Botón "Editar" en cada término → abre `EditTermForm`
- Botón "Eliminar" en cada término → SweetAlert2 confirmación
- **Cuando se abre el form de crear/editar, la gráfica muestra el término siendo editado resaltado** (color diferente o más grueso)

#### 5.5 Integrar `TermsEditor` en `VariableCard` expandido (1h)

Cuando el usuario hace click en "Ver Términos" en una `VariableCard`, se expande o abre un panel con `TermsEditor`.

Alternativa: el `TermsEditor` se muestra inline debajo de cada variable en `VariableSection` (acordeón expandible).

---

### Fase 6: Frontend — CRUD de Reglas con Rule Builder (Día 6)

> **Objetivo:** Crear y editar reglas fuzzy con un builder visual que muestre condiciones y consecuentes.

#### 6.1 Componente: `ConditionBuilder` (3h)

Ubicación: `components/fuzzy/forms/ConditionBuilder.tsx`

Builder visual para las condiciones de una regla:

```
┌─────────────────────────────────────────────────────┐
│  Condiciones                                         │
│                                                      │
│  SI  [Variable ▼]  [IS / IS_NOT ▼]  [Término ▼]    │
│  [AND / OR ▼]                                        │
│  [Variable ▼]  [IS / IS_NOT ▼]  [Término ▼]        │
│  [+ Agregar condición]                               │
│                                                      │
│  Preview: "SI Temperatura IS alta AND Humedad IS     │
│            baja"                                      │
└─────────────────────────────────────────────────────┘
```

Props:
```ts
interface ConditionBuilderProps {
  conditions: RuleConditionDto[];
  connectors: RuleConnector[];
  inputVariables: FuzzyVariableDto[];  // solo variables de entrada
  terms: FuzzyTermDto[];               // todos los términos del sistema
  onChange: (conditions: RuleConditionDto[], connectors: RuleConnector[]) => void;
  errors?: Record<string, string>;
}
```

Funcionalidades:
- Agregar/eliminar condiciones dinámicamente
- Select de variable → filtra términos disponibles
- Select de operador (IS / IS_NOT)
- Select de conector entre condiciones (AND / OR)
- Preview en texto natural: "SI X es Y AND Z es W"
- Validación: no variables duplicadas, al menos 1 condición

#### 6.2 Componente: `ConsequentBuilder` (3h)

Ubicación: `components/fuzzy/forms/ConsequentBuilder.tsx`

Builder para los consecuentes Mamdani:

```
┌─────────────────────────────────────────────────────┐
│  Consecuentes (ENTONCES)                             │
│                                                      │
│  [Variable de salida ▼]                              │
│  Términos: [✓ baja] [✓ media] [☐ alta]             │
│  Agregación: [max ▼]                                 │
│  [+ Agregar consecuente]                             │
│                                                      │
│  Preview: "ENTONCES Ventilador = {baja, media} (max)"│
└─────────────────────────────────────────────────────┘
```

Props:
```ts
interface ConsequentBuilderProps {
  consequents: RuleConsequentDto[];
  outputVariables: FuzzyVariableDto[];  // solo variables de salida
  terms: FuzzyTermDto[];
  onChange: (consequents: RuleConsequentDto[]) => void;
  errors?: Record<string, string>;
}
```

Funcionalidades:
- Agregar/eliminar consecuentes
- Select de variable de salida
- Checkboxes de términos (filtrados por variable seleccionada)
- Select de método de agregación (max/sum/probabilistic_or)
- Preview en texto natural
- Validación: al menos 1 consecuente, al menos 1 término seleccionado

#### 6.3 Formulario: `CreateRuleForm` (2h)

Ubicación: `components/fuzzy/forms/CreateRuleForm.tsx`

Modal con:
| Sección | Contenido |
|---------|-----------|
| **Info** | Nombre (text), Descripción (textarea, opcional) |
| **Condiciones** | `ConditionBuilder` |
| **Consecuentes** | `ConsequentBuilder` |
| **Preview** | Texto completo: "SI ... ENTONCES ..." |

Props:
```ts
interface CreateRuleFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (req: CreateFuzzyRuleRequest) => Promise<void>;
  systemId: string;
  inputVariables: FuzzyVariableDto[];
  outputVariables: FuzzyVariableDto[];
  terms: FuzzyTermDto[];
  isLoading?: boolean;
}
```

#### 6.4 Formulario: `EditRuleForm` (1h)

Pre-populated con datos existentes.

#### 6.5 Refactorizar `RulesSection` (2h)

Agregar funcionalidad CRUD:
- Botón "+ Nueva Regla" arriba de la tabla
- Botón "Editar" y "Eliminar" por fila
- Integrar `CreateRuleForm` y `EditRuleForm` como modals
- SweetAlert2 para confirmar eliminación

#### 6.6 Build verification (1h)

---

### Fase 7: Frontend — Integración completa + Flujo de Creación Guiado (Día 7)

> **Objetivo:** Integrar todo en un flujo cohesivo. Opcionalmente crear un wizard de creación paso a paso.

#### 7.1 Página: `CreateSystemPage` o `SystemWizard` (4h)

Flujo guiado multi-paso para crear un sistema completo:

```
Paso 1: Info del Sistema
  → nombre, método defuzz, operadores
  
Paso 2: Variables de Entrada
  → agregar variables + configurar términos con gráfica en vivo
  
Paso 3: Variables de Salida
  → agregar variables + configurar términos + tipo actuador
  
Paso 4: Reglas
  → crear reglas con ConditionBuilder + ConsequentBuilder
  
Paso 5: Revisión + Activación
  → resumen read-only, botón "Guardar como DRAFT" o "Activar"
```

Opciones de UX:
- **Opción A (Wizard modal):** Steps dentro de un modal grande (xl/2xl). Más sencillo de implementar.
- **Opción B (Página dedicada):** Ruta `/rutinas/crear` con `StepIndicator`. Más espacio, mejor UX.

**Recomendación: Opción B** — la complejidad del formulario (especialmente términos con gráficas) justifica una página completa.

#### 7.2 Ruta: `/rutinas/crear` (1h)

- `app/rutinas/crear/page.tsx` → renderiza el wizard
- Agregar botón "Crear Sistema" en `RoutineListPage` que navega a esta ruta

#### 7.3 Edición inline en `RoutineDetailPage` (3h)

Mejorar las tabs existentes para soportar edición:

| Tab | Mejora |
|-----|--------|
| **Info** | Botón "Editar" → `EditSystemForm` modal |
| **Variables** | `VariableSection` refactorizada con CRUD + `TermsEditor` expandible |
| **Reglas** | `RulesSection` refactorizada con CRUD |
| **Simular** | Sin cambios (ya funciona) |

#### 7.4 Testing manual E2E (2h)

- Crear un sistema nuevo desde el wizard
- Agregar variables input y output con términos y gráficas
- Crear reglas con condiciones y consecuentes
- Editar cada entidad
- Eliminar entidades
- Activar el sistema
- Simular

---

## 4) Resumen de Componentes por Tipo (Arquitectura Atómica)

### Atoms (reutilizables, sin lógica de negocio)

| Componente | Nuevo/Existente | Propósito |
|------------|----------------|-----------|
| `FormField` | ✏️ Extender | Agregar type `'textarea'` |
| `Tabs` | 🆕 Nuevo | Extraer de RoutineDetailPage, hacer reutilizable |
| `StepIndicator` | 🆕 Nuevo | Para el wizard de creación |
| `Badge` | ✅ Existe | Reutilizar para tipos de variable, MF, etc. |
| `Button` | ✅ Existe | Reutilizar |
| `Modal` | ✅ Existe | Reutilizar |
| `ActionButtons` | ✅ Existe | Reutilizar para formularios |

### Molecules (combinan atoms, poca lógica)

| Componente | Tipo | Propósito |
|------------|------|-----------|
| `NumberSlider` | 🆕 Nuevo | Input numérico con slider para parámetros de MF |
| `TermCard` | 🆕 Nuevo | Card mini para un término (label, tipo MF, params, acciones) |
| `ConditionRow` | 🆕 Nuevo | Una fila del condition builder (variable + operator + term + connector) |
| `ConsequentRow` | 🆕 Nuevo | Una fila del consequent builder (variable + terms checkboxes + aggregation) |
| `RulePreview` | 🆕 Nuevo | Texto "SI ... ENTONCES ..." construido dinámicamente |
| `VariableCard` | 🆕 Nuevo | Card de variable con info + acciones (editar/eliminar/ver términos) |

### Organisms (combinan molecules, lógica de negocio)

| Componente | Tipo | Propósito |
|------------|------|-----------|
| `MembershipFunctionEditor` | 🆕 Nuevo | Editor de MF con tipo + parámetros + preview gráfico |
| `TermsEditor` | 🆕 Nuevo | Panel de CRUD de términos con gráfica combinada en vivo |
| `ConditionBuilder` | 🆕 Nuevo | Builder de condiciones de regla |
| `ConsequentBuilder` | 🆕 Nuevo | Builder de consecuentes Mamdani |

### Components (páginas/secciones, lógica de dominio completa)

| Componente | Tipo | Propósito |
|------------|------|-----------|
| `CreateSystemForm` | 🆕 Nuevo | Modal de creación de sistema |
| `EditSystemForm` | 🆕 Nuevo | Modal de edición de sistema |
| `CreateVariableForm` | 🆕 Nuevo | Modal de creación de variable |
| `EditVariableForm` | 🆕 Nuevo | Modal de edición de variable |
| `CreateTermForm` | 🆕 Nuevo | Modal de creación de término con MF editor |
| `EditTermForm` | 🆕 Nuevo | Modal de edición de término con MF editor |
| `CreateRuleForm` | 🆕 Nuevo | Modal de creación de regla con builders |
| `EditRuleForm` | 🆕 Nuevo | Modal de edición de regla con builders |
| `SystemWizard` | 🆕 Nuevo | Página de creación guiada multi-paso |
| `VariableSection` | ✏️ Refactorizar | Agregar CRUD + TermsEditor |
| `RulesSection` | ✏️ Refactorizar | Agregar CRUD |
| `RoutineListPage` | ✏️ Extender | Agregar botón crear |
| `RoutineDetailPage` | ✏️ Extender | Agregar edición inline |

---

## 5) Orden de Ejecución Recomendado

| Fase | Duración | Entregable |
|------|----------|------------|
| **Fase 1** | ~7h | Backend completo: BFF endpoints + DTOs + client methods |
| **Fase 2** | ~5h | Shared TS: tipos + API + store con CRUD |
| **Fase 3** | ~10h | Crear/editar sistema + atoms base (Tabs, StepIndicator) |
| **Fase 4** | ~10h | CRUD variables + VariableCard |
| **Fase 5** | ~12h | CRUD términos + MembershipFunctionEditor + gráfica en vivo |
| **Fase 6** | ~12h | CRUD reglas + ConditionBuilder + ConsequentBuilder |
| **Fase 7** | ~10h | SystemWizard + integración + testing |
| **Total** | ~66h | |

---

## 6) Dependencias Técnicas

| Dependencia | Estado | Notas |
|-------------|--------|-------|
| `react-plotly.js` | ✅ Instalado | Para gráficas de MF en tiempo real |
| `sweetalert2` | ✅ Instalado | Para confirmaciones y feedback |
| `next/dynamic` (SSR: false) | ✅ Usado | Para Plotly.js |
| `@hydroespinaca/shared` | ✅ Existe | Extender con tipos/api/store |
| `FormField` | ✅ Existe | Extender con textarea |
| `Modal` | ✅ Existe | Reutilizar |
| `Table` | ✅ Existe | Reutilizar en reglas |
| `MembershipFunctionChart` | ✅ Existe | Reutilizar en TermsEditor y MembershipFunctionEditor |

---

## 7) Riesgos y Mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| MembershipFunctionEditor es complejo con muchos tipos de MF | Empezar con triangular/trapezoidal/gaussian (los más comunes), agregar el resto iterativamente |
| Performance de Plotly con re-renders frecuentes | Debounce de 100-150ms en onChange de parámetros, `React.memo` en el chart |
| Rule builder complejo con muchas combinaciones | Mantener validación estricta, preview en texto natural para feedback |
| Formularios con mucho estado local | Mantener patrón simple (useState + validate), evitar over-engineering |
| Flujo wizard largo puede confundir | StepIndicator claro, permitir guardar borrador en cualquier paso |
| Variables/términos/reglas son interdependientes | Refetch completo del detail desde el BFF después de cada mutación |
