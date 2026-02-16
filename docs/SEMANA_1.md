# Semana 1 (3 días × 10h) — BI Service (base) + vertical slice

Duración estimada: **30h**
Objetivo macro: dejar el **nuevo microservicio de BI** creado con la misma base técnica del monorepo (Clean Architecture + CQRS + Mongo + Auth), y exponer un **vertical slice** completo consumible vía **BFF** que incluya costos operativos, registros de producción y análisis de rentabilidad.

> Nota: Este documento es intencionalmente "mecánico" para poder ejecutar sin ambigüedades.

---

## 0) Progreso General

| Tarea | Estado | Notas |
|-------|--------|-------|
| Scaffold bi-service (Clean Architecture) | ✅ Completado | Api, Application, Domain, Infrastructure |
| Health check `/health` | ✅ Completado | AllowAnonymous, 200 OK |
| Auth JWT + policies (BiRead/BiWrite) | ✅ Completado | Usa scopes de HydroEspinaca.Shared |
| MongoDB (colecciones BI) | ✅ Completado | `bi_cost_config_versions`, `bi_manual_consumption_entries`, `bi_production_records` |
| CQRS con MediatR 13.0.0 | ✅ Completado | 12+ handlers + ValidationBehavior |
| FluentValidation | ✅ Completado | 7+ validators con reglas de negocio |
| GlobalExceptionMiddleware + ProblemDetailsFactory | ✅ Completado | Patrón compartido |
| Dockerfile multi-stage | ✅ Completado | sdk:9.0 → aspnet:9.0, usuario non-root |
| docker-compose.yml integrado | ✅ Completado | Puerto 5020, profile development |
| Integración BFF completa | ✅ Completado | IBiServiceClient, BiServiceClient, BiController — 13 endpoints |
| Smoke test e2e (13 steps) | ✅ Completado | Login → CRUD → Summary → Validación |
| DELETE consumption entry | ✅ Completado | bi-service + BFF forward |
| Campo `PowerConsumptionWatts` en actuadores | ✅ Completado | Domain, Document, Mapper (3 métodos), Validators (Create+Update), Shared NuGet DTOs |
| CRUD de Producciones (registros de cosecha) | ✅ Completado | Create, GetAll, GetById, Delete — colección `bi_production_records` |
| Costo operativo por actuador | ✅ Completado | Multi-versión de costos, proporcional por días, BFF orquesta |
| Rentabilidad (profitability) | ✅ Completado | Gastos operativos + manuales vs ingresos, beneficio neto, margen % |
| Integración BFF nuevos endpoints | ✅ Completado | 13/13 endpoints integrados (7 forward + 2 orquestados) |
| **Frontend: Módulo BI completo** | ✅ Completado | **Trabajo adelantado (originalmente Semana 2)** |
| — Shared: tipos BI (TypeScript) | ✅ Completado | `bi.ts`: 17 interfaces + enum `ConsumptionType` + mapas de labels/units/icons |
| — Shared: API service BI | ✅ Completado | `biService.ts`: clase `BiApiService` con 12 métodos + `BiApiError` |
| — Shared: Zustand store BI | ✅ Completado | `biStore.ts`: `useBiStore` con CRUD para 5 dominios |
| — Web: Átomos reutilizables nuevos | ✅ Completado | `StatCard`, `EmptyState`, `DateRangeFilter` — 3 nuevos componentes UI |
| — Web: Organismos BI (12 componentes) | ✅ Completado | BiTabs, CostConfigForm/Section, ConsumptionForm/Summary/Section, ProductionForm/Section, ProfitabilityResult/Section, BiPage |
| — Web: Ruta `/consumo` + navegación | ✅ Completado | Page route, SideNavigation, BottomNavigation actualizados |
| — Web: TypeScript compila sin errores | ✅ Completado | `tsc --noEmit` exitoso en shared + web |

---

## 1) Objetivos de la semana (qué debe quedar listo sí o sí)

### 1.1 Entregable mínimo (Definition of Done de Semana 1)
Al final del día 3:

1) ✅ Existe `bi-service` en `software-project/` con Clean Architecture + CQRS + Mongo + Auth.

2) CRUD + Cálculos:
   - ✅ **CostConfig**: `GET current` + `GET versions` + `POST` versionado.
   - ✅ **ManualConsumptionEntry**: `POST` + `GET` con filtros.
   - ✅ **ManualConsumptionEntry DELETE** por ID.
   - ✅ **Summary por rango**: consumos + costo total histórico.
   - ✅ **ProductionRecord CRUD**: Create, GetAll, GetById, Delete.
   - ✅ **Costo operativo**: cálculo basado en duración de actuadores × potencia × costo/kWh (multi-versión).
   - ✅ **Rentabilidad**: gastos totales vs ingresos por producción = beneficio neto + margen %.

