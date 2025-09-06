# PASO 2: Escuchar y recibir lecturas del sensor-service por MQTT (solo suscripción)
# Este suscriptor gestiona la suscripción a tópicos y el enrutamiento de mensajes a handlers.
# Librería prevista: asyncio-mqtt
#
# Responsabilidades:
# - Suscribirse a tópicos de sensores: ej. 'sensors/+/readings' o 'sensors/<esp32Id>/batch'.
# - Filtrar y validar topics y payloads (JSON). Rechazar mensajes inválidos.
# - (Opcional) Agrupar lecturas en lotes por ventana temporal para evaluación conjunta. NO
# - Enviar cada mensaje válido al MqttMessageHandler para su procesamiento.
# - Configurar QoS por tipo de sensor (por defecto 1) según criticidad.
# - Manejar reintentos de suscripción y reconexión.
# - Registrar métricas: mensajes procesados, descartados, latencia, errores.
# - No publicar nunca (solo lectura), sin side-effects fuera del handler.
#
# Flujo interno esperado:
# 1) Preparar lista de tópicos y QoS a partir de configuración.
# 2) Realizar subscribe() y escuchar client.messages().
# 3) Por cada mensaje: validar topic y decodificar JSON.
# 4) Si es batch: dividir por sensor y timestamp; si es single: pasar tal cual. 
# 5) Llamar MqttMessageHandler.handle(message) para orquestar el resto del flujo.
