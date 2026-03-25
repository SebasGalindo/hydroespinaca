# Semana 2 (6 días × 10h) — Rutinas Fuzzy: Plataforma de Experimentación

Duración estimada: **60h**
Objetivo macro: evolucionar el `fuzzy-service` existente para que el usuario pueda **crear, guardar, duplicar, activar y alternar entre múltiples rutinas** de control difuso, administrables desde la web con seguridad de sesión, convirtiendo el sistema en una **plataforma de experimentación** para el agricultor.

> Nota: Las vistas frontend de BI (originalmente planificadas para Semana 2) ya fueron adelantadas y completadas en la Semana 1. Esto libera la Semana 2 para dedicarse **100% a Rutinas Fuzzy**.

---

## 0) Progreso General

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| **Backend — fuzzy-service (Python/FastAPI)** | | | |
| 1 | Endpoint `POST /{id}/activate` (activación exclusiva) | ✅ Completado | Desactiva otros, activa el objetivo |
| 2 | Endpoint `POST /{id}/clone` (duplicar sistema completo) | ✅ Completado | Deep copy: sistema + variables + términos + reglas |
| 3 | Endpoint `GET /{id}/export` (serialización JSON completa) | ✅ Completado | Para backup/compartir rutinas |
| 4 | Endpoint `POST /import` (importar sistema desde JSON) | ✅ Completado | Validación + creación atómica |
| 5 | Endpoint `POST /{id}/simulate` (simular sin persistir) | ✅ Completado | Evaluar con inputs arbitrarios |
| 6 | Revisión de validaciones y reglas de negocio | ✅ Completado | Solo 1 ACTIVE, editable solo en DRAFT/INACTIVE |
| **Backend — BFF (.NET 9)** | | | |
| 7 | `FuzzyController` dedicado con endpoints tipados | ✅ Completado | 9 endpoints tipados + endpoint orquestado `/detail` |
| 8 | `IFuzzyServiceClient` expandido | ✅ Completado | CRUD + activate + clone + simulate + export/import + variables/terms/rules |
| 9 | DTOs fuzzy en BFF Domain | ✅ Completado | 7 archivos DTO: System, Variable, Term, Rule, MembershipFunction, Detail, Requests |
| 10 | docker-compose: agregar fuzzy-service a depends_on del BFF | ✅ Completado | + env var `Services__FuzzyService__Url` |
| **Shared TS Package** | | | |
| 11 | Tipos TypeScript completos para fuzzy | ✅ Completado | ~260 líneas, alineados con DTOs del fuzzy-service real |
| 12 | `FuzzyApiService` (API service completo) | ✅ Completado | 9 métodos: getSystems, getDetail, activate, clone, delete, export, import, simulate |
| 13 | `useFuzzyStore` (Zustand store) | ✅ Completado | ~271 líneas, estado para sistemas, detalle, simulación, operaciones |
| 14 | Barrel exports actualizados | ✅ Completado | types, api, store en index.ts e index.web.ts |
| **Frontend Web** | | | |
| 15 | `MembershipFunctionChart` — visualización Plotly.js | ✅ Completado | 5 tipos de MF soportados (triangular, trapezoidal, gaussian, sigmoid, bell) |
| 16 | `VariableSection` — variables + términos + gráficas | ✅ Completado | Con BaseCard, Badge, chart por variable |
| 17 | `RulesSection` — tabla/cards de reglas | ✅ Completado | Con Table atom, consecuentes Mamdani |
| 18 | `FuzzySystemCard` — refactorizar con acciones | ✅ Completado | Botones: activar, duplicar, exportar, eliminar via SystemActionButtons |
| 19 | `RoutineListPage` — lista de rutinas/sistemas | ✅ Completado | Grid + filtros por status y búsqueda + acciones |
| 20 | `RoutineDetailPage` — detalle completo de un sistema | ✅ Completado | Tabs: Info + Variables/Términos + Reglas + Simular |
| 21 | Ruta `/rutinas` + navegación actualizada | ✅ Completado | Page route + SideNav + BottomNav con BrainIcon |
| 22 | Build verification (shared + web) | ✅ Completado | `tsc --noEmit` exitoso |
| **Documentación XML (C# services)** | | | |
| 23 | Documentación XML — BI Service | ✅ Completado | 70 archivos documentados (en español) |
| 24 | Documentación XML — Sensor Service | ✅ Completado | 151 archivos documentados (en inglés) |
| 25 | Documentación XML — Auth Service | ✅ Completado | 165 archivos documentados (en inglés) |
| 26 | Documentación XML — BFF Service | ✅ Completado | 87 archivos documentados (en inglés) |
| 27 | Documentación XML — Actuator Service | ✅ Completado | 70 archivos documentados (en inglés) |
| **Pendientes (para mañana)** | | | |
| 28 | Tests de integración — fuzzy-service (activate, clone, export, import, simulate) | 🔲 Pendiente | Solo existen tests de dominio (12 archivos), faltan tests API |
| 29 | Tests frontend — componentes fuzzy | 🔲 Pendiente | No existen tests para componentes fuzzy en web |
| 30 | Tests shared package — fuzzy service/store | 🔲 Pendiente | No existen tests para FuzzyApiService ni useFuzzyStore |

---

### Resumen de progreso

- **22/22 tareas originales completadas** ✅
- **5 servicios C# documentados con XML** (543 archivos total) ✅
- **3 tareas de testing pendientes** para mañana 🔲
- **Funcionalidad de CRUD de sistemas fuzzy desde la web: NO EXISTE** — ver sección 14

---

## 1) Objetivo de la semana

### 1.1 Entregable (Definition of Done de Semana 2)

Al final del día 6:

1) El usuario puede **listar todos los sistemas fuzzy** desde la web con sus estados (DRAFT, ACTIVE, INACTIVE, TESTING).
2) El usuario puede **ver el detalle** de un sistema: variables con gráficas de funciones de membresía (Plotly.js), términos lingüísticos, y reglas.
3) El usuario puede **activar** un sistema como "la rutina vigente" (desactivando automáticamente cualquier otro activo).
4) El usuario puede **duplicar** un sistema completo (deep copy) para experimentar con variaciones.
5) El usuario puede **exportar** un sistema a JSON e **importar** uno desde JSON.
6) El usuario puede **simular** una evaluación con inputs arbitrarios sin afectar el estado real.
7) Todo funciona a través del **BFF** con seguridad de sesión (no acceso directo al fuzzy-service).
8) El frontend compila sin errores TypeScript.

