# Diagramas de flujo pendientes por servicio

> **Convención de referencia:**
> - Los procesos de autenticación ya están descritos en los diagramas existentes.
> - Las operaciones de **lectura** (GET) referencian: _"Autenticación en procesos no mutables"_
> - Las operaciones de **escritura** (POST/PUT/PATCH/DELETE) referencian: _"Autenticación en procesos mutables"_
> - Las integraciones entre servicios se hacen con tokens M2M (service-to-service), no con sesión de usuario.

---

## 1. Weather Service

### 1.1 Consulta de clima actual y pronóstico

**Tipo:** No mutable (GET)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Weather-Service → OpenWeather API / MongoDB (caché)

**Diagrama de secuencia:**
- Usuario solicita vista de clima
- Frontend referencia _Autenticación en procesos no mutables_
- BFF-Service reenvía petición al Weather-Service
- Weather-Service consulta caché MongoDB (`ForecastCache`); si existe y no expiró (30 min), devuelve datos
- Si la caché expiró, Weather-Service llama a OpenWeather One Call 3.0 API
- Persiste el resultado en MongoDB y lo devuelve al BFF-Service
- BFF-Service organiza y devuelve la información al Frontend

**Diagrama de actividades (swimlanes: Frontend/Mobile | BFF-Service | Weather-Service | MongoDB | OpenWeather API):**
- Solicitud del usuario → Autenticación → Petición al Weather-Service
- Decisión: ¿Existe caché válida? → Sí: devuelve caché | No: llama a OpenWeather, actualiza caché, devuelve
- Frontend muestra datos de clima

---

### 1.2 Evaluación automática de alertas climáticas (Worker)

**Tipo:** Proceso background (sin interacción de usuario directa)
**Actores:** WeatherAlertEvaluationWorker → OpenWeather API → MongoDB → Notification-Service → Usuario

> Este es el flujo más importante del Weather-Service. Es un proceso periódico (cada 30 min).

**Diagrama de secuencia:**
- Worker se activa por temporizador
- Llama a OpenWeather One Call 3.0 API y persiste resultado en caché MongoDB
- Carga todas las configuraciones activas de alertas (`WeatherAlertConfig`) desde MongoDB
- Por cada configuración:
  - Evalúa datos diarios contra umbrales configurados (calor extremo, frío, humedad, nubosidad, viento, UV, tormentas)
  - Evalúa datos horarios para lluvia fuerte (ventanas de 3 horas)
  - Incluye alertas gubernamentales si las hay
  - Para cada alerta generada: verifica deduplicación (`ExistsRecentAsync`) antes de crear
- Llama a Notification-Service `GET /api/preferences/subscribers?fuzzySystemId=` para obtener usuarios suscritos
- Por cada usuario suscrito: llama a Notification-Service `POST /api/notifications/multi` con el grupo de alertas
- Registra la entrega en `AlertDeliveryLog`

**Diagrama de actividades (swimlanes: Worker | MongoDB | OpenWeather API | Notification-Service):**
- Timer activa Worker → Fetch pronóstico → Persistir caché
- Para cada `WeatherAlertConfig`: evaluar umbrales → ¿Alerta generada? → Verificar duplicado → ¿Duplicado? → Sí: omitir | No: crear alerta
- Obtener suscriptores → Para cada usuario: enviar notificación multi-canal → Registrar entrega

---

### 1.3 Configuración de umbrales de alertas climáticas

**Tipo:** Mutable (PUT) — también incluye seed inicial (POST)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Weather-Service → MongoDB

**Diagrama de secuencia:**
- Usuario edita umbrales en pantalla de configuración de alertas
- Frontend referencia _Autenticación en procesos mutables_
- BFF-Service reenvía `PUT /api/weather/alerts/config/{fuzzySystemId}` al Weather-Service
- Weather-Service valida y actualiza el documento `WeatherAlertConfig` en MongoDB
- Devuelve confirmación al Frontend

**Nota de seed:** Al crear un nuevo sistema fuzzy, se llama automáticamente a `POST /api/weather/alerts/config/{fuzzySystemId}/seed` para poblar los umbrales con valores por defecto. Este flujo es M2M (Fuzzy-Service → Weather-Service).

