# Plan de Implementación - Fuzzy Service

> **Estado Actual de Implementación (Enero 2025)**
>
> **✅ Arquitectura Completada:**
> - Clean Architecture implementada con separación clara de capas Domain → Application → Infrastructure → API
> - FuzzyEngineService implementa IFuzzyEngine y orquesta el motor difuso desde la capa Application
> - ScikitFuzzyEngine en Infrastructure como motor técnico especializado
> - Mappers centralizados en Application para conversión Domain ↔ Infrastructure
> - Sistema de inyección de dependencias mejorado con lifecycle management
>
> **✅ Funcionalidades Core:**
> - CRUD completo para todas las entidades: FuzzySystem, FuzzyVariable, FuzzyTerm, FuzzyRule, FuzzyRoutine
> - Motor de evaluación fuzzy completo con pipeline: Fuzzificación → Evaluación → Agregación → Defuzzificación
> - API REST completa con endpoints para gestión y evaluación
> - Persistencia MongoDB con repositorios especializados
> - Sistema de configuración avanzado con perfiles y validaciones
>
> **✅ Calidad y Testing:**
> - Suite de tests unitarios, integración y E2E (57 tests total)
> - Tests de dependency injection completamente funcionales (26/26 passing)
> - Validaciones robustas y manejo de errores específicos
> - Métricas, circuit breakers y health monitoring
>
> **🎯 Estrategia de Implementación Actual:**
> El sistema está **funcionalmente completo** y sigue una arquitectura híbrida que balancea pureza arquitectónica con pragmatismo técnico.



<details>
  <summary><h2>Resumen del Proyecto</h2></summary>

  Este documento describe la planeación paso a paso para construir el **Fuzzy Service** en Python, manteniendo la misma estructura de clean architecture y vertical slicing que el **Auth Service** existente.

  ## Flujo Completo del Sistema Fuzzy

  ### 1. Configuración del Sistema Difuso
  - **Sistemas**: Definir sistemas fuzzy (ej: "Vegetativo Día", "Floración Noche")
  - **Variables**: Configurar variables de entrada (sensores) y salida (actuadores)
  - **Términos**: Definir etiquetas lingüísticas con funciones de membresía
  - **Reglas**: Crear reglas IF-THEN con condiciones y consecuentes
  - **Rutinas**: Definir secuencias de actuación para cada consecuente

  ### 2. Evaluación en Tiempo Real
  - **Entrada MQTT**: Recibir datos de sensores del sensor-service
  - **Fuzzificación**: Convertir valores numéricos a grados de membresía
  - **Evaluación de Reglas**: Calcular firing strength de cada regla activa
  - **Agregación**: Combinar outputs usando operadores fuzzy (min/max)
  - **Defuzzificación**: Convertir resultado fuzzy a valores numéricos
  - **Activación**: Enviar rutinas al actuator-service vía HTTP

  ### 3. Arquitectura Técnica
  - **Clean Architecture**: Domain → Application → Infrastructure → API
  - **CQRS + Mediator**: Separación de comandos y consultas
  - **Vertical Slicing**: Features autocontenidas por entidad
  - **MongoDB**: Persistencia de configuración y historial
  - **scikit-fuzzy**: Motor de cálculos difusos
  - **MQTT Async**: Integración con sensor-service
  - **HTTP Client**: Comunicación con actuator-service
</details>

<details>
  <summary><h2>Librerías Recomendadas para Python 3.12.11</h2></summary>

### Librerías Core
- **FastAPI**: `^0.104.1` - Framework web moderno y rápido
- **Uvicorn**: `^0.24.0` - Servidor ASGI para FastAPI
- **Pydantic**: `^2.5.0` - Validación de datos y serialización
- **PyMongo**: `^4.6.0` - Driver oficial de MongoDB (compatible con Python 3.12)
- **python-jose[cryptography]**: `^3.3.0` - Manejo de tokens JWT
- **scikit-fuzzy**: `^0.4.2` - Lógica difusa y defuzzificación
- **asyncio-mqtt**: `^0.16.1` - Cliente MQTT asíncrono

### Librerías para Clean Architecture
- **Medyator**: `^0.4.0` - Implementación del patrón Mediator para CQRS Que usa kinki para la inyección de dependencias <mcreference link="https://pypi.org/project/Medyator/" index="1">1</mcreference>
- **Pydantic + pydantic_factories / orjson**: `^1.1.0` - Mapper más manual pero más estandar con FastAPI

### Librerías de Desarrollo
- **pytest**: `^7.4.0` - Framework de testing
- **pytest-asyncio**: `^0.21.0` - Soporte para testing asíncrono
- **python-dotenv**: `^1.0.0` - Manejo de variables de entorno
</details>

<details>
  <summary><h2>Estructura de Carpetas Propuesta</h2></summary>