### 1.2 Qué NO entra (se aplaza)
- Editor visual de variables/términos/reglas (crear/editar desde UI) — **Semana 3** o posterior.
- Comparador A/B de rutinas por KPIs — **Semana 3**.
- Dashboard de rendimiento por rutina — **Semana 3**.
- Asistente RAG para crear rutinas — **Semana 7**.

---

## 2) Análisis de lo que ya existe

### 2.1 fuzzy-service (Python 3.12 + FastAPI) — MADURO

El fuzzy-service ya es un microservicio robusto con:

| Capa | Qué existe | Estado |
|------|-----------|--------|
| **Domain** | `FuzzySystem` (name, status DRAFT/ACTIVE/INACTIVE/TESTING, defuzzification_method, operators, variable IDs, rule IDs, `created_by`) | ✅ Maduro |
| **Domain** | `FuzzyVariable` (name, type input/output, actuator_type PWM/DIGITAL, reference_code, terms, universe_min/max) | ✅ Maduro |
| **Domain** | `FuzzyTerm` (label, variable_id, MembershipFunction: triangular/trapezoidal/gaussian/sigmoid/bell) | ✅ Maduro |
| **Domain** | `FuzzyRule` (name, system_id, conditions IS/IS_NOT, connectors AND/OR, consequents Mamdani) | ✅ Maduro |
| **Domain** | `FuzzyEvaluation` (system_id, timestamp, inputs, activated_rules, output_values) | ✅ Maduro |
| **Application** | CQRS completo: Create/Update/Delete/GetAll/GetById para System, Variable, Term, Rule, Evaluation | ✅ Maduro |
| **Application** | `UpdateFuzzySystemStatusCommand` — cambia status del sistema | ✅ Existe |
| **Infrastructure** | `ScikitFuzzyEngine` — motor Mamdani completo (1095 líneas, fuzzificación → evaluación de reglas → defuzzificación) | ✅ Maduro |
| **Infrastructure** | MQTT subscriber → fuzzification → actuator commands pipeline | ✅ Maduro |
| **Infrastructure** | MongoDB repos para todas las entidades, JWT + M2M auth | ✅ Maduro |
| **Infrastructure** | `SeedData.py` — sistema completo con variables, términos y reglas de ejemplo (1438 líneas) | ✅ Maduro |

**Lo que FALTA en fuzzy-service:**

| Feature | Descripción | Prioridad |
|---------|-------------|-----------|
| `activate` exclusivo | `PATCH /status` existe pero NO desactiva otros sistemas. Necesita lógica: "desactivar todos → activar este" atómicamente | Alta |
| `clone` (deep copy) | No existe. Necesita duplicar sistema + todas sus variables + todos los términos + todas las reglas | Alta |
| `export` / `import` | No existe. Serialización JSON completa de un sistema con todos sus hijos | Media |
| `simulate` | No existe. Evaluar con inputs arbitrarios sin persistir la evaluación | Media |
| Filtro por `created_by` | `created_by` existe en la entidad pero las queries no filtran por usuario | Baja |

### 2.2 BFF — MÍNIMO para fuzzy

| Qué existe | Estado |
|-----------|--------|
| `IFuzzyServiceClient` con solo `GetFuzzyRuleSummariesAsync()` | Mínimo |
| `FuzzyServiceClient` implementando solo `GET /api/fuzzy-rules/all-names-descriptions` | Mínimo |
| `ProxyController` en `/proxy/fuzzy/**` que redirige a `fuzzy-service:8000` | Funciona (sin tipado) |
| DTOs: solo `FuzzyRuleSummaryDto` (Id, Name, Description) | Mínimo |
| `FuzzyService__Url` configurado en `ExternalServicesConfiguration.cs` | ✅ |

**Lo que FALTA en BFF:**

| Feature | Descripción |
|---------|-------------|
| `FuzzyController` dedicado | Endpoints tipados `/fuzzy/systems/*`, `/fuzzy/systems/{id}/activate`, `/fuzzy/systems/{id}/clone` |
| `IFuzzyServiceClient` expandido | Métodos para CRUD + activate + clone + simulate + export/import |
| DTOs completos | `FuzzySystemDto`, `FuzzyVariableDto`, `FuzzyTermDto`, `FuzzyRuleDto`, request/response types |
| docker-compose dependency | `fuzzy-service` no está en `depends_on` del BFF |

