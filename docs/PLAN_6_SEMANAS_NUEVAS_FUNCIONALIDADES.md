# HydroEspinaca — Plan de 7 semanas (Nuevas funcionalidades)

Fecha: 2026-02-11  
Horizonte: **7 semanas de desarrollo** (con colchón) + semanas 8-10 para calidad, artículo y documentación.

## 0) Contexto (lo que ya existe y se debe respetar)

**Arquitectura actual (confirmada en el repo):**
- Microservicios:
  - **.NET 9**: `auth-service`, `bff-service`, `sensor-service`, `actuator-service`, `notification-service`
  - **Python 3.12 + FastAPI**: `fuzzy-service` (Clean Architecture + CQRS con Medyator)
- Persistencia: **MongoDB**
- Mensajería: **MQTT**
- Frontends:
  - Web: **Next.js** (`apps/web`) consumiendo al BFF
  - Mobile: **React Native (Expo)** (`apps/mobile`) consumiendo al BFF
- Reverse proxy: **Nginx** ya configurado en `docker-compose.yml`

**Observaciones útiles para planear:**
- El BFF ya tiene endpoints y cache para:
  - `GET /weather` (OpenWeather actual, cache por hora)
  - `GET /system/status` (status consolidado)
  - `GET /system/fuzzy-rules` (resumen de reglas, cache 24h)
- `actuator-service` ya expone `POST /api/commands/analytics` y persiste `routine_commands` con `TotalDurationSeconds`, lo cual es una base fuerte para cálculo de costos/consumos.

**Principios no negociables (según tu requerimiento):**
1. Mantener **Clean Architecture** por servicio.
2. Mantener **CQRS** (Commands/Queries + Handlers) donde aplique.
3. **BFF como intermediario** para web/móvil (API lista para UI).
4. Nginx y perfiles Docker bien configurados (dev/prod).
5. Seguridad: JWT, sesiones del BFF, scopes/policies.

---

## 1) Objetivo del ciclo (7 semanas)

Entregar 5 bloques funcionales, integrados end-to-end (backend + BFF + web) y con soporte parcial en móvil (prioridad al final):

1) **Personalización y análisis de rutinas Fuzzy** (multi-rutina, activar/alternar, comparar objetivamente)
2) **Módulo de análisis de consumo y rentabilidad** (kWh/agua/nutrientes, costos, costo/kg)
3) **Integración meteorológica mejorada** (pronóstico + alertas proactivas)
4) **Mejora de notificaciones** (push y/o WhatsApp; mínimo viable definido)
5) **Asistente inteligente (Chatbot) con RAG** (ayuda para crear rutinas y responder dudas sobre datos)

## 1.1 Alcance cerrado (decisiones ya tomadas)

- **Invernadero:** solo existe **1** (no se modela multi-tenant ni `greenhouseId`).
- **Rutinas Fuzzy:** son **globales** para el invernadero, pero se prefiere **conservar `ownerUserId`** como metadato para control futuro por roles.
- **Consumos:** por ahora serán **registros manuales** ingresados por usuario (incluye electricidad/agua/nutrientes).
- **Electricidad:** hay medición **real** del consumo del invernadero con un **conector inteligente** (kWh global). Esto se modela como entrada manual (y luego se puede automatizar).
- **Notificaciones:** objetivo 6 semanas = **Email + Push + WhatsApp**.
- **Asistente:** debe poder **crear** un sistema/rutina fuzzy al final, tras confirmación explícita del usuario, y sugerir ediciones/borrado.
- **LLM:** preferencia por **API** (por viabilidad). Se deja diseño “pluggable” para poder correr local con GPU (RTX 4060 Laptop) más adelante.

---

## 2) Definición de “DONE” (por funcionalidad)

