# PASO 3: Procesar mensajes MQTT y disparar evaluación fuzzy
# Este handler transforma los mensajes de sensores en comandos de evaluación y coordina el siguiente paso.
# Librerías/Patrones previstos: json, pydantic para validación, medyator (CQRS) para ejecutar comandos.
#
# Responsabilidades:
# - Parsear y validar payloads JSON (lecturas de sensores). Asegurar tipos y rangos válidos.
# - Mapear a objetos de dominio InputValue (sensor_id, value).
# - Orquestar una evaluación fuzzy (ExecuteFuzzyEvaluationCommand) vía Medyator.
# - Registrar qué reglas se dispararon y resultados (para auditoría/observabilidad).
# - Transformar la salida (OutputValues) a payload de actuadores (ver ActuatorServiceClient).
# - Manejar idempotencia y duplicados (p. ej., ignorar mismo messageId dentro de ventana).
# - Registrar métricas de procesamiento y errores.
#
# Flujo interno esperado:
# 1) Recibir UnfilteredMessage: topic, payload, timestamp.
# 2) Decodificar JSON; validar esquema y valores.
# 3) Construir lista de InputValue y determinar system_id destino.
# 4) Enviar comando ExecuteFuzzyEvaluationCommand al motor (FuzzyEngineService) vía Medyator.
# 5) Convertir resultados a lista de rutinas/steps con expiración absoluta.
# 6) Invocar ActuatorServiceClient.send_routines(payload) y manejar respuesta.
# 7) Loggear resultados y manejar errores con backoff/transient retries.
