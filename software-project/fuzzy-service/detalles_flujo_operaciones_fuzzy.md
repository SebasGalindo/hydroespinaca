Arquitectura del Sistema de Control con Lógica Difusa

Equipo de Desarrollo

28 de agosto de 2025

---

## 1. Introducción

Este documento detalla la arquitectura y el flujo de control del sistema de lógica difusa, un motor que combina la flexibilidad del razonamiento fuzzy con mecanismos de seguridad y una ejecución determinista. El sistema está diseñado para operar en ciclos de un minuto, coordinando tres componentes principales: el **fuzzy-service**, el **actuator-service**, y el **firmware en el ESP32**.

El objetivo principal es asegurar un control flexible, seguro y determinista, donde el fuzzy-service actúa como el cerebro interpretativo, el actuator-service garantiza la correcta ejecución y secuencialidad de los comandos, y las reglas de control directas funcionan como un cinturón de seguridad.

---

## 2. Flujo de Operación General

El sistema de control basado en lógica difusa (Mamdani puro) funciona en ciclos de un minuto. En cada ciclo, se recogen y validan los datos de los sensores. Con esos valores, el fuzzy-service evalúa el conjunto de reglas difusas. Cada regla tiene como antecedente una combinación de condiciones sobre variables de entrada y como consecuente una rutina, es decir, un conjunto de acciones sobre uno o varios actuadores.

Estas acciones llegan resueltas con parámetros concretos: **pin físico, potencia (ON/OFF o PWM)** y un **tiempo de expiración absoluto** en lugar de una duración relativa.

Ejemplo: Un comando se interpreta como *“mantener ON hasta T = now + X”* y no como *“mantener ON durante X”*.

### 2.1. Mecanismos de Control y Prioridad

* **Motor Difuso:** El fuzzy-service registra en su log qué reglas se dispararon, qué valores de sensores se usaron y qué acciones se derivaron. Luego envía al actuator-service un payload con dichas acciones.
* **Reglas de Seguridad:** Estas tienen prioridad absoluta sobre cualquier rutina fuzzy. Ejemplos:

  * Si TemperaturaAgua > 30°C → Apagar el calefactor de agua.
  * Si HumedadRelativa > 90% → Apagar el humidificador.
  * Si NivelAgua < MínimoSeguro → Apagar la motobomba.
* **Manejo de Actuadores:** El actuator-service mantiene una cola por actuador. Un comando nuevo reemplaza al anterior si aún no se ejecuta, o ajusta la potencia/expiración si ya está en curso.

### 2.2. Coordinación de Rutinas

* **Extensión de Rutinas:** Si una rutina activa tiene varios pasos y el primero sigue activo, puede extenderse si es del mismo tipo.
* **Gestión de Recursos:** Una rutina que requiere un actuador ocupado debe esperar en la cola.
* **Condiciones de Activación:** Si un sensor está desactivado, las reglas asociadas se suspenden. La iluminación artificial se activa tras varias lecturas consecutivas y se apaga forzosamente a las 20:00 h.

---

## 3. Ciclo de Vida de una Rutina

### 3.1. Evaluación Fuzzy

Cada minuto, el fuzzy-service evalúa sensores y dispara reglas, generando rutinas completas. Estas se envían al actuator-service vía POST.

### 3.2. Registro de un sistema fuzzy

```json
{
  "_id": "fuzzy_1",
  "name": "Vegetativo Día",
  "status": "in_use",
  "operators": { "and": "min", "or": "max", "not": "complement" },
  "defuzzMethod": "centroid",
  "variables": ["lsdkfjladskfv23", "oirlskdfl2314"],
  "rules": ["aofiualekfj", "aofiualekfj34342"],
  "createdBy": "fuzzy-service",
  "createdAt": "20250812T100000Z",
  "updatedAt": "20250812T150000Z"
}
```

### 3.3. Registro de Variables en fuzzy

```json
{
  "_id": "var_temp_air",
  "systemId": "fuzzy_1",
  "name": "temp_air",
  "type": "input",
  "sensor_id": "5783qhgg445tu",
  "termIds": ["term_temp_low", "term_temp_ok", "term_temp_high"]
}
```

### 3.4. Registro de etiquetas de una variable fuzzy

```json
{
  "_id": "term_temp_high",
  "label": "high",
  "mf": {
    "type": "trapezoid",
    "params": [28, 30, 40, 40]
  }
}
```

### 3.5. Registro en actuator-service

El actuator-service consulta detalles de cada actuador, valida coherencia y crea un registro inicial en *scheduled*.

### 3.6. Ejecución en Firmware (ESP32)

El firmware gestiona conflictos y extensiones de rutinas. Si una rutina es modificada, actualiza temporizadores y parámetros.