### 2.1 Rutinas Fuzzy (personalización + análisis)
**DONE cuando:**
- El usuario puede **crear/editar/duplicar** rutinas (reglas + funciones de membresía + umbrales de acción).
- El usuario puede **activar** una rutina como “la vigente” (y el sistema usa esa rutina para control automático).
- El sistema registra qué rutina se usó para cada evaluación/periodo (trazabilidad).
- Existe una vista web para:
  - administrar rutinas
  - comparar 2+ rutinas por periodo (KPIs definidos abajo)

**KPIs sugeridos para comparar rutinas (objetivo, no “opinión”):**
- Estabilidad por variable: desviación estándar / % tiempo en rango óptimo (usa aggregates del sensor-service si están).
- Recursos: runtime total por actuador (de `actuator-service` analytics) y “% tiempo activo”.
- Alertas: conteo de alertas por periodo (sensor alerts / esp32 offline).

### 2.2 Consumo + rentabilidad
**DONE cuando:**
- Se pueden registrar:
  - costo de electricidad (por kWh)
  - costo de agua (por litro o m³)
  - costo de solución nutritiva (por litro)
  - rendimiento (kg producidos) por ciclo
- El sistema calcula:
  - consumo eléctrico desde **registro manual** (kWh del conector inteligente) por rango o por ciclo
  - consumos de agua/nutrientes (manual; estimación por caudal queda como mejora)
  - costo total del ciclo
  - costo por kilogramo
- Existe Dashboard web: consumo total + costo total + costo/kg + series temporales.

### 2.3 Meteorología (pronóstico + alertas)
**DONE cuando:**
- Existe endpoint BFF para pronóstico (ej. 48h/7d) + cache.
- Existen alertas proactivas configurables (ej. heladas, calor extremo, lluvia fuerte) y se notifican por canales disponibles.
- Vista web con:
  - pronóstico
  - configuración de umbrales
  - historial de alertas

### 2.4 Notificaciones (push/WhatsApp)
**DONE cuando:**
- El `notification-service` soporta al menos **2 canales** (Email + Push) o (Email + WhatsApp) con feature flags.
- Hay pantalla de configuración en web (tokens/dispositivos / preferencias / silencios).
- Se pueden enviar notificaciones desde otros servicios vía M2M.

### 2.5 Asistente Inteligente (RAG)
**DONE cuando:**
- Existe un servicio (nuevo) que permite:
  - consultas conversacionales
  - sugerencias guiadas para crear una rutina fuzzy (plantillas + validaciones)
  - responder dudas con referencias a datos agregados (no PII)
- Integrado en Web (UI chat) y, si alcanza el tiempo, integrado en móvil.

---

## 3) Decisiones de arquitectura (recomendación)

### 3.1 ¿“Rutina” = FuzzySystem o entidad nueva?
**Recomendación:** tratar cada “rutina” como un **Fuzzy System versionable** (porque `fuzzy-service` ya soporta múltiples sistemas con variables/términos/reglas).

Como el alcance es **1 invernadero** y rutinas **globales**, se simplifica:
- Se mantiene `status` (ACTIVE/INACTIVE) pero se garantiza regla: **solo 1 sistema ACTIVE**.
- Se conserva `ownerUserId` como **metadato opcional** (útil si más adelante algunos roles no deben ver/editar ciertos sistemas).
- Versionado:
  - agregar `version` (int/string) y usar `updatedAt` existente como auditoría temporal.
  - no usar `parentSystemId` (evita complejidad y acoplamiento innecesario).

### 3.2 Nuevo microservicio para BI (consumo/rentabilidad)
**Recomendación:** crear un microservicio nuevo en `.NET 9` dentro de `software-project/`:
- nombre sugerido: `bi-service` o `profitability-service`
- razón: el dominio de costos/ciclos y reportes de rentabilidad es **negocio**, no fuzzy.
- se integra por HTTP con:
  - `actuator-service` (analytics)
  - `sensor-service` (aggregates/alerts)

**Persistencia:** MongoDB (colecciones propias).

### 3.3 Meteorología: on-demand en BFF + alertas con worker
Actualmente el BFF ya hace `GET /weather` (OpenWeather + cache). Hay 2 caminos:

- **Opción A (mantener en BFF):** lo más rápido; el BFF sigue encapsulando “cosas de UI” (composition + cache).
- **Opción B (recomendada por separación):** crear un `weather-service` dedicado y dejar al BFF como **gateway** (el frontend no cambia).

**Mi opinión:** si tu objetivo es “cada servicio solo lo suyo”, **sí es conveniente moverlo** a `weather-service`, pero manteniendo el **endpoint público en BFF** para no romper web/móvil.

**Dificultad real de migración (moderada, no alta):**
1) Crear `weather-service` (.NET 9) con su propio `WeatherController` + `OpenWeatherClient` + cache.
2) Agregarlo a `docker-compose.yml` y a Nginx (solo tráfico interno; BFF lo consume por red docker).
3) Cambiar el BFF para que su `IWeatherService` llame a `weather-service` (en vez de llamar a OpenWeather directo).
4) Mantener los contratos `GET /weather` y el modelo `WeatherDto` intactos → UI no se toca.

**Alertas meteorológicas:** ponerlas en `weather-service` (HostedService) y que dispare notificaciones (push/email/whatsapp) vía `notification-service`.

### 3.4 Notificaciones: evolucionar `notification-service` a “multi-channel”
`notification-service` hoy es email robusto. Extenderlo a multi-canal con una abstracción:
- `INotificationSender` por canal (EmailSender, PushSender, WhatsAppSender)
- entidad `NotificationMessage` con `Channel`, `Template`, `Recipient`, `Payload`

**Canales objetivo (según tu decisión):** Email + Push + WhatsApp.

Recomendación realista por tiempo:
- Push: **Expo Push Notifications** (encaja con Expo RN, rápido de integrar).
- WhatsApp: implementar adaptador para **Twilio WhatsApp** (rápido para prototipo) y dejar interfaz lista para **Meta WhatsApp Cloud API** si luego quieres migrar.
  - En entorno dev se puede usar Twilio Sandbox.
  - En prod, lo ideal para institución suele ser Cloud API (depende de verificación).

### 3.5 Asistente RAG: servicio Python nuevo
**Recomendación:** nuevo servicio en Python (FastAPI) por afinidad con ecosistema de IA, sin contaminar micros .NET.
- nombre sugerido: `assistant-service`
- vector DB recomendada: **Qdrant** en Docker (simple y productivo)

LLM “pluggable”:
- `LLM_PROVIDER=openai|azure_openai|ollama`
- Si es `ollama`, se puede correr local en tu laptop con RTX 4060 (cuantizado) para demos o investigación.

**Fuentes de conocimiento para RAG (MVP realista):**
1) Manual/Docs del proyecto (README y docs internos) indexados.
2) Rutinas fuzzy del usuario (nombres, variables, reglas, membresías) indexadas.
3) Resúmenes agregados (no series crudas) del sensor-service y actuator analytics.

---

## 4) Pantallas a desarrollar (Web primero)

### 4.1 Rutinas Fuzzy
- Lista de rutinas (con filtros: activas/inactivas, plantillas, por usuario).
- Editor de rutina:
  - variables (entrada/salida)
  - términos + funciones de membresía
  - reglas (builder)
  - “validación” (simular con lecturas históricas)
- Acciones: duplicar, exportar/importar JSON, activar.

### 4.2 Comparador de rutinas
- Selección: rutina A vs rutina B + rango de fechas.
- KPIs: estabilidad, runtime por actuador, alertas.
- Recomendación: exportar PDF/CSV.

### 4.3 Rentabilidad
- Configuración de costos (kWh, agua, nutrientes) y perfiles de actuadores (W, L/min).
- Ciclos de cultivo: crear/editar/cerrar ciclo, registrar kg producidos.
- Dashboard:
  - costo total, costo/kg
  - consumo total (kWh, L)
  - breakdown por actuador y por periodo