---

### 1.4 Consulta y lectura de alertas del usuario

**Tipo:** No mutable (GET) y mutable parcial (PATCH)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Weather-Service → MongoDB

**Diagrama de secuencia:**
- Usuario abre sección de alertas
- Frontend referencia _Autenticación en procesos no mutables_
- BFF-Service llama a `GET /api/weather/alerts` con filtros (fuzzySystemId, userId, rango de fechas, solo no leídas)
- Weather-Service consulta MongoDB y devuelve lista de alertas
- Al marcar como leída: `PATCH /api/weather/alerts/{alertId}/read` referencia _Autenticación en procesos mutables_

---

## 2. Chatbot Service

### 2.1 Envío de mensaje con pipeline RAG (flujo principal)

**Tipo:** Mutable (POST, respuesta SSE streaming)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Chatbot-Service → [Gemini API | MongoDB | Sensor-Service | Fuzzy-Service | Actuator-Service]

> Este es el flujo más importante del Chatbot-Service. Implementa un pipeline RAG de 8 pasos con streaming de tokens.

**Diagrama de secuencia:**
1. Usuario escribe mensaje en el chat
2. Frontend referencia _Autenticación en procesos mutables_
3. BFF-Service hace `POST /api/chat/stream/{sessionId}` al Chatbot-Service
4. Chatbot-Service carga la sesión y valida propiedad del usuario
5. Persiste el mensaje del usuario en MongoDB
6. Genera embedding del query enriquecido con historial (últimos 4 mensajes) via Gemini API
7. Realiza búsqueda vectorial en Atlas (`topK=5`) contra la base de conocimiento
8. En paralelo: obtiene lecturas de sensores, evaluaciones fuzzy recientes y estados de actuadores (via HTTP a Sensor-Service, Fuzzy-Service, Actuator-Service)
9. Ensambla el system prompt: prompt base + contexto vectorial + datos en vivo
10. Llama a Gemini API y transmite tokens como eventos SSE (`token`, `done`, `error`) al Frontend
11. Al finalizar: persiste respuesta completa en MongoDB; si es el primer intercambio, genera título automático de sesión

**Diagrama de actividades (swimlanes: Frontend/Mobile | BFF-Service | Chatbot-Service | Gemini API | MongoDB | Servicios externos):**
- Autenticación → Persistir mensaje usuario → Generar embedding
- Búsqueda vectorial → (En paralelo) Fetch datos en vivo de 3 servicios
- Ensamblar prompt → Stream tokens Gemini → Por cada token: enviar evento SSE al cliente
- Al terminar stream: persistir respuesta → ¿Primera interacción? → Sí: generar título de sesión

---

### 2.2 Gestión de sesiones de chat

**Tipo:** No mutable y mutable
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Chatbot-Service → MongoDB

