# Plan de ImplementaciÃ³n - Fuzzy Service

> **Estado Actual de ImplementaciÃ³n**
>
> **âœ… Arquitectura Completada:**
> - Clean Architecture implementada con separaciÃ³n clara de capas Domain â†’ Application â†’ Infrastructure â†’ API
> - FuzzyEngineService implementa IFuzzyEngine y orquesta el motor difuso desde la capa Application
> - ScikitFuzzyEngine en Infrastructure como motor tÃ©cnico especializado
> - Mappers centralizados en Application para conversiÃ³n Domain â†” Infrastructure
> - Sistema de inyecciÃ³n de dependencias mejorado con lifecycle management
>
> **âœ… Funcionalidades Core:**
> - CRUD completo para todas las entidades: FuzzySystem, FuzzyVariable, FuzzyTerm, FuzzyRule, FuzzyRoutine
> - Motor de evaluaciÃ³n fuzzy completo con pipeline: FuzzificaciÃ³n â†’ EvaluaciÃ³n â†’ AgregaciÃ³n â†’ DefuzzificaciÃ³n
> - API REST completa con endpoints para gestiÃ³n y evaluaciÃ³n
> - Persistencia MongoDB con repositorios especializados
> - Sistema de configuraciÃ³n avanzado con perfiles y validaciones
>
> **âœ… Calidad y Testing:**
> - Suite de tests unitarios, integraciÃ³n y E2E (57 tests total)
> - Tests de dependency injection completamente funcionales (26/26 passing)
> - Validaciones robustas y manejo de errores especÃ­ficos
> - MÃ©tricas, circuit breakers y health monitoring
>
> **ðŸŽ¯ Estrategia de ImplementaciÃ³n Actual:**
> El sistema estÃ¡ **funcionalmente completo** y sigue una arquitectura hÃ­brida que balancea pureza arquitectÃ³nica con pragmatismo tÃ©cnico.



<details>
  <summary><h2>Resumen del Proyecto</h2></summary>

  Este documento describe la planeaciÃ³n paso a paso para construir el **Fuzzy Service** en Python, manteniendo la misma estructura de clean architecture y vertical slicing que el **Auth Service** existente.

  ## Flujo Completo del Sistema Fuzzy

  ### 1. ConfiguraciÃ³n del Sistema Difuso
  - **Sistemas**: Definir sistemas fuzzy (ej: "Vegetativo DÃ­a", "FloraciÃ³n Noche")
  - **Variables**: Configurar variables de entrada (sensores) y salida (actuadores)
  - **TÃ©rminos**: Definir etiquetas lingÃ¼Ã­sticas con funciones de membresÃ­a
  - **Reglas**: Crear reglas IF-THEN con condiciones y consecuentes
  - **Rutinas**: Definir secuencias de actuaciÃ³n para cada consecuente

  ### 2. EvaluaciÃ³n en Tiempo Real
  - **Entrada MQTT**: Recibir datos de sensores del sensor-service
  - **FuzzificaciÃ³n**: Convertir valores numÃ©ricos a grados de membresÃ­a
  - **EvaluaciÃ³n de Reglas**: Calcular firing strength de cada regla activa
  - **AgregaciÃ³n**: Combinar outputs usando operadores fuzzy (min/max)
  - **DefuzzificaciÃ³n**: Convertir resultado fuzzy a valores numÃ©ricos
  - **ActivaciÃ³n**: Enviar rutinas al actuator-service vÃ­a HTTP

  ### 3. Arquitectura TÃ©cnica
  - **Clean Architecture**: Domain â†’ Application â†’ Infrastructure â†’ API
  - **CQRS + Mediator**: SeparaciÃ³n de comandos y consultas
  - **Vertical Slicing**: Features autocontenidas por entidad
  - **MongoDB**: Persistencia de configuraciÃ³n y historial
  - **scikit-fuzzy**: Motor de cÃ¡lculos difusos
  - **MQTT Async**: IntegraciÃ³n con sensor-service
  - **HTTP Client**: ComunicaciÃ³n con actuator-service
</details>

<details>
  <summary><h2>LibrerÃ­as Recomendadas para Python 3.12.11</h2></summary>

### LibrerÃ­as Core
- **FastAPI**: `^0.104.1` - Framework web moderno y rÃ¡pido
- **Uvicorn**: `^0.24.0` - Servidor ASGI para FastAPI
- **Pydantic**: `^2.5.0` - ValidaciÃ³n de datos y serializaciÃ³n
- **PyMongo**: `^4.6.0` - Driver oficial de MongoDB (compatible con Python 3.12)
- **python-jose[cryptography]**: `^3.3.0` - Manejo de tokens JWT
- **scikit-fuzzy**: `^0.4.2` - LÃ³gica difusa y defuzzificaciÃ³n
- **asyncio-mqtt**: `^0.16.1` - Cliente MQTT asÃ­ncrono

### LibrerÃ­as para Clean Architecture
- **Medyator**: `^0.4.0` - ImplementaciÃ³n del patrÃ³n Mediator para CQRS Que usa kinki para la inyecciÃ³n de dependencias <mcreference link="https://pypi.org/project/Medyator/" index="1">1</mcreference>
- **Pydantic + pydantic_factories / orjson**: `^1.1.0` - Mapper mÃ¡s manual pero mÃ¡s estandar con FastAPI

### LibrerÃ­as de Desarrollo
- **pytest**: `^7.4.0` - Framework de testing
- **pytest-asyncio**: `^0.21.0` - Soporte para testing asÃ­ncrono
- **python-dotenv**: `^1.0.0` - Manejo de variables de entorno
</details>