### 4.4 Clima
- Pronóstico 48h/7d
- Alertas meteorológicas configurables

### 4.5 Notificaciones
- Preferencias por canal
- Registro de dispositivo móvil (expoPushToken)
- Historial de envíos

### 4.6 Asistente (Chat)
- UI tipo chat
- “Modo guía” para crear rutina fuzzy (preguntas paso a paso)
- “Modo análisis” (preguntas sobre costos/consumos/alertas)

**Móvil:** replicar después de que los endpoints estén estables (ideal semana 6).

---

## 5) Backlog sugerido (orden y por qué)

Orden recomendado para minimizar retrabajo:
1) **Rutinas Fuzzy completas** (CRUD + activar + simular) → es el núcleo de experimentación y lo usará el asistente.
2) **Fundación BI** (ciclos + consumos manuales + costos) → habilita rentabilidad rápido, aunque aún no existan potencias/caudales.
3) **Notificaciones Push** → se reutiliza para meteo y eventos BI.
4) **Meteorología avanzada** → se apoya en notificaciones.
5) **WhatsApp** (canal adicional) → se apoya en notification-service ya extendido.
6) **RAG** al final → se apoya en rutinas + BI para ser realmente útil.

---

## 6) Cronograma de 7 semanas (con semana 8-10)

Capacidad aproximada (según tu mensaje):
- **Semana 1:** 3 días × 10h = **30h**
- **Semanas 2–7:** 6 días × 10h = **60h/semana**
- **Semana 8:** calidad (SonarQube, tests, refactor, hardening)
- **Semanas 9–10:** documentación + artículo científico + ponencia

> Suposición: 6 semanas “full-time” con foco backend+web. Si el tiempo real es parcial, hay que recortar alcance (especialmente WhatsApp y RAG).

### Semana 1 (30h) — Arranque BI + bases de contrato
**Objetivo:** dejar el `bi-service` creado, con auth, persistencia y 1 vertical slice funcionando desde web→BFF→BI.
- Rutinas fuzzy:
  - confirmar modelo: rutina = `FuzzySystem`
  - definir (en docs) los campos mínimos: `version`, `ownerUserId?`, `updatedAt`

- **BI Service (scaffold e inicio real):**
  - crear solución `.NET 9` con Clean Architecture + CQRS
  - MongoDB repo base (patrón similar a otros servicios)
  - endpoints mínimos: health + `CostConfig` (GET/PUT)
  - entidad `ManualConsumptionEntry` (electricidad kWh, agua L/m³, nutrientes L)
  - 1 query “summary” por rango (para validar cálculo base)

- **BFF:**
  - agregar cliente HTTP a `bi-service`
  - exponer `GET/PUT /bi/cost-config` y `POST/GET /bi/consumption-entries`

**Entregable:**
- Guardar configuración de costos y 1 registro manual desde la UI (o Postman) y ver summary.

### Semana 2 — Rutinas Fuzzy completas + primeras pantallas BI
**Objetivo:** que el agricultor pueda experimentar.
- `fuzzy-service`:
  - CRUD de rutinas por usuario (apoyado en endpoints existentes)
  - duplicar/exportar/importar
  - endpoint “simulate” (evaluar con lecturas históricas o sample payload)
- BFF:
  - endpoints BFF para rutinas (evitar que web pegue directo al fuzzy-service)
- Web:
  - UI de administración de rutinas (lista + activar + duplicar)

**Entregable:**
- Rutinas administrables desde web con seguridad de sesión.

### Semana 3 — Comparador de Rutinas + Dashboard BI v1 (manual)
**Objetivo:** análisis objetivo.
- `bi-service`:
  - integrar con `actuator-service` analytics
  - integrar con `sensor-service` aggregates/alerts
  - endpoints para KPIs por periodo y por rutina
- BFF:
  - endpoints “composed” para comparador
- Web:
  - vista comparador (A/B + KPIs + charts)

**Entregable:**
- Comparador A/B funcionando con datos reales.