**Diagrama de secuencia (crear sesión):**
- Usuario abre nueva conversación
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/chat/sessions` → Chatbot-Service crea documento de sesión vacío en MongoDB → Devuelve `sessionId`

**Diagrama de secuencia (listar sesiones / ver historial):**
- Frontend referencia _Autenticación en procesos no mutables_
- `GET /api/chat/sessions` → Lista sesiones paginadas del usuario
- `GET /api/chat/sessions/{sessionId}/messages` → Carga historial de mensajes

**Diagrama de secuencia (eliminar sesión):**
- Frontend referencia _Autenticación en procesos mutables_
- `DELETE /api/chat/sessions/{sessionId}` → Chatbot-Service realiza soft-delete (archiva sesión)

---

### 2.3 Sincronización de base de conocimiento RAG (M2M)

**Tipo:** M2M — disparado por Fuzzy-Service al cambiar entidades
**Actores:** Fuzzy-Service → Chatbot-Service → Gemini API → MongoDB (vector store)

**Diagrama de secuencia:**
- Fuzzy-Service detecta cambio en sistema/variable/regla
- Llama a `POST /api/rag/sync` con el chunk actualizado (o señal de eliminación)
- Chatbot-Service re-vectoriza el chunk usando Gemini Embeddings
- Actualiza o elimina el documento en MongoDB Atlas Vector Store
- Devuelve confirmación al Fuzzy-Service

---

### 2.4 Reindexación manual completa de la base de conocimiento

**Tipo:** Mutable (POST) — operación administrativa M2M
**Actores:** Administrador → Herramienta admin / BFF-Service → Chatbot-Service → Fuzzy-Service → Gemini API → MongoDB (vector store)

> Cubre el RF-C04 en su variante manual. Diferente al flujo 2.3 porque no procesa una entidad puntual sino la base completa.

**Diagrama de secuencia:**
- Administrador dispara la reindexación (operación poco frecuente, típicamente tras cambios masivos o despliegue inicial)
- La petición se autentica con credenciales máquina a máquina (`POST /api/rag/reindex`)
- Chatbot-Service consulta al Fuzzy-Service la totalidad de sistemas, variables, términos y reglas vigentes
- Por cada entidad obtenida: genera el chunk de texto, calcula su embedding via Gemini Embeddings y lo persiste en MongoDB Atlas Vector Store
- Marca como obsoletos o elimina los chunks cuyo `entityId` ya no exista en Fuzzy-Service
- Devuelve un resumen de la operación (entidades procesadas, errores, duración)

**Diagrama de actividades (swimlanes: Admin | Chatbot-Service | Fuzzy-Service | Gemini API | MongoDB):**
- Disparar reindexación → Autenticación M2M → Solicitar inventario completo a Fuzzy-Service
- Para cada entidad: generar chunk → calcular embedding → upsert en vector store
- Limpiar chunks huérfanos → Devolver reporte resumen

---

## 3. BI Service

### 3.1 Registro de consumo de recursos

**Tipo:** Mutable (POST)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → BI-Service → MongoDB

**Diagrama de secuencia:**
- Usuario ingresa un nuevo registro de consumo (agua, electricidad, nutrientes, otro)
- Frontend referencia _Autenticación en procesos mutables_
- BFF-Service reenvía `POST /api/bi/consumption-entries` al BI-Service
- BI-Service crea `ManualConsumptionEntry` en MongoDB
- Devuelve confirmación con el ID generado

**Diagrama de actividades (swimlanes: Usuario | Frontend/Mobile | BFF-Service | BI-Service | MongoDB):**
- Usuario completa formulario → Autenticación → Validar datos de entrada
- Crear entrada en MongoDB → Devolver resultado → Frontend actualiza lista

---

### 3.2 Registro de producción de cultivos

**Tipo:** Mutable (POST/DELETE)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → BI-Service → MongoDB

**Diagrama de secuencia:**
- Usuario registra ciclo productivo (nombre cultivo, fechas, kilos producidos, precio/kilo)
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/bi/production-records` → BI-Service crea `ProductionRecord` en MongoDB
- Devuelve confirmación

---

### 3.3 Cálculo de rentabilidad de un ciclo productivo

**Tipo:** No mutable (POST de cálculo — no persiste)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → BI-Service → MongoDB

> Este es el flujo más importante del BI-Service desde la perspectiva de negocio.

**Diagrama de secuencia:**
- Usuario selecciona un registro de producción y proporciona duraciones de actuadores e inversión inicial
- Frontend referencia _Autenticación en procesos no mutables_
- BFF-Service llama a `POST /api/bi/profitability/calculate`
- BI-Service carga el `ProductionRecord` desde MongoDB
- Carga la `CostConfigVersion` activa para obtener precios unitarios
- Calcula ingresos (kilos × precio/kilo)
- Calcula costos eléctricos de actuadores usando `CalculateOperationalCostQuery` internamente
- Suma costos de consumo manuales del período
- Suma inversión inicial
- Calcula margen de ganancia/pérdida y devuelve el reporte al Frontend

**Diagrama de actividades (swimlanes: Frontend/Mobile | BFF-Service | BI-Service | MongoDB):**
- Autenticación → Cargar registro de producción → Cargar configuración de costos activa
- Calcular ingresos brutos → Calcular costos operativos actuadores → Calcular costos de consumo
- Sumar inversión inicial → Calcular rentabilidad neta → Devolver reporte al frontend