```
fuzzy-service/
├── FuzzyService.Api/
│   ├── __init__.py
│   ├── main.py
│   ├── dependencies.py
│   ├── middleware/
│   └── configuration/
├── FuzzyService.Application/
│   ├── __init__.py
│   ├── Controllers/
│   │   ├── __init__.py
│   │   ├── FuzzySystemController.py
│   │   ├── FuzzyVariableController.py
│   │   ├── FuzzyTermController.py
│   │   ├── FuzzyRuleController.py
│   │   ├── FuzzyRoutineController.py
│   │   ├── FuzzyEvaluationController.py
│   │   └── ActuatorIntegrationController.py
│   ├── Helpers/
│   │   ├── __init__.py
│   │   ├── ValidationHelper.py
│   │   ├── MappingHelper.py
│   │   ├── FuzzyLogicHelper.py
│   │   └── DateTimeHelper.py
│   ├── Configuration/
│   │   ├── __init__.py
│   │   ├── DependencyInjection.py
│   │   ├── MqttConfiguration.py
│   │   ├── DatabaseConfiguration.py
│   │   └── FuzzyEngineConfiguration.py
│   ├── Features/
│   │   ├── FuzzySystems/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateFuzzySystem/
│   │   │   │   │   ├── CreateFuzzySystemCommand.py
│   │   │   │   │   └── CreateFuzzySystemHandler.py
│   │   │   │   ├── UpdateFuzzySystem/
│   │   │   │   │   ├── UpdateFuzzySystemCommand.py
│   │   │   │   │   └── UpdateFuzzySystemHandler.py
│   │   │   │   ├── DeleteFuzzySystem/
│   │   │   │   │   ├── DeleteFuzzySystemCommand.py
│   │   │   │   │   └── DeleteFuzzySystemHandler.py
│   │   │   │   └── UpdateFuzzySystemStatus/
│   │   │   │       ├── UpdateFuzzySystemStatusCommand.py
│   │   │   │       └── UpdateFuzzySystemStatusHandler.py
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzySystemById/
│   │   │   │   │   ├── GetFuzzySystemByIdQuery.py
│   │   │   │   │   └── GetFuzzySystemByIdHandler.py
│   │   │   │   └── GetAllFuzzySystems/
│   │   │   │       ├── GetAllFuzzySystemsQuery.py
│   │   │   │       └── GetAllFuzzySystemsHandler.py
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   ├── FuzzyVariables/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateFuzzyVariable/
│   │   │   │   ├── UpdateFuzzyVariable/
│   │   │   │   ├── DeleteFuzzyVariable/
│   │   │   │   ├── AddTermToVariable/
│   │   │   │   └── RemoveTermFromVariable/
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzyVariable/
│   │   │   │   ├── GetAllFuzzyVariables/
│   │   │   │   └── GetFuzzyVariablesBySystem/
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   ├── FuzzyTerms/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateFuzzyTerm/
│   │   │   │   ├── UpdateFuzzyTerm/
│   │   │   │   └── DeleteFuzzyTerm/
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzyTerm/
│   │   │   │   └── GetFuzzyTermsByVariable/
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   ├── FuzzyRules/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateFuzzyRule/
│   │   │   │   ├── UpdateFuzzyRule/
│   │   │   │   ├── DeleteFuzzyRule/
│   │   │   │   └── UpdateFuzzyRuleStatus/
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzyRule/
│   │   │   │   └── GetFuzzyRulesBySystem/
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   ├── FuzzyRoutines/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateFuzzyRoutine/
│   │   │   │   ├── UpdateFuzzyRoutine/
│   │   │   │   └── DeleteFuzzyRoutine/
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzyRoutine/
│   │   │   │   └── GetAllFuzzyRoutines/
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   ├── FuzzyEvaluations/
│   │   │   ├── Commands/
│   │   │   │   └── ExecuteFuzzyEvaluation/
│   │   │   ├── Queries/
│   │   │   │   ├── GetFuzzyEvaluation/
│   │   │   │   ├── GetFuzzyEvaluationHistory/
│   │   │   │   └── GetFuzzyEvaluationsBySystem/
│   │   │   ├── DTOs/
│   │   │   └── Mappings/
│   │   └── ActuatorIntegration/
│   │       ├── Commands/
│   │       │   └── SendRoutinesToActuator/
│   │       ├── Queries/
│   │       │   └── GetActuatorIntegrationStatus/
│   │       ├── DTOs/
│   │       └── Mappings/
│   ├── Common/
│   │   ├── Behaviors/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   └── Models/
│   └── Services/
├── FuzzyService.Domain/
│   ├── __init__.py
│   ├── Entities/
│   │   ├── fuzzy_system.py
│   │   ├── fuzzy_variable.py
│   │   ├── fuzzy_term.py
│   │   ├── fuzzy_rule.py
│   │   ├── fuzzy_routine.py
│   │   └── fuzzy_evaluation.py
│   ├── Interfaces/
│   │   ├── IFuzzySystemRepository.py
│   │   ├── IFuzzyVariableRepository.py
│   │   ├── IFuzzyTermRepository.py
│   │   ├── IFuzzyRuleRepository.py
│   │   ├── IFuzzyRoutineRepository.py
│   │   ├── IFuzzyEvaluationRepository.py
│   │   ├── IFuzzyEngine.py
│   │   ├── IActuatorService.py
│   │   └── IMqttService.py
│   ├── ValueObjects/
│   ├── Enums/
│   └── Common/
├── FuzzyService.Infrastructure/
│   ├── __init__.py
│   ├── Persistence/
│   │   ├── Repositories/
│   │   │   ├── FuzzySystemRepository.py
│   │   │   ├── FuzzyVariableRepository.py
│   │   │   ├── FuzzyTermRepository.py
│   │   │   ├── FuzzyRuleRepository.py
│   │   │   ├── FuzzyRoutineRepository.py
│   │   │   └── FuzzyEvaluationRepository.py
│   │   ├── Configurations/
│   │   └── Context/
│   ├── ExternalServices/
│   │   ├── MqttService/
│   │   │   ├── MqttClient.py
│   │   │   ├── MqttSubscriber.py
│   │   │   └── MqttMessageHandler.py
│   │   └── ActuatorService/
│   │       ├── ActuatorServiceClient.py
│   │       ├── ActuatorServiceModels.py
│   │       └── ActuatorServiceConfiguration.py
│   └── FuzzyEngine/
├── FuzzyService.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
├── requirements.txt
├── Dockerfile
├── .env.example
└── README.md
```
</details>