### Semana 4 — Rentabilidad (ciclos + costo/kg) + electricidad por conector
**Objetivo:** dashboard de rentabilidad usable.
- `bi-service`:
  - CRUD de `CostConfig`
  - CRUD de `CultivationCycle` (kg producidos)
  - cálculo costo total + costo/kg por ciclo
- Web:
  - pantallas de configuración + dashboard de rentabilidad

**Entregable:**
- Costo/kg calculado para un ciclo y visible en dashboard.

### Semana 5 — Weather-service + meteorología avanzada + push
**Objetivo:** pronóstico + alertas proactivas.
- BFF:
  - endpoint forecast (cache)
- `bi-service`:
  - worker programado para evaluar pronóstico y crear alertas
  - integración con `notification-service`
- `notification-service`:
  - agregar canal Push (Expo) como MVP
  - endpoint para registrar `expoPushToken` por usuario
- Web:
  - vista de pronóstico + configuración de alertas

**Entregable:**
- Alerta meteo → notificación push (y email si quieres redundancia).

### Semana 6 — Notification multi-channel (push+whatsapp) + alertas
### Semana 7 — Asistente RAG + mobile pass
**Objetivo:** cerrar el ciclo con IA y llevar lo esencial a móvil.
- `assistant-service`:
  - pipeline de indexación a Qdrant
  - endpoints chat + modo “guía de rutina”
  - seguridad: solo datos del usuario (y rate limits)
- Web:
  - UI chat integrada
- Móvil:
  - mínimo: pantallas de rentabilidad/resumen rutinas + notificaciones push
  - usar `@hydroespinaca/shared` para consumo BFF

### Semana 8 — Calidad (SonarQube + tests + hardening)
- Subir cobertura de tests en BFF/BI/Notification.
- SonarQube: code smells, duplicación, complejidad ciclomática.
- Resiliencia: timeouts, retries (Polly) donde aplique.
- Observabilidad: logs y trazas de requests críticos.

### Semanas 9–10 — Artículo + documentación + ponencia
- Documentación técnica por servicio (APIs, modelos, despliegue, seguridad).
- Guía de usuario (rutinas fuzzy, rentabilidad, alertas).
- Material de ponencia: narrativa, resultados, capturas, métricas.

**Entregable:**
- Chat funcional en web y push en móvil.

---

## 7) Riesgos y mitigaciones

- **Riesgo:** RAG se vuelve muy grande (embeddings, costos, prompts, privacidad).  
  **Mitigación:** MVP con 3 fuentes y respuestas sobre *agregados*, no series crudas.

- **Riesgo:** WhatsApp consume mucho tiempo (verificación, plantillas, límites).  
  **Mitigación:** feature flag; priorizar Push.

- **Riesgo:** Costo/consumo requiere “potencias/caudales” que hoy no existen.  
-  **Mitigación:** empezar con consumos **manuales**; agregar `ActuatorProfile` opcional después cuando tengas potencias/caudales.

- **Riesgo:** multi-invernadero / multi-tenant no definido.  
  **Mitigación:** definir temprano si habrá `greenhouseId` en todo.

---

## 8) Respuestas registradas (para no perder el hilo)

1) Solo hay **1 invernadero**.
2) Rutinas fuzzy son **globales** para el invernadero.
3) Consumos: por ahora **manuales** (ingresados por usuario).
4) Potencias/caudales: **pendiente** (no bloquear el módulo de rentabilidad).
5) Notificaciones: objetivo = **Email + Push** y **probablemente WhatsApp** (se planifica para completar en 6 semanas).
6) LLM: preferible por **API**; se deja posibilidad de local (GPU) como mejora.
7) Asistente: debe **crear** sistema/rutina fuzzy tras confirmación y sugerir cambios.

## 9) Contrato de APIs propuesto (para implementar sin romper BFF)

> Nota: los paths son sugeridos; la idea es que Web/Mobile solo hablen con BFF, y BFF orqueste.