<details>
  <summary><h2>Estructura de Carpetas Propuesta</h2></summary>

```
fuzzy-service/
â”œâ”€â”€ FuzzyService.Api/
â”‚   â”œâ”€â”€ __init__.py
â”‚   â”œâ”€â”€ main.py
â”‚   â”œâ”€â”€ dependencies.py
â”‚   â”œâ”€â”€ middleware/
â”‚   â””â”€â”€ configuration/
â”œâ”€â”€ FuzzyService.Application/
â”‚   â”œâ”€â”€ __init__.py
â”‚   â”œâ”€â”€ Controllers/
â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”œâ”€â”€ FuzzySystemController.py
â”‚   â”‚   â”œâ”€â”€ FuzzyVariableController.py
â”‚   â”‚   â”œâ”€â”€ FuzzyTermController.py
â”‚   â”‚   â”œâ”€â”€ FuzzyRuleController.py
â”‚   â”‚   â”œâ”€â”€ FuzzyRoutineController.py
â”‚   â”‚   â”œâ”€â”€ FuzzyEvaluationController.py
â”‚   â”‚   â””â”€â”€ ActuatorIntegrationController.py
â”‚   â”œâ”€â”€ Helpers/
â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”œâ”€â”€ ValidationHelper.py
â”‚   â”‚   â”œâ”€â”€ MappingHelper.py
â”‚   â”‚   â”œâ”€â”€ FuzzyLogicHelper.py
â”‚   â”‚   â””â”€â”€ DateTimeHelper.py
â”‚   â”œâ”€â”€ Configuration/
â”‚   â”‚   â”œâ”€â”€ __init__.py
â”‚   â”‚   â”œâ”€â”€ DependencyInjection.py
â”‚   â”‚   â”œâ”€â”€ MqttConfiguration.py
â”‚   â”‚   â”œâ”€â”€ DatabaseConfiguration.py
â”‚   â”‚   â””â”€â”€ FuzzyEngineConfiguration.py
â”‚   â”œâ”€â”€ Features/
â”‚   â”‚   â”œâ”€â”€ FuzzySystems/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzySystem/
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzySystemCommand.py
â”‚   â”‚   â”‚   â”‚   â”‚   â””â”€â”€ CreateFuzzySystemHandler.py
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzySystem/
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzySystemCommand.py
â”‚   â”‚   â”‚   â”‚   â”‚   â””â”€â”€ UpdateFuzzySystemHandler.py
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ DeleteFuzzySystem/
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ DeleteFuzzySystemCommand.py
â”‚   â”‚   â”‚   â”‚   â”‚   â””â”€â”€ DeleteFuzzySystemHandler.py
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ UpdateFuzzySystemStatus/
â”‚   â”‚   â”‚   â”‚       â”œâ”€â”€ UpdateFuzzySystemStatusCommand.py
â”‚   â”‚   â”‚   â”‚       â””â”€â”€ UpdateFuzzySystemStatusHandler.py
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzySystemById/
â”‚   â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzySystemByIdQuery.py
â”‚   â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetFuzzySystemByIdHandler.py
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetAllFuzzySystems/
â”‚   â”‚   â”‚   â”‚       â”œâ”€â”€ GetAllFuzzySystemsQuery.py
â”‚   â”‚   â”‚   â”‚       â””â”€â”€ GetAllFuzzySystemsHandler.py
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â”œâ”€â”€ FuzzyVariables/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzyVariable/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzyVariable/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ DeleteFuzzyVariable/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ AddTermToVariable/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ RemoveTermFromVariable/
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyVariable/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetAllFuzzyVariables/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetFuzzyVariablesBySystem/
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â”œâ”€â”€ FuzzyTerms/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzyTerm/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzyTerm/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ DeleteFuzzyTerm/
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyTerm/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetFuzzyTermsByVariable/
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â”œâ”€â”€ FuzzyRules/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzyRule/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzyRule/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ DeleteFuzzyRule/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ UpdateFuzzyRuleStatus/
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyRule/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetFuzzyRulesBySystem/
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â”œâ”€â”€ FuzzyRoutines/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ CreateFuzzyRoutine/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ UpdateFuzzyRoutine/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ DeleteFuzzyRoutine/
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyRoutine/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetAllFuzzyRoutines/
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â”œâ”€â”€ FuzzyEvaluations/
â”‚   â”‚   â”‚   â”œâ”€â”€ Commands/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ ExecuteFuzzyEvaluation/
â”‚   â”‚   â”‚   â”œâ”€â”€ Queries/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyEvaluation/
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ GetFuzzyEvaluationHistory/
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ GetFuzzyEvaluationsBySystem/
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/
â”‚   â”‚   â””â”€â”€ ActuatorIntegration/
â”‚   â”‚       â”œâ”€â”€ Commands/
â”‚   â”‚       â”‚   â””â”€â”€ SendRoutinesToActuator/
â”‚   â”‚       â”œâ”€â”€ Queries/
â”‚   â”‚       â”‚   â””â”€â”€ GetActuatorIntegrationStatus/
â”‚   â”‚       â”œâ”€â”€ DTOs/
â”‚   â”‚       â””â”€â”€ Mappings/
â”‚   â”œâ”€â”€ Common/
â”‚   â”‚   â”œâ”€â”€ Behaviors/
â”‚   â”‚   â”œâ”€â”€ Exceptions/
â”‚   â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â””â”€â”€ Models/
â”‚   â””â”€â”€ Services/
â”œâ”€â”€ FuzzyService.Domain/
â”‚   â”œâ”€â”€ __init__.py
â”‚   â”œâ”€â”€ Entities/
â”‚   â”‚   â”œâ”€â”€ fuzzy_system.py
â”‚   â”‚   â”œâ”€â”€ fuzzy_variable.py
â”‚   â”‚   â”œâ”€â”€ fuzzy_term.py
â”‚   â”‚   â”œâ”€â”€ fuzzy_rule.py
â”‚   â”‚   â”œâ”€â”€ fuzzy_routine.py
â”‚   â”‚   â””â”€â”€ fuzzy_evaluation.py
â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”œâ”€â”€ IFuzzySystemRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyVariableRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyTermRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyRuleRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyRoutineRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyEvaluationRepository.py
â”‚   â”‚   â”œâ”€â”€ IFuzzyEngine.py
â”‚   â”‚   â”œâ”€â”€ IActuatorService.py
â”‚   â”‚   â””â”€â”€ IMqttService.py
â”‚   â”œâ”€â”€ ValueObjects/
â”‚   â”œâ”€â”€ Enums/
â”‚   â””â”€â”€ Common/
â”œâ”€â”€ FuzzyService.Infrastructure/
â”‚   â”œâ”€â”€ __init__.py
â”‚   â”œâ”€â”€ Persistence/
â”‚   â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â”‚   â”œâ”€â”€ FuzzySystemRepository.py
â”‚   â”‚   â”‚   â”œâ”€â”€ FuzzyVariableRepository.py
â”‚   â”‚   â”‚   â”œâ”€â”€ FuzzyTermRepository.py
â”‚   â”‚   â”‚   â”œâ”€â”€ FuzzyRuleRepository.py
â”‚   â”‚   â”‚   â”œâ”€â”€ FuzzyRoutineRepository.py
â”‚   â”‚   â”‚   â””â”€â”€ FuzzyEvaluationRepository.py
â”‚   â”‚   â”œâ”€â”€ Configurations/
â”‚   â”‚   â””â”€â”€ Context/
â”‚   â”œâ”€â”€ ExternalServices/
â”‚   â”‚   â”œâ”€â”€ MqttService/
â”‚   â”‚   â”‚   â”œâ”€â”€ MqttClient.py
â”‚   â”‚   â”‚   â”œâ”€â”€ MqttSubscriber.py
â”‚   â”‚   â”‚   â””â”€â”€ MqttMessageHandler.py
â”‚   â”‚   â””â”€â”€ ActuatorService/
â”‚   â”‚       â”œâ”€â”€ ActuatorServiceClient.py
â”‚   â”‚       â”œâ”€â”€ ActuatorServiceModels.py
â”‚   â”‚       â””â”€â”€ ActuatorServiceConfiguration.py
â”‚   â””â”€â”€ FuzzyEngine/
â”œâ”€â”€ FuzzyService.Tests/
â”‚   â”œâ”€â”€ Unit/
â”‚   â”œâ”€â”€ Integration/
â”‚   â””â”€â”€ E2E/
â”œâ”€â”€ requirements.txt
â”œâ”€â”€ Dockerfile
â”œâ”€â”€ .env
â””â”€â”€ README.md
```
</details>