### 2.3 Shared TS Package — MÍNIMO

| Qué existe | Estado |
|-----------|--------|
| `FuzzyRuleSummary` type (id, name, description) | Mínimo |
| `FuzzyRulesService` con solo `getFuzzyRules()` | Mínimo |

### 2.4 Frontend Web — PARCIAL

| Qué existe | Estado | Acción |
|-----------|--------|--------|
| `FuzzySystemCard.tsx` — card con nombre, status, reglas/variables/rutinas preview | ✅ Existe en proyecto actual | Refactorizar: agregar botones de acción |
| `FuzzyRulesInfo.tsx` — modal read-only de resumen de reglas | ✅ Existe en proyecto actual | Mantener como está (dashboard) |

### 2.5 Proyecto anterior (`old_frontend_version`) — COMPONENTES MIGRABLES

Componentes del proyecto anterior que se pueden **migrar y adaptar** al nuevo:

| Componente anterior | Qué hace | Plan de migración |
|---------------------|----------|-------------------|
| `MembershipChart.tsx` | Plotly.js: renderiza curvas triangular/trapezoidal/gaussian con colores y fill | **Migrar**: adaptar tipos a los nuevos DTOs del fuzzy-service real, usar `hidro-card` styling |
| `VariableSection.tsx` | Lista variables con badge input/output, grid de términos, chart por variable | **Migrar**: adaptar a nuevos tipos, integrar con `BaseCard` y atoms existentes |
| `RulesSection.tsx` | Tabla desktop + cards mobile, construye "SI... ENTONCES..." strings | **Migrar**: adaptar a consecuentes Mamdani (no rutinas), usar `Table` atom existente |
| `RoutinesSection.tsx` | Cards con pasos de rutina (condición + potencia + duración) | **NO migrar**: el modelo actual es Mamdani (sin rutinas separadas como entidad). La "rutina" = FuzzySystem |
| `sistemas-fuzzy/page.tsx` | Grid de FuzzySystemCards con navegación a detalle | **Migrar**: adaptar a API real, agregar acciones (activar, duplicar, eliminar) |
| `sistemas-fuzzy/[id]/page.tsx` | Detalle con VariableSection + RulesSection + RoutinesSection | **Migrar**: adaptar a API real, quitar RoutinesSection, agregar info de sistema |
| `fuzzyStore.ts` (Zustand) | Store con CRUD local + computed getters + mock data | **Reescribir**: conectar a API real via `FuzzyApiService`, eliminar mocks |
| `fuzzyTypes.ts` | Tipos con ObjectId/MongoDate + tipos simplificados | **Reescribir**: alinear con DTOs reales del fuzzy-service (que ya no usa ObjectId wrapper) |

**Decisiones clave de migración:**
1. **Rutinas ≠ entidad separada**: En el proyecto anterior existían `FuzzyRoutine` como entidad separada con `steps`. En el modelo actual del fuzzy-service, las "rutinas" son los `FuzzySystem` completos (cada sistema = una rutina de control). Los consecuentes son Mamdani (apuntan a output variables + términos + agregación), no a rutinas.
2. **MembershipChart se reutiliza casi tal cual**: Solo necesita adaptar tipos de `SimpleFuzzyVariable`/`SimpleFuzzyTerm` a los nuevos DTOs.
3. **Store se reescribe completamente**: El anterior usaba mocks. El nuevo conecta a API real con loading/error states por dominio.

---

## 3) Modelo de datos (ya existente en MongoDB)

El fuzzy-service ya tiene las colecciones. No se crean colecciones nuevas. Solo se agregan endpoints.

### 3.1 `fuzzy_systems` (existente)
```
{ _id, name, status, defuzzification_method, operators, input_variable_ids, output_variable_ids, rule_ids, created_at, updated_at, created_by }
```
Statuses: `DRAFT`, `ACTIVE`, `INACTIVE`, `TESTING`

### 3.2 `fuzzy_variables` (existente)
```
{ _id, name, variable_type, actuator_type, reference_code, terms, defuzzification_threshold, universe_min, universe_max, created_at, updated_at }
```

### 3.3 `fuzzy_terms` (existente)
```
{ _id, variable_id, label, membership_function: { function_type, parameters, universe_min, universe_max }, created_at, updated_at }
```

### 3.4 `fuzzy_rules` (existente)
```
{ _id, name, system_id, description, conditions: [{ variableId, operator, value }], connectors: ['AND'|'OR'], consequents: [{ variable_id, terms, aggregation_method }], created_at, updated_at }
```

### 3.5 `fuzzy_evaluations` (existente)
```
{ _id, system_id, timestamp, inputs: [{ variable_id, reference_code, value }], activated_rules: [{ rule_id, activation_strength, output_values }] }
```

---

## 4) Endpoints nuevos — fuzzy-service

### 4.1 Activación exclusiva

| Método | Ruta | Scope | Descripción |
|--------|------|-------|-------------|
| `POST` | `/api/fuzzy-systems/{id}/activate` | `fuzzy:system:update` | Desactiva todos los sistemas ACTIVE → activa el indicado |