---

### 3.4 Gestión de versiones de configuración de costos

**Tipo:** Mutable (POST/PUT/DELETE) y no mutable (GET)
**Actores:** Usuario administrador → Frontend → BFF-Service → BI-Service → MongoDB

**Diagrama de secuencia (crear versión):**
- Usuario crea nueva versión de precios (electricidad/kWh, agua/litro, nutrientes/litro)
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/bi/cost-config/versions` → BI-Service desactiva la versión anterior y crea la nueva como activa

---

### 3.5 Cálculo de costos operacionales de actuadores (consulta independiente)

**Tipo:** No mutable (POST de cálculo — no persiste)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → BI-Service → MongoDB

> Cubre el RF-B04. Aunque el cálculo también se invoca internamente desde 3.3 (rentabilidad), aquí se documenta el flujo cuando el usuario solo quiere conocer el costo eléctrico de los actuadores sin involucrar producción ni rentabilidad.

**Diagrama de secuencia:**
- Usuario selecciona un rango de fechas y proporciona las duraciones de operación de cada actuador (junto con su consumo en vatios)
- Frontend referencia _Autenticación en procesos no mutables_
- BFF-Service llama a `POST /api/bi/operational-cost/calculate`
- BI-Service carga las `CostConfigVersion` vigentes que solapan con el rango solicitado
- Distribuye proporcionalmente la duración de cada actuador entre las versiones aplicables
- Calcula kWh consumidos y costo total por actuador y agregado del período
- Devuelve el desglose al Frontend

**Diagrama de actividades (swimlanes: Frontend/Mobile | BFF-Service | BI-Service | MongoDB):**
- Autenticación → Validar entradas (rango y duraciones) → Cargar versiones de costos vigentes
- Distribución proporcional por período → Cálculo por actuador → Agregación total → Devolver respuesta

---

## 4. Notification Service

### 4.1 Envío de notificación multi-canal

**Tipo:** M2M interno — llamado por Weather-Service
**Actores:** Weather-Service → Notification-Service → [Email | Push Expo | Web Push | WhatsApp] → Usuario

> Este es el flujo más importante del Notification-Service. El `CompositeNotificationDispatcher` orquesta el envío.

**Diagrama de secuencia:**
1. Weather-Service llama a `POST /api/notifications/multi` con `{userId, templateKey, title, body, data}`
2. Notification-Service carga preferencias del usuario desde MongoDB (crea defaults si no existen)
3. Verifica horario silencioso (hora Colombia UTC-5): push y WhatsApp se omiten en horario silencioso; email siempre pasa
4. Intersecta canales habilitados por el usuario con canales permitidos por la petición
5. Para cada canal activo:
   - **email:** obtiene dirección → envía via SMTP/Resend → registra resultado en `NotificationLog`
   - **push:** obtiene tokens Expo del usuario → envía via Expo Push API → registra resultado
   - **web_push:** obtiene suscripciones web → envía via Web Push API → registra resultado
   - **whatsapp:** obtiene número → envía via Meta Cloud API / Twilio → registra resultado
6. Devuelve resumen de envíos al Weather-Service

**Diagrama de actividades (swimlanes: Caller | Notification-Service | MongoDB | Email/Push/WebPush/WhatsApp):**
- Recibir petición → Cargar preferencias de usuario
- Verificar horario silencioso → Determinar canales activos
- Para cada canal: resolver destinatario → enviar → registrar en log
- Devolver resultado

---

### 4.2 Resumen diario automático (Job programado)

**Tipo:** Proceso background (Quartz.NET, por usuario, en su hora/minuto configurado)
**Actores:** DailySummaryJob → Sensor-Service | Fuzzy-Service | Actuator-Service → Notification-Service (dispatcher) → Usuario

> Flujo importante porque integra múltiples servicios y puede enviarse por todos los canales del usuario.

**Diagrama de secuencia:**
1. Quartz.NET activa `DailySummaryJob` a la hora configurada del usuario
2. Lee preferencias del usuario; si resumen diario está deshabilitado, termina
3. `IDailySummaryDataAggregator.AggregateAsync` hace fetch en paralelo:
   - Sensor-Service: lecturas de las últimas 24h
   - Actuator-Service: historial de actuadores de las últimas 24h
   - Fuzzy-Service: evaluaciones fuzzy de las últimas 24h
4. `DailySummaryContentFormatter` formatea cuerpo HTML y texto plano
5. `ITemplateRenderer` renderiza el email HTML completo con Fluid templates
6. `ISanitizer` sanitiza el HTML
7. `INotificationDispatcher.DispatchAsync` envía a todos los canales de resumen diario configurados
8. Registra entrega en `NotificationLog`

**Diagrama de actividades (swimlanes: Quartz.NET | DailySummaryJob | Servicios externos | Dispatcher | Canales):**
- Timer activa Job → Verificar preferencias activas
- Fetch paralelo de datos de 3 servicios → Formatear contenido → Renderizar template HTML
- Sanitizar → Despachar por canales configurados → Registrar en log

---

### 4.3 Registro y gestión de suscripciones push

**Tipo:** Mutable (POST/DELETE)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Notification-Service → MongoDB

**Diagrama de secuencia (registrar):**
- Usuario inicia sesión en dispositivo nuevo o actualiza token
- Frontend/Mobile referencia _Autenticación en procesos mutables_
- `POST /api/push/register` con `{userId, platform, token, deviceName}` (plataformas: expo/android/ios, web_push)
- Notification-Service guarda/actualiza la suscripción en MongoDB
- Devuelve confirmación con `subscriptionId`

**Diagrama de secuencia (desregistrar):**
- Usuario cierra sesión o revoca permisos de notificaciones
- `DELETE /api/push/register/{subscriptionId}` → elimina suscripción de MongoDB

---

### 4.4 Actualización de preferencias de notificación

**Tipo:** Mutable (PUT)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Notification-Service → MongoDB → Quartz.NET

**Diagrama de secuencia:**
- Usuario configura sus preferencias (canales, resumen diario, suscripción a alertas climáticas, horario silencioso)
- Frontend referencia _Autenticación en procesos mutables_
- `PUT /api/preferences/{userId}` al Notification-Service
- Notification-Service actualiza el documento en MongoDB
- Si cambió la configuración de resumen diario: llama a `IDailySummaryScheduler.RescheduleAsync` o `RemoveAsync` para sincronizar con Quartz.NET
- Devuelve confirmación

**Diagrama de actividades (swimlanes: Frontend/Mobile | BFF-Service | Notification-Service | MongoDB | Quartz.NET):**
- Autenticación → Actualizar preferencias en MongoDB
- Decisión: ¿Cambió configuración de resumen diario? → Sí: ¿Habilitado? → Reprogramar en Quartz | Deshabilitar en Quartz → No: continuar
- Devolver confirmación al frontend

---

### 4.5 Gestión de grupos de notificación

**Tipo:** Mutable (POST/PUT/DELETE) y no mutable (GET)
**Actores:** Usuario administrador → Frontend → BFF-Service → Notification-Service → MongoDB

> Cubre el RF-N02. Permite mantener listas nominales de destinatarios reutilizables para envíos colectivos.

**Diagrama de secuencia (crear/actualizar/eliminar):**
- Usuario administrador define un grupo (nombre, descripción y lista de destinatarios)
- Frontend referencia _Autenticación en procesos mutables_
- BFF-Service reenvía `POST /api/notification-groups`, `PUT /api/notification-groups/{groupName}` o `DELETE /api/notification-groups/{groupName}` al Notification-Service
- Notification-Service valida y persiste el cambio en la colección `NotificationGroup` de MongoDB
- Devuelve confirmación con el grupo resultante

**Diagrama de secuencia (consultar):**
- Frontend referencia _Autenticación en procesos no mutables_
- `GET /api/notification-groups` lista todos los grupos o `GET /api/notification-groups/{groupName}` obtiene uno
- Notification-Service consulta MongoDB y devuelve el resultado

---

### 4.6 Consulta de historial de notificaciones del usuario

**Tipo:** No mutable (GET)
**Actores:** Usuario → Frontend/Mobile → BFF-Service → Notification-Service → MongoDB

> Cubre el RF-N06. Le permite al usuario ver todo lo que el sistema le ha enviado y por qué canal.

**Diagrama de secuencia:**
- Usuario abre la sección "Mis notificaciones"
- Frontend referencia _Autenticación en procesos no mutables_
- BFF-Service llama a `GET /api/notifications/log/{userId}` con filtros opcionales (canal, estado de entrega, rango de fechas)
- Notification-Service consulta la colección `NotificationLog` con paginación y los filtros aplicados
- Devuelve la lista paginada de entregas con su estado (enviado, fallado, omitido) y canal usado
- Frontend renderiza el historial agrupado por fecha

---

## 5. Fuzzy Service

> El Fuzzy-Service ahora tiene su propio módulo de vistas en el frontend para crear, editar y configurar sistemas fuzzy. Los flujos de lectura referencian _no mutable_ y los de escritura _mutable_.

### 5.1 Crear sistema fuzzy

**Tipo:** Mutable (POST)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB | Weather-Service | Chatbot-Service

**Diagrama de secuencia:**
- Usuario llena formulario de nuevo sistema (nombre, estado inicial DRAFT/ACTIVE)
- Frontend referencia _Autenticación en procesos mutables_
- BFF-Service llama a `POST /api/fuzzy-systems`
- Fuzzy-Service crea el sistema en MongoDB
- Llama a Weather-Service `POST /api/weather/alerts/config/{id}/seed` para crear umbrales por defecto
- Llama a Chatbot-Service `POST /api/rag/sync` para indexar el nuevo sistema en la base de conocimiento
- Devuelve el sistema creado al Frontend

---

### 5.2 Activación exclusiva de sistema fuzzy

**Tipo:** Mutable (POST)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

> Flujo crítico: al activar un sistema, todos los demás se desactivan automáticamente.

**Diagrama de secuencia:**
- Usuario selecciona un sistema DRAFT y hace clic en "Activar"
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-systems/{id}/activate`
- Fuzzy-Service desactiva todos los sistemas activos existentes en MongoDB (actualización masiva)
- Activa el sistema seleccionado
- Devuelve el sistema actualizado

