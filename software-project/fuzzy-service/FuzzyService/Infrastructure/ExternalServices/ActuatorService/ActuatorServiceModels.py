# TODO: Modelos para comunicación con ActuatorService
# PROPÓSITO: Definir estructuras de datos específicas para la API del ActuatorService
# - ActuatorCommandRequest: Modelo para enviar comandos (actuatorId, power, dutyCycle, duration)
# - ActuatorCommandResponse: Modelo para respuestas del servicio (success, message, timestamp)
# - ActuatorStatusResponse: Modelo para consultar estado de actuadores
# - Validaciones específicas del servicio externo
# - Separación entre modelos de dominio interno y contratos externos
# - Facilita cambios en la API externa sin afectar el dominio
# - Mapeo entre modelos internos y externos
