# PASO 4: Enviar payload de rutinas al actuator-service (HTTP)
# Cliente HTTP asíncrono para comunicar al actuator-service las rutinas resultantes del motor fuzzy.
# Librería prevista: httpx.AsyncClient
# Variables de entorno esperadas: ACTUATOR_SERVICE_URL, ACTUATOR_SERVICE_TIMEOUT, ACTUATOR_SERVICE_ENDPOINT,
#   ACTUATOR_SERVICE_API_KEY (si aplica)
#
# Responsabilidades:
# - Construir payload a partir de las salidas del motor (rutinas con steps y expiración absoluta).
# - Realizar POST al endpoint configurado con timeouts y reintentos exponenciales.
# - Manejar autenticación (header, API key) y errores (5xx, 4xx) con mapeo a excepciones internas.
# - Registrar request/response (sin datos sensibles) y métricas (latencia, tasa de éxito).
# - Devolver resultado normalizado (aceptado, rechazado, ya en cola, etc.).
#
# Flujo interno esperado:
# 1) Construir httpx.AsyncClient(base_url, timeout, headers).
# 2) POST ACTUATOR_SERVICE_ENDPOINT con payload JSON.
# 3) Si 2xx: mapear a modelos internos y retornar estado.
# 4) Si 4xx/5xx: aplicar política de reintentos/CircuitBreaker si aplica.
# 5) Cerrar cliente de forma segura al terminar el ciclo de vida.