**Diagrama de actividades (swimlanes: Frontend | BFF-Service | Fuzzy-Service | MongoDB):**
- Autenticación → Buscar sistemas activos → Desactivar todos → Activar sistema seleccionado → Devolver resultado
- Decisión: ¿El sistema ya está activo? → Sí: respuesta sin cambios | No: ejecutar activación exclusiva

---

### 5.3 Clonar sistema fuzzy

**Tipo:** Mutable (POST)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

> Flujo complejo: copia profunda del sistema completo con remapeo de IDs.

**Diagrama de secuencia:**
- Usuario selecciona un sistema y usa la opción "Clonar"
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-systems/{id}/clone` con nombre opcional
- Fuzzy-Service carga sistema original + todas sus variables + términos + reglas
- Crea sistema clon como DRAFT en MongoDB
- Bulk-insert de variables → construye mapa viejo_id → nuevo_id
- Bulk-insert de términos con variable_id remapeado → construye mapa de términos
- Actualiza listas de term_ids en las variables clonadas
- Bulk-insert de reglas con variable_ids y term_ids remapeados en condiciones y consecuentes
- Devuelve el sistema clonado al Frontend

---

### 5.4 Gestión de variables y términos (funciones de membresía)

**Tipo:** Mutable (POST/PUT/DELETE) y no mutable (GET)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

**Diagrama de secuencia (agregar variable):**
- Usuario crea una variable de entrada u salida para el sistema
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-variables` con `{name, type, universe, role, systemId}`
- Fuzzy-Service crea la variable en MongoDB y actualiza la referencia en el sistema