3) ✅ Integración BFF completa — 13/13 endpoints (7 forward directo + 2 orquestados).

4) ✅ **Frontend BI (adelantado de Semana 2)**:
   - Shared: tipos, API service, Zustand store para BI.
   - Web: 3 átomos nuevos + 12 organismos BI + ruta `/consumo` + navegación.

---

## 2) Alcance funcional (solo Semana 1)

### 2.1 Qué entra
- ✅ Registro manual de consumos y costos (base para rentabilidad).
- ✅ Electricidad: kWh global del invernadero ingresado manualmente.
- ✅ Eliminación de entradas manuales (corrección de errores).
- ✅ **Costo operativo automático**: duración real de actuadores × `powerConsumptionWatts` (campo en colección `actuators`).
- ✅ **Registros de producción**: CRUD para cosechas (cultivo, fechas, kilos, valor/kg).
- ✅ **Análisis de rentabilidad**: filtrar por producción → ver gastos vs ingresos → beneficio neto.
- ✅ **Vistas frontend BI**: módulo completo de consumo/costos con 4 pestañas (configuración, consumo, producción, rentabilidad).

### 2.2 Qué NO entra (se aplaza)
- ~~Dashboard de rentabilidad frontend (Semana 2 — web).~~ ✅ Adelantado.
- Alertas meteorológicas, push, WhatsApp, asistente (semanas posteriores).
- Rutinas Fuzzy (Semana 2).

---

## 3) Tecnología y metodología

### 3.1 Lenguaje y stack ✅
- **.NET 9** (igual que el resto de microservicios).
- Clean Architecture (Api/Application/Domain/Infrastructure).
- **HydroEspinaca.Shared** v1.0.3-dev para Auth JWT, Mongo, ProblemDetailsFactory.

### 3.2 CQRS ✅
- **MediatR 13.0.0** con `ValidationBehavior<TRequest, TResponse>` pipeline.
- **FluentValidation 12.0.0** vía DependencyInjectionExtensions.
- Feature-based: `Features/{Feature}/Commands/`, `Features/{Feature}/Queries/`, etc.

### 3.3 Convenciones ✅
- API base route: `/api/bi/...`
- Fechas en UTC (ISO-8601).
- Scopes: `bi:read`, `bi:write` en `PolicyNames`.

---

## 4) Modelo de datos (MongoDB) — Semana 1

### 4.1 Colección: `bi_cost_config_versions` ✅

**Propósito:** historial de costos unitarios por vigencia (sin sobreescribir histórico).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `currency` | string | ej. `COP` |
| `electricity_cost_per_kwh` | Decimal128 | |
| `water_cost_per_liter` | Decimal128 | |
| `nutrient_cost_per_liter` | Decimal128 | |
| `effective_from` | Date (UTC) | |
| `effective_to` | Date \| null | null = versión activa |
| `is_active` | bool | |
| `created_at` | Date | |
| `created_by_user_id` | string | |

Reglas de negocio (implementadas):
- Solo **1 versión activa** (`is_active=true`) con `effective_to=null`.
- Al crear nueva versión: cerrar anterior + activar nueva (transacción MongoDB).

### 4.2 Colección: `bi_manual_consumption_entries` ✅

**Propósito:** registrar consumos manuales.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `date` | Date | |
| `type` | string (enum) | `ElectricityKwh`, `WaterLiters`, `NutrientLiters` |
| `amount` | Decimal128 | |
| `unit_cost_snapshot` | Decimal128 | precio al momento del registro |
| `currency_snapshot` | string | |
| `cost_config_version_id` | ObjectId | |
| `cost_amount` | Decimal128 | `amount × unitCost` persistido |
| `note` | string \| null | |
| `created_at` | Date | |
| `created_by_user_id` | string | |

### 4.3 Colección: `bi_production_records` ✅ IMPLEMENTADA

**Propósito:** registrar ciclos de producción/cosecha para calcular rentabilidad.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `crop_name` | string | ej. `"Espinaca"` |
| `start_date` | Date (UTC) | inicio del ciclo de cultivo |
| `harvest_date` | Date (UTC) | fecha de cosecha |
| `kilos_produced` | Decimal128 | puede ser decimal (ej. 25.5) |
| `price_per_kilo` | Decimal128 | valor de venta por kilo |
| `currency` | string | ej. `COP` |
| `note` | string \| null | observaciones opcionales |
| `created_at` | Date | |
| `created_by_user_id` | string | |