**Lógica:**
1. Validar que el sistema tiene variables de entrada, salida y reglas.
2. Buscar todos los sistemas con `status = ACTIVE` y cambiarlos a `INACTIVE`.
3. Cambiar el sistema indicado a `ACTIVE`.
4. Retornar el sistema actualizado.
5. Si el sistema ya está ACTIVE, no-op (retornar 200).

### 4.2 Clonar sistema

| Método | Ruta | Scope | Descripción |
|--------|------|-------|-------------|
| `POST` | `/api/fuzzy-systems/{id}/clone` | `fuzzy:system:create` | Deep copy del sistema con todas sus dependencias |

**Lógica:**
1. Cargar el sistema original + todas sus variables + todos los términos de esas variables + todas las reglas.
2. Crear nuevas entidades con nuevos IDs:
   - Sistema: `name = "Copia de {nombre}" `, `status = DRAFT`.
   - Variables: nuevos IDs, mismos datos.
   - Términos: nuevos IDs, `variable_id` actualizado al nuevo ID de variable.
   - Reglas: nuevos IDs, `system_id` actualizado, `conditions.variableId` actualizados, `consequents.variable_id` actualizados.
3. Persistir todo en transacción.
4. Retornar el nuevo sistema con sus conteos.

**Request body (opcional):**
```json
{ "name": "Mi variación experimental" }
```

### 4.3 Export / Import

| Método | Ruta | Scope | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/fuzzy-systems/{id}/export` | `fuzzy:system:read` | JSON completo del sistema + hijos |
| `POST` | `/api/fuzzy-systems/import` | `fuzzy:system:create` | Crear sistema desde JSON exportado |

**Formato de export:**
```json
{
  "version": "1.0",
  "exportedAt": "2026-02-14T...",
  "system": { "name": "...", "defuzzification_method": "...", "operators": {...} },
  "variables": [ { "name": "...", "variable_type": "...", ... } ],
  "terms": [ { "variable_ref": 0, "label": "...", "membership_function": {...} } ],
  "rules": [ { "name": "...", "conditions": [...], "consequents": [...] } ]
}
```
> Nota: `variable_ref` usa índices de posición (no IDs) para portabilidad.

### 4.4 Simulación

| Método | Ruta | Scope | Descripción |
|--------|------|-------|-------------|
| `POST` | `/api/fuzzy-systems/{id}/simulate` | `fuzzy:system:read` | Evaluar con inputs arbitrarios sin persistir |

**Request:**
```json
{
  "inputs": [
    { "reference_code": "T_AMB", "value": 32.5 },
    { "reference_code": "H_REL", "value": 45.0 }
  ]
}
```

**Response:**
```json
{
  "system_id": "...",
  "system_name": "...",
  "inputs": [...],
  "activated_rules": [
    { "rule_id": "...", "rule_name": "...", "activation_strength": 0.75, "output_values": [...] }
  ],
  "final_outputs": [
    { "variable_name": "Ventilador", "reference_code": "FAN", "crisp_value": 85.2, "actuator_type": "PWM" }
  ],
  "simulated_at": "2026-02-14T..."
}
```

---

## 5) Endpoints nuevos — BFF

### 5.1 FuzzyController

| BFF Route | Método | Tipo | Descripción |
|-----------|--------|------|-------------|
| `GET /fuzzy/systems` | GET | Forward | Listar todos los sistemas (con filtros opcionales) |
| `GET /fuzzy/systems/{id}` | GET | Forward | Obtener sistema por ID |
| `GET /fuzzy/systems/{id}/detail` | GET | **Orquesta** | Sistema + variables + términos + reglas en un solo response |
| `POST /fuzzy/systems/{id}/activate` | POST | Forward | Activación exclusiva |
| `POST /fuzzy/systems/{id}/clone` | POST | Forward | Clonar sistema |
| `GET /fuzzy/systems/{id}/export` | GET | Forward | Exportar sistema a JSON |
| `POST /fuzzy/systems/import` | POST | Forward | Importar sistema desde JSON |
| `POST /fuzzy/systems/{id}/simulate` | POST | Forward | Simular evaluación |
| `DELETE /fuzzy/systems/{id}` | DELETE | Forward | Eliminar sistema (solo DRAFT/INACTIVE) |

### 5.2 Endpoint compuesto: `GET /fuzzy/systems/{id}/detail`

El BFF orquesta múltiples llamadas al fuzzy-service para devolver un **detalle completo** en un solo request:

```
BFF recibe GET /fuzzy/systems/{id}/detail
  → GET /api/fuzzy-systems/{id}                    → sistema
  → GET /api/fuzzy-variables?system_id={id}        → variables
  → Para cada variable: GET terms (ya incluidos)   → términos
  → GET /api/fuzzy-rules?system_id={id}            → reglas
  ← Retorna { system, variables, terms, rules }