<details>
  <summary><h2>Endpoints del Fuzzy Service</h2></summary>

### Fuzzy Systems (Sistemas Difusos)
- âœ… `POST /api/fuzzy-systems` - Crear sistema difuso
- âœ… `GET /api/fuzzy-systems` - Obtener todos los sistemas difusos
- âœ… `GET /api/fuzzy-systems/{id}` - Obtener sistema difuso por ID
- âœ… `PUT /api/fuzzy-systems/{id}` - Actualizar sistema difuso
- âœ… `DELETE /api/fuzzy-systems/{id}` - Eliminar sistema difuso
- âœ… `PATCH /api/fuzzy-systems/{id}/status` - Cambiar estado (in_use/inactive)

### Fuzzy Variables (Variables del Sistema)
- âœ… `POST /api/fuzzy-variables` - Crear variable difusa
- âœ… `GET /api/fuzzy-variables` - Obtener todas las variables
- âœ… `GET /api/fuzzy-variables/system/{systemId}` - Obtener variables por sistema
- âœ… `GET /api/fuzzy-variables/{id}` - Obtener variable por ID
- âœ… `PUT /api/fuzzy-variables/{id}` - Actualizar variable
- âš ï¸ `DELETE /api/fuzzy-variables/{id}` - Eliminar variable (Falta validaciÃ³n: remover variable de sistema)
- `POST /api/fuzzy-variables/{id}/terms` - Agregar tÃ©rmino a variable (No implementado)
- `DELETE /api/fuzzy-variables/{id}/terms/{termId}` - Remover tÃ©rmino de variable (No implementado)

### Fuzzy Terms (TÃ©rminos LingÃ¼Ã­sticos)
- âœ… `POST /api/fuzzy-terms` - Crear tÃ©rmino lingÃ¼Ã­stico (Con validaciÃ³n bidireccional)
- âœ… `GET /api/fuzzy-terms/variable/{variableId}` - Obtener tÃ©rminos por variable
- âœ… `GET /api/fuzzy-terms/{id}` - Obtener tÃ©rmino por ID
- âœ… `PUT /api/fuzzy-terms/{id}` - Actualizar tÃ©rmino
- âœ… `DELETE /api/fuzzy-terms/{id}` - Eliminar tÃ©rmino (Con validaciÃ³n bidireccional)

