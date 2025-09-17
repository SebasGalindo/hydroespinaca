# =================================================
# Mosquitto MQTT Broker Configuration Template
# HydroEspinaca Project - Dynamic Configuration
# =================================================

# General settings
persistence true
persistence_location /mosquitto/data/
log_dest file /mosquitto/log/mosquitto.log
log_type all

# Connection logging
connection_messages true
log_timestamp true

# Global authentication settings
allow_anonymous false
password_file /mosquitto/config/pwfile
acl_file /mosquitto/config/acl

# =================================================
# LISTENERS CONFIGURATION
# =================================================

# Internal MQTT listener (no encryption - Docker network only)
listener 1883 0.0.0.0
protocol mqtt

${MQTT_WEBSOCKET_DEV}

${MQTT_TLS_LISTENERS}

# =================================================
# SECURITY SETTINGS
# =================================================

# Message settings
max_queued_messages 100
max_packet_size 512K
max_inflight_messages 20

# Connection settings
max_connections 1000
max_keepalive 300

# Client settings
persistent_client_expiration 1d