**Uso principal:** al seleccionar una producción, sus fechas (`start_date` → `harvest_date`) definen el rango para calcular costos operativos y consumos manuales, y sus datos de producción (`kilos_produced × price_per_kilo`) definen los ingresos.

### 4.4 Modificación a colección `actuators` (actuator-service) ✅

**Campo nuevo:** `power_consumption_watts` (decimal)

La potencia eléctrica (en Watts) de cada actuador se almacena **en la colección de actuadores** del actuator-service, no hardcodeada en bi-service. Esto es lógico porque la potencia es una propiedad física del actuador.

| PhysicalId | Code | Watts estimados |
|------------|------|-----------------|
| FAN-001 | Ventiladores | 25 |
| HEATER-001 | termoventilador | 1500 |
| LED-001 | luz-amplio-espectro | 150 |
| AIR-001 | piedra-difusora | 5 |
| WATER-001 | bomba-agua | 45 |
| WATER-HEATER-001 | calefactor-agua | 1200 |
| HUMIDIFIER-001 | humidificador-ultrasonico | 35 |

> **Nota:** El usuario actualizará manualmente los documentos existentes en MongoDB tras el cambio de código. El seed ya incluirá el campo para nuevos despliegues.

### 4.5 Regla de oro de cálculo histórico ✅
- Los reportes usan **`costAmount` persistido** en cada entrada manual.
- Cambiar precios hoy **no altera** costos de entradas pasadas.
- Para costo operativo: se calculan en tiempo real usando los `cost_config_versions` que apliquen al rango.

---

## 5) Endpoints — Semana 1

### 5.1 CostConfig (bi-service) ✅

| Método | Ruta | Policy | Estado |
|--------|------|--------|--------|
| `GET` | `/api/bi/cost-config/current` | BiRead | ✅ |
| `GET` | `/api/bi/cost-config/versions?from=&to=` | BiRead | ✅ |
| `POST` | `/api/bi/cost-config/versions` | BiWrite | ✅ |

### 5.2 ManualConsumptionEntry (bi-service)

| Método | Ruta | Policy | Estado |
|--------|------|--------|--------|
| `POST` | `/api/bi/consumption-entries` | BiWrite | ✅ |
| `GET` | `/api/bi/consumption-entries?from=&to=&type=` | BiRead | ✅ |
| `GET` | `/api/bi/consumption-entries/summary?from=&to=` | BiRead | ✅ |
| `DELETE` | `/api/bi/consumption-entries/{id}` | BiWrite | ✅ |

### 5.3 ProductionRecord (bi-service) ✅ IMPLEMENTADO

| Método | Ruta | Policy | Estado |
|--------|------|--------|--------|
| `POST` | `/api/bi/production-records` | BiWrite | ✅ |
| `GET` | `/api/bi/production-records` | BiRead | ✅ |
| `GET` | `/api/bi/production-records/{id}` | BiRead | ✅ |
| `DELETE` | `/api/bi/production-records/{id}` | BiWrite | ✅ |

### 5.4 Costo Operativo (bi-service) ✅ IMPLEMENTADO

| Método | Ruta | Policy | Estado |
|--------|------|--------|--------|
| `POST` | `/api/bi/operational-cost/calculate` | BiRead | ✅ |

> **Nota:** Es `POST` porque recibe datos de actuadores (duración + potencia) en el body desde el BFF. El BFF orquesta la obtención de esos datos.

### 5.5 Rentabilidad (bi-service) ✅ IMPLEMENTADO

| Método | Ruta | Policy | Estado |
|--------|------|--------|--------|
| `POST` | `/api/bi/profitability/calculate` | BiRead | ✅ |

> **Nota:** Recibe `productionRecordId` + datos de actuadores. bi-service busca la producción, calcula todo internamente.

---

## 6) Seguridad (scopes/policies) ✅

- `BiRead = "bi:read"` → Lecturas y cálculos
- `BiWrite = "bi:write"` → Escrituras y eliminaciones

---

## 7) Integración con BFF

### 7.1 Cliente HTTP ✅
- `IBiServiceClient` + `BiServiceClient` (Typed HttpClient, forwarding access token)

### 7.2 Endpoints BFF