### Fuzzy Rules (Reglas del Sistema)
- âœ… `POST /api/fuzzy-rules` - Crear regla difusa
- âœ… `GET /api/fuzzy-rules/system/{systemId}` - Obtener reglas por sistema
- âœ… `GET /api/fuzzy-rules/{id}` - Obtener regla por ID
- âœ… `PUT /api/fuzzy-rules/{id}` - Actualizar regla completa
- âœ… `DELETE /api/fuzzy-rules/{id}` - Eliminar regla
- âœ… `PUT /api/fuzzy-rules/{id}/status` - Activar/desactivar regla
- `PUT /api/fuzzy-rules/{id}/conditions` - Actualizar condiciones de la regla (No implementado)
- `PUT /api/fuzzy-rules/{id}/connectors` - Actualizar conectores de la regla (No implementado)
- `PUT /api/fuzzy-rules/{id}/consequent` - Actualizar consecuente de la regla (No implementado)
- `POST /api/fuzzy-rules/{id}/conditions` - Agregar nueva condiciÃ³n (No implementado)
- `DELETE /api/fuzzy-rules/{id}/conditions/{conditionId}` - Eliminar condiciÃ³n especÃ­fica (No implementado)

### Fuzzy Routines (Rutinas de ActuaciÃ³n)
- âœ… `POST /api/fuzzy-routines` - Crear rutina
- âœ… `GET /api/fuzzy-routines` - Obtener todas las rutinas
- âœ… `GET /api/fuzzy-routines/{id}` - Obtener rutina por ID
- âœ… `PUT /api/fuzzy-routines/{id}` - Actualizar rutina completa
- âœ… `DELETE /api/fuzzy-routines/{id}` - Eliminar rutina
- âœ… `POST /api/fuzzy-routines/{id}/steps` - Agregar nuevo paso a la rutina
- âœ… `PUT /api/fuzzy-routines/{id}/steps/{stepId}` - Actualizar paso especÃ­fico
- âœ… `DELETE /api/fuzzy-routines/{id}/steps/{stepId}` - Eliminar paso especÃ­fico

### Fuzzy Evaluations (Evaluaciones y CÃ¡lculos)
- **Nota**: Las evaluaciones fuzzy se ejecutan automÃ¡ticamente cuando se reciben datos del sensor-service vÃ­a MQTT, no mediante endpoint POST
- `GET /api/fuzzy-evaluations` - Obtener historial de evaluaciones (No implementado)
- `GET /api/fuzzy-evaluations/{id}` - Obtener evaluaciÃ³n especÃ­fica (No implementado)
- `GET /api/fuzzy-evaluations/system/{systemId}` - Obtener evaluaciones por sistema (No implementado)

### Actuator Integration (IntegraciÃ³n con Actuadores)
- `POST /api/actuator-integration/send-routines` - Enviar rutinas al actuator-service (No implementado)
- `GET /api/actuator-integration/status` - Estado de comunicaciÃ³n con actuator-service (No implementado)
</details>

<details>
  <summary><h2>Estructuras JSON</h2></summary>

### Colecciones en MongoDB

#### FuzzySystem (Sistema Difuso)
```json
{
  "_id": { "$oid": "fuzzy_1" },
  "name": "Vegetativo DÃ­a",
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
  "device_id": { "$oid": "5783qhgg445tu" },
  "termIds": [
    { "$oid": "term_temp_low" }, 
    { "$oid": "term_temp_ok" }, 
    { "$oid": "term_temp_high" }
  ],
  "createdAt": "2025-01-27T23:39:17.917+00:00"
}
```

#### FuzzyTerm (Etiquetas/TÃ©rminos LingÃ¼Ã­sticos)
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
  "description": "Si EC es baja, activar recirculaciÃ³n de agua",
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

#### FuzzyRoutine (Rutinas de ActuaciÃ³n)
```json
{
  "routineId":  { "$oid": "11118bb77b05e378b3731341" },
  "routine_name": "Calentar Invernadero",
  "steps": [
    {
      "stepId": "step_001",
      "actuator": { "$oid": "68ab8bb77b05e378b3731341" },
      "power_term_id": { "$oid": "68ab8b887b05e378b3732332" },
      "duration_term_id": { "$oid": "68ab8b887b05e378b3731344" }
    },
    {
      "stepId": "step_002",
      "actuator": { "$oid": "68ab8b887b05e378b3731341" },
      "power_term_id": { "$oid": "68ab8b337b05e378b3731341" },
      "duration_term_id": { "$oid": "68aaab887b05e378b3731341" }
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
  "inputs": 
    {
      "sensor_id_uuid_1": 6.5,
      "sensor_id_uuid_2": 1.2,
      "sensor_id_uuid_3": 28.3,
      "sensor_id_uuid_4": 55.2,
      "sensor_id_uuid_5": 11000,
      "sensor_id_uuid_6": 12.5,
      "sensor_id_uuid_7": 22.0,
    },
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

### Payload: Fuzzy Service â†’ Actuator Service

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
  <summary><h2>Estrategia de ImplementaciÃ³n Actual</h2></summary>

## ðŸŽ¯ Enfoque ArquitectÃ³nico HÃ­brido

El FuzzyService implementa una **arquitectura hÃ­brida** que combina los principios de Clean Architecture con pragmatismo tÃ©cnico para lograr un sistema robusto y mantenible.

### ðŸ—ï¸ Capas ArquitectÃ³nicas

#### **Domain Layer (NÃºcleo del Negocio)**
- **Entidades**: FuzzySystem, FuzzyVariable, FuzzyTerm, FuzzyRule, FuzzyRoutine, FuzzyEvaluation
- **Value Objects**: DomainId, RuleCondition, MembershipFunction, InputValue
- **Interfaces**: IFuzzyEngine, IFuzzySystemRepository, IFuzzyVariableRepository, etc.
- **Enums**: FuzzySystemStatus, DefuzzificationMethod, AggregationMethod

#### **Application Layer (OrquestaciÃ³n)**
- **Services**: FuzzyEngineService (implementa IFuzzyEngine)
- **Mappers**: DomainToInfrastructureMapper, InfrastructureToDomainMapper
- **Configuration**: DependencyInjection, ImprovedDependencyInjection
- **Features**: Commands y Queries por entidad (CQRS pattern)

#### **Infrastructure Layer (Detalles TÃ©cnicos)**
- **Persistence**: Repositorios MongoDB especializados
- **FuzzyEngine**: ScikitFuzzyEngine con pipeline completo
- **ExternalServices**: MQTT, HTTP clients
- **Configuration**: DatabaseConfiguration, MqttConfiguration

#### **API Layer (Interfaz Externa)**
- **Controllers**: REST endpoints por entidad
- **Middleware**: ErrorHandling, CORS, Authentication
- **Configuration**: FastAPI setup y routing

### ðŸ”„ Flujo de Datos Implementado

```
API Request â†’ Controller â†’ Command/Query â†’ Application Service â†’ Domain Repository â†’ Infrastructure Repository â†’ MongoDB
                                     â†“
