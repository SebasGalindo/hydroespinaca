# PASO 1: Establecer conexión MQTT (solo lectura)
# Este cliente configura y mantiene una conexión de SOLO LECTURA al broker MQTT usando asyncio-mqtt.
# Librería prevista: asyncio-mqtt (Client, connect, subscribe, messages, UnfilteredMessage)
# Variables de entorno esperadas: MQTT_BROKER_URL, MQTT_BROKER_PORT, MQTT_USERNAME, MQTT_PASSWORD,
#   MQTT_KEEPALIVE, MQTT_TLS_ENABLED, MQTT_TLS_CA_FILE
#
# Responsabilidades:
# - Configurar conexión (host, puerto, credenciales, TLS opcional) y keep-alive.
# - Manejar reconexión automática con backoff exponencial y límites configurables.
# - Suscribirse a los topics de sensores definidos por configuración.
# - Exponer un stream asíncrono de mensajes para ser procesados por el suscriptor/handler.
# - Logging detallado de conexión, reconexiones y errores.
# - Garantizar QoS apropiado (por defecto 1) para asegurar al menos una entrega.
# - IMPORTANTE: Sin publicar mensajes (no se usa publish), SOLO escucha.
# - Integración: delega el procesamiento a MqttMessageHandler.
#
# Flujo interno esperado:
# 1) Crear Client(host, port, username, password, tls_context?)
# 2) await client.connect(); await client.subscribe('<sensors/topic/#>', qos)
# 3) async with client.messages() as messages: async for message in messages: entregar al Suscriptor
# 4) Manejo de excepciones (TimeoutError, MQTTError) con reintentos y sleep exponencial
# 5) Cierre ordenado de la conexión al finalizar el ciclo de vida del servicio