| BFF Route | Método | Orquestación | Estado |
|-----------|--------|-------------|--------|
| `GET /bi/cost-config/current` | GET | Forward directo | ✅ |
| `GET /bi/cost-config/versions` | GET | Forward directo | ✅ |
| `POST /bi/cost-config/versions` | POST | Forward directo | ✅ |
| `POST /bi/consumption-entries` | POST | Forward directo | ✅ |
| `GET /bi/consumption-entries` | GET | Forward directo | ✅ |
| `GET /bi/consumption-entries/summary` | GET | Forward directo | ✅ |
| `DELETE /bi/consumption-entries/{id}` | DELETE | Forward directo | ✅ |
| `POST /bi/production-records` | POST | Forward directo | ✅ |
| `GET /bi/production-records` | GET | Forward directo | ✅ |
| `GET /bi/production-records/{id}` | GET | Forward directo | ✅ |
| `DELETE /bi/production-records/{id}` | DELETE | Forward directo | ✅ |
| `GET /bi/operational-cost?from=&to=` | GET | **Orquesta** (ver 11.3) | ✅ |
| `GET /bi/profitability?productionId=` | GET | **Orquesta** (ver 12.3) | ✅ |

### 7.3 Patrón de orquestación del BFF (endpoints compuestos)

Para **operational-cost** y **profitability**, el BFF NO hace forward directo. En su lugar:

1. BFF obtiene datos de actuadores (GET actuators + POST analytics) del **actuator-service**
2. BFF combina la potencia (`PowerConsumptionWatts`) de cada actuador con su duración (`TotalDurationSeconds`)
3. BFF envía esos datos combinados al **bi-service** vía POST
4. bi-service hace los cálculos de costo usando sus `cost_config_versions`
5. BFF retorna el resultado al frontend

**¿Por qué BFF orquesta en vez de bi-service llamando a actuator-service?**
- Mantiene bi-service desacoplado: solo recibe datos y calcula costos.
- No necesita M2M auth ni HTTP client hacia actuator-service.
- BFF ya tiene acceso al actuator-service (usa `AnalyticsService` + `ProxyService`).
- Sigue el patrón existente donde BFF es el punto de coordinación entre microservicios.

---

## 8) Plan por día (30h) — Actualizado

### Día 1 (10h) — Scaffold + CQRS + BFF ✅ COMPLETADO
1. ✅ Creación completa del bi-service (Clean Architecture, 4 capas).
2. ✅ CQRS con MediatR 13.0.0 (6 handlers + ValidationBehavior + 5 validators).
3. ✅ Integración BFF completa (IBiServiceClient + BiServiceClient + BiController).
4. ✅ Smoke test e2e exitoso (13 steps, todos ✅).

### Día 2 (10h) — Nuevas features en bi-service ✅ COMPLETADO
1. ✅ Agregar `PowerConsumptionWatts` al dominio de actuadores (entity, document, mapper 3 métodos, validators Create+Update, shared DTO).
2. ✅ Implementar `DELETE /api/bi/consumption-entries/{id}`.
3. ✅ Implementar CRUD de `ProductionRecord` (Create, GetAll, GetById, Delete).
4. ✅ Implementar `POST /api/bi/operational-cost/calculate` (multi-versión de costos).
5. ✅ Implementar `POST /api/bi/profitability/calculate` (beneficio neto + margen %).

### Día 3 (10h) — BFF integration + Frontend BI ✅ COMPLETADO
1. ✅ Integrar nuevos endpoints en BFF (DELETE entry, Production CRUD, Operational Cost orchestration, Profitability orchestration) — 13/13 endpoints.
2. ✅ Rebuild shared NuGet package v1.0.3-dev con `PowerConsumptionWatts` en `ActuatorDto`.
3. ✅ Verificar builds completos (bi-service, actuator-service, BFF).
4. ✅ **Frontend BI (adelantado)**: tipos TS, API service, Zustand store, 3 átomos nuevos, 12 organismos, ruta `/consumo`, navegación.
5. ✅ TypeScript compila sin errores (`tsc --noEmit` exitoso en shared + web).

---

## 9) Checklist final