**Diagrama de secuencia (agregar término a variable):**
- Usuario define una función de membresía (triangular, trapezoidal, gaussiana, etc.)
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-variables/{variableId}/terms` con `{label, functionType, parameters}`
- Fuzzy-Service crea el término y actualiza la lista de términos de la variable

**Diagrama de actividades (swimlanes: Frontend | BFF-Service | Fuzzy-Service | MongoDB):**
- Autenticación → Crear variable → Agregar términos (1..n) → Configurar rango del universo
- Actualizar referencias en el sistema → Devolver variable actualizada

---

### 5.5 Gestión de reglas difusas

**Tipo:** Mutable (POST/PUT/DELETE)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

**Diagrama de secuencia (crear regla):**
- Usuario define una regla (condiciones IF con conectores AND/OR, consecuentes THEN)
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-rules` con condiciones y consecuentes
- `POST /api/fuzzy-rules/{ruleId}/conditions` para agregar condiciones adicionales
- `PUT /api/fuzzy-rules/{ruleId}/connectors` para configurar conectores AND/OR
- `PUT /api/fuzzy-rules/{ruleId}/consequent` para definir consecuentes Mamdani
- Fuzzy-Service persiste cada cambio en MongoDB

---

### 5.6 Simulación de sistema fuzzy