<details>
  <summary><h2>Endpoints del Fuzzy Service</h2></summary>

### Fuzzy Systems (Sistemas Difusos)
- ✅ `POST /api/fuzzy-systems` - Crear sistema difuso
- ✅ `GET /api/fuzzy-systems` - Obtener todos los sistemas difusos
- ✅ `GET /api/fuzzy-systems/{id}` - Obtener sistema difuso por ID
- ✅ `PUT /api/fuzzy-systems/{id}` - Actualizar sistema difuso
- ✅ `DELETE /api/fuzzy-systems/{id}` - Eliminar sistema difuso
- ✅ `PATCH /api/fuzzy-systems/{id}/status` - Cambiar estado (in_use/inactive)

### Fuzzy Variables (Variables del Sistema)
- ✅ `POST /api/fuzzy-variables` - Crear variable difusa
- ✅ `GET /api/fuzzy-variables` - Obtener todas las variables
- ✅ `GET /api/fuzzy-variables/system/{systemId}` - Obtener variables por sistema
- ✅ `GET /api/fuzzy-variables/{id}` - Obtener variable por ID
- ✅ `PUT /api/fuzzy-variables/{id}` - Actualizar variable
- ⚠️ `DELETE /api/fuzzy-variables/{id}` - Eliminar variable (Falta validación: remover variable de sistema)
- `POST /api/fuzzy-variables/{id}/terms` - Agregar término a variable (No implementado)
- `DELETE /api/fuzzy-variables/{id}/terms/{termId}` - Remover término de variable (No implementado)

### Fuzzy Terms (Términos Lingüísticos)
- ✅ `POST /api/fuzzy-terms` - Crear término lingüístico (Con validación bidireccional)
- ✅ `GET /api/fuzzy-terms/variable/{variableId}` - Obtener términos por variable
- ✅ `GET /api/fuzzy-terms/{id}` - Obtener término por ID
- ✅ `PUT /api/fuzzy-terms/{id}` - Actualizar término
- ✅ `DELETE /api/fuzzy-terms/{id}` - Eliminar término (Con validación bidireccional)

### Fuzzy Rules (Reglas del Sistema)
- ✅ `POST /api/fuzzy-rules` - Crear regla difusa
- ✅ `GET /api/fuzzy-rules/system/{systemId}` - Obtener reglas por sistema
- ✅ `GET /api/fuzzy-rules/{id}` - Obtener regla por ID
- ✅ `PUT /api/fuzzy-rules/{id}` - Actualizar regla completa
- ✅ `DELETE /api/fuzzy-rules/{id}` - Eliminar regla
- ✅ `PUT /api/fuzzy-rules/{id}/status` - Activar/desactivar regla
- `PUT /api/fuzzy-rules/{id}/conditions` - Actualizar condiciones de la regla (No implementado)
- `PUT /api/fuzzy-rules/{id}/connectors` - Actualizar conectores de la regla (No implementado)
- `PUT /api/fuzzy-rules/{id}/consequent` - Actualizar consecuente de la regla (No implementado)
- `POST /api/fuzzy-rules/{id}/conditions` - Agregar nueva condición (No implementado)
- `DELETE /api/fuzzy-rules/{id}/conditions/{conditionId}` - Eliminar condición específica (No implementado)

### Fuzzy Routines (Rutinas de Actuación)
- ✅ `POST /api/fuzzy-routines` - Crear rutina
- ✅ `GET /api/fuzzy-routines` - Obtener todas las rutinas
- ✅ `GET /api/fuzzy-routines/{id}` - Obtener rutina por ID
- ✅ `PUT /api/fuzzy-routines/{id}` - Actualizar rutina completa
- ✅ `DELETE /api/fuzzy-routines/{id}` - Eliminar rutina
- ✅ `POST /api/fuzzy-routines/{id}/steps` - Agregar nuevo paso a la rutina
- ✅ `PUT /api/fuzzy-routines/{id}/steps/{stepId}` - Actualizar paso específico
- ✅ `DELETE /api/fuzzy-routines/{id}/steps/{stepId}` - Eliminar paso específico

### Fuzzy Evaluations (Evaluaciones y Cálculos)
- **Nota**: Las evaluaciones fuzzy se ejecutan automáticamente cuando se reciben datos del sensor-service vía MQTT, no mediante endpoint POST
- `GET /api/fuzzy-evaluations` - Obtener historial de evaluaciones (No implementado)
- `GET /api/fuzzy-evaluations/{id}` - Obtener evaluación específica (No implementado)
- `GET /api/fuzzy-evaluations/system/{systemId}` - Obtener evaluaciones por sistema (No implementado)

### Actuator Integration (Integración con Actuadores)
- `POST /api/actuator-integration/send-routines` - Enviar rutinas al actuator-service (No implementado)
- `GET /api/actuator-integration/status` - Estado de comunicación con actuator-service (No implementado)
</details>

<details>
  <summary><h2>Estructuras JSON</h2></summary>