- [x] `bi-service` compila y corre en Docker.
- [x] `GET /health` responde 200.
- [x] Auth funciona con JWT.
- [x] `CostConfig` versionado funciona.
- [x] `ManualConsumptionEntry` POST/GET funciona.
- [x] `ManualConsumptionEntry` DELETE funciona.
- [x] `Summary` calcula costos históricos correctamente.
- [x] BFF expone endpoints `bi/*` base.
- [x] Smoke test e2e exitoso (13 steps).
- [x] `PowerConsumptionWatts` agregado a actuadores (domain + mapper + validators).
- [x] `ProductionRecord` CRUD funciona (Create, GetAll, GetById, Delete).
- [x] `OperationalCost` calcula costos por duración × potencia × costo/kWh (multi-versión).
- [x] `Profitability` combina gastos operativos + manuales vs ingresos de producción.
- [x] BFF orquesta endpoints compuestos (operational-cost, profitability).
- [x] **Frontend BI**: shared types + API service + Zustand store.
- [x] **Frontend BI**: 3 átomos nuevos (StatCard, EmptyState, DateRangeFilter).
- [x] **Frontend BI**: 12 organismos BI + BiPage.
- [x] **Frontend BI**: ruta `/consumo` + navegación (Side + Bottom).
- [x] **TypeScript**: `tsc --noEmit` exitoso en shared package y web app.

---

## 10) Notas importantes

- ✅ El seed de permisos funciona — `SGalindo@demo.com` tiene scopes para BI.
- ✅ `ConsumptionType` es enum numérico (1=ElectricityKwh, 2=WaterLiters, 3=NutrientLiters).
- ⚠️ Puertos `5020` (bi-service) y `8081` (BFF) son temporales para desarrollo.
- ⚠️ Al agregar `PowerConsumptionWatts` al código, el usuario debe actualizar los documentos existentes en MongoDB manualmente.
- ⚠️ Tras modificar `ActuatorDto` en HydroEspinaca.Shared, se debe hacer rebuild del NuGet package.

---

## 11) Costo Operativo por Actuador — Diseño Detallado ✅

### 11.1 Requisito del Proyecto
> *"Utilizando el tiempo de activación de cada actuador (luces, bombas) registrado por la plataforma, se calculará el costo operativo del ciclo."*

### 11.2 Fuente de datos de duración

**Endpoint existente:** `actuator-service` → `POST /api/commands/analytics`

Request:
```json
{ "startDate": "2026-01-01T00:00:00Z", "endDate": "2026-02-28T23:59:59Z", "view": "daily" }
```

Response (campo relevante):
```json
{
  "totalDurationByActuator": [
    { "actuatorCode": "luz-amplio-espectro", "totalDurationSeconds": 36000.0, "activationCount": 10 },
    { "actuatorCode": "bomba-agua", "totalDurationSeconds": 7200.0, "activationCount": 24 }
  ]
}
```

### 11.3 Fuente de datos de potencia

**Nuevo campo en colección `actuators`:** `power_consumption_watts` (decimal).

El BFF obtiene los actuadores via `GET /api/actuators` del actuator-service, que ahora incluye `PowerConsumptionWatts`. El BFF combina este dato con `TotalDurationSeconds` del analytics y envía todo al bi-service.

### 11.4 Flujo de datos (BFF orquesta)

```
Frontend
  → BFF: GET /bi/operational-cost?from=X&to=Y
      → BFF llama actuator-service: GET /api/actuators → [{ code, powerConsumptionWatts, ... }]
      → BFF llama actuator-service: POST /api/commands/analytics → { totalDurationByActuator }
      → BFF combina: por cada actuador → { code, powerWatts, durationSeconds, activationCount }
      → BFF llama bi-service: POST /api/bi/operational-cost/calculate
          → bi-service busca cost_config_versions para [from, to]
          → bi-service calcula costos por período si hay múltiples versiones
          → bi-service retorna OperationalCostResponse
  ← Frontend recibe resultado
```

### 11.5 Fórmula de cálculo

Para cada actuador:
```
horasActivas = totalDurationSeconds / 3600
consumoKwh = horasActivas × powerConsumptionWatts / 1000
costoActuador = consumoKwh × electricityCostPerKwh
```

### 11.6 Manejo de múltiples cost_config_versions

Si el rango `[from, to]` cruza varias versiones de costos:

1. bi-service busca todas las versiones que se superponen con el rango.
2. Calcula la **proporción de días** que cada versión cubre dentro del rango.
3. Para cada versión, calcula: `duración_proporcional = totalDurationSeconds × (días_versión / días_totales)`.
4. Aplica la fórmula con el `electricityCostPerKwh` de esa versión.
5. Suma los costos de todas las versiones parciales.