**Tipo:** No mutable (POST de consulta — no persiste)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service

**Diagrama de secuencia:**
- Usuario ingresa valores de prueba para las variables de entrada
- Frontend referencia _Autenticación en procesos no mutables_
- `POST /api/fuzzy-systems/{id}/simulate` con `{inputs: {variable: valor}}`
- Fuzzy-Service carga variables, términos y reglas del sistema
- Ejecuta el motor fuzzy scikit-fuzzy (Mamdani)
- Devuelve los valores de salida calculados sin guardar en BD

---

### 5.7 Exportar e importar sistema fuzzy

**Tipo:** No mutable (GET export) y mutable (POST import)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

**Diagrama de secuencia (exportar):**
- Usuario selecciona opción "Exportar" en un sistema
- Frontend referencia _Autenticación en procesos no mutables_
- `GET /api/fuzzy-systems/{id}/export`
- Fuzzy-Service serializa sistema completo (variables + términos + reglas) a JSON portable
- Frontend descarga el archivo JSON

**Diagrama de secuencia (importar):**
- Usuario sube un archivo JSON exportado previamente
- Frontend referencia _Autenticación en procesos mutables_
- `POST /api/fuzzy-systems/import` con el JSON
- Fuzzy-Service reconstruye todas las entidades con IDs nuevos en MongoDB
- Llama a Weather-Service para seed de alertas y a Chatbot-Service para indexación RAG
- Devuelve el sistema importado

---

### 5.8 Procesamiento automático de lecturas de sensores (flujo core de automatización)

**Tipo:** Proceso interno — disparado por eventos MQTT de Sensor-Service
**Actores:** Sensor-Service → Fuzzy-Service → Actuator-Service | MongoDB

> Este es el flujo más crítico del sistema completo. Cierra el ciclo de control de la planta.

**Diagrama de secuencia:**
1. Sensor-Service publica lecturas via MQTT
2. Fuzzy-Service recibe evento con los valores del sensor
3. Encuentra el sistema fuzzy con estado ACTIVE en MongoDB
4. Carga variables de entrada, variables de salida y reglas del sistema activo
5. Mapea los valores del sensor a las variables de entrada por `reference_code`
6. Ejecuta `IFuzzyEngine.complete_fuzzy_evaluation` (motor scikit-fuzzy Mamdani)
7. Guarda `FuzzyEvaluation` en MongoDB con entradas, salidas y timestamp
8. `CommandAggregator` agrega valores de salida (max aggregation)
9. `SendCommandsToActuatorCommand` → HTTP al Actuator-Service con los comandos calculados
10. Actuator-Service aplica los comandos a los actuadores físicos

**Diagrama de actividades (swimlanes: Sensor-Service | Fuzzy-Service | MongoDB | Actuator-Service):**
- Evento MQTT recibido → Encontrar sistema ACTIVE → Cargar variables y reglas
- Mapear lecturas a variables → Ejecutar motor fuzzy → Guardar evaluación
- Agregar salidas → ¿Valores de salida válidos? → Sí: enviar comandos a actuadores | No: registrar error
- Actuator-Service aplica comandos

