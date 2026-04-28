# Casos de Prueba Completos — HydroEspinaca v2

> **Propósito:** Documento exhaustivo de casos de prueba para garantizar la cobertura total del sistema en el libro de proyecto. Cubre pruebas automatizadas (unitarias e integración), pruebas funcionales de usuario, pruebas de seguridad, pruebas de rendimiento y casos borde. Incluye tanto las funcionalidades de v1 como las 7 nuevas funcionalidades de v2.

---

## Índice

1. [Convenciones y Taxonomía](#1-convenciones-y-taxonomía)
2. [Estrategia de Pruebas Automatizadas (SonarQube)](#2-estrategia-de-pruebas-automatizadas-sonarqube)
3. [Módulo: Autenticación y Sesiones](#3-módulo-autenticación-y-sesiones)
4. [Módulo: Gestión de Usuarios y Roles](#4-módulo-gestión-de-usuarios-y-roles)
5. [Módulo: Sensores e IoT (Lecturas MQTT)](#5-módulo-sensores-e-iot-lecturas-mqtt)
6. [Módulo: Alertas de Sensores y ESP32](#6-módulo-alertas-de-sensores-y-esp32)
7. [Módulo: Agregados y Worker de Datos](#7-módulo-agregados-y-worker-de-datos)
8. [Módulo: Lógica Difusa Mamdani (v1 + CRUD v2)](#8-módulo-lógica-difusa-mamdani-v1--crud-v2)
9. [Módulo: BI — Consumo y Rentabilidad (v2)](#9-módulo-bi--consumo-y-rentabilidad-v2)
10. [Módulo: Servicio Meteorológico y Alertas Proactivas (v2)](#10-módulo-servicio-meteorológico-y-alertas-proactivas-v2)
11. [Módulo: Notificaciones (WhatsApp, Push, Email) (v2)](#11-módulo-notificaciones-whatsapp-push-email-v2)
12. [Módulo: Chatbot RAG (v2)](#12-módulo-chatbot-rag-v2)
13. [Módulo: Vistas Web (v2)](#13-módulo-vistas-web-v2)
14. [Módulo: Vistas Móviles (v2)](#14-módulo-vistas-móviles-v2)
15. [Pruebas de Seguridad Transversales](#15-pruebas-de-seguridad-transversales)
16. [Pruebas de Integración End-to-End](#16-pruebas-de-integración-end-to-end)
17. [Pruebas de Rendimiento y Carga](#17-pruebas-de-rendimiento-y-carga)
18. [Casos Borde y Escenarios de Error Globales](#18-casos-borde-y-escenarios-de-error-globales)
19. [Checklist de Métricas SonarQube por Servicio](#19-checklist-de-métricas-sonarqube-por-servicio)

---

## 1. Convenciones y Taxonomía

### 1.1 Identificador de Caso de Prueba

```
[MÓDULO]-[TIPO]-[NÚMERO]
```

| Campo   | Valores posibles                                         |
|---------|----------------------------------------------------------|
| MÓDULO  | AUTH, USR, SNS, ALT, AGG, FUZ, BI, WEA, NOT, RAG, WEB, MOB, SEC, INT, PERF |
| TIPO    | U (Unitaria), I (Integración), F (Funcional/usuario), S (Seguridad), P (Rendimiento), E (Edge/Borde) |
| NÚMERO  | 001, 002, …                                              |

### 1.2 Niveles de Prioridad

| Nivel | Descripción                                              |
|-------|----------------------------------------------------------|
| P1    | Crítico — bloquea el flujo principal del sistema         |
| P2    | Alto — funcionalidad importante pero con workaround      |
| P3    | Medio — mejora de experiencia o caso poco frecuente      |
| P4    | Bajo — cosmético o muy edge                              |

### 1.3 Estado esperado de cobertura automatizada

- **Dominio (Domain):** ≥ 95 % cobertura de líneas
- **Aplicación (Application/UseCases):** ≥ 90 % cobertura de líneas
- **Infraestructura:** no se prueba con mocks; se prueba con integración real
- **SonarQube Quality Gate:** 0 bugs, 0 vulnerabilidades, code smells < 5 % de deuda técnica, duplicaciones < 3 %

---

## 2. Estrategia de Pruebas Automatizadas (SonarQube)

### 2.1 Capas sujetas a prueba unitaria

Solo las capas **Domain** y **Application** de cada microservicio .NET se cubren con pruebas unitarias (xUnit + Moq). La capa Infrastructure se prueba mediante pruebas de integración contra una instancia MongoDB de test real o un contenedor Docker.

### 2.2 Estructura de proyecto de pruebas recomendada

```
SensorService.Tests/
  Unit/
    Domain/
      Entities/
      ValueObjects/
      Services/
    Application/
      UseCases/
  Integration/
    Repositories/
    Workers/
```

### 2.3 Convenciones de nombre de método de prueba

```
[MetodoOClase]_[Escenario]_[ResultadoEsperado]
```

Ejemplo: `CreateAggregate_WithValidData_SetsCorrectTimestamp`

### 2.4 Configuración de SonarQube

```bash
# Ejecutar análisis completo (CI)
dotnet sonarscanner begin \
  /k:"hydroespinaca-sensor-service" \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
  /d:sonar.coverage.exclusions="**/*Document.cs,**/*Mapper.cs,**/*DI*.cs,**/Program.cs"

dotnet build
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

dotnet sonarscanner end
```

Los archivos excluidos de cobertura son documentos de infraestructura sin lógica de negocio.

---

## 3. Módulo: Autenticación y Sesiones

### AUTH-U-001 — Login con credenciales válidas genera tokens RSA
**Prioridad:** P1 | **Tipo:** Unitaria (Application layer)

**Precondiciones:**
- Usuario existente en MongoDB con hash bcrypt válido
- Par de claves RSA (user-private.pem / user-public.pem) presentes

**Pasos (prueba unitaria):**
1. Mockear `IUserRepository.GetByEmailAsync("admin@test.com")` → devuelve usuario con hash correcto
2. Mockear `ISessionRepository.CreateAsync()` → OK
3. Llamar `LoginUseCase.ExecuteAsync(new LoginRequest { Email = "admin@test.com", Password = "ValidPass123!" })`

**Resultado esperado:**
- Devuelve `LoginResult` con `AccessToken` (JWT firmado RS256), `RefreshToken` (UUID), y `ExpiresIn`
- El AccessToken decodificado contiene claims `sub`, `role`, `sessionId`
- `ISessionRepository.CreateAsync` fue invocado exactamente 1 vez

**Criterios de aceptación SonarQube:** 0 code smells en LoginUseCase, cobertura de rama ≥ 90 %

---

### AUTH-U-002 — Login con contraseña incorrecta lanza excepción de dominio
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear usuario con hash bcrypt de "OtraContraseña"
2. Llamar `LoginUseCase.ExecuteAsync` con `Password = "Incorrecta"`

**Resultado esperado:**
- Se lanza `UnauthorizedException` (o dominio equivalente)
- `ISessionRepository.CreateAsync` NO fue invocado
- El mensaje de error NO revela si el usuario existe (mensaje genérico)

---

### AUTH-U-003 — Login con email no registrado lanza excepción genérica
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `IUserRepository.GetByEmailAsync` → devuelve `null`
2. Llamar `LoginUseCase.ExecuteAsync`

**Resultado esperado:**
- Se lanza excepción de credenciales inválidas (misma excepción que AUTH-U-002 para no revelar enumeración)

---

### AUTH-U-004 — RefreshToken válido genera nuevo AccessToken
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear sesión activa con `RefreshToken = "uuid-valido"` y `ExpiresAt > DateTime.UtcNow`
2. Llamar `RefreshTokenUseCase.ExecuteAsync("uuid-valido")`

**Resultado esperado:**
- Devuelve nuevo AccessToken
- `UpdateAsync` de sesión fue invocado (actualiza `LastActivity`)

---

### AUTH-U-005 — RefreshToken expirado lanza excepción
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear sesión con `ExpiresAt = DateTime.UtcNow.AddHours(-1)`

**Resultado esperado:** Se lanza excepción de token expirado

---

### AUTH-U-006 — Logout invalida sesión activa
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear sesión activa
2. Llamar `LogoutUseCase.ExecuteAsync(sessionId)`

**Resultado esperado:**
- `ISessionRepository.RevokeAsync(sessionId)` invocado 1 vez
- Sesión queda con `RevokedAt` establecido

---

### AUTH-F-001 — Login exitoso desde interfaz web
**Prioridad:** P1 | **Tipo:** Funcional

**Precondiciones:** Servidor web corriendo; usuario `admin@test.com` registrado

**Pasos:**
1. Navegar a `/login`
2. Ingresar email y contraseña correctos
3. Clic en "Iniciar sesión"

**Resultado esperado:**
- Redirección a dashboard principal (`/dashboard`)
- Cookie o localStorage contiene AccessToken válido
- Navegación lateral muestra nombre y rol del usuario

---

### AUTH-F-002 — Sesión expira y redirige a login
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Iniciar sesión correctamente
2. Esperar a que expire el AccessToken (o manipularlo en localStorage)
3. Intentar navegar a cualquier página protegida

**Resultado esperado:**
- Si el RefreshToken es válido: se renueva automáticamente (silent refresh), el usuario no percibe interrupción
- Si el RefreshToken también expiró: redirección a `/login` con mensaje "Sesión expirada"

---

### AUTH-F-003 — Límite de sesiones concurrentes por usuario
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Iniciar sesión desde navegador A
2. Iniciar sesión desde navegador B con el mismo usuario
3. Si existe límite de N sesiones, verificar qué ocurre

**Resultado esperado (según configuración del sistema):**
- O bien se rechaza el nuevo login con mensaje claro
- O bien se invalida la sesión más antigua
- La sesión desplazada recibe 401 en su próxima solicitud

---

### AUTH-E-001 — Inyección SQL/NoSQL en campo email
**Prioridad:** P1 | **Tipo:** Edge/Seguridad

**Pasos:**
1. En el campo email enviar: `" OR 1=1 --`, `{"$gt": ""}`, `admin@test.com\x00malicious`

**Resultado esperado:**
- Todas devuelven 400 Bad Request o 401 Unauthorized
- Ninguna consulta bypass de autenticación
- Logs no revelan detalles de error interno

---

## 4. Módulo: Gestión de Usuarios y Roles

### USR-U-001 — Crear usuario con rol Administrador asigna permisos correctos
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Llamar `CreateUserUseCase.ExecuteAsync` con datos válidos y `Role = "Administrator"`
2. Verificar que la entidad creada tiene `Role = Administrator`
3. Verificar que la contraseña fue hasheada con bcrypt (no almacenada en plano)

**Resultado esperado:** Hash no igual a contraseña original; entidad `User` tiene `Role.Administrator`

---

### USR-U-002 — Actualización de rol de usuario modifica permisos sin afectar sesiones activas
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Usuario con rol `Operator` tiene sesión activa
2. Admin llama `UpdateUserRoleUseCase.ExecuteAsync(userId, "Viewer")`

**Resultado esperado:**
- `IUserRepository.UpdateAsync` invocado con rol nuevo
- Las sesiones activas del usuario son invalidadas (o se fuerza reautenticación en próximo request)

---

### USR-F-001 — Admin crea nuevo usuario desde panel web
**Prioridad:** P1 | **Tipo:** Funcional

**Precondiciones:** Sesión de administrador activa

**Pasos:**
1. Ir a `/admin/users/create`
2. Completar formulario: nombre, email, contraseña, rol
3. Guardar

**Resultado esperado:**
- Usuario aparece en listado con estado "Activo"
- El nuevo usuario puede iniciar sesión con las credenciales definidas
- Email de bienvenida enviado (si la funcionalidad está habilitada)

---

### USR-F-002 — Operador no puede acceder a panel de administración de usuarios
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Iniciar sesión como usuario con rol `Operator`
2. Intentar navegar a `/admin/users`

**Resultado esperado:** Redirección a página de acceso denegado (403) o ruta protegida oculta en navegación

---

### USR-E-001 — Crear usuario con email duplicado
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Crear usuario con `email = "test@test.com"`
2. Intentar crear otro usuario con el mismo email

**Resultado esperado:** Error 409 Conflict con mensaje "El email ya está registrado"

---

### USR-E-002 — Contraseña no cumple política mínima
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:** Intentar crear usuario con contraseñas: `"123"`, `"password"`, `"      "`, `"a"` (1 carácter)

**Resultado esperado:** 400 Bad Request con mensaje indicando los requisitos mínimos (longitud, mayúsculas, símbolos)

---

## 5. Módulo: Sensores e IoT (Lecturas MQTT)

### SNS-U-001 — Entidad Reading valida rango de valor de sensor
**Prioridad:** P1 | **Tipo:** Unitaria (Domain)

**Pasos:**
1. Crear `Reading` con `Value = -999.9` (fuera de rango)
2. Crear `Reading` con `Value = 0.0` (límite inferior válido)
3. Crear `Reading` con `Value = 100.0` (valor normal)

**Resultado esperado:**
- Valor fuera de rango lanza excepción de dominio con mensaje claro
- Valores válidos crean entidad correctamente con `Timestamp` en UTC

---

### SNS-U-002 — Procesamiento de mensaje MQTT genera lectura correctamente
**Prioridad:** P1 | **Tipo:** Unitaria (Application)

**Pasos:**
1. Construir payload MQTT simulado: `{"sensorCode": "SNS001", "variableCode": "HUM", "value": 72.5}`
2. Llamar `ProcessReadingUseCase.ExecuteAsync(payload)`
3. Mockear `IReadingRepository.CreateAsync`

**Resultado esperado:**
- `IReadingRepository.CreateAsync` invocado con `SensorCode="SNS001"`, `VariableCode="HUM"`, `Value=72.5`
- `Timestamp` asignado en UTC (no nulo)

---

### SNS-U-003 — Mensaje MQTT con payload malformado lanza excepción controlada
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Enviar payload: `"no-es-json"`, `{}`, `{"sensorCode": null}`

**Resultado esperado:**
- Excepción de validación lanzada antes de llamar al repositorio
- No se persiste ningún dato corrupto

---

### SNS-U-004 — Sensor inactivo no genera lecturas
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `ISensorRepository.GetByCodeAsync("SNS999")` → sensor con `Status = Inactive`
2. Enviar lectura para `SNS999`

**Resultado esperado:** Lectura descartada; `IReadingRepository.CreateAsync` NO invocado

---

### SNS-F-001 — Dashboard muestra lectura en tiempo real via WebSocket/polling
**Prioridad:** P1 | **Tipo:** Funcional

**Precondiciones:** ESP32 enviando datos por MQTT; servicio de sensores procesando mensajes

**Pasos:**
1. Abrir dashboard de sensores en web
2. Observar valores de temperatura, humedad, luminosidad
3. Modificar físicamente la lectura del sensor (o simular vía MQTT)

**Resultado esperado:**
- Los valores en pantalla se actualizan en < 5 segundos
- No se requiere recarga manual de página

---

### SNS-F-002 — Filtro de rango de fechas en historial de lecturas
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Ir a historial de sensor `SNS001`
2. Seleccionar rango: últimos 7 días
3. Seleccionar rango: última hora

**Resultado esperado:**
- La tabla/gráfica muestra únicamente lecturas dentro del rango seleccionado
- El filtro responde en < 2 segundos para datos del último mes

---

### SNS-E-001 — Lectura con timestamp en el futuro (> 5 minutos)
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Publicar en MQTT una lectura con `timestamp = DateTime.UtcNow.AddHours(2)`

**Resultado esperado:** Lectura rechazada o normalizada al timestamp de recepción del servidor

---

### SNS-E-002 — Burst de lecturas (100 mensajes simultáneos del mismo sensor)
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Publicar 100 mensajes MQTT para `SNS001` en < 1 segundo

**Resultado esperado:**
- Todos son procesados sin pérdida (cola de mensajes no se satura)
- No se generan duplicados
- Tiempo de procesamiento total < 10 segundos

---

## 6. Módulo: Alertas de Sensores y ESP32

### ALT-U-001 — Generar alerta cuando lectura supera umbral
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Configurar sensor `SNS001` con umbral de temperatura máxima = 35 °C
2. Procesar lectura con `Value = 38.0, VariableCode = "TEMP"`
3. Verificar llamada a `ISensorAlertRepository.CreateAsync`

**Resultado esperado:** Alerta creada con `SensorCode`, `VariableCode`, `ThresholdExceeded = true`, `AlertType = OverThreshold`

---

### ALT-U-002 — No generar alerta duplicada si ya existe alerta activa
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear alerta activa existente para `SNS001/TEMP`
2. Procesar nueva lectura que también supera umbral

**Resultado esperado:** `ISensorAlertRepository.CreateAsync` NO invocado (evita spam de alertas)

---

### ALT-U-003 — Alerta se auto-resuelve cuando lectura vuelve a rango normal
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Alerta activa para `SNS001/TEMP`
2. Procesar lectura con `Value = 28.0` (dentro del rango)

**Resultado esperado:** `IAlertRepository.UpdateAsync` invocado con `ResolvedAt = DateTime.UtcNow`, `Status = Resolved`

---

### ALT-F-001 — Alerta aparece en panel de alertas con estado correcto
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Forzar condición de alerta en sensor
2. Navegar a `/alerts`

**Resultado esperado:**
- Alerta visible con sensor, variable, valor detectado, timestamp
- Indicador visual diferenciado (color rojo para crítica, amarillo para advertencia)

---

### ALT-F-002 — Operador puede reconocer (acknowledge) una alerta
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Alerta activa visible en panel
2. Clic en "Reconocer"
3. Confirmar acción

**Resultado esperado:**
- Alerta pasa a estado "Reconocida" con timestamp de reconocimiento y usuario que la reconoció
- Ya no aparece como nueva en el filtro de alertas nuevas

---

### ALT-E-001 — Alerta con umbral exactamente igual al valor (caso límite)
**Prioridad:** P3 | **Tipo:** Edge

**Pasos:**
1. Umbral máximo = 35.000
2. Procesar lectura con `Value = 35.000`

**Resultado esperado:** Definido claramente en reglas de negocio: ¿se genera alerta o no? (documentar decisión y asegurar test cubre esa rama)

---

## 7. Módulo: Agregados y Worker de Datos

### AGG-U-001 — AggregationService calcula avg/min/max correctamente
**Prioridad:** P1 | **Tipo:** Unitaria (Domain)

**Pasos:**
1. Crear lista de valores: `[10.0, 20.0, 30.0, 40.0, 50.0]`
2. Llamar `AggregateData.FromValues(values)`

**Resultado esperado:**
- `Average = 30.0`
- `Min = 10.0`
- `Max = 50.0`
- `Count = 5`

---

### AGG-U-002 — AggregationService con lista vacía lanza excepción
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Llamar `AggregateData.FromValues(new List<double>())`

**Resultado esperado:** `InvalidOperationException` o similar; no división por cero silenciosa

---

### AGG-U-003 — GenerateAggregateId produce ID determinístico y único
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Llamar `GenerateAggregateId("SNS001", "HUM", new DateTime(2025, 1, 15, 14, 30, 0))` → esperar `"SNS001-HUM-202501151430"`
2. Verificar que cambiar cualquier parámetro produce ID diferente

**Resultado esperado:** Formato `{sensorCode}-{variableCode}-{yyyyMMddHHmm}` exacto

---

### AGG-U-004 — ProcessAggregatesUseCase omite ventana ya procesada
**Prioridad:** P1 | **Tipo:** Unitaria (Application)

**Pasos:**
1. Mockear `IAggregateRepository.GetBySensorAndVariableAndTimestampAsync` → devuelve agregado existente
2. Ejecutar `ProcessAggregatesUseCase.ExecuteAsync(referenceTime)`

**Resultado esperado:**
- `IAggregateRepository.CreateAsync` NO invocado
- `result.SkippedAggregates` incrementado en 1
- Log de debug "Aggregate already exists" presente

---

### AGG-U-005 — ProcessAggregatesUseCase crea agregado cuando hay lecturas sin procesar
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear repositorio de agregado → `null` (no existe)
2. Mockear repositorio de lecturas → lista con 5 lecturas válidas
3. Ejecutar `ProcessAggregatesUseCase.ExecuteAsync`

**Resultado esperado:**
- `IAggregateRepository.CreateAsync` invocado 1 vez
- El agregado tiene `Avg`, `Min`, `Max`, `Count = 5`
- `result.ProcessedAggregates = 1`

---

### AGG-U-006 — CleanupOldReadings elimina lecturas más antiguas que el período de retención
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `IReadingRepository.DeleteOlderThanAsync(cutoff)` → devuelve 150
2. Verificar que el cutoff es `DateTime.UtcNow - AggregationConstants.ReadingRetentionHours`

**Resultado esperado:**
- `DeleteOlderThanAsync` invocado con cutoff correcto
- `result.DeletedReadings = 150`

---

### AGG-U-007 — TimeWindow.CreateAggregationWindow produce ventana de 1 hora exacta
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. `referenceTime = new DateTime(2025, 6, 10, 14, 45, 30)` (45 minutos 30 segundos)
2. Llamar `TimeWindow.CreateAggregationWindow(referenceTime)`

**Resultado esperado:**
- `window.End` truncado a la hora completa anterior: `2025-06-10T14:00:00`
- `window.Start = 2025-06-10T13:00:00`
- Duración exacta = 1 hora

---

### AGG-I-001 — Worker de agregación procesa ciclo completo en MongoDB de integración
**Prioridad:** P1 | **Tipo:** Integración

**Precondiciones:** MongoDB real (o contenedor) con colecciones `sensors`, `readings`, `aggregates`

**Pasos:**
1. Insertar sensor activo con variables `["HUM", "TEMP"]`
2. Insertar 20 lecturas en ventana horaria pasada
3. Ejecutar `ProcessAggregatesUseCase.ExecuteAsync(referenceTime)`

**Resultado esperado:**
- 2 documentos insertados en `aggregates` (uno por variable)
- Los valores de `avg`, `min`, `max` son matemáticamente correctos
- Las lecturas más antiguas que el período de retención fueron eliminadas

---

## 8. Módulo: Lógica Difusa Mamdani (v1 + CRUD v2)

### FUZ-U-001 — Motor difuso produce salidas en rango válido para entradas conocidas
**Prioridad:** P1 | **Tipo:** Unitaria (Python / pytest)

**Precondiciones:** Servicio de lógica difusa corriendo; reglas por defecto cargadas

```python
def test_fuzzy_outputs_in_valid_range():
    engine = FuzzyEngine.load_default_rules()
    result = engine.compute(humidity=60, temperature=25, luminosity=400)
    assert 0 <= result.irrigation <= 100
    assert 0 <= result.ventilation <= 100
    assert 0 <= result.lighting <= 100
```

**Resultado esperado:** Todas las salidas dentro de `[0, 100]`

---

### FUZ-U-002 — Humedad alta desactiva irrigación
**Prioridad:** P1 | **Tipo:** Unitaria

```python
def test_high_humidity_reduces_irrigation():
    result = engine.compute(humidity=95, temperature=22, luminosity=400)
    assert result.irrigation < 20  # irrigación muy baja cuando humedad es alta
```

---

### FUZ-U-003 — Temperatura alta activa ventilación
**Prioridad:** P1 | **Tipo:** Unitaria

```python
def test_high_temperature_increases_ventilation():
    result = engine.compute(humidity=50, temperature=38, luminosity=400)
    assert result.ventilation > 70
```

---

### FUZ-U-004 — Luminosidad baja activa iluminación artificial
**Prioridad:** P1 | **Tipo:** Unitaria

```python
def test_low_luminosity_increases_lighting():
    result = engine.compute(humidity=50, temperature=22, luminosity=50)
    assert result.lighting > 60
```

---

### FUZ-U-005 — Reglas personalizadas (CRUD v2): crear regla nueva y verificar efecto en motor
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Crear regla: `IF humidity IS low AND temperature IS high THEN irrigation IS medium`
2. Recargar motor con regla nueva
3. Calcular con `humidity=20, temperature=35`

**Resultado esperado:** Salida de irrigación refleja la nueva regla (`medium` ≈ 50)

---

### FUZ-U-006 — Reglas en conflicto (CRUD v2): dos reglas con antecedentes similares y consecuentes opuestos
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Definir regla A: `IF humidity IS medium THEN irrigation IS high`
2. Definir regla B: `IF humidity IS medium THEN irrigation IS low`
3. Calcular con `humidity=55`

**Resultado esperado:**
- El motor aplica defuzzificación correcta (centroide de ambas reglas)
- No lanza excepción; produce un valor razonable en el rango medio

---

### FUZ-U-007 — Eliminar regla personalizada y verificar que motor usa regla base
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Crear y cargar regla personalizada para irrigation
2. Eliminar la regla personalizada
3. Recalcular

**Resultado esperado:** Salida vuelve a los valores de las reglas base (sin la regla eliminada)

---

### FUZ-F-001 — Panel web muestra reglas difusas actuales y permite edición (v2)
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Navegar a `/fuzzy/rules` como administrador
2. Ver lista de reglas activas con antecedentes y consecuentes
3. Clic en "Editar" en una regla
4. Modificar el consecuente de `high` a `medium`
5. Guardar

**Resultado esperado:**
- Regla actualizada en lista
- El motor difuso recarga reglas (sin reinicar el servicio completo)
- La nueva regla se refleja en próximas inferencias del actuador

---

### FUZ-F-002 — Operador puede crear rutina personalizada vinculada a reglas (v2)
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Ir a `/fuzzy/routines/create`
2. Definir nombre de rutina: "Rutina Verano"
3. Asignar reglas específicas para condiciones de verano (alta temperatura, baja humedad)
4. Activar rutina
5. Verificar que el actuador usa la rutina activa

**Resultado esperado:**
- Rutina visible en listado
- Motor difuso carga reglas de la rutina activa
- Solo una rutina puede estar activa a la vez; activar otra desactiva la anterior

---

### FUZ-F-003 — Simulador de reglas difusas permite probar antes de aplicar (v2)
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. En panel de reglas, ir a "Simulador"
2. Ingresar valores manuales: humidity=70, temperature=30, luminosity=300
3. Clic en "Simular"

**Resultado esperado:**
- Gráfica de conjuntos difusos visible (o tabla de resultados)
- Valores de salida (irrigation, ventilation, lighting) mostrados en porcentaje
- No se aplica la simulación al actuador real (solo es lectura)

---

### FUZ-E-001 — Valores de entrada fuera del universo de discurso
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Enviar `humidity = -5` (debajo del mínimo)
2. Enviar `temperature = 200` (absurdo)
3. Enviar `luminosity = null`

**Resultado esperado:**
- Valores fuera de rango son clipeados al límite del universo O se devuelve error de validación 400
- No crash del motor difuso

---

### FUZ-E-002 — Motor sin reglas cargadas (lista vacía) no crashea
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Eliminar todas las reglas personalizadas
2. Verificar que existe un fallback a reglas base o error controlado

**Resultado esperado:** Motor usa reglas base por defecto O lanza excepción controlada con mensaje claro

---

## 9. Módulo: BI — Consumo y Rentabilidad (v2)

### BI-U-001 — Cálculo de costo por m³ de agua consumida
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Definir: `VolumeM3 = 5.0`, `CostPerM3 = 1200` (COP)
2. Llamar `ConsumptionCalculator.CalculateWaterCost(volume, costPerM3)`

**Resultado esperado:** `TotalCost = 6000.0` (sin errores de punto flotante > 0.01)

---

### BI-U-002 — Cálculo de rentabilidad considera costos de insumos e ingresos de venta
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Definir: `revenue = 500000`, `inputCosts = 350000`, `energyCosts = 80000`
2. Llamar `ProfitabilityCalculator.Calculate(revenue, inputCosts, energyCosts)`

**Resultado esperado:**
- `GrossProfit = 150000`
- `ProfitMargin = 30.0 %`
- `NetProfit = 70000`

---

### BI-U-003 — Generación de reporte mensual agrega datos de todos los sensores
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear 3 sensores con agregados del mes
2. Llamar `GenerateMonthlyReportUseCase.ExecuteAsync(year=2025, month=6)`

**Resultado esperado:**
- Reporte contiene sección por sensor con avg, min, max, consumo
- Total de recursos calculado sumando todos los sensores

---

### BI-F-001 — Dashboard BI muestra gráfica de consumo de agua por período
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Navegar a `/bi/consumption`
2. Seleccionar período: "Último mes"
3. Observar gráfica de barras de consumo diario

**Resultado esperado:**
- Gráfica renderizada con datos reales de MongoDB
- Eje Y en unidades correctas (m³ o litros)
- Suma total del período visible en tarjeta resumen

---

### BI-F-002 — Dashboard BI muestra índice de rentabilidad
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Navegar a `/bi/profitability`
2. Ingresar costos de insumos del mes (formulario editable)
3. Ingresar ingresos de venta
4. Ver resultado

**Resultado esperado:**
- Porcentaje de rentabilidad calculado y visible
- Indicador verde/rojo según rentabilidad positiva/negativa
- Opción de exportar a PDF o CSV

---

### BI-F-003 — Exportar reporte de consumo a CSV
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. En `/bi/consumption`, clic en "Exportar CSV"

**Resultado esperado:**
- Descarga de archivo `consumo_[mes]_[año].csv`
- Columnas: fecha, sensor, variable, avg, min, max, unidad
- No contiene datos de otros meses

---

### BI-E-001 — Período sin datos (mes sin lecturas)
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Consultar `/bi/consumption` para un mes donde no hay datos en la BD

**Resultado esperado:**
- Gráfica vacía con mensaje "Sin datos para el período seleccionado"
- No error 500; no NaN en cálculos

---

### BI-E-002 — División por cero en cálculo de rentabilidad con ingresos = 0
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Ingresar `revenue = 0`, `costs = 50000`
2. Calcular rentabilidad

**Resultado esperado:** Rentabilidad = -100% (pérdida total) o mensaje "Ingresos en cero" — sin excepción DivideByZero

---

## 10. Módulo: Servicio Meteorológico y Alertas Proactivas (v2)

### WEA-U-001 — Parseo de respuesta de API meteorológica externa
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Definir respuesta JSON simulada de la API (OpenWeatherMap u equivalente)
2. Llamar `WeatherResponseMapper.MapToWeatherData(jsonResponse)`

**Resultado esperado:**
- `WeatherData.Temperature`, `Humidity`, `Condition`, `Forecast` correctamente extraídos
- Campos opcionales nulos manejados sin excepción

---

### WEA-U-002 — Generación de alerta proactiva cuando se pronostica lluvia intensa
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `WeatherData` con `Condition = "HeavyRain"`, `RainMM = 25.0`
2. Llamar `EvaluateWeatherAlertsUseCase.ExecuteAsync(weatherData)`

**Resultado esperado:**
- Se genera alerta de tipo `WeatherAlert` con `Priority = High`
- `INotificationService.SendAsync` invocado con mensaje de lluvia intensa

---

### WEA-U-003 — No se genera alerta duplicada si ya existe alerta meteorológica activa del mismo tipo
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Alerta de lluvia ya existente y activa
2. Nueva consulta meteorológica devuelve lluvia nuevamente

**Resultado esperado:** `INotificationService.SendAsync` NO invocado de nuevo

---

### WEA-U-004 — Ajuste de plan de irrigación ante pronóstico de lluvia
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Pronóstico con `RainMM > 15.0` para próximas 6 horas
2. Llamar `AdjustIrrigationPlanUseCase.ExecuteAsync(forecast)`

**Resultado esperado:**
- Plan de irrigación pospuesto o reducido
- Log indica "Irrigación ajustada por pronóstico de lluvia"

---

### WEA-F-001 — Panel web muestra clima actual y pronóstico para la ubicación del invernadero
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Navegar a `/weather`
2. Observar temperatura, humedad, condición actual
3. Ver pronóstico de 5 días

**Resultado esperado:**
- Datos actualizados (no más de 30 minutos de antigüedad)
- Ícono de condición climática visible
- Alerta banner si se pronostica evento severo

---

### WEA-F-002 — Alerta meteorológica proactiva llega por notificación push y WhatsApp
**Prioridad:** P1 | **Tipo:** Funcional (Integración)

**Precondiciones:** Notificaciones habilitadas; dispositivo con token push registrado

**Pasos:**
1. Forzar condición de alerta meteorológica (simular API response con lluvia)
2. Esperar ejecución del worker meteorológico

**Resultado esperado:**
- Notificación push recibida en dispositivo móvil en < 60 segundos
- Mensaje WhatsApp enviado al número configurado
- Alerta visible en panel web en sección `/alerts`

---

### WEA-E-001 — API meteorológica no disponible (timeout)
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Simular timeout de la API externa (mock HttpClient con `TaskCanceledException`)
2. Ejecutar `FetchWeatherUseCase`

**Resultado esperado:**
- Error capturado; se usa último dato cacheado si existe
- Log de error con `Level = Warning` (no exception sin capturar)
- No se interrumpe el worker principal

---

### WEA-E-002 — Coordenadas de ubicación inválidas en configuración
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Configurar `Latitude = 999.0` (inválido)
2. Ejecutar consulta meteorológica

**Resultado esperado:** Error de configuración detectado al iniciar servicio (no en runtime); log claro de error de configuración

---

## 11. Módulo: Notificaciones (WhatsApp, Push, Email) (v2)

### NOT-U-001 — Despacho compuesto envía por todos los canales habilitados
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `IWhatsAppNotifier`, `IPushNotifier`, `IEmailNotifier`
2. Configurar `NotificationPreferences` con todos los canales habilitados
3. Llamar `CompositeNotificationDispatcher.DispatchAsync(notification)`

**Resultado esperado:**
- Los 3 mocks fueron invocados exactamente 1 vez cada uno
- Si uno falla, los demás siguen ejecutándose (no cortocircuito)

---

### NOT-U-002 — Canal deshabilitado en preferencias no es invocado
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. `NotificationPreferences` con `WhatsApp = false`, `Push = true`, `Email = true`
2. Llamar `CompositeNotificationDispatcher.DispatchAsync`

**Resultado esperado:** `IWhatsAppNotifier.SendAsync` NO invocado; los otros dos sí

---

### NOT-U-003 — Fallo en un canal no cancela los demás (resiliencia)
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Mockear `IWhatsAppNotifier.SendAsync` para lanzar `HttpRequestException`
2. Mockear `IPushNotifier` y `IEmailNotifier` normalmente
3. Llamar `DispatchAsync`

**Resultado esperado:**
- `IPushNotifier` e `IEmailNotifier` aún son invocados
- El error de WhatsApp es capturado y logueado
- No se propaga excepción al caller

---

### NOT-U-004 — Template de mensaje de alerta de sensor formateado correctamente
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Crear `SensorAlertNotification` con `SensorCode = "SNS001"`, `Variable = "TEMP"`, `Value = 38.5`
2. Llamar `NotificationTemplateBuilder.BuildSensorAlert(notification)`

**Resultado esperado:**
- Mensaje contiene: nombre del sensor, variable, valor, timestamp formateado en español
- Sin caracteres de escape mal formados

---

### NOT-F-001 — Usuario configura sus preferencias de notificación
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Ir a `/profile/notifications`
2. Habilitar WhatsApp, deshabilitar email
3. Ingresar número de WhatsApp
4. Guardar

**Resultado esperado:**
- Configuración persistida en BD
- Próxima notificación usa solo WhatsApp y Push (no email)

---

### NOT-F-002 — Push notification llega en móvil cuando app está en segundo plano
**Prioridad:** P1 | **Tipo:** Funcional

**Precondiciones:** App móvil instalada; token FCM/APNs registrado

**Pasos:**
1. Minimizar la app
2. Disparar notificación desde el servidor (alerta de prueba)

**Resultado esperado:**
- Notificación aparece en bandeja del sistema del dispositivo
- Al tocarla, abre la app en la sección de alertas relevante (deep link)

---

### NOT-F-003 — Push notification llega cuando app está cerrada
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Cerrar completamente la app
2. Disparar notificación

**Resultado esperado:** Notificación recibida igualmente vía FCM background delivery

---

### NOT-E-001 — Número de WhatsApp con formato incorrecto
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Configurar número: `"123"`, `"+57abc"`, `""`

**Resultado esperado:** 400 con mensaje de validación; no se intenta envío al proveedor

---

### NOT-E-002 — Token push expirado o revocado
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Token push inválido en BD
2. Intentar enviar notificación

**Resultado esperado:**
- Error capturado; token marcado como inválido en BD
- Log de warning; el usuario no recibe error (fallo silencioso con logging)

---

## 12. Módulo: Chatbot RAG (v2)

### RAG-U-001 — Generación de embedding produce vector de dimensión correcta
**Prioridad:** P1 | **Tipo:** Unitaria (Python)

```python
def test_embedding_dimension():
    embedder = SentenceEmbedder()
    vector = embedder.embed("¿Cómo funciona el sistema de irrigación?")
    assert len(vector) == 768  # dimensión del modelo configurado
```

---

### RAG-U-002 — Búsqueda vectorial devuelve documentos relevantes para consulta específica
**Prioridad:** P1 | **Tipo:** Integración

**Precondiciones:** MongoDB Atlas Vector Search configurado con documentos de manual del sistema indexados

**Pasos:**
1. Ingresar query: "¿Cuál es la temperatura óptima para el cultivo?"
2. Ejecutar búsqueda vectorial con `numCandidates = 100`, `limit = 3`

**Resultado esperado:**
- Los 3 documentos más cercanos son semánticamente relevantes a temperatura/cultivo
- Puntuación de similitud > 0.75

---

### RAG-U-003 — Generación de respuesta incluye contexto recuperado
**Prioridad:** P1 | **Tipo:** Unitaria (mock LLM)

**Pasos:**
1. Simular recuperación de 2 fragmentos relevantes
2. Construir prompt con contexto
3. Mockear LLM → respuesta generada

**Resultado esperado:**
- El prompt enviado al LLM contiene los fragmentos recuperados
- La respuesta no es inventada (grounded en el contexto)

---

### RAG-U-004 — Detección de pregunta fuera del dominio del sistema
**Prioridad:** P2 | **Tipo:** Unitaria

**Pasos:**
1. Query: "¿Cuál es la capital de Francia?"
2. Búsqueda vectorial devuelve 0 documentos con similitud > umbral mínimo

**Resultado esperado:**
- Respuesta: "No tengo información sobre ese tema en el sistema HydroEspinaca"
- No se genera respuesta inventada (sin alucinación)

---

### RAG-U-005 — Verificación de versión de chunk: documentos actualizados invalidan chunks viejos
**Prioridad:** P1 | **Tipo:** Unitaria

**Pasos:**
1. Documento con `version = 1` en BD
2. Subir nueva versión del documento (version = 2)
3. Verificar que los chunks viejos (version = 1) son eliminados o marcados obsoletos

**Resultado esperado:**
- Solo chunks con `version = 2` aparecen en resultados de búsqueda
- No se devuelven respuestas basadas en contenido obsoleto

---

### RAG-F-001 — Usuario escribe pregunta y obtiene respuesta en < 10 segundos
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Abrir chat en interfaz web o móvil
2. Escribir: "¿Cómo interpreto una alerta de humedad?"
3. Enviar

**Resultado esperado:**
- Respuesta coherente en < 10 segundos
- Respuesta en español
- Si hay fuentes, se muestran referencias al manual del sistema

---

### RAG-F-002 — Historial de conversación persiste en la sesión
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Iniciar conversación con 3 preguntas
2. Cerrar chat y volver a abrirlo en la misma sesión

**Resultado esperado:**
- Las 3 preguntas anteriores y sus respuestas son visibles
- El chatbot mantiene contexto de preguntas anteriores dentro de la misma sesión

---

### RAG-F-003 — Panel de administración permite cargar nuevo documento al knowledge base
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Ir a `/admin/chatbot/documents`
2. Subir PDF del manual actualizado
3. Confirmar procesamiento

**Resultado esperado:**
- PDF procesado, chunkeado y vectorizado en < 2 minutos para documentos de hasta 50 páginas
- Confirmación de cuántos chunks fueron generados
- Los nuevos chunks están disponibles inmediatamente para búsqueda

---

### RAG-E-001 — Pregunta con caracteres especiales o muy larga
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Enviar pregunta con 2000 caracteres
2. Enviar pregunta con emojis y caracteres especiales: `"¿Cómo? 🌿 <script>alert(1)</script>"`

**Resultado esperado:**
- Pregunta larga: truncada o rechazada con límite claro (ej. "Máximo 500 caracteres")
- XSS en input: sanitizado; el script no se ejecuta en la interfaz

---

### RAG-E-002 — Atlas Vector Search temporalmente no disponible
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Simular timeout de Atlas Vector Search
2. Enviar pregunta al chatbot

**Resultado esperado:**
- Mensaje de error amigable: "El chatbot no está disponible en este momento, intenta más tarde"
- No se muestra stack trace al usuario

---

## 13. Módulo: Vistas Web (v2)

### WEB-F-001 — Navegación entre módulos sin recarga completa de página (SPA)
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Navegar entre: Dashboard → Sensores → BI → Clima → Chatbot → Fuzzy
2. Verificar que no hay recarga completa (white flash) entre páginas

**Resultado esperado:**
- Transición suave (React Router); el layout principal persiste
- URL en barra del navegador actualizada correctamente
- Back/Forward del navegador funciona

---

### WEB-F-002 — Dashboard principal muestra estado en tiempo real de todos los sensores
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Abrir `/dashboard`
2. Verificar tarjetas de sensor activo/inactivo
3. Verificar últimas lecturas

**Resultado esperado:**
- Sensores activos con valor actual y timestamp
- Sensores inactivos claramente identificados (gris/deshabilitado)
- Sin valores `null` o `undefined` visibles en UI

---

### WEB-F-003 — Responsive design: todas las vistas funcionales en 768px (tablet)
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Usar DevTools → emulación de tablet (768x1024)
2. Navegar por todas las secciones principales

**Resultado esperado:**
- No hay overflow horizontal
- Menú lateral colapsa a hamburger menu o bottom navigation
- Tablas con scroll horizontal cuando es necesario

---

### WEB-F-004 — Gráficas de datos históricos renderizan con > 1000 puntos de datos
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Navegar a historial de sensor con 30 días de datos
2. Observar rendimiento de renderizado

**Resultado esperado:**
- Gráfica renderizada en < 3 segundos
- Zoom y pan funcionan sin lag perceptible
- El navegador no se congela

---

### WEB-F-005 — Formulario de creación de regla difusa valida campos obligatorios
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Ir a formulario de nueva regla difusa
2. Intentar guardar sin seleccionar antecedente
3. Intentar guardar sin consecuente

**Resultado esperado:**
- Mensajes de error inline bajo cada campo obligatorio vacío
- El formulario no envía la solicitud al servidor

---

### WEB-E-001 — Acceso directo a URL protegida sin sesión activa
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Sin sesión, navegar directamente a `/admin/users`

**Resultado esperado:** Redirección a `/login` con query param `?redirect=/admin/users` para volver post-login

---

### WEB-E-002 — Sesión expirada durante uso activo de la app (middleware interceptor)
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Iniciar sesión; forzar expiración de AccessToken
2. Ejecutar una acción que requiera autenticación (ej. guardar formulario)

**Resultado esperado:**
- El interceptor de Axios/Fetch detecta 401
- Intenta silent refresh con RefreshToken
- Si refresh falla: modal "Tu sesión expiró, por favor vuelve a iniciar sesión"

---

## 14. Módulo: Vistas Móviles (v2)

### MOB-F-001 — Login en app móvil (Android/iOS)
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Abrir app
2. Ingresar credenciales correctas
3. Tocar "Iniciar sesión"

**Resultado esperado:**
- Navegación al dashboard principal en < 3 segundos
- Token almacenado de forma segura (SecureStore / Keychain, no AsyncStorage sin cifrar)

---

### MOB-F-002 — Dashboard móvil muestra lecturas actuales con indicadores visuales
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Abrir dashboard en app
2. Verificar cards de temperatura, humedad, luminosidad

**Resultado esperado:**
- Valores actualizados (polling o WebSocket)
- Indicadores de color: verde (normal), amarillo (advertencia), rojo (crítico)
- Cards táctiles que navegan al detalle del sensor

---

### MOB-F-003 — Notificaciones push recibidas con app en primer plano
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Tener app abierta en el dashboard
2. Disparar notificación desde servidor

**Resultado esperado:**
- Banner de notificación in-app visible
- No duplicado con notificación del sistema operativo
- Tocar banner navega al contexto correcto (ej. alerta específica)

---

### MOB-F-004 — Chatbot funcional en móvil con teclado suave
**Prioridad:** P1 | **Tipo:** Funcional

**Pasos:**
1. Abrir chat en app
2. Tocar input text → teclado sube
3. Escribir pregunta → enviar

**Resultado esperado:**
- La vista del chat sube con el teclado (KeyboardAvoidingView funciona)
- El historial de mensajes es scrollable
- No hay flickering ni desbordamiento de contenido

---

### MOB-F-005 — Vista de alertas móvil muestra alertas activas y permite reconocerlas
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Abrir `/alerts` en app
2. Ver alerta activa
3. Deslizar o tocar "Reconocer"
4. Confirmar

**Resultado esperado:**
- Alerta cambia de estado en tiempo real (optimistic update + confirmación del servidor)
- Si falla el servidor: alerta vuelve a estado anterior con mensaje de error

---

### MOB-F-006 — App funciona correctamente sin conexión a internet (modo offline)
**Prioridad:** P2 | **Tipo:** Funcional

**Pasos:**
1. Iniciar sesión con conexión
2. Desactivar red (modo avión)
3. Navegar por la app

**Resultado esperado:**
- Dashboard muestra últimos datos cacheados con indicador "Sin conexión"
- Las acciones que requieren red muestran mensaje "Sin conexión, intenta más tarde"
- Al recuperar red, la app actualiza datos automáticamente

---

### MOB-E-001 — App en dispositivo con versión Android antigua (API 26 / Android 8)
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Instalar y usar la app en emulador Android API 26

**Resultado esperado:**
- App inicia sin crash
- Funcionalidades core disponibles
- Si alguna función no está disponible, se informa al usuario (ej. biometría si no es compatible)

---

### MOB-E-002 — Pantalla muy pequeña (320px de ancho)
**Prioridad:** P3 | **Tipo:** Edge

**Pasos:**
1. Emular dispositivo 320x568 (iPhone SE 1ª gen)
2. Navegar por todas las pantallas

**Resultado esperado:**
- No hay texto cortado o elementos solapados
- Scroll disponible donde es necesario

---

## 15. Pruebas de Seguridad Transversales

### SEC-S-001 — JWT con firma inválida es rechazado
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Tomar un JWT válido
2. Modificar el payload (cambiar `role` a `"Administrator"`)
3. No re-firmar (firma queda inválida)
4. Enviar en header `Authorization: Bearer {token_modificado}`

**Resultado esperado:** 401 Unauthorized; el servicio no procesa la solicitud

---

### SEC-S-002 — JWT de machine-to-machine no acepta en endpoints de usuario
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Usar token M2M (firmado con `machinetomachine-private.pem`)
2. Intentar llamar a endpoint de usuario (ej. `/api/users/me`)

**Resultado esperado:** 403 Forbidden (el endpoint requiere token de usuario, no M2M)

---

### SEC-S-003 — Enumeración de usuarios en endpoint de login
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Login con email existente + contraseña incorrecta → anotar tiempo de respuesta
2. Login con email inexistente + contraseña cualquiera → anotar tiempo de respuesta

**Resultado esperado:**
- Ambos devuelven el mismo mensaje genérico (no revelan si el email existe)
- Tiempos de respuesta similares (para resistir timing attacks con bcrypt)

---

### SEC-S-004 — Autorización por rol: Viewer no puede crear sensores
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Autenticarse como usuario con rol `Viewer`
2. Enviar `POST /api/sensors` con datos válidos

**Resultado esperado:** 403 Forbidden

---

### SEC-S-005 — CSRF: acción de estado no posible sin token CSRF (si aplica)
**Prioridad:** P2 | **Tipo:** Seguridad

**Pasos:**
1. Construir solicitud cross-origin a endpoint de creación sin el token CSRF

**Resultado esperado:** Solicitud rechazada. (Si la API es solo JSON con JWT en header, el riesgo CSRF ya está mitigado por diseño)

---

### SEC-S-006 — Rate limiting en endpoint de login
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Enviar 20 solicitudes de login fallidas consecutivas en < 30 segundos desde la misma IP

**Resultado esperado:**
- A partir del intento N (configurable), se recibe 429 Too Many Requests
- El bloqueo tiene una duración definida (ej. 5 minutos)

---

### SEC-S-007 — Secretos no expuestos en respuestas de API
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Consultar `GET /api/users/{id}`
2. Consultar `GET /api/sensors`
3. Revisar todas las respuestas JSON

**Resultado esperado:**
- Ninguna respuesta contiene: passwords, hashes, claves RSA, connection strings, API keys de servicios externos
- Los campos de contraseña están ausentes o son `null`

---

### SEC-S-008 — Headers de seguridad HTTP presentes en producción
**Prioridad:** P2 | **Tipo:** Seguridad

**Pasos:**
1. Hacer cualquier solicitud a la API/web en producción
2. Inspeccionar headers de respuesta

**Resultado esperado:** Presencia de:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Strict-Transport-Security` (HSTS)
- `Content-Security-Policy` (en la web)

---

### SEC-S-009 — Datos sensibles no logueados en texto plano
**Prioridad:** P1 | **Tipo:** Seguridad

**Pasos:**
1. Ejecutar login con credenciales
2. Revisar logs de la aplicación (stdout/archivo)

**Resultado esperado:**
- La contraseña no aparece en logs en ningún formato
- El token completo no aparece en logs (solo primeros/últimos 4 caracteres si se logea el correlationId)

---

## 16. Pruebas de Integración End-to-End

### INT-I-001 — Flujo completo: ESP32 → MQTT → Sensor Service → MongoDB → Dashboard Web
**Prioridad:** P1 | **Tipo:** Integración E2E

**Pasos:**
1. ESP32 (o simulador) publica en topic `hydroespinaca/sensors/SNS001/HUM` → valor `72.5`
2. Sensor Service recibe el mensaje vía MQTT
3. Guarda Reading en MongoDB
4. Dashboard web hace polling/WebSocket
5. Dashboard muestra `72.5 %` para humedad de SNS001

**Resultado esperado:** Dato visible en dashboard en < 5 segundos desde publicación MQTT

---

### INT-I-002 — Flujo completo: lectura → alerta → notificación
**Prioridad:** P1 | **Tipo:** Integración E2E

**Pasos:**
1. Publicar lectura que supera umbral configurado
2. Sensor Service detecta umbral y crea SensorAlert
3. Notification Service recibe evento de nueva alerta
4. Notificación push enviada al dispositivo registrado

**Resultado esperado:**
- Alerta visible en panel web
- Push notification recibida en < 60 segundos

---

### INT-I-003 — Flujo completo: worker de agregados procesa y datos disponibles en BI
**Prioridad:** P1 | **Tipo:** Integración E2E

**Pasos:**
1. Insertar lecturas en MongoDB para ventana horaria pasada
2. Worker de agregados ejecuta su ciclo (forzar ejecución o esperar al siguiente ciclo)
3. Navegar a módulo BI y ver datos de esa hora

**Resultado esperado:**
- Agregado visible en colección `aggregates`
- Dashboard BI muestra los valores promedio/min/max correctos

---

### INT-I-004 — Flujo completo: pregunta en chatbot → respuesta basada en documentos reales
**Prioridad:** P1 | **Tipo:** Integración E2E

**Pasos:**
1. Cargar documento de manual al knowledge base
2. Hacer pregunta relacionada al contenido del manual
3. Verificar que la respuesta cita información del documento cargado

**Resultado esperado:**
- Respuesta coherente y grounded en el documento cargado
- No alucinación de información no presente en el documento

---

### INT-I-005 — Flujo completo: pronóstico de lluvia → ajuste de irrigación → notificación
**Prioridad:** P1 | **Tipo:** Integración E2E

**Pasos:**
1. API meteorológica devuelve pronóstico de lluvia intensa
2. Weather Service evalúa el pronóstico
3. Plan de irrigación es ajustado
4. Notificación enviada al operador

**Resultado esperado:**
- Motor difuso recibe parámetros ajustados
- Notificación recibida con mensaje: "Plan de irrigación ajustado por pronóstico de lluvia"

---

## 17. Pruebas de Rendimiento y Carga

### PERF-P-001 — API de lecturas soporta 50 solicitudes por segundo sin degradación
**Prioridad:** P2 | **Tipo:** Rendimiento

**Herramienta:** k6, Artillery, o JMeter

```javascript
// k6 script
export const options = {
  vus: 50,
  duration: '30s',
};
export default function () {
  const res = http.post('http://api/readings', payload, params);
  check(res, { 'status 201': r => r.status === 201 });
}
```

**Resultado esperado:**
- P95 latencia < 200 ms
- P99 latencia < 500 ms
- 0 errores 5xx durante la prueba
- CPU del contenedor < 80 % en el peak

---

### PERF-P-002 — Dashboard web carga en < 3 segundos en conexión 4G simulada
**Prioridad:** P2 | **Tipo:** Rendimiento

**Herramienta:** Lighthouse en Chrome DevTools

**Pasos:**
1. Abrir DevTools → Network → throttle "Fast 4G"
2. Reload del dashboard
3. Medir LCP (Largest Contentful Paint)

**Resultado esperado:**
- LCP < 2.5 segundos
- Time to Interactive < 5 segundos
- Lighthouse Performance Score > 70

---

### PERF-P-003 — Búsqueda vectorial en Atlas responde en < 500 ms para 1M de vectores
**Prioridad:** P2 | **Tipo:** Rendimiento

**Pasos:**
1. Con knowledge base de 1 millón de chunks (carga de prueba)
2. Ejecutar 100 búsquedas consecutivas

**Resultado esperado:**
- Tiempo promedio de búsqueda < 500 ms
- No hay degradación notable entre la búsqueda 1 y la 100

---

### PERF-P-004 — Worker de agregados procesa 1000 sensor-variable pares en < 5 minutos
**Prioridad:** P2 | **Tipo:** Rendimiento

**Pasos:**
1. Configurar 100 sensores activos con 10 variables cada uno
2. Insertar lecturas para cada par en la ventana horaria
3. Ejecutar `ProcessAggregatesUseCase`

**Resultado esperado:**
- Tiempo de ejecución < 5 minutos
- Todos los 1000 agregados creados correctamente

---

### PERF-P-005 — App móvil renderiza lista de 500 lecturas sin jank
**Prioridad:** P3 | **Tipo:** Rendimiento

**Pasos:**
1. Cargar historial de sensor con 500 lecturas en móvil
2. Hacer scroll rápido por toda la lista

**Resultado esperado:**
- FPS ≥ 50 durante el scroll (sin jank perceptible)
- Memoria de la app no supera 200 MB

---

## 18. Casos Borde y Escenarios de Error Globales

### EDGE-E-001 — MongoDB no disponible: servicio responde 503 correctamente
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Detener contenedor MongoDB
2. Hacer solicitud a cualquier endpoint que acceda a BD

**Resultado esperado:**
- 503 Service Unavailable con mensaje "Servicio temporalmente no disponible"
- Health check endpoint (`/health`) reporta `status: degraded`
- No se expone el stack trace de MongoDB

---

### EDGE-E-002 — Mensaje MQTT malformado no crashea el listener
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Publicar en topic de sensores un mensaje binario corrupto
2. Publicar mensaje JSON con campos adicionales desconocidos

**Resultado esperado:**
- Mensaje rechazado con log de error
- El listener MQTT continúa funcionando (no se detiene el servicio)

---

### EDGE-E-003 — Zona horaria: lectura recibida en UTC vs. local
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. ESP32 envía timestamp en UTC
2. Dashboard web está configurado para Colombia (UTC-5)

**Resultado esperado:**
- Todos los timestamps almacenados en UTC en MongoDB
- Dashboard muestra timestamps convertidos a la zona horaria local del usuario
- No hay confusiones de hora entre lecturas y agregados

---

### EDGE-E-004 — Caracteres especiales en código de sensor o variable
**Prioridad:** P2 | **Tipo:** Edge

**Pasos:**
1. Crear sensor con `Code = "SNS/001"`, `Code = "SNS 001"`, `Code = "SNS\x00001"`

**Resultado esperado:**
- Solo códigos alfanuméricos con guiones son aceptados (validación a nivel de dominio)
- Los inválidos devuelven 400 con mensaje claro

---

### EDGE-E-005 — Concurrencia: dos workers intentan crear el mismo agregado simultáneamente
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Simular ejecución concurrente de dos instancias de `ProcessAggregatesUseCase` para el mismo período

**Resultado esperado:**
- Solo un agregado creado (sin duplicados)
- El segundo intento recibe error de índice único o verifica la existencia antes de insertar
- No hay deadlocks ni errores 500 propagados

---

### EDGE-E-006 — Valor de sensor extremo (Infinity, NaN en JSON)
**Prioridad:** P1 | **Tipo:** Edge

**Pasos:**
1. Publicar vía MQTT: `{"sensorCode": "SNS001", "variableCode": "TEMP", "value": Infinity}`
2. Publicar: `{"value": NaN}`
3. Publicar: `{"value": null}`

**Resultado esperado:**
- `Infinity` y `NaN`: rechazados (400) — MongoDB no soporta estos valores en campos Double
- `null`: rechazado (400) — valor requerido

---

## 19. Checklist de Métricas SonarQube por Servicio

### 19.1 Objetivos por microservicio

| Microservicio          | Cobertura Líneas (Target) | Bugs | Vulnerabilidades | Smells (Deuda) | Duplicaciones |
|------------------------|---------------------------|------|-----------------|----------------|---------------|
| sensor-service         | ≥ 90 %                    | 0    | 0               | < 5 %          | < 3 %         |
| auth-service           | ≥ 92 %                    | 0    | 0               | < 5 %          | < 3 %         |
| bi-service             | ≥ 88 %                    | 0    | 0               | < 5 %          | < 3 %         |
| weather-service        | ≥ 85 %                    | 0    | 0               | < 5 %          | < 3 %         |
| notification-service   | ≥ 87 %                    | 0    | 0               | < 5 %          | < 3 %         |
| chatbot-service (Python)| ≥ 80 %                   | 0    | 0               | < 5 %          | < 3 %         |
| fuzzy-service (Python) | ≥ 80 %                    | 0    | 0               | < 5 %          | < 3 %         |

### 19.2 Exclusiones de cobertura justificadas

Los siguientes archivos deben ser excluidos del análisis de cobertura en SonarQube (no contienen lógica de negocio):

- `**/Persistence/Models/**Document.cs` — clases de datos puras (POCOs)
- `**/Persistence/Mappers/**Mapper.cs` — transformaciones triviales 1:1
- `**/DependencyInjection*.cs` — registro de servicios en DI
- `**/Program.cs` — punto de entrada de la aplicación
- `**/*Options.cs` — clases de configuración (options pattern)
- `**/Migrations/**` — migraciones de BD si aplican

### 19.3 Áreas críticas que SÍ deben tener cobertura ≥ 95 %

- `Domain/Entities/` — invariantes de negocio
- `Domain/ValueObjects/` — lógica de validación inmutable
- `Domain/Services/` — servicios de dominio con cálculos clave
- `Application/UseCases/` — flujos de caso de uso principales

### 19.4 Tipos de prueba por capa

| Capa            | Framework .NET       | Framework Python | Comentario                           |
|-----------------|---------------------|-----------------|--------------------------------------|
| Domain          | xUnit + FluentAssertions | pytest      | Sin mocks; lógica pura               |
| Application     | xUnit + Moq         | pytest + unittest.mock | Mockear repositorios e interfaces    |
| Infrastructure  | xUnit (integración) | pytest + mongomock/real DB | Contra MongoDB real o en contenedor  |
| API (endpoints) | WebApplicationFactory | httpx + pytest | Pruebas de contrato HTTP             |

### 19.5 Pipeline CI/CD con Quality Gate

```yaml
# Ejemplo GitHub Actions
- name: Test & SonarQube Analysis
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    dotnet sonarscanner begin /k:"$SERVICE_KEY" /d:sonar.token="$SONAR_TOKEN"
    dotnet build
    dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

- name: Quality Gate Check
  run: |
    # Falla el pipeline si Quality Gate no pasa
    curl -f "https://sonar.yourhost.com/api/qualitygates/project_status?projectKey=$SERVICE_KEY"
```

---

## Resumen de Cobertura por Funcionalidad Nueva (v2)

| Funcionalidad Nueva (v2)                 | Unit Tests | Integration Tests | Functional Tests | Security Tests | Edge Cases |
|------------------------------------------|------------|-------------------|------------------|----------------|------------|
| CRUD Reglas Difusas / Rutinas            | FUZ-U-005 a 007 | FUZ-I-* | FUZ-F-001 a 003 | SEC-S-004 | FUZ-E-001,002 |
| Módulo BI Consumo/Rentabilidad           | BI-U-001 a 003 | BI-I-* | BI-F-001 a 003 | SEC-S-007 | BI-E-001,002 |
| Servicio Meteorológico + Alertas Proactivas | WEA-U-001 a 004 | WEA-I-* | WEA-F-001,002 | — | WEA-E-001,002 |
| Notificaciones WhatsApp + Push           | NOT-U-001 a 004 | NOT-I-* | NOT-F-001 a 003 | SEC-S-007 | NOT-E-001,002 |
| Chatbot RAG                             | RAG-U-001 a 005 | RAG-I-* | RAG-F-001 a 003 | SEC-S-007 | RAG-E-001,002 |
| Vistas Web nuevas                       | — | WEB-I-* | WEB-F-001 a 005 | SEC-S-001 a 008 | WEB-E-001,002 |
| Vistas Móviles nuevas                   | — | MOB-I-* | MOB-F-001 a 006 | SEC-S-001 | MOB-E-001,002 |

---

*Documento generado para HydroEspinaca v2 — Continuación de proyecto. Cubre todas las funcionalidades originales (v1) y las 7 nuevas funcionalidades del plan de continuación aprobado. Se recomienda ejecutar este plan de pruebas completo antes de la presentación final del libro de proyecto.*
