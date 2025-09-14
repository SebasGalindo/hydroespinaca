#!/bin/sh

# =================================================
# Wait for MQTT Broker Script
# Waits for MQTT to be available before starting services
# =================================================

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[$(date)] WAIT-MQTT: $1${NC}"
}

info() {
    echo -e "${BLUE}[$(date)] WAIT-MQTT: $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] WAIT-MQTT: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] WAIT-MQTT: $1${NC}"
}

# Configuration
MQTT_HOST=${MQTT_HOST:-mqtt}
MQTT_PORT=${MQTT_PORT:-1883}
MQTT_USER=${MQTT_USER:-}
MQTT_PASS=${MQTT_PASS:-}
MAX_WAIT=${MAX_WAIT:-60}
CHECK_INTERVAL=${CHECK_INTERVAL:-2}

info "Waiting for MQTT broker at $MQTT_HOST:$MQTT_PORT"
info "Maximum wait time: ${MAX_WAIT}s, Check interval: ${CHECK_INTERVAL}s"

# Function to test MQTT connection
test_mqtt_connection() {
    if command -v mosquitto_pub >/dev/null 2>&1; then
        # Use mosquitto_pub if available (more reliable)
        if [ -n "$MQTT_USER" ] && [ -n "$MQTT_PASS" ]; then
            mosquitto_pub -h "$MQTT_HOST" -p "$MQTT_PORT" \
                         -u "$MQTT_USER" -P "$MQTT_PASS" \
                         -t "test/healthcheck" -m "ping" -q 0 \
                         --timeout 5 >/dev/null 2>&1
        else
            mosquitto_pub -h "$MQTT_HOST" -p "$MQTT_PORT" \
                         -t "test/healthcheck" -m "ping" -q 0 \
                         --timeout 5 >/dev/null 2>&1
        fi
    else
        # Fallback to netcat connection test
        if command -v nc >/dev/null 2>&1; then
            echo "test" | timeout 3 nc "$MQTT_HOST" "$MQTT_PORT" >/dev/null 2>&1
        else
            error "Neither mosquitto_pub nor nc available for testing MQTT connection"
            return 1
        fi
    fi
}

# Wait loop
waited=0
while [ $waited -lt $MAX_WAIT ]; do
    if test_mqtt_connection; then
        log "✅ MQTT broker is available at $MQTT_HOST:$MQTT_PORT"
        log "Service can now start safely"
        exit 0
    else
        info "⏳ MQTT not ready yet, waiting... (${waited}s/${MAX_WAIT}s)"
        sleep $CHECK_INTERVAL
        waited=$((waited + CHECK_INTERVAL))
    fi
done

error "❌ Timeout waiting for MQTT broker after ${MAX_WAIT}s"
error "MQTT broker at $MQTT_HOST:$MQTT_PORT is not available"
exit 1