---

### 5.9 Consulta de historial y estadísticas de evaluaciones fuzzy

**Tipo:** No mutable (GET)
**Actores:** Usuario → Frontend → BFF-Service → Fuzzy-Service → MongoDB

> Cubre el RF-F07. Da visibilidad sobre el comportamiento del sistema fuzzy a lo largo del tiempo y permite analizar tendencias.

**Diagrama de secuencia (historial):**
- Usuario abre la sección "Historial de evaluaciones" o "Evaluaciones recientes"
- Frontend referencia _Autenticación en procesos no mutables_
- Para listado paginado con filtros: `GET /api/fuzzy-evaluations` con parámetros (sistema, rango de fechas, ordenamiento, paginación)
- Para últimas 24 horas: `GET /api/fuzzy-evaluations/recent` (configurable hasta 7 días)
- Para una evaluación específica: `GET /api/fuzzy-evaluations/{evaluationId}` con detalle completo (entradas, reglas disparadas, salidas)
- Fuzzy-Service ejecuta la consulta en MongoDB y devuelve el resultado al BFF-Service

**Diagrama de secuencia (estadísticas):**
- Usuario abre la pestaña "Estadísticas"
- Frontend referencia _Autenticación en procesos no mutables_
- `GET /api/fuzzy-evaluations/stats/summary` con sistema y rango opcional
- Fuzzy-Service ejecuta agregaciones MongoDB (conteos por sistema, totales diarios, promedios de salidas) y devuelve los indicadores
- Frontend renderiza tabla y gráficos

**Diagrama de actividades (swimlanes: Frontend | BFF-Service | Fuzzy-Service | MongoDB):**
- Autenticación → Decisión: ¿Listado, recientes, detalle o estadísticas? → Construir query con filtros
- Ejecutar consulta MongoDB (find + paginación, o aggregate para estadísticas) → Mapear a DTOs → Devolver respuesta
- Frontend muestra tabla, detalle o gráficos según la vista

---

## Resumen de prioridades de diagramas

| Prioridad | Servicio | Flujo | Tipo |
|-----------|---------|-------|------|
| 1 | Fuzzy Service | Procesamiento automático de sensores | Secuencia + Actividades |
| 2 | Chatbot Service | Envío de mensaje RAG con streaming | Secuencia + Actividades |
| 3 | Weather Service | Evaluación automática de alertas (Worker) | Secuencia + Actividades |
| 4 | Notification Service | Envío multi-canal (CompositeDispatcher) | Secuencia + Actividades |
| 5 | Notification Service | Resumen diario automático | Secuencia + Actividades |
| 6 | Fuzzy Service | Activación exclusiva de sistema | Secuencia + Actividades |
| 7 | Fuzzy Service | Clonar sistema | Secuencia |
| 8 | BI Service | Cálculo de rentabilidad | Secuencia + Actividades |
| 9 | Chatbot Service | Sincronización RAG (M2M) | Secuencia |
| 10 | Notification Service | Actualización de preferencias + Quartz | Secuencia + Actividades |
| 11 | Weather Service | Consulta de pronóstico con caché | Secuencia + Actividades |
| 12 | Fuzzy Service | Simulación de sistema | Secuencia |
| 13 | Fuzzy Service | Exportar / Importar sistema | Secuencia |
| 14 | BI Service | Registro de consumo y producción | Secuencia |
| 15 | Notification Service | Registro de suscripción push | Secuencia |
| 16 | BI Service | Cálculo de costos operacionales (standalone) | Secuencia + Actividades |
| 17 | Fuzzy Service | Historial y estadísticas de evaluaciones | Secuencia + Actividades |
| 18 | Notification Service | Historial de notificaciones del usuario | Secuencia |
| 19 | Notification Service | Gestión de grupos de notificación | Secuencia |
| 20 | Chatbot Service | Reindexación manual completa de la base de conocimiento | Secuencia + Actividades |