**Ejemplo:**
- Rango: 1 ene → 28 feb (59 días)
- Versión A (1 ene → 31 ene): 31 días, electricidad = $800/kWh
- Versión B (1 feb → 28 feb): 28 días, electricidad = $900/kWh
- LED con 36000 seg totales:
  - Proporción A: 36000 × (31/59) = 18915 seg → 5.25h → 0.79 kWh → $631.19
  - Proporción B: 36000 × (28/59) = 17085 seg → 4.75h → 0.71 kWh → $640.68
  - Total LED: $1,271.87

### 11.7 Endpoint: `POST /api/bi/operational-cost/calculate`

**Request body (enviado por BFF):**
```json
{
  "from": "2026-01-01T00:00:00Z",
  "to": "2026-02-28T23:59:59Z",
  "actuatorDurations": [
    {
      "actuatorCode": "luz-amplio-espectro",
      "powerConsumptionWatts": 150,
      "totalDurationSeconds": 36000.0,
      "activationCount": 10
    },
    {
      "actuatorCode": "bomba-agua",
      "powerConsumptionWatts": 45,
      "totalDurationSeconds": 7200.0,
      "activationCount": 24
    }
  ]
}
```

**Response:**
```json
{
  "from": "2026-01-01T00:00:00Z",
  "to": "2026-02-28T23:59:59Z",
  "costConfigPeriodsUsed": [
    { "versionId": "abc", "from": "2026-01-01", "to": "2026-01-31", "electricityCostPerKwh": 800, "currency": "COP" },
    { "versionId": "def", "from": "2026-02-01", "to": "2026-02-28", "electricityCostPerKwh": 900, "currency": "COP" }
  ],
  "actuators": [
    {
      "actuatorCode": "luz-amplio-espectro",
      "powerConsumptionWatts": 150,
      "totalDurationSeconds": 36000.0,
      "totalHours": 10.0,
      "estimatedKwh": 1.5,
      "estimatedCost": 1271.87,
      "activationCount": 10
    }
  ],
  "currency": "COP",
  "totalEstimatedKwh": 1.59,
  "totalOperationalCost": 1352.30
}
```

### 11.8 Implementación (archivos nuevos en bi-service)

**DTOs:**
- `DTOs/OperationalCost/CalculateOperationalCostRequest.cs`
- `DTOs/OperationalCost/ActuatorDurationInput.cs`
- `DTOs/OperationalCost/OperationalCostResponse.cs`

**CQRS:**
- `Features/OperationalCost/Queries/CalculateOperationalCost/CalculateOperationalCostQuery.cs`
- `Features/OperationalCost/Queries/CalculateOperationalCost/CalculateOperationalCostQueryHandler.cs`
- `Features/OperationalCost/Queries/CalculateOperationalCost/CalculateOperationalCostQueryValidator.cs`

**Controller:**
- `Controllers/OperationalCostController.cs`

**Repo (método nuevo en existente):**
- `ICostConfigVersionRepository.GetVersionsForRangeAsync(from, to)` — busca versiones que se superponen con el rango.

---

## 12) Registros de Producción (CRUD) — Diseño Detallado ✅

### 12.1 Concepto

Un registro de producción representa un **ciclo de cultivo completo**: desde la siembra hasta la cosecha. Contiene la información necesaria para calcular los ingresos y, en combinación con los gastos (operativos + manuales), determinar la rentabilidad.

### 12.2 Modelo de datos

```json
{
  "cropName": "Espinaca",
  "startDate": "2026-01-01T00:00:00Z",
  "harvestDate": "2026-02-28T00:00:00Z",
  "kilosProduced": 25.5,
  "pricePerKilo": 8500.00,
  "currency": "COP",
  "note": "Primera cosecha del año"
}
```

### 12.3 Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/bi/production-records` | Crear registro de producción |
| `GET` | `/api/bi/production-records` | Listar todos los registros |
| `GET` | `/api/bi/production-records/{id}` | Obtener registro por ID |
| `DELETE` | `/api/bi/production-records/{id}` | Eliminar registro |

### 12.4 Implementación (archivos nuevos)

**Domain:**
- `Entities/ProductionRecord.cs`
- `Interfaces/IProductionRecordRepository.cs`

**Application DTOs:**
- `DTOs/Production/CreateProductionRecordRequest.cs`
- `DTOs/Production/ProductionRecordDto.cs`

**CQRS Features:**
- `Features/Production/Commands/CreateProductionRecord/` (Command, Handler, Validator)
- `Features/Production/Commands/DeleteProductionRecord/` (Command, Handler)
- `Features/Production/Queries/GetProductionRecords/` (Query, Handler)
- `Features/Production/Queries/GetProductionRecordById/` (Query, Handler)