API Response â† DTO â† Domain Entity â† Mapper â† Infrastructure Entity â† Database Document
```

### âš¡ Motor Fuzzy - Pipeline de EvaluaciÃ³n

1. **Entrada**: Datos de sensores vÃ­a API o MQTT
2. **Carga de ConfiguraciÃ³n**: Repositorios cargan sistema, variables, tÃ©rminos y reglas
3. **ConversiÃ³n**: Mappers convierten entidades Domain â†’ Infrastructure
4. **FuzzificaciÃ³n**: Valores numÃ©ricos â†’ grados de membresÃ­a
5. **EvaluaciÃ³n de Reglas**: CÃ¡lculo de firing strength
6. **AgregaciÃ³n**: CombinaciÃ³n de outputs fuzzy
7. **DefuzzificaciÃ³n**: Resultado fuzzy â†’ valores numÃ©ricos
8. **Respuesta**: ConversiÃ³n Infrastructure â†’ Domain â†’ DTO

### ðŸŽ¯ Principios de DiseÃ±o Aplicados

- **Clean Architecture**: SeparaciÃ³n clara de responsabilidades
- **SOLID**: Single Responsibility, Open/Closed, Dependency Inversion
- **DRY**: Mappers centralizados eliminan duplicaciÃ³n
- **CQRS**: SeparaciÃ³n de comandos y consultas
- **Repository Pattern**: AbstracciÃ³n de persistencia
- **Dependency Injection**: InversiÃ³n de control con lifecycle management
  - FuzzyRuleRepository âœ…
  - FuzzyRoutineRepository âœ…
  - FuzzyEvaluationRepository âœ…
- Ãndices y validaciones:
  - Ãndices en Systems/Variables/Terms creados y togglables por FUZZY_ENSURE_INDEXES_ON_STARTUP âœ…
  - Validaciones referenciales bÃ¡sicas (Variableâ†’System, Termâ†’Variable) âœ…
- DI de repositorios y ensure_indexes en startup configurable âœ…

#### Resumen de Dependency Injection (DI)
- Problemas detectados: instanciaciÃ³n directa de handlers/servicios, wiring monolÃ­tico, lifecycle ambiguo y difÃ­cil de testear, cargas perezosas inconsistentes.
- Soluciones aplicadas: Medyator + contenedor DI (kink), factories y convenciones para wiring, lifecycle claro (singleton/scoped/transient) por dependencia, ensure_indexes activable por variable de entorno, health checks y mÃ©tricas en roadmap.
- Estado actual: repositorios principales cableados por DI (FuzzyRule, FuzzyRoutine, FuzzyEvaluation) y tests de DI pasando; configuraciÃ³n centralizada y fÃ¡cilmente extensible.
- PrÃ³ximos pasos: aÃ±adir circuit breakers y reintentos para servicios externos (httpx), mÃ©tricas de latencia/errores, overrides de contenedor para pruebas de integraciÃ³n y E2E.
- Referencia tÃ©cnica ampliada: ver <mcfile name="DEPENDENCY_INJECTION_ANALYSIS.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\DEPENDENCY_INJECTION_ANALYSIS.md"></mcfile>.

### Fase 4: Application Layer - NÃºcleo (Mediator, DTOs, Mappings) âœ…
1. Configurar Mediator (Medyator) y Dependency Injection âœ…
2. Implementar DTOs y mappings Pydantic para entidades âœ…
3. Implementar helpers de validaciÃ³n y utilidades comunes âœ…

### Fase 5: Vertical Slice - Sistemas Difusos (API â†’ Mongo end-to-end) âœ…
1. Commands y Queries de FuzzySystems âœ…
2. Handlers CRUD y validaciÃ³n âœ…
3. Controlador API de FuzzySystems y wiring âœ…
4. Pruebas de integraciÃ³n bÃ¡sicas APIâ†’Mongo para sistemas âœ…

### Fase 6: Vertical Slice - Variables y TÃ©rminos (API â†’ Mongo end-to-end) âœ…
1. Commands y Queries para FuzzyVariables âœ…
2. Commands y Queries para FuzzyTerms âœ…
3. Controladores y handlers âœ…
4. ValidaciÃ³n de funciones de membresÃ­a âœ…
5. Pruebas de integraciÃ³n APIâ†’Mongo para variables y tÃ©rminos âœ…
6. ValidaciÃ³n bidireccional Termâ†”Variable âœ…

### Fase 7: Vertical Slice - Reglas y Rutinas (API â†’ Mongo end-to-end) âœ…
1. Commands y Queries para FuzzyRules âœ…
2. Commands y Queries para FuzzyRoutines âœ…
3. Validaciones de condiciones y consecuentes âœ…
4. Controladores y handlers âœ…
5. Pruebas de integraciÃ³n APIâ†’Mongo para reglas y rutinas âœ…

### Fase 8: API Layer - Middleware, AutenticaciÃ³n y Observabilidad â³
1. Middleware de manejo de errores y logging âœ…
2. Configurar CORS y documentaciÃ³n Swagger â³
3. Observabilidad bÃ¡sica (logs) y respuestas unificadas âœ…

### Fase 9: IntegraciÃ³n MQTT (solo escucha)
1. Configurar cliente MQTT asÃ­ncrono (suscripciÃ³n)
2. Suscribirse a tÃ³picos de sensores y parseo de mensajes
3. Persistir entradas crudas o normalizadas si aplica
4. Asegurar resiliencia (reconexiÃ³n, QoS necesario)
Nota: Por ahora no se publicarÃ¡ a MQTT, solo suscripciÃ³n/escucha.

### Fase 10: Motor de EvaluaciÃ³n Difusa
1. Integrar scikit-fuzzy para cÃ¡lculos difusos
2. Implementar algoritmo de defuzzificaciÃ³n (centroid, bisector, mom, som, lom)
3. EvaluaciÃ³n de reglas con firing strength y agregaciÃ³n
4. Persistir evaluaciones y outputs
5. Exponer Queries de historial de evaluaciones

### Fase 11: IntegraciÃ³n con Actuadores
1. Implementar Commands para ActuatorIntegration
2. Crear servicio de comunicaciÃ³n HTTP con actuator-service
3. Implementar transformaciÃ³n de rutinas a payload de actuadores
4. Implementar manejo de respuestas y estados

### Fase 12: Testing y CI/CD
1. Implementar tests unitarios y de integraciÃ³n para las capas crÃ­ticas
2. Crear tests E2E mÃ­nimos del flujo completo
3. Generar documentaciÃ³n de API con OpenAPI/Swagger
4. Configurar CI/CD y deployment

## ðŸ“‹ Lista de Tareas Pendientes

### ðŸ”§ Validaciones y Mejoras de Endpoints Existentes

#### Variables
- âš ï¸ **Implementar validaciÃ³n bidireccional Variableâ†”System en DELETE**: Cuando se elimina una variable, removerla de la lista de variables del sistema
- ðŸ“ **Implementar endpoints de gestiÃ³n de tÃ©rminos en variables**:
  - `POST /api/fuzzy-variables/{id}/terms` - Agregar tÃ©rmino a variable
  - `DELETE /api/fuzzy-variables/{id}/terms/{termId}` - Remover tÃ©rmino de variable

#### Reglas - Endpoints Granulares
- ðŸ“ **Implementar endpoints de gestiÃ³n granular de reglas**:
  - `PUT /api/fuzzy-rules/{id}/conditions` - Actualizar condiciones de la regla
  - `PUT /api/fuzzy-rules/{id}/connectors` - Actualizar conectores de la regla
  - `PUT /api/fuzzy-rules/{id}/consequent` - Actualizar consecuente de la regla
  - `POST /api/fuzzy-rules/{id}/conditions` - Agregar nueva condiciÃ³n
  - `DELETE /api/fuzzy-rules/{id}/conditions/{conditionId}` - Eliminar condiciÃ³n especÃ­fica

### ðŸš€ Nuevas Funcionalidades

#### Evaluaciones Fuzzy
- ðŸ“ **Implementar endpoints de evaluaciones**:
  - `GET /api/fuzzy-evaluations` - Obtener historial de evaluaciones
  - `GET /api/fuzzy-evaluations/{id}` - Obtener evaluaciÃ³n especÃ­fica
  - `GET /api/fuzzy-evaluations/system/{systemId}` - Obtener evaluaciones por sistema

#### IntegraciÃ³n con Actuadores
- ðŸ“ **Implementar endpoints de integraciÃ³n con actuadores**:
  - `POST /api/actuator-integration/send-routines` - Enviar rutinas al actuator-service
  - `GET /api/actuator-integration/status` - Estado de comunicaciÃ³n con actuator-service

#### Motor de EvaluaciÃ³n Fuzzy (Fase 10)
- ðŸ”¬ **Integrar scikit-fuzzy para cÃ¡lculos difusos**
- ðŸ§® **Implementar algoritmo de defuzzificaciÃ³n** (centroid, bisector, mom, som, lom)
- ðŸ“Š **EvaluaciÃ³n de reglas con firing strength y agregaciÃ³n**
- ðŸ’¾ **Persistir evaluaciones y outputs**
- ðŸ“ˆ **Exponer Queries de historial de evaluaciones**

#### IntegraciÃ³n MQTT (Fase 9)
- ðŸ“¡ **Configurar cliente MQTT asÃ­ncrono** (suscripciÃ³n)
- ðŸ“¨ **Suscribirse a tÃ³picos de sensores y parseo de mensajes**
- ðŸ’¾ **Persistir entradas crudas o normalizadas si aplica**
- ðŸ”„ **Asegurar resiliencia** (reconexiÃ³n, QoS necesario)

### ðŸ› ï¸ ConfiguraciÃ³n y DocumentaciÃ³n

#### API y DocumentaciÃ³n
- â³ **Completar configuraciÃ³n CORS**
- ðŸ“š **Asegurar documentaciÃ³n Swagger completa** con todos los endpoints
- ðŸ” **Revisar y completar esquemas OpenAPI**

#### Testing y Calidad
- ðŸ§ª **Implementar tests unitarios** para las capas crÃ­ticas
- ðŸ”„ **Crear tests E2E** mÃ­nimos del flujo completo
- ðŸ“‹ **Tests de integraciÃ³n** para nuevas funcionalidades

#### DevOps y Deployment
- ðŸ³ **Configurar CI/CD** y deployment
- ðŸ“¦ **Optimizar Dockerfile** y configuraciÃ³n de contenedores
- ðŸ”§ **Configurar variables de entorno** para diferentes ambientes

### ðŸŽ¯ Prioridades Recomendadas

1. **Alta Prioridad**:
   - ValidaciÃ³n bidireccional Variableâ†”System en DELETE
   - Completar configuraciÃ³n CORS y Swagger
   - Endpoints de evaluaciones fuzzy

2. **Media Prioridad**:
   - Endpoints granulares de reglas
   - Endpoints de gestiÃ³n de tÃ©rminos en variables
   - Motor de evaluaciÃ³n fuzzy bÃ¡sico

3. **Baja Prioridad**:
   - IntegraciÃ³n MQTT
   - IntegraciÃ³n con actuadores
   - CI/CD y deployment avanzado
</details>

## ðŸ§  LÃ³gica Fuzzy Core - Flujo de EvaluaciÃ³n

### ðŸ“‹ Flujo Propuesto de EvaluaciÃ³n en Tiempo Real

#### **1. RecepciÃ³n de Datos MQTT**
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
- **FuzzyEvaluationService**: Orquestar evaluaciÃ³n completa
- **Filtrado de Sistema**: Identificar sistema activo (status="in_use")

#### **2. FuzzificaciÃ³n de Variables de Entrada**
```python
# Ejemplo: Temperatura = 25.5Â°C
# Variable: "temperatura" con tÃ©rminos [bajo, medio, alto]
terms_membership = {
    "bajo": 0.0,    # trimf([15, 18, 22]) â†’ 0.0 para 25.5
    "medio": 0.7,   # trimf([20, 25, 30]) â†’ 0.7 para 25.5  
    "alto": 0.3     # trimf([25, 30, 35]) â†’ 0.3 para 25.5
}
```

**Proceso:**
1. **Mapeo Sensorâ†’Variable**: Buscar variables del sistema activo por `device_id`
2. **Carga de TÃ©rminos**: Obtener tÃ©rminos de cada variable con sus funciones de membresÃ­a
3. **ConversiÃ³n a scikit-fuzzy**: Convertir `MembershipFunction` a funciones numpy
4. **CÃ¡lculo de MembresÃ­a**: Evaluar grado de pertenencia para cada tÃ©rmino

**Mejoras TÃ©cnicas:**
- âœ… **Cache de Funciones**: Precalcular funciones de membresÃ­a en memoria
- âœ… **ValidaciÃ³n de Rangos**: Verificar que valores estÃ©n dentro del universo de discurso
- âœ… **Manejo de Sensores Faltantes**: Estrategia para datos incompletos

#### **3. EvaluaciÃ³n de Reglas Activas**
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
2. **EvaluaciÃ³n de Antecedentes**: Para cada regla, evaluar condiciones
3. **AplicaciÃ³n de Conectores**: Usar operadores lÃ³gicos (ANDâ†’min, ORâ†’max, NOTâ†’complement)
4. **CÃ¡lculo de Firing Strength**: Determinar fuerza de activaciÃ³n de cada regla

**Operadores Configurables:**
```python
# ConfiguraciÃ³n en FuzzySystem.operators
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
            "power_term_id": "potencia_media",    # TÃ©rmino fuzzy
            "duration_term_id": "duracion_corta"  # TÃ©rmino fuzzy
        }
    ]
}
```

**Proceso:**
1. **IdentificaciÃ³n de Rutinas**: Mapear consecuentes a rutinas en BD
2. **Carga de Pasos**: Obtener steps con actuator_id y term_ids
3. **PreparaciÃ³n para DefuzzificaciÃ³n**: Agrupar tÃ©rminos de salida por variable (power/duration) para procesar cada paso de cada rutina de manera independiente

#### **5. DefuzzificaciÃ³n de TÃ©rminos de Salida**
```python
# DefuzzificaciÃ³n de "potencia_media" con firing_strength = 0.7
power_universe = np.linspace(0, 100, 101)  # 0-100% PWM
power_mf = trapmf(power_universe, [30, 50, 70, 90])  # FunciÃ³n trapezoidal