### 9.1 BFF Service (nuevos endpoints)

- Rutinas fuzzy (proxy/orquestación)
  - `GET /fuzzy/routines` (lista)
  - `POST /fuzzy/routines` (crear)
  - `PUT /fuzzy/routines/{id}` (actualizar)
  - `POST /fuzzy/routines/{id}/clone`
  - `POST /fuzzy/routines/{id}/activate`
  - `POST /fuzzy/routines/{id}/simulate` (opcional)

- BI / Rentabilidad
  - `GET /bi/cycles`
  - `POST /bi/cycles`
  - `PUT /bi/cycles/{id}`
  - `POST /bi/cycles/{id}/close` (setea fecha fin)
  - `GET /bi/cost-config` / `PUT /bi/cost-config`
  - `GET /bi/consumption-entries?from&to`
  - `POST /bi/consumption-entries`
  - `GET /bi/profitability/summary?cycleId=...` (costo total, costo/kg)

- Clima
  - `GET /weather/forecast?hours=48` (nuevo; cacheado)
  - `GET /weather/alerts` (historial)
  - `PUT /weather/alerts/config` (umbrales)

- Asistente
  - `POST /assistant/chat` (streaming si se puede; si no, normal)
  - `POST /assistant/actions/preview` (devuelve “plan” de cambios)
  - `POST /assistant/actions/commit` (ejecuta create/update con confirmación)

### 9.2 Fuzzy Service (extensiones mínimas)

- Activación única
  - `POST /api/fuzzy-systems/{id}/activate` (desactiva otras, deja 1 ACTIVE)

- Export/Import (si no existe)
  - `GET /api/fuzzy-systems/{id}/export`
  - `POST /api/fuzzy-systems/import`

- Simulación (opcional)
  - `POST /api/fuzzy-systems/{id}/simulate` (entrada: lecturas; salida: comandos + métricas)

### 9.3 BI Service (nuevo)

- `GET/POST/PUT` para ciclos, costos, consumos manuales.
- Query de rentabilidad (summary) y series.
- Worker opcional para alertas meteorológicas (si se decide allí).

### 9.4 Notification Service (extensiones)

- Push:
  - `POST /api/push/register` (expoPushToken asociado a usuario)
  - `POST /api/notifications/push` (enviar push)
- WhatsApp:
  - `POST /api/notifications/whatsapp` (enviar; Twilio/Cloud API según config)
- Preferencias:
  - `GET/PUT /api/notification-preferences` (canales, silencios)

## 10) Checklist semanal (más accionable)

### Semana 1
- Especificación de entidades/colecciones finales (fuzzy + BI + notificaciones).
- Implementar `activate routine` en fuzzy-service y exponerlo en BFF.
- Crear scaffolding `bi-service` con healthcheck + auth M2M.
- Crear páginas web vacías: Rutinas, Comparador, Rentabilidad, Clima, Asistente.

### Semana 2
- CRUD rutinas desde Web vía BFF.
- Clone + export/import JSON.
- Validaciones (no romper reglas/variables existentes).

### Semana 3
- Implementar KPIs comparador (mínimo 2 métricas: runtime por actuador + alertas).
- Vista comparador con rango de fechas.

### Semana 4
- Rentabilidad: ciclos + costos + consumos manuales.
- Dashboard: costo total + costo/kg + breakdown.

### Semana 5
- Push end-to-end: RN registra token → notification-service envía → usuario recibe.
- Forecast + alertas proactivas (y notificar por push/email).

### Semana 6
- WhatsApp end-to-end (Twilio) + preferencia de canal.
- Assistant-service (RAG) + UI chat + acción: crear rutina fuzzy con confirmación.
- Mobile pass: pantallas mínimas + push.

---

## 11) Próximo paso inmediato

Si quieres, el siguiente paso es que yo cree un segundo documento: **diseño de datos** (colecciones Mongo por servicio + ejemplos de DTOs), para que implementar sea casi mecánico.