**Infrastructure:**
- `Persistence/Models/ProductionRecordDocument.cs`
- `Persistence/Mappings/ProductionRecordMapper.cs`
- `Persistence/Repositories/ProductionRecordRepository.cs`

**API:**
- `Controllers/ProductionRecordsController.cs`

---

## 13) Rentabilidad (Profitability) — Diseño Detallado ✅

### 13.1 Concepto

La **vista de rentabilidad** es el objetivo final del módulo BI. Combina:

1. **Gastos operativos** (costo eléctrico por duración de actuadores).
2. **Gastos manuales** (consumos de agua, nutrientes, electricidad manual).
3. **Ingresos de producción** (kilos producidos × precio/kg).

= **Beneficio neto** (ingresos − gastos totales).

### 13.2 Flujo desde la perspectiva del usuario (frontend)

1. El usuario abre la vista de rentabilidad.
2. Ve un **select/dropdown** con todos los registros de producción (ej. "Espinaca — Ene 1 a Feb 28").
3. Al seleccionar una producción:
   - Las fechas del cultivo definen automáticamente el rango de consulta.
   - Se muestran los **costos operativos** (actuadores) para ese rango.
   - Se muestran los **costos manuales** (summary) para ese rango.
   - Se muestra el **total de gastos**.
   - Se muestra la **sección de ingresos** (kilos × precio/kg = total revenue).
   - Se muestra el **beneficio neto** (revenue − expenses).
   - Se muestra el **margen de beneficio** (%).

### 13.3 Flujo de datos (BFF orquesta)

```
Frontend
  → BFF: GET /bi/profitability?productionId=abc123
      → BFF llama bi-service: GET /api/bi/production-records/abc123
          → Obtiene: cropName, startDate, harvestDate, kilosProduced, pricePerKilo
      → BFF llama actuator-service: GET /api/actuators
      → BFF llama actuator-service: POST /api/commands/analytics (con startDate → harvestDate)
      → BFF combina datos de actuadores
      → BFF llama bi-service: POST /api/bi/profitability/calculate
          Body: { productionRecordId, actuatorDurations: [...] }
          → bi-service internamente:
              1. Busca la producción por ID → obtiene fechas + revenue
              2. Busca cost_config_versions para ese rango
              3. Calcula costo operativo (actuadores × potencia × costo/kWh)
              4. Calcula summary de consumos manuales del rango
              5. Calcula ingresos = kilosProduced × pricePerKilo
              6. Calcula beneficio neto = ingresos − gastos totales
          → Retorna ProfitabilityResponse
  ← Frontend recibe resultado completo
```

### 13.4 Endpoint: `POST /api/bi/profitability/calculate`

**Request body (enviado por BFF):**
```json
{
  "productionRecordId": "abc123",
  "actuatorDurations": [
    { "actuatorCode": "luz-amplio-espectro", "powerConsumptionWatts": 150, "totalDurationSeconds": 36000.0, "activationCount": 10 },
    { "actuatorCode": "bomba-agua", "powerConsumptionWatts": 45, "totalDurationSeconds": 7200.0, "activationCount": 24 }
  ]
}
```

**Response:**
```json
{
  "production": {
    "id": "abc123",
    "cropName": "Espinaca",
    "startDate": "2026-01-01T00:00:00Z",
    "harvestDate": "2026-02-28T00:00:00Z",
    "kilosProduced": 25.5,
    "pricePerKilo": 8500.00,
    "currency": "COP"
  },
  "expenses": {
    "operationalCost": {
      "actuators": [
        {
          "actuatorCode": "luz-amplio-espectro",
          "powerConsumptionWatts": 150,
          "totalHours": 10.0,
          "estimatedKwh": 1.5,
          "estimatedCost": 1275.75
        }
      ],
      "totalEstimatedKwh": 1.59,
      "totalOperationalCost": 1352.30
    },
    "manualConsumptionCost": {
      "totalElectricityKwh": 15.75,
      "totalWaterLiters": 200.0,
      "totalNutrientLiters": 15.0,
      "costElectricity": 13395.37,
      "costWater": 6150.00,
      "costNutrients": 1125.00,
      "totalManualCost": 20670.37
    },
    "totalExpenses": 22022.67
  },
  "revenue": {
    "kilosProduced": 25.5,
    "pricePerKilo": 8500.00,
    "totalRevenue": 216750.00
  },
  "netBenefit": 194727.33,
  "profitMarginPercent": 89.84,
  "currency": "COP"
}
```

### 13.5 Implementación (archivos nuevos)