# Aplicar firing strength (recorte)
clipped_mf = np.minimum(power_mf, 0.7)

# DefuzzificaciÃ³n (centroid)
power_crisp = defuzz(power_universe, clipped_mf, 'centroid')  # â‰ˆ 60%
```

**MÃ©todos Soportados:**
- **CENTROID**: Centro de gravedad (recomendado)
- **BISECTOR**: Bisector del Ã¡rea
- **MOM/SOM/LOM**: MÃ©todos de mÃ¡ximo

**Casos Especiales:**
- **TÃ©rminos ON/OFF**: No requieren defuzzificaciÃ³n (valores booleanos)
- **MÃºltiples Reglas**: Cuando varias reglas activan el mismo tÃ©rmino de salida, se usa agregaciÃ³n MAX de los grados de pertenencia (firing strengths) antes de defuzzificar

#### **6. ConstrucciÃ³n del Payload para Actuator-Service**
```python
# Payload final - MÃºltiples rutinas activadas
payload = [
    {
        "routineId": "rutina_riego",
        "steps": [
            {
                "actuator": {"$oid": "pump_001"},
                "power": 60,  # DefuzzificaciÃ³n independiente para este paso
                "duration": 45  # DefuzzificaciÃ³n independiente para este paso
            }
        ]
    },
    {
        "routineId": "rutina_ventilacion",
        "steps": [
            {
                "actuator": {"$oid": "fan_001"},
                "power": 80,  # DefuzzificaciÃ³n independiente
                "duration": 30  # DefuzzificaciÃ³n independiente
            }
        ]
    }
]
```

**CaracterÃ­sticas Importantes:**
- âœ… **MÃºltiples Rutinas**: Cada regla activada puede generar una rutina independiente
- âœ… **DefuzzificaciÃ³n por Paso**: Cada step tiene su propia defuzzificaciÃ³n de power y duration
- âœ… **Sin Expiration**: El actuator-service calcula la expiraciÃ³n para optimizar tiempo de ejecuciÃ³n
- âœ… **ValidaciÃ³n de Rangos**: Asegurar power (0-100) y duration (5-60) dentro de lÃ­mites
- âœ… **AgregaciÃ³n MAX**: Solo aplica cuando mÃºltiples reglas activan el mismo tÃ©rmino de la misma variable de salida

#### **7. Persistencia de EvaluaciÃ³n**
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

### ðŸ”§ Consideraciones TÃ©cnicas y Mejoras

#### **Optimizaciones de Rendimiento**
1. **Cache de ConfiguraciÃ³n**: Mantener sistema activo y sus componentes en memoria
2. **PrecÃ¡lculo de Funciones**: Generar arrays numpy de funciones de membresÃ­a al startup
3. **EvaluaciÃ³n Paralela**: Procesar mÃºltiples reglas concurrentemente
4. **Batch Processing**: Agrupar evaluaciones si llegan mÃºltiples lecturas

#### **Manejo de Errores**
1. **Sensores Faltantes**: Continuar evaluaciÃ³n con sensores disponibles
2. **Reglas InvÃ¡lidas**: Log y skip reglas con condiciones no satisfacibles
3. **Actuator-Service Down**: Queue rutinas para reintento
4. **Valores Fuera de Rango**: Clamp a lÃ­mites del universo de discurso

#### **ConfiguraciÃ³n Flexible**
```python
# Variables de entorno
FUZZY_EVALUATION_CYCLE_MS=1000        # Ciclo de evaluaciÃ³n
FUZZY_CACHE_SYSTEM_CONFIG=true        # Cache de configuraciÃ³n
FUZZY_PARALLEL_RULE_EVALUATION=true   # EvaluaciÃ³n paralela
FUZZY_MAX_CONCURRENT_RULES=10         # LÃ­mite de concurrencia
FUZZY_ACTUATOR_TIMEOUT_MS=5000        # Timeout para actuator-service
```

#### **Monitoreo y Observabilidad**
1. **MÃ©tricas**: Tiempo de evaluaciÃ³n, reglas activadas, errores
2. **Logs Estructurados**: JSON logs con contexto de evaluaciÃ³n
3. **Health Checks**: Estado de MQTT, MongoDB, Actuator-Service
4. **Alertas**: Notificaciones por fallos crÃ­ticos

### ðŸš€ ImplementaciÃ³n Propuesta

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

#### **Flujo de IntegraciÃ³n**
1. **MQTT Handler** â†’ `FuzzyEngine.evaluate()`
2. **FuzzyEngine** â†’ Orquesta servicios especializados
3. **Resultado** â†’ `ActuatorServiceClient.send_routines()`
4. **ConfirmaciÃ³n** â†’ `FuzzyEvaluationRepository.save()`

Este diseÃ±o asegura **separaciÃ³n de responsabilidades**, **testabilidad** y **escalabilidad** del motor fuzzy.

<details>
  <summary><h2>ConfiguraciÃ³n del Entorno de Desarrollo</h2></summary>

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

1. **Mediator Pattern**: Utilizaremos la librerÃ­a `medyator` que es un port directo de MediatR de .NET <mcreference link="https://pypi.org/project/Medyator/" index="1">1</mcreference>
2. **Object Mapping**: `Pydantic` proporciona funcionalidad nativa para el mapeo entre DTOs y entidades
3. **Vertical Slicing**: Cada feature tendrÃ¡ su propia carpeta con Commands, Queries, DTOs y Mappings
4. **Clean Architecture**: Mantendremos separaciÃ³n clara entre capas Domain, Application, Infrastructure y API
5. **Compatibilidad**: Todas las librerÃ­as son compatibles con Python 3.12.11

Este plan asegura una implementaciÃ³n gradual y sistemÃ¡tica del Fuzzy Service, manteniendo la consistencia arquitectÃ³nica con el Auth Service existente.
</details>