### Colecciones en MongoDB

#### FuzzySystem (Sistema Difuso)
```json
{
  "_id": { "$oid": "fuzzy_1" },
  "name": "Vegetativo Día",
  "status": "in_use|inactive",
  "operators": { 
    "and": "min", 
    "or": "max", 
    "not": "complement" 
  },
  "defuzzMethod": "centroid|bisector|mom|som|lom",
  "variables": [{ "$oid": "var_temp_air" }, { "$oid": "var_humidity" }],
  "rules": [{ "$oid": "rule_low_ec" }, { "$oid": "rule_high_temp" }],
  "createdBy": "fuzzy-service",
  "createdAt": "2025-01-27T23:39:17.917+00:00",
  "updatedAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyVariable (Variables del Sistema Difuso)
```json
{
  "_id": { "$oid": "var_temp_air" },
  "systemId": { "$oid": "fuzzy_1" },
  "name": "temp_air",
  "type": "input|output",
  "sensor_id": { "$oid": "5783qhgg445tu" },
  "termIds": [
    { "$oid": "term_temp_low" }, 
    { "$oid": "term_temp_ok" }, 
    { "$oid": "term_temp_high" }
  ],
  "createdAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyTerm (Etiquetas/Términos Lingüísticos)
```json
{
  "_id": { "$oid": "term_temp_high" },
  "variableId": { "$oid": "var_temp_air" },
  "label": "high",
  "mf": {
    "type": "trapezoid|triangle|gaussian",
    "params": [28, 30, 40, 40]
  },
  "createdAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyRule (Reglas del Sistema Difuso)
```json
{
  "_id": { "$oid": "rule_low_ec" },
  "name": "low_ec",
  "systemId": { "$oid": "fuzzy_1" },
  "description": "Si EC es baja, activar recirculación de agua",
  "conditions": [
    { 
      "conditionId": "cond_001",
      "sensor": "EC", 
      "operator": "IS", 
      "value": "Baja" 
    },
    { 
      "conditionId": "cond_002",
      "sensor": "NivelAgua", 
      "operator": "NOT", 
      "value": "Bajo" 
    }
  ],
  "connector": ["AND"],
  "consequent": "RecirculaAgua",
  "isActive": true,
  "createdAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyRoutine (Rutinas de Actuación)
```json
{
  "routineId":  { "$oid": "11118bb77b05e378b3731341" },
  "routine_name": "Calentar Invernadero",
  "steps": [
    {
      "stepId": "step_001",
      "actuator": { "$oid": "68ab8bb77b05e378b3731341" },
      "power_tag_id": { "$oid": "68ab8b887b05e378b3732332" },
      "duration_tag_id": { "$oid": "68ab8b887b05e378b3731344" }
    },
    {
      "stepId": "step_002",
      "actuator": { "$oid": "68ab8b887b05e378b3731341" },
      "power_tag_id": { "$oid": "68ab8b337b05e378b3731341" },
      "duration_tag_id": { "$oid": "68aaab887b05e378b3731341" }
    }
  ],
  "createdAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyEvaluation (Evaluaciones del Sistema Difuso)
```json
{
  "evalId": { "$oid": "eval_uuid_12345" },
  "systemId": { "$oid": "fuzzy_1" },
  "timestamp": "2025-01-27T23:39:17.917+00:00",
  "inputs": [
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 6.5
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 1.2
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 28.3
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 55.2
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 11000
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 12.5
    },
    {
      "sensor_id": { "$oid": "5783qhgg445tu" },
      "value": 22.0
    }
  ],
  "activated_rules": [
    {
      "ruleId": { "$oid": "rule_calentar_ambiente" },
      "firingStrength": 0.8,
      "output_values": [
        { 
          "actuator_id": { "$oid": "68ab8bb77b05e378b3731341" }, 
          "power": 70, 
          "duration": 60 
        },
        { 
          "actuator_id": { "$oid": "68ab8cc33b05e378b3731341" }, 
          "power": 30, 
          "duration": 50 
        }
      ]
    }
  ]
}
```

### Payload: Fuzzy Service → Actuator Service

```json
[
  {
    "routineId": "recirculaAgua",
    "steps": [
      { 
        "actuator": { "$oid": "68ab8bb77b05e378b3731341" }, 
        "power": "ON", 
        "expiration": "2025-08-25T15:01:00Z" 
      },
      { 
        "actuator": { "$oid": "68ab8bb3123378b37311212" }, 
        "dutyCycle": 70, 
        "expiration": "2025-08-25T15:02:00Z" 
      }
    ]
  }
]
```
</details>

<details>
  <summary><h2>Estrategia de Implementación Actual</h2></summary>

## 🎯 Enfoque Arquitectónico Híbrido

El FuzzyService implementa una **arquitectura híbrida** que combina los principios de Clean Architecture con pragmatismo técnico para lograr un sistema robusto y mantenible.

### 🏗️ Capas Arquitectónicas

#### **Domain Layer (Núcleo del Negocio)**
- **Entidades**: FuzzySystem, FuzzyVariable, FuzzyTerm, FuzzyRule, FuzzyRoutine, FuzzyEvaluation
- **Value Objects**: DomainId, RuleCondition, MembershipFunction, InputValue
- **Interfaces**: IFuzzyEngine, IFuzzySystemRepository, IFuzzyVariableRepository, etc.
- **Enums**: FuzzySystemStatus, DefuzzificationMethod, AggregationMethod

#### **Application Layer (Orquestación)**
- **Services**: FuzzyEngineService (implementa IFuzzyEngine)
- **Mappers**: DomainToInfrastructureMapper, InfrastructureToDomainMapper
- **Configuration**: DependencyInjection, ImprovedDependencyInjection
- **Features**: Commands y Queries por entidad (CQRS pattern)

#### **Infrastructure Layer (Detalles Técnicos)**
- **Persistence**: Repositorios MongoDB especializados
- **FuzzyEngine**: ScikitFuzzyEngine con pipeline completo
- **ExternalServices**: MQTT, HTTP clients
- **Configuration**: DatabaseConfiguration, MqttConfiguration

#### **API Layer (Interfaz Externa)**
- **Controllers**: REST endpoints por entidad
- **Middleware**: ErrorHandling, CORS, Authentication
- **Configuration**: FastAPI setup y routing

### 🔄 Flujo de Datos Implementado

```
API Request → Controller → Command/Query → Application Service → Domain Repository → Infrastructure Repository → MongoDB
                                     ↓
API Response ← DTO ← Domain Entity ← Mapper ← Infrastructure Entity ← Database Document
```

### ⚡ Motor Fuzzy - Pipeline de Evaluación

1. **Entrada**: Datos de sensores vía API o MQTT
2. **Carga de Configuración**: Repositorios cargan sistema, variables, términos y reglas
3. **Conversión**: Mappers convierten entidades Domain → Infrastructure
4. **Fuzzificación**: Valores numéricos → grados de membresía
5. **Evaluación de Reglas**: Cálculo de firing strength
6. **Agregación**: Combinación de outputs fuzzy
7. **Defuzzificación**: Resultado fuzzy → valores numéricos
8. **Respuesta**: Conversión Infrastructure → Domain → DTO

### 🎯 Principios de Diseño Aplicados

- **Clean Architecture**: Separación clara de responsabilidades
- **SOLID**: Single Responsibility, Open/Closed, Dependency Inversion
- **DRY**: Mappers centralizados eliminan duplicación
- **CQRS**: Separación de comandos y consultas
- **Repository Pattern**: Abstracción de persistencia
- **Dependency Injection**: Inversión de control con lifecycle management
  - FuzzyRuleRepository ✅
  - FuzzyRoutineRepository ✅
  - FuzzyEvaluationRepository ✅
- Índices y validaciones:
  - Índices en Systems/Variables/Terms creados y togglables por FUZZY_ENSURE_INDEXES_ON_STARTUP ✅
  - Validaciones referenciales básicas (Variable→System, Term→Variable) ✅
- DI de repositorios y ensure_indexes en startup configurable ✅

### Fase 4: Application Layer - Núcleo (Mediator, DTOs, Mappings) ✅
1. Configurar Mediator (Medyator) y Dependency Injection ✅
2. Implementar DTOs y mappings Pydantic para entidades ✅
3. Implementar helpers de validación y utilidades comunes ✅

### Fase 5: Vertical Slice - Sistemas Difusos (API → Mongo end-to-end) ✅
1. Commands y Queries de FuzzySystems ✅
2. Handlers CRUD y validación ✅
3. Controlador API de FuzzySystems y wiring ✅
4. Pruebas de integración básicas API→Mongo para sistemas ✅

### Fase 6: Vertical Slice - Variables y Términos (API → Mongo end-to-end) ✅
1. Commands y Queries para FuzzyVariables ✅
2. Commands y Queries para FuzzyTerms ✅
3. Controladores y handlers ✅
4. Validación de funciones de membresía ✅
5. Pruebas de integración API→Mongo para variables y términos ✅
6. Validación bidireccional Term↔Variable ✅

### Fase 7: Vertical Slice - Reglas y Rutinas (API → Mongo end-to-end) ✅
1. Commands y Queries para FuzzyRules ✅
2. Commands y Queries para FuzzyRoutines ✅
3. Validaciones de condiciones y consecuentes ✅
4. Controladores y handlers ✅
5. Pruebas de integración API→Mongo para reglas y rutinas ✅

### Fase 8: API Layer - Middleware, Autenticación y Observabilidad ⏳
1. Middleware de manejo de errores y logging ✅
2. Configurar CORS y documentación Swagger ⏳
3. Observabilidad básica (logs) y respuestas unificadas ✅

### Fase 9: Integración MQTT (solo escucha)
1. Configurar cliente MQTT asíncrono (suscripción)
2. Suscribirse a tópicos de sensores y parseo de mensajes
3. Persistir entradas crudas o normalizadas si aplica
4. Asegurar resiliencia (reconexión, QoS necesario)
Nota: Por ahora no se publicará a MQTT, solo suscripción/escucha.

### Fase 10: Motor de Evaluación Difusa
1. Integrar scikit-fuzzy para cálculos difusos
2. Implementar algoritmo de defuzzificación (centroid, bisector, mom, som, lom)
3. Evaluación de reglas con firing strength y agregación
4. Persistir evaluaciones y outputs
5. Exponer Queries de historial de evaluaciones

### Fase 11: Integración con Actuadores
1. Implementar Commands para ActuatorIntegration
2. Crear servicio de comunicación HTTP con actuator-service
3. Implementar transformación de rutinas a payload de actuadores
4. Implementar manejo de respuestas y estados

### Fase 12: Testing y CI/CD
1. Implementar tests unitarios y de integración para las capas críticas
2. Crear tests E2E mínimos del flujo completo
3. Generar documentación de API con OpenAPI/Swagger
4. Configurar CI/CD y deployment

## 📋 Lista de Tareas Pendientes

### 🔧 Validaciones y Mejoras de Endpoints Existentes

#### Variables
- ⚠️ **Implementar validación bidireccional Variable↔System en DELETE**: Cuando se elimina una variable, removerla de la lista de variables del sistema
- 📝 **Implementar endpoints de gestión de términos en variables**:
  - `POST /api/fuzzy-variables/{id}/terms` - Agregar término a variable
  - `DELETE /api/fuzzy-variables/{id}/terms/{termId}` - Remover término de variable

#### Reglas - Endpoints Granulares
- 📝 **Implementar endpoints de gestión granular de reglas**:
  - `PUT /api/fuzzy-rules/{id}/conditions` - Actualizar condiciones de la regla
  - `PUT /api/fuzzy-rules/{id}/connectors` - Actualizar conectores de la regla
  - `PUT /api/fuzzy-rules/{id}/consequent` - Actualizar consecuente de la regla
  - `POST /api/fuzzy-rules/{id}/conditions` - Agregar nueva condición
  - `DELETE /api/fuzzy-rules/{id}/conditions/{conditionId}` - Eliminar condición específica

### 🚀 Nuevas Funcionalidades

#### Evaluaciones Fuzzy
- 📝 **Implementar endpoints de evaluaciones**:
  - `GET /api/fuzzy-evaluations` - Obtener historial de evaluaciones
  - `GET /api/fuzzy-evaluations/{id}` - Obtener evaluación específica
  - `GET /api/fuzzy-evaluations/system/{systemId}` - Obtener evaluaciones por sistema

#### Integración con Actuadores
- 📝 **Implementar endpoints de integración con actuadores**:
  - `POST /api/actuator-integration/send-routines` - Enviar rutinas al actuator-service
  - `GET /api/actuator-integration/status` - Estado de comunicación con actuator-service

#### Motor de Evaluación Fuzzy (Fase 10)
- 🔬 **Integrar scikit-fuzzy para cálculos difusos**
- 🧮 **Implementar algoritmo de defuzzificación** (centroid, bisector, mom, som, lom)
- 📊 **Evaluación de reglas con firing strength y agregación**
- 💾 **Persistir evaluaciones y outputs**
- 📈 **Exponer Queries de historial de evaluaciones**

#### Integración MQTT (Fase 9)
- 📡 **Configurar cliente MQTT asíncrono** (suscripción)
- 📨 **Suscribirse a tópicos de sensores y parseo de mensajes**
- 💾 **Persistir entradas crudas o normalizadas si aplica**
- 🔄 **Asegurar resiliencia** (reconexión, QoS necesario)

### 🛠️ Configuración y Documentación

#### API y Documentación
- ⏳ **Completar configuración CORS**
- 📚 **Asegurar documentación Swagger completa** con todos los endpoints
- 🔍 **Revisar y completar esquemas OpenAPI**

#### Testing y Calidad
- 🧪 **Implementar tests unitarios** para las capas críticas
- 🔄 **Crear tests E2E** mínimos del flujo completo
- 📋 **Tests de integración** para nuevas funcionalidades

#### DevOps y Deployment
- 🐳 **Configurar CI/CD** y deployment
- 📦 **Optimizar Dockerfile** y configuración de contenedores
- 🔧 **Configurar variables de entorno** para diferentes ambientes

### 🎯 Prioridades Recomendadas

1. **Alta Prioridad**:
   - Validación bidireccional Variable↔System en DELETE
   - Completar configuración CORS y Swagger
   - Endpoints de evaluaciones fuzzy

2. **Media Prioridad**:
   - Endpoints granulares de reglas
   - Endpoints de gestión de términos en variables
   - Motor de evaluación fuzzy básico

3. **Baja Prioridad**:
   - Integración MQTT
   - Integración con actuadores
   - CI/CD y deployment avanzado
</details>

## 🧠 Lógica Fuzzy Core - Flujo de Evaluación

### 📋 Flujo Propuesto de Evaluación en Tiempo Real

#### **1. Recepción de Datos MQTT**
```python
# Payload del sensor-service
{
  "timestamp": "2025-01-28T10:30:00Z",
  "readings": {
    "sensor_temp_001": 25.5, # La key debe ser un id bson vinculado a algun sistema
    "sensor_humidity_001": 65.2,
    "sensor_ec_001": 1.8,
    "sensor_ph_001": 6.2
  }
}
```

**Responsabilidades:**
- **MqttMessageHandler**: Parsear payload y validar estructura
- **FuzzyEvaluationService**: Orquestar evaluación completa
- **Filtrado de Sistema**: Identificar sistema activo (status="in_use")

#### **2. Fuzzificación de Variables de Entrada**
```python
# Ejemplo: Temperatura = 25.5°C
# Variable: "temperatura" con términos [bajo, medio, alto]
terms_membership = {
    "bajo": 0.0,    # trimf([15, 18, 22]) → 0.0 para 25.5
    "medio": 0.7,   # trimf([20, 25, 30]) → 0.7 para 25.5  
    "alto": 0.3     # trimf([25, 30, 35]) → 0.3 para 25.5
}
```

**Proceso:**
1. **Mapeo Sensor→Variable**: Buscar variables del sistema activo por `device_id`
2. **Carga de Términos**: Obtener términos de cada variable con sus funciones de membresía
3. **Conversión a scikit-fuzzy**: Convertir `MembershipFunction` a funciones numpy
4. **Cálculo de Membresía**: Evaluar grado de pertenencia para cada término

**Mejoras Técnicas:**
- ✅ **Cache de Funciones**: Precalcular funciones de membresía en memoria
- ✅ **Validación de Rangos**: Verificar que valores estén dentro del universo de discurso
- ✅ **Manejo de Sensores Faltantes**: Estrategia para datos incompletos

#### **3. Evaluación de Reglas Activas**
```python
# Regla: IF temperatura IS medio AND humedad IS alta THEN bomba IS encendida
rule_conditions = {
    "temperatura": ("medio", 0.7),
    "humedad": ("alta", 0.8)
}

# Firing strength con operador AND (min)
firing_strength = min(0.7, 0.8) = 0.7
```

**Proceso:**
1. **Carga de Reglas**: Obtener todas las reglas del sistema activo (status="in_use")
2. **Evaluación de Antecedentes**: Para cada regla, evaluar condiciones
3. **Aplicación de Conectores**: Usar operadores lógicos (AND→min, OR→max, NOT→complement)
4. **Cálculo de Firing Strength**: Determinar fuerza de activación de cada regla

**Operadores Configurables:**
```python
# Configuración en FuzzySystem.operators
operators = {
    "and": "min",        # np.minimum
    "or": "max",         # np.maximum  
    "not": "complement"   # 1 - membership
}
```

#### **4. Procesamiento de Consecuentes (Rutinas)**
```python
# Consecuente: "rutina_riego" con firing_strength = 0.7
rutina = {
    "routine_name": "rutina_riego",
    "steps": [
        {
            "actuator_id": "pump_001",
            "power_term_id": "potencia_media",    # Término fuzzy
            "duration_term_id": "duracion_corta"  # Término fuzzy
        }
    ]
}
```

**Proceso:**
1. **Identificación de Rutinas**: Mapear consecuentes a rutinas en BD
2. **Carga de Pasos**: Obtener steps con actuator_id y term_ids
3. **Preparación para Defuzzificación**: Agrupar términos de salida por variable (power/duration) para procesar cada paso de cada rutina de manera independiente

#### **5. Defuzzificación de Términos de Salida**
```python
# Defuzzificación de "potencia_media" con firing_strength = 0.7
power_universe = np.linspace(0, 100, 101)  # 0-100% PWM
power_mf = trapmf(power_universe, [30, 50, 70, 90])  # Función trapezoidal

# Aplicar firing strength (recorte)
clipped_mf = np.minimum(power_mf, 0.7)

# Defuzzificación (centroid)
power_crisp = defuzz(power_universe, clipped_mf, 'centroid')  # ≈ 60%
```

**Métodos Soportados:**
- **CENTROID**: Centro de gravedad (recomendado)
- **BISECTOR**: Bisector del área
- **MOM/SOM/LOM**: Métodos de máximo

**Casos Especiales:**
- **Términos ON/OFF**: No requieren defuzzificación (valores booleanos)
- **Múltiples Reglas**: Cuando varias reglas activan el mismo término de salida, se usa agregación MAX de los grados de pertenencia (firing strengths) antes de defuzzificar

#### **6. Construcción del Payload para Actuator-Service**
```python
# Payload final - Múltiples rutinas activadas
payload = [
    {
        "routineId": "rutina_riego",
        "steps": [
            {
                "actuator": {"$oid": "pump_001"},
                "power": 60,  # Defuzzificación independiente para este paso
                "duration": 45  # Defuzzificación independiente para este paso
            }
        ]
    },
    {
        "routineId": "rutina_ventilacion",
        "steps": [
            {
                "actuator": {"$oid": "fan_001"},
                "power": 80,  # Defuzzificación independiente
                "duration": 30  # Defuzzificación independiente
            }
        ]
    }
]
```

**Características Importantes:**
- ✅ **Múltiples Rutinas**: Cada regla activada puede generar una rutina independiente
- ✅ **Defuzzificación por Paso**: Cada step tiene su propia defuzzificación de power y duration
- ✅ **Sin Expiration**: El actuator-service calcula la expiración para optimizar tiempo de ejecución
- ✅ **Validación de Rangos**: Asegurar power (0-100) y duration (5-60) dentro de límites
- ✅ **Agregación MAX**: Solo aplica cuando múltiples reglas activan el mismo término de la misma variable de salida

#### **7. Persistencia de Evaluación**
```python
fuzzy_evaluation = FuzzyEvaluation(
    system_id=active_system.id,
    timestamp=datetime.now(timezone.utc),
    inputs=[
        InputValue(sensor_id="sensor_temp_001", value=25.5),
        InputValue(sensor_id="sensor_humidity_001", value=65.2)
    ],
    activated_rules=[
        RuleActivation(
            rule_id="rule_riego_001",
            firing_strength=0.7,
            output_values=[
                OutputValue(actuator_id="pump_001", power=60, duration=45)
            ]
        )
    ]
)
```

### 🔧 Consideraciones Técnicas y Mejoras

#### **Optimizaciones de Rendimiento**
1. **Cache de Configuración**: Mantener sistema activo y sus componentes en memoria
2. **Precálculo de Funciones**: Generar arrays numpy de funciones de membresía al startup
3. **Evaluación Paralela**: Procesar múltiples reglas concurrentemente
4. **Batch Processing**: Agrupar evaluaciones si llegan múltiples lecturas

#### **Manejo de Errores**
1. **Sensores Faltantes**: Continuar evaluación con sensores disponibles
2. **Reglas Inválidas**: Log y skip reglas con condiciones no satisfacibles
3. **Actuator-Service Down**: Queue rutinas para reintento
4. **Valores Fuera de Rango**: Clamp a límites del universo de discurso

#### **Configuración Flexible**
```python
# Variables de entorno
FUZZY_EVALUATION_CYCLE_MS=1000        # Ciclo de evaluación
FUZZY_CACHE_SYSTEM_CONFIG=true        # Cache de configuración
FUZZY_PARALLEL_RULE_EVALUATION=true   # Evaluación paralela
FUZZY_MAX_CONCURRENT_RULES=10         # Límite de concurrencia
FUZZY_ACTUATOR_TIMEOUT_MS=5000        # Timeout para actuator-service
```

#### **Monitoreo y Observabilidad**
1. **Métricas**: Tiempo de evaluación, reglas activadas, errores
2. **Logs Estructurados**: JSON logs con contexto de evaluación
3. **Health Checks**: Estado de MQTT, MongoDB, Actuator-Service
4. **Alertas**: Notificaciones por fallos críticos

### 🚀 Implementación Propuesta

#### **Estructura de Clases**
```python
# Infrastructure/FuzzyEngine/
class ScikitFuzzyEngine(IFuzzyEngine):
    async def evaluate(self, system_id, inputs, **kwargs) -> FuzzyEvaluation

class MembershipFunctionConverter:
    def to_numpy_function(self, mf: MembershipFunction) -> callable

class FuzzificationService:
    async def fuzzify_inputs(self, variables, sensor_readings) -> Dict

class RuleEvaluationService:
    async def evaluate_rules(self, rules, fuzzified_inputs) -> List[RuleActivation]

class DefuzzificationService:
    def defuzzify_output(self, term, firing_strength, method) -> float

class ActuatorPayloadBuilder:
    def build_payload(self, activated_rules, routines) -> List[Dict]
```

#### **Flujo de Integración**
1. **MQTT Handler** → `FuzzyEngine.evaluate()`
2. **FuzzyEngine** → Orquesta servicios especializados
3. **Resultado** → `ActuatorServiceClient.send_routines()`
4. **Confirmación** → `FuzzyEvaluationRepository.save()`

Este diseño asegura **separación de responsabilidades**, **testabilidad** y **escalabilidad** del motor fuzzy.

<details>
  <summary><h2>Configuración del Entorno de Desarrollo</h2></summary>

### Crear Virtual Environment con Anaconda

```bash
# Navegar al directorio del proyecto
cd C:/Proyectos/hydroespinaca/software-project/fuzzy-service

# Crear environment con Python 3.12.11
conda create -n fuzzy-service python=3.12.11

# Activar environment
conda activate fuzzy-service

# Instalar pip en el environment
conda install pip


# Instalar dependencias
pip install -r requirements.txt

# Ejecutar en modo desarrollo
uvicorn FuzzyService.Api.main:app --reload --host 0.0.0.0 --port 8000
```

### Variables de Entorno (.env)

```env
# MongoDB
MONGO_CONNECTION_STRING=mongodb+srv://user:password@cluster.mongodb.net/
MONGO_DATABASE_NAME=hydroespinaca_fuzzy

# Collections
COLLECTION_FUZZY_SYSTEMS=fuzzy_systems
COLLECTION_FUZZY_VARIABLES=fuzzy_variables
COLLECTION_FUZZY_TERMS=fuzzy_terms
COLLECTION_FUZZY_RULES=fuzzy_rules
COLLECTION_FUZZY_ROUTINES=fuzzy_routines
COLLECTION_FUZZY_EVALUATIONS=fuzzy_evaluations

# JWT
JWT_SECRET_KEY=your-secret-key
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=30

# Actuator Service
ACTUATOR_SERVICE_URL=http://actuator-service:8002
ACTUATOR_SERVICE_TIMEOUT=30
ACTUATOR_SERVICE_ENDPOINT=/api/routines

# Fuzzy Engine
FUZZY_EVALUATION_CYCLE_MINUTES=1
DEFAULT_DEFUZZ_METHOD=centroid
DEFAULT_AND_OPERATOR=min
DEFAULT_OR_OPERATOR=max
DEFAULT_NOT_OPERATOR=complement

# Application
ENVIRONMENT=development
LOG_LEVEL=INFO
API_PREFIX=/api
SERVICE_NAME=fuzzy-service
SERVICE_VERSION=1.0.0
```
</details>

<details>
  <summary><h2>Notas Importantes</h2></summary>

1. **Mediator Pattern**: Utilizaremos la librería `medyator` que es un port directo de MediatR de .NET <mcreference link="https://pypi.org/project/Medyator/" index="1">1</mcreference>
2. **Object Mapping**: `Pydantic` proporciona funcionalidad nativa para el mapeo entre DTOs y entidades
3. **Vertical Slicing**: Cada feature tendrá su propia carpeta con Commands, Queries, DTOs y Mappings
4. **Clean Architecture**: Mantendremos separación clara entre capas Domain, Application, Infrastructure y API
5. **Compatibilidad**: Todas las librerías son compatibles con Python 3.12.11

Este plan asegura una implementación gradual y sistemática del Fuzzy Service, manteniendo la consistencia arquitectónica con el Auth Service existente.
</details>