```

**¿Por qué orquestar?** Evitar que el frontend haga 4+ requests separados para mostrar el detalle.

### 5.3 DTOs en BFF Domain

```
BffService.Domain/DTOs/Fuzzy/
├── FuzzySystemDto.cs
├── FuzzySystemDetailDto.cs    (sistema + variables + términos + reglas)
├── FuzzyVariableDto.cs
├── FuzzyTermDto.cs
├── MembershipFunctionDto.cs
├── FuzzyRuleDto.cs
├── RuleConditionDto.cs
├── RuleConsequentDto.cs
├── CloneSystemRequest.cs
├── SimulateRequest.cs
├── SimulateResponse.cs
├── ExportSystemResponse.cs
└── ImportSystemRequest.cs
```

---

## 6) Shared TS Package — Tipos y servicios

### 6.1 Tipos (`packages/shared/src/types/fuzzy.ts`)

Alineados con los DTOs del fuzzy-service real (no con el modelo anterior que usaba ObjectId):

```typescript
// Enums
FuzzySystemStatus: 'DRAFT' | 'ACTIVE' | 'INACTIVE' | 'TESTING'
VariableType: 'input' | 'output'
ActuatorType: 'PWM' | 'DIGITAL'
MembershipFunctionType: 'triangular' | 'trapezoidal' | 'gaussian' | 'sigmoid' | 'bell'
RuleOperator: 'IS' | 'IS_NOT'
RuleConnector: 'AND' | 'OR'

// Interfaces principales
FuzzySystemDto, FuzzyVariableDto, FuzzyTermDto, MembershipFunctionDto
FuzzyRuleDto, RuleConditionDto, RuleConsequentDto
FuzzySystemDetailDto (sistema + variables[] + terms[] + rules[])

// Request/Response
CloneSystemRequest, SimulateRequest, SimulateResponse
ExportSystemResponse, ImportSystemRequest

// Mapas de labels (para UI)
FUZZY_STATUS_LABELS, FUZZY_STATUS_COLORS
VARIABLE_TYPE_LABELS, MF_TYPE_LABELS
```

### 6.2 API Service (`packages/shared/src/api/fuzzyService.ts`)

```typescript
class FuzzyApiService {
  // Sistemas
  getSystems(filters?): Promise<FuzzySystemDto[]>
  getSystemById(id): Promise<FuzzySystemDto>
  getSystemDetail(id): Promise<FuzzySystemDetailDto>  // endpoint compuesto del BFF
  deleteSystem(id): Promise<void>

  // Acciones sobre sistema
  activateSystem(id): Promise<FuzzySystemDto>
  cloneSystem(id, name?): Promise<FuzzySystemDto>
  exportSystem(id): Promise<ExportSystemResponse>
  importSystem(data): Promise<FuzzySystemDto>
  simulateSystem(id, inputs): Promise<SimulateResponse>

  // Variables (lectura — el CRUD de variables/términos/reglas se agrega en semana 3)
  getVariablesBySystem(systemId): Promise<FuzzyVariableDto[]>
  
  // Reglas (lectura)
  getRulesBySystem(systemId): Promise<FuzzyRuleDto[]>
}
```

### 6.3 Store (`packages/shared/src/store/fuzzyStore.ts`)

```typescript
interface FuzzyStoreState {
  // Listas
  systems: FuzzySystemDto[]
  systemsLoading: boolean
  systemsError: string | null

  // Detalle
  currentDetail: FuzzySystemDetailDto | null
  detailLoading: boolean
  detailError: string | null

  // Simulación
  simulationResult: SimulateResponse | null
  simulationLoading: boolean
}