**DTOs:**
- `DTOs/Profitability/CalculateProfitabilityRequest.cs`
- `DTOs/Profitability/ProfitabilityResponse.cs`

**CQRS:**
- `Features/Profitability/Queries/CalculateProfitability/CalculateProfitabilityQuery.cs`
- `Features/Profitability/Queries/CalculateProfitability/CalculateProfitabilityQueryHandler.cs`
- `Features/Profitability/Queries/CalculateProfitability/CalculateProfitabilityQueryValidator.cs`

**Controller:**
- `Controllers/ProfitabilityController.cs`

---

## 14) Cambios en actuator-service — Resumen

### 14.1 Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `ActuatorService.Domain/Entities/Actuator.cs` | Agregar `PowerConsumptionWatts` (decimal, default 0) |
| `ActuatorService.Infrastructure/Persistence/Models/ActuatorDocument.cs` | Agregar `power_consumption_watts` campo BSON |
| `ActuatorService.Infrastructure/Persistence/Mappings/ActuatorMapper.cs` | Mapear nuevo campo en ambas direcciones |
| `ActuatorService.Infrastructure/Services/DataSeedingService.cs` | Agregar valores de watts en seed |
| `shared/HydroEspinaca.Shared/DTOs/Actuator/ActuatorDto.cs` | Agregar `PowerConsumptionWatts` |
| `shared/HydroEspinaca.Shared/DTOs/Actuator/CreateActuatorDto.cs` | Agregar `PowerConsumptionWatts` |
| `shared/HydroEspinaca.Shared/DTOs/Actuator/UpdateActuatorDto.cs` | Agregar `PowerConsumptionWatts` |

### 14.2 Retrocompatibilidad
- El campo es `decimal` con default `0` en la entidad.
- En el BSON document se usa `[BsonIgnoreIfDefault]` o simplemente default 0.
- Documentos existentes sin el campo → se deserializan con 0 (sin error).
- El usuario actualizará los documentos existentes manualmente en MongoDB.

---

## 15) Resumen de endpoints totales (bi-service + BFF)

### bi-service (13 endpoints totales)

| # | Método | Ruta | Policy | Estado |
|---|--------|------|--------|--------|
| 1 | GET | `/api/bi/cost-config/current` | BiRead | ✅ |
| 2 | GET | `/api/bi/cost-config/versions` | BiRead | ✅ |
| 3 | POST | `/api/bi/cost-config/versions` | BiWrite | ✅ |
| 4 | POST | `/api/bi/consumption-entries` | BiWrite | ✅ |
| 5 | GET | `/api/bi/consumption-entries` | BiRead | ✅ |
| 6 | GET | `/api/bi/consumption-entries/summary` | BiRead | ✅ |
| 7 | DELETE | `/api/bi/consumption-entries/{id}` | BiWrite | ✅ |
| 8 | POST | `/api/bi/production-records` | BiWrite | ✅ |
| 9 | GET | `/api/bi/production-records` | BiRead | ✅ |
| 10 | GET | `/api/bi/production-records/{id}` | BiRead | ✅ |
| 11 | DELETE | `/api/bi/production-records/{id}` | BiWrite | ✅ |
| 12 | POST | `/api/bi/operational-cost/calculate` | BiRead | ✅ |
| 13 | POST | `/api/bi/profitability/calculate` | BiRead | ✅ |

### BFF (13 endpoints totales)

| # | Método | BFF Route | Tipo | Estado |
|---|--------|-----------|------|--------|
| 1 | GET | `/bi/cost-config/current` | Forward | ✅ |
| 2 | GET | `/bi/cost-config/versions` | Forward | ✅ |
| 3 | POST | `/bi/cost-config/versions` | Forward | ✅ |
| 4 | POST | `/bi/consumption-entries` | Forward | ✅ |
| 5 | GET | `/bi/consumption-entries` | Forward | ✅ |
| 6 | GET | `/bi/consumption-entries/summary` | Forward | ✅ |
| 7 | DELETE | `/bi/consumption-entries/{id}` | Forward | ✅ |
| 8 | POST | `/bi/production-records` | Forward | ✅ |
| 9 | GET | `/bi/production-records` | Forward | ✅ |
| 10 | GET | `/bi/production-records/{id}` | Forward | ✅ |
| 11 | DELETE | `/bi/production-records/{id}` | Forward | ✅ |
| 12 | GET | `/bi/operational-cost?from=&to=` | **Orquesta** | ✅ |
| 13 | GET | `/bi/profitability?productionId=` | **Orquesta** | ✅ |