### 3.7. Finalización de la Rutina

```json
{
  "routineId": "recirculaAgua_20250825T1500",
  "status": "completed",
  "finishedAt": "2025-08-25T15:01:30Z",
  "results": [
    { "pin": 12, "status": "ok" },
    { "pin": 27, "status": "cancelled" }
  ],
  "logs": [
    "Se extendio el tiempo del paso 2 a 15:02 por rutina X",
    "Se cancelo paso 3 porque rutina Y lo sobrescribió"
  ]
}
```

### 3.8. Actualización del Log

El actuator-service actualiza el estado final y logs de la rutina, cerrando el ciclo.

---

## 4. Estructuras de Datos y Contratos entre Servicios

### 4.1. Estructura de Actuadores en la Base de Datos

```json
{
  "_id": { "$oid": "68ab8bb77b05e378b3731341" },
  "Esp32Id": "6883fff7b079309f3ba4f238",
  "Name": "Ventiladores 12V",
  "Type": "Fan",
  "PhysicalId": "FANS-001",
  "Pin": 16,
  "Mode": "PWM",
  "Status": "Active",
  "Location": "rack-1",
  "CreatedAt": { "$date": { "$numberLong": "1756072887865" } }
}
```

### 4.2. Estructura de Reglas (fuzzy-service)

```json
{
  "ruleId": "aofiualekfj",
  "name": "low_ec",
  "systemId": "fuzzy_1",
  "description": "Si EC es baja, activar recirculación de agua",
  "conditions": [
    { "sensor": "EC", "operator": "IS", "value": "Baja" },
    { "sensor": "NivelAgua", "operator": "NOT", "value": "Bajo" }
  ],
  "conector": ["AND"],
  "consequent": "RecirculaAgua",
  "createdAt": "2025-08-25T15:00:00Z"
}
```

### 4.3. Estructura de una rutina (fuzzy-service)

```json
{
  "routineId": "aofuhakjfbv123124325wsf",
  "routine_name": "Calentar Invernadero",
  "steps": [
    {
      "actuator": "68ab8bb77b05e378b3731341",
      "power_tag_id": "askfjalkrjesf34",
      "duration_tag_id": "askfjalkrjesf34"
    }
  ]
}
```

### 4.4. Payload recibido por actuator-service

```json
[
  {
    "routineId": "recirculaAgua",
    "steps": [
      { "actuator": "68ab8bb77b05e378b3731341", "power": "ON", "expiration": "2025-08-25T15:01:00Z" },
      { "actuator": "68ab8bb77b3123378b37311212", "dutyCycle": 70, "expiration": "2025-08-25T15:02:00Z" }
    ]
  }
]
```

### 4.5. Esquema de registro en fuzzy-service

```json
{
  "evalId": "uuid-12345",
  "systemId": "fuzzy-v1",
  "timestamp": "2025-08-25T15:00:00Z",
  "inputs": {
    "ph": 6.5,
    "ec": 1.2,
    "air_temp": 28.3,
    "humidity": 55.2,
    "lux": 11000,
    "water_level": 12.5,
    "water_temp": 22.0
  },
  "activated_rules": [
    {
      "Rule ID": "calentarAmbiente",
      "firingStrength": 0.8,
      "output_values": [
        { "actuator_id": "adfjaldskf", "Power": 70, "Duration": 80 }
      ]
    }
  ]
}
```

### 4.6. Esquema de registro en actuator-service

```json
{
  "commandId": "recirculaAgua_20250825T1500",
  "routineId": "recirculaAgua",
  "statusGeneral": "scheduled",
  "createdAt": "2025-08-25T15:00:00Z",
  "finishedAt": "2025-08-25T15:01:30Z",
  "results": [
    { "pin": 12, "status": "ok" },
    { "pin": 27, "status": "cancelled", "executionLog": "Rutina de control 'apagar-bomba' interrumpió a los 12s" }
  ]
}
```

### 4.7. Payload al firmware ESP32

```json
{
  "esp32Id": "6883fff7b079309f3ba4f238",
  "jobSchedule": [
    {
      "queue": [
        {
          "commandId": "recirculacion-dd/mm/aaaa hh.mm.ss",
          "steps": [
            { "pin": 16, "power": 80, "duration": 30 }
          ]
        }
      ]
    }
  ]
}
```

### 4.8. Confirmación del Firmware

```json
{
  "esp32Id": "6883fff7b079309f3ba4f238",
  "commandId": "recirc-20250828T150000",
  "steps": [
    { "pin": 16, "status": "ok" },
    { "pin": 25, "status": "cancelled", "executionLog": "Rutina 'apagar-bomba' interrumpió a los 12s" }
  ]
}
```

---