interface FuzzyStoreActions {
  fetchSystems(): Promise<void>
  fetchSystemDetail(id: string): Promise<void>
  activateSystem(id: string): Promise<void>
  cloneSystem(id: string, name?: string): Promise<void>
  deleteSystem(id: string): Promise<void>
  simulateSystem(id: string, inputs: SimulateInput[]): Promise<void>
  exportSystem(id: string): Promise<ExportSystemResponse>
  importSystem(data: ImportSystemRequest): Promise<void>
  clearDetail(): void
  clearSimulation(): void
  clearErrors(): void
}
```

---

## 7) Frontend Web — Componentes

### 7.1 Componentes migrados del proyecto anterior (adaptar)

| Componente | Ubicación nueva | Cambios respecto al original |
|------------|----------------|------------------------------|
| `MembershipFunctionChart` | `components/fuzzy/MembershipFunctionChart.tsx` | Adaptar tipos (`FuzzyVariableDto`/`FuzzyTermDto`), agregar soporte `sigmoid`/`bell`, usar `hidro-card` |
| `VariableSection` | `components/fuzzy/VariableSection.tsx` | Usar `BaseCard`, `Badge` atoms, nuevos tipos DTO, quitar `device_id` (ahora es `reference_code`) |
| `RulesSection` | `components/fuzzy/RulesSection.tsx` | Usar `Table` atom, adaptar a consecuentes **Mamdani** (variable + términos, no rutinas), responsive |

### 7.2 Componentes nuevos

| Componente | Ubicación | Descripción |
|------------|-----------|-------------|
| `SystemStatusBadge` | `components/fuzzy/SystemStatusBadge.tsx` | Badge coloreado por status: DRAFT=gris, ACTIVE=verde, INACTIVE=rojo, TESTING=amarillo |
| `SystemActionButtons` | `components/fuzzy/SystemActionButtons.tsx` | Botones: Activar (si no activo), Duplicar, Exportar, Eliminar (si DRAFT/INACTIVE). SweetAlert2 confirmaciones |
| `SystemInfoHeader` | `components/fuzzy/SystemInfoHeader.tsx` | Header del detalle: nombre, status badge, método defuzz, operadores, conteos, fecha |
| `SimulationPanel` | `components/fuzzy/SimulationPanel.tsx` | Panel: inputs numéricos por variable de entrada, botón "Simular", resultado con reglas activadas y outputs |
| `RoutineListPage` | `components/fuzzy/RoutineListPage.tsx` | Grid de `FuzzySystemCard` + filtros (status, búsqueda) + botones "Importar JSON" |
| `RoutineDetailPage` | `components/fuzzy/RoutineDetailPage.tsx` | Tabs: Info + Variables/Términos + Reglas + Simular. Carga `getSystemDetail()` |
| `FuzzyPage` | `components/fuzzy/FuzzyPage.tsx` | Wrapper: `PageLayout` + `PageHeader` + contenido según ruta |

### 7.3 Páginas / Rutas

| Ruta | Archivo | Componente | Descripción |
|------|---------|------------|-------------|
| `/rutinas` | `app/rutinas/page.tsx` | `RoutineListPage` | Lista de todos los sistemas fuzzy |
| `/rutinas/[id]` | `app/rutinas/[id]/page.tsx` | `RoutineDetailPage` | Detalle de un sistema específico |

### 7.4 Navegación

Agregar a `SideNavigation.tsx` y `BottomNavigation.tsx`:
```
{ href: '/rutinas', label: 'Rutinas Fuzzy', icon: BrainIcon }
```
> `BrainIcon` ya existe en `Icons.tsx` (confirmado en investigación).

---

## 8) Plan por día (60h)

### Día 1 (10h) — Backend fuzzy-service: activate + clone

1. 🔲 Crear `ActivateFuzzySystemCommand` + handler (desactivar todos ACTIVE → activar target).
2. 🔲 Crear `CloneFuzzySystemCommand` + handler (deep copy sistema + variables + términos + reglas).
3. 🔲 Agregar endpoints en `fuzzy_system_controller.py`: `POST /{id}/activate`, `POST /{id}/clone`.
4. 🔲 Escribir tests unitarios para activate y clone.
5. 🔲 Verificar reglas de negocio: solo 1 ACTIVE, solo clonar sistemas con variables/reglas.

### Día 2 (10h) — Backend fuzzy-service: export + import + simulate

1. 🔲 Crear `ExportFuzzySystemQuery` + handler (serializar sistema completo a JSON portátil).
2. 🔲 Crear `ImportFuzzySystemCommand` + handler (deserializar + crear entidades + persistir).
3. 🔲 Crear `SimulateFuzzySystemQuery` + handler (usar `ScikitFuzzyEngine` sin persistir `FuzzyEvaluation`).
4. 🔲 Agregar endpoints: `GET /{id}/export`, `POST /import`, `POST /{id}/simulate`.
5. 🔲 Tests unitarios para export/import roundtrip y simulación.
6. 🔲 Verificar que todos los endpoints existentes siguen funcionando.

### Día 3 (10h) — BFF integration completa

1. 🔲 Crear DTOs fuzzy en `BffService.Domain/DTOs/Fuzzy/`.
2. 🔲 Expandir `IFuzzyServiceClient` con métodos para todos los nuevos endpoints.
3. 🔲 Implementar `FuzzyServiceClient` con `HttpClient` tipado.
4. 🔲 Crear `FuzzyController` en `BffService.Api/Controllers/`.
5. 🔲 Implementar endpoint compuesto `GET /fuzzy/systems/{id}/detail` (orquestado).
6. 🔲 Actualizar `docker-compose.yml`: agregar `fuzzy-service` a `depends_on` del BFF, env var `Services__FuzzyService__Url`.
7. 🔲 Verificar build del BFF.

### Día 4 (10h) — Shared TS package + átomos

1. 🔲 Crear `packages/shared/src/types/fuzzy.ts` con todos los tipos alineados al fuzzy-service real.
2. 🔲 Crear `packages/shared/src/api/fuzzyService.ts` (clase `FuzzyApiService` con todos los métodos).
3. 🔲 Crear `packages/shared/src/store/fuzzyStore.ts` (`useFuzzyStore` Zustand).
4. 🔲 Actualizar barrel exports (types, api, store, index.ts, index.web.ts).
5. 🔲 Migrar y adaptar `MembershipFunctionChart.tsx` (Plotly.js, nuevos tipos, styling hidro).
6. 🔲 Verificar build del shared package (`tsc --noEmit`).

### Día 5 (10h) — Frontend: organismos fuzzy

1. 🔲 Migrar y adaptar `VariableSection.tsx` (usar `BaseCard`, `Badge`, nuevos tipos DTO).
2. 🔲 Migrar y adaptar `RulesSection.tsx` (usar `Table` atom, consecuentes Mamdani).
3. 🔲 Crear `SystemStatusBadge.tsx`, `SystemActionButtons.tsx`, `SystemInfoHeader.tsx`.
4. 🔲 Crear `SimulationPanel.tsx` (inputs + resultado).
5. 🔲 Refactorizar `FuzzySystemCard.tsx` existente: agregar acciones (activar/duplicar/exportar/eliminar).
6. 🔲 Crear barrel export `components/fuzzy/index.ts`.

### Día 6 (10h) — Frontend: páginas + integración + build

1. 🔲 Crear `RoutineListPage.tsx` (grid + filtros + acciones + importar JSON).
2. 🔲 Crear `RoutineDetailPage.tsx` (tabs: Info + Variables + Reglas + Simular).
3. 🔲 Crear rutas: `app/rutinas/page.tsx`, `app/rutinas/[id]/page.tsx`.
4. 🔲 Actualizar navegación: SideNavigation + BottomNavigation con "Rutinas Fuzzy".
5. 🔲 Build verification: `tsc --noEmit` en shared + web.
6. 🔲 Smoke test manual (si contenedores disponibles).

---

## 9) Seguridad (scopes/policies)

Scopes ya definidos en `HydroEspinaca.Shared`:
- `fuzzy:system:read` → Listar, obtener detalle, exportar, simular
- `fuzzy:system:create` → Crear, clonar, importar
- `fuzzy:system:update` → Activar, actualizar
- `fuzzy:system:delete` → Eliminar

---

## 10) Decisiones técnicas

### 10.1 "Rutina" = FuzzySystem

Según el plan general: *"tratar cada rutina como un FuzzySystem versionable"*. No se crea una entidad "Routine" separada. El modelo actual del fuzzy-service ya soporta esto:
- Cada `FuzzySystem` es una configuración completa de control (= una "rutina").
- Solo 1 puede estar `ACTIVE` a la vez (la rutina vigente).
- El agricultor experimenta creando sistemas nuevos o duplicando existentes.

### 10.2 Consecuentes Mamdani (no "rutinas" del modelo anterior)

El proyecto anterior (`old_frontend_version`) tenía `FuzzyRoutine` como entidad separada con `steps` (condición + power_term + duration_term). El modelo actual usa **consecuentes Mamdani**: cada regla apunta a variables de salida + términos activados + método de agregación. Esto es más potente y estándar.

**Impacto en migración:** `RoutinesSection.tsx` NO se migra. `RulesSection.tsx` se adapta para mostrar consecuentes Mamdani en lugar de nombres de rutina.

### 10.3 MembershipFunctionChart con Plotly.js

Se reutiliza `react-plotly.js` (ya instalado para analytics). Dynamic import (`next/dynamic` con `ssr: false`) para evitar problemas de SSR. El componente anterior soporta triangular/trapezoidal/gaussian — se agrega sigmoid y bell.

### 10.4 BFF orquesta el detalle

El endpoint `GET /fuzzy/systems/{id}/detail` en el BFF recopila sistema + variables + términos + reglas en un solo response. Esto evita 4+ requests del frontend y es consistente con el patrón de orquestación usado en BI (operational-cost, profitability).

---

## 11) Checklist final

### Backend — fuzzy-service
- [x] `POST /api/fuzzy-systems/{id}/activate` funciona y desactiva otros.
- [x] `POST /api/fuzzy-systems/{id}/clone` crea deep copy con nuevos IDs.
- [x] `GET /api/fuzzy-systems/{id}/export` retorna JSON completo portátil.
- [x] `POST /api/fuzzy-systems/import` crea sistema desde JSON exportado.
- [x] `POST /api/fuzzy-systems/{id}/simulate` evalúa sin persistir.
- [ ] Tests de integración para los 5 nuevos endpoints (pendiente).
- [x] Tests de dominio existen (12 archivos: entidades, value objects, reglas).

### Backend — BFF
- [x] `FuzzyController` con 9 endpoints tipados.
- [x] `GET /fuzzy/systems/{id}/detail` orquesta y retorna detalle completo.
- [x] `IFuzzyServiceClient` con todos los métodos implementados.
- [x] DTOs fuzzy completos en `BffService.Domain` (7 archivos).
- [x] `docker-compose.yml` actualizado con dependency + env var.
- [x] BFF compila sin errores.

### Shared TS Package
- [x] `types/fuzzy.ts` con todos los tipos alineados al fuzzy-service (~260 líneas).
- [x] `api/fuzzyService.ts` con clase `FuzzyApiService` completa (9 métodos).
- [x] `store/fuzzyStore.ts` con `useFuzzyStore` Zustand (~271 líneas).
- [x] Barrel exports actualizados (index.ts + index.web.ts).
- [x] `tsc --noEmit` exitoso.
- [ ] Tests para FuzzyApiService y useFuzzyStore (pendiente).

### Frontend Web
- [x] `MembershipFunctionChart` migrado con Plotly.js (5 tipos de MF).
- [x] `VariableSection` migrado con `BaseCard` + `Badge` + chart por variable.
- [x] `RulesSection` migrado con `Table` atom + consecuentes Mamdani.
- [x] `FuzzySystemCard` refactorizado con botones de acción.
- [x] `SystemStatusBadge`, `SystemActionButtons`, `SystemInfoHeader` creados.
- [x] `SimulationPanel` funcional con inputs + resultado.
- [x] `RoutineListPage` con grid + filtros + acciones.
- [x] `RoutineDetailPage` con tabs (Info/Variables/Reglas/Simular).
- [x] Rutas `/rutinas` y `/rutinas/[id]` creadas.
- [x] Navegación actualizada (Side + Bottom con BrainIcon).
- [x] `tsc --noEmit` exitoso en web app.
- [ ] Tests para componentes fuzzy (pendiente).

### Documentación XML — Servicios C#
- [x] BI Service: 70 archivos documentados (en español).
- [x] Sensor Service: 151 archivos documentados (en inglés).
- [x] Auth Service: 165 archivos documentados (en inglés).
- [x] BFF Service: 87 archivos documentados (en inglés).
- [x] Actuator Service: 70 archivos documentados (en inglés).
- [x] Total: **543 archivos** con documentación XML `<summary>`.

---

## 12) Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| Clone atómico puede fallar a mitad si hay muchas entidades | Usar transacción MongoDB (session) para rollback automático |
| Export/Import puede tener incompatibilidades entre versiones del schema | Incluir campo `version` en el JSON y validar al importar |
| Plotly.js SSR issues en Next.js | Dynamic import con `ssr: false` (patrón ya probado en analytics) |
| Simulación puede faltar variables de entrada configuradas | Validar que el request incluye todas las variables de entrada del sistema |
| Múltiples usuarios activando sistemas al mismo tiempo | Usar update atómico en MongoDB (`findOneAndUpdate`) con filtro de status |
| Consecuentes Mamdani son más complejos de visualizar que "rutinas" | Mostrar como tabla: "Variable de salida → Términos activados → Método de agregación" |

---

## 13) Notas importantes

- ⚠️ El fuzzy-service usa **Python 3.12 + FastAPI + Medyator** (no MediatR de .NET). El patrón CQRS es similar pero la implementación es diferente.
- ⚠️ El motor `ScikitFuzzyEngine` ya tiene toda la lógica de fuzzificación/defuzzificación. `simulate` reutiliza este motor.
- ⚠️ `react-plotly.js` ya está instalado como dependencia en `apps/web/package.json` (usado en analytics).
- ⚠️ El `BrainIcon` para navegación ya existe en `Icons.tsx`.
- ⚠️ La tabla `fuzzy_evaluations` ya registra qué sistema se usó — esto será útil para el comparador de Semana 3.
- ✅ Las vistas BI/consumo ya están completas (adelantadas de Semana 1). Semana 2 es 100% Fuzzy.
---

## 14) Análisis: CRUD de Sistemas Fuzzy desde la Web

> **Conclusión: NO existe forma de crear ni editar un sistema fuzzy desde la web.**

### 14.1 Lo que SÍ se puede hacer desde la web

| Operación | Disponible | Notas |
|-----------|-----------|-------|
| **Ver/listar** sistemas fuzzy | ✅ | Lista completa con filtros por status y búsqueda |
| **Ver detalle** de un sistema (variables, términos, reglas) | ✅ | Endpoint orquestado del BFF |
| **Activar** un sistema | ✅ | Desactiva otros automáticamente |
| **Duplicar/Clonar** un sistema | ✅ | Deep copy con nombre personalizado |
| **Eliminar** un sistema | ✅ | Solo permitido para DRAFT o INACTIVE |
| **Exportar** a JSON | ✅ | Descarga archivo JSON completo |
| **Importar** desde JSON | ✅ | Sube un archivo JSON previamente exportado |
| **Simular** con inputs arbitrarios | ✅ | Panel interactivo con inputs numéricos |

### 14.2 Lo que NO se puede hacer desde la web

| Operación | Disponible | Impacto |
|-----------|-----------|---------|
| **Crear** un sistema nuevo desde cero | ❌ | No hay formulario, API, ni endpoint en BFF |
| **Editar** propiedades del sistema (nombre, operadores, método defuzz) | ❌ | No existe formulario ni API de update |
| **Crear/editar/eliminar** variables | ❌ | Las variables se muestran read-only |
| **Crear/editar/eliminar** términos lingüísticos | ❌ | Los términos se muestran read-only |
| **Crear/editar/eliminar** reglas | ❌ | Las reglas se muestran read-only |
| **Cambiar status manualmente** (DRAFT→TESTING, etc.) | ❌ | Solo "activar" está disponible, no un cambio general de status |

### 14.3 Consecuencia actual

La única forma de obtener un sistema nuevo en la web es:
1. **Importar** un JSON previamente construido (fuera de la web).
2. **Clonar** un sistema existente (pero no se puede editar la copia).
3. Crear directamente contra la API del fuzzy-service (sin la web).

### 14.4 Recomendación: siguiente bloque a desarrollar

El **CRUD completo de sistemas fuzzy** (crear + editar sistemas, variables, términos y reglas desde la web) debería ser la siguiente prioridad de desarrollo. Esto alinea con lo que el plan de 7 semanas define en la Semana 3:

> *"Editor visual de variables/términos/reglas (crear/editar desde UI) — Semana 3 o posterior."*

**Alcance sugerido para el próximo sprint:**

| Capa | Trabajo necesario |
|------|-------------------|
| **fuzzy-service** | Los endpoints CRUD ya existen (Create/Update/Delete para System, Variable, Term, Rule). Solo falta verificar que funcionen correctamente con las validaciones de negocio. |
| **BFF** | Agregar endpoints al `FuzzyController` para CRUD de variables, términos y reglas. Expandir `IFuzzyServiceClient` con métodos de escritura. |
| **Shared TS** | Agregar funciones en `FuzzyApiService` para create/update/delete de cada entidad. Expandir `useFuzzyStore` con acciones de mutación. |
| **Frontend** | Crear formularios/modals para: crear sistema, editar sistema, CRUD de variables (con editor de universe_min/max), CRUD de términos (con editor visual de MF), CRUD de reglas (builder de condiciones + consecuentes). |