## 5. Conclusiones

La arquitectura propuesta separa claramente las responsabilidades:

* El **fuzzy-service** define la lógica de alto nivel.
* El **actuator-service** orquesta y registra las rutinas.
* El **firmware en ESP32** ejecuta en tiempo real.

Este diseño modular garantiza **flexibilidad, seguridad y trazabilidad completa**, mejorando la solidez del sistema mediante comunicación enriquecida entre servicios.

---

## 5. Flujo técnico MQTT → FuzzyEngine → Actuator (mapa de código)

- PASO 1: Establecer conexión MQTT (solo lectura) — ver <mcfile name="MqttClient.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\ExternalServices\MqttService\MqttClient.py"></mcfile>
- PASO 2: Suscribirse y recibir lecturas/batches — ver <mcfile name="MqttSubscriber.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\ExternalServices\MqttService\MqttSubscriber.py"></mcfile>
- PASO 3: Manejar mensajes y disparar evaluación — ver <mcfile name="MqttMessageHandler.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\ExternalServices\MqttService\MqttMessageHandler.py"></mcfile>
- PASO 4: Evaluar reglas y obtener salidas — servicio de aplicación <mcsymbol name="evaluate" filename="FuzzyEngineService.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Application\Services\FuzzyEngineService.py" startline="66" type="function"></mcsymbol> (ver análisis en <mcfile name="ANALISIS_CODIGO_FUZZY_ENGINE.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\ANALISIS_CODIGO_FUZZY_ENGINE.md"></mcfile>)
- PASO 5: Transformar salidas a rutinas/actuadores y enviar — ver <mcfile name="ActuatorServiceClient.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\ExternalServices\ActuatorService\ActuatorServiceClient.py"></mcfile>

Estado actual:
- MQTT (cliente/suscriptor/handler): archivos presentes pero con implementación pendiente (solo lectura; sin publish).
- Motor Fuzzy: Implementado y funcional en <mcfile name="ScikitFuzzyEngine.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\FuzzyEngine\ScikitFuzzyEngine.py"></mcfile> y motores auxiliares (fuzzificación, evaluación de reglas, agregación, defuzzificación).
- Cliente de Actuadores: presente pero sin implementación (HTTP via httpx planeado).

## 6. Librerías y métodos utilizados (núcleo actual)

- Numpy/SciPy: usados en defuzzificación para integrar y calcular centroides (por ejemplo, numpy.trapz en <mcfile name="DefuzzificationEngine.py" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\FuzzyService\Infrastructure\FuzzyEngine\DefuzzificationEngine.py"></mcfile>).
- asyncio-mqtt/paho-mqtt: declaradas en requirements; se planifica usar asyncio-mqtt para suscripción asíncrona (no hay código activo aún).
- httpx: cliente HTTP asíncrono planeado para actuator-service (aún sin código activo).
- FastAPI/Medyator/Kink: capa API y orquestación CQRS/DI ya presentes; el disparo de evaluaciones desde MQTT se integrará con Medyator.

Referencias:
- Estructura/estado del proyecto: <mcfile name="PLAN_IMPLEMENTACION_FUZZY_SERVICE.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\PLAN_IMPLEMENTACION_FUZZY_SERVICE.md"></mcfile>
- Análisis del motor: <mcfile name="ANALISIS_CODIGO_FUZZY_ENGINE.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\ANALISIS_CODIGO_FUZZY_ENGINE.md"></mcfile>

## 7. Crítica y mejoras sugeridas

- Completar la integración MQTT (solo lectura): implementar conexión resiliente, suscripción con QoS 1, validación de payloads y enrutamiento al handler. Añadir tests de integración con un broker de prueba.
- Implementar ActuatorServiceClient con httpx, reintentos y circuit breaker. Acordar contrato final de payload y estados devueltos.
- Definir idempotencia de mensajes (messageId/ts) y agrupación por ventanas para evitar evaluaciones redundantes.
- Añadir métricas y trazas: latencia de evaluación, tasa de reglas disparadas, errores por sensor.
- Revisar consistencia: el motor actual es robusto; documentar claramente que no usa directamente skfuzzy sino una implementación propia optimizada con Numpy/SciPy.

## 8. Dónde leer más

- Flujo detallado del motor y pipeline: <mcfile name="ANALISIS_CODIGO_FUZZY_ENGINE.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\ANALISIS_CODIGO_FUZZY_ENGINE.md"></mcfile>
- Estructura de carpetas y endpoints: <mcfile name="PLAN_IMPLEMENTACION_FUZZY_SERVICE.md" path="C:\Proyectos\hydroespinaca\software-project\fuzzy-service\PLAN_IMPLEMENTACION_FUZZY_SERVICE.md"></mcfile>
