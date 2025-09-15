#!/bin/sh

# =================================================
# Mosquitto Startup Script with Profile Support
# HydroEspinaca Project - Profile-aware Configuration
# =================================================

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[$(date)] MOSQUITTO-STARTUP: $1${NC}"
}

info() {
    echo -e "${BLUE}[$(date)] MOSQUITTO-STARTUP: $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] MOSQUITTO-STARTUP: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] MOSQUITTO-STARTUP: $1${NC}"
}

# Load environment variables
USE_TLS=${USE_TLS:-false}
ENVIRONMENT=${ENVIRONMENT:-Development}
DOMAIN=${DOMAIN:-hydroespinaca.online}
MQTT_SUBDOMAIN=${MQTT_SUBDOMAIN:-mqtt}
COMPOSE_PROFILE=${COMPOSE_PROFILE:-development}

# Derived variables
MQTT_DOMAIN="${MQTT_SUBDOMAIN}.${DOMAIN}"

info "Starting Mosquitto with profile: $COMPOSE_PROFILE"
info "Environment: $ENVIRONMENT (TLS: $USE_TLS)"

# Determine configuration strategy based on profile
if [ "$COMPOSE_PROFILE" = "production" ]; then
    info "Production profile detected - full TLS configuration"
    
    # Ensure certificates exist for production
    if [ "$USE_TLS" = "true" ] && [ ! -f "/etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem" ]; then
        error "Production mode requires TLS certificates but they don't exist"
        error "Certificate path: /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem"
        exit 1
    fi
    
    # Production: Use dynamic template with TLS
    export MQTT_WEBSOCKET_DEV=""
    export MQTT_TLS_LISTENERS="
# External MQTT listener with TLS (for ESP32 nodes)
listener 8883 0.0.0.0
protocol mqtt
cafile /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem
certfile /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem
keyfile /etc/letsencrypt/live/${MQTT_DOMAIN}/privkey.pem

# WebSocket TLS listener (production - with TLS)
listener 9002 0.0.0.0
protocol websockets
cafile /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem
certfile /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem
keyfile /etc/letsencrypt/live/${MQTT_DOMAIN}/privkey.pem"

elif [ "$COMPOSE_PROFILE" = "development" ]; then
    info "Development profile detected - minimal configuration (no TLS)"
    
    # Development: Only basic listeners, no TLS
    export MQTT_WEBSOCKET_DEV="
# WebSocket listener (development - no TLS)
listener 9001 0.0.0.0
protocol websockets"
    
    export MQTT_TLS_LISTENERS=""
    
else
    warn "Unknown profile '$COMPOSE_PROFILE', defaulting to development"
    # Default to development configuration
    export MQTT_WEBSOCKET_DEV="
# WebSocket listener (development - no TLS)
listener 9001 0.0.0.0
protocol websockets"
    
    export MQTT_TLS_LISTENERS=""
fi

# Check if template exists
if [ ! -f "/mosquitto/config/mosquitto.conf.tpl" ]; then
    error "Template file /mosquitto/config/mosquitto.conf.tpl not found!"
    error "Expected locations:"
    error "  - /mosquitto/config/mosquitto.conf.tpl"
    error "  - /scripts/mosquitto.conf.tpl"
    
    # Try to use fallback template from scripts
    if [ -f "/scripts/mosquitto.conf.tpl" ]; then
        warn "Using fallback template from /scripts/"
        cp /scripts/mosquitto.conf.tpl /mosquitto/config/mosquitto.conf.tpl
    else
        error "No template found. Exiting..."
        exit 1
    fi
fi

# Generate the final mosquitto.conf
log "Generating mosquitto.conf from template..."
envsubst '${MQTT_WEBSOCKET_DEV} ${MQTT_TLS_LISTENERS}' \
    < /mosquitto/config/mosquitto.conf.tpl > /mosquitto/config/mosquitto.conf

log "Mosquitto configuration generated for profile: $COMPOSE_PROFILE"

# Show configuration preview
if [ "$ENVIRONMENT" = "Development" ]; then
    log "Configuration preview (first 30 lines):"
    echo "=========================="
    head -30 /mosquitto/config/mosquitto.conf
    echo "=========================="
fi

# Validate configuration syntax if possible
if command -v mosquitto >/dev/null 2>&1; then
    info "Validating configuration syntax..."
    if mosquitto -c /mosquitto/config/mosquitto.conf -v -d; then
        log "Configuration syntax validation passed"
        pkill mosquitto || true
    else
        error "Configuration validation failed!"
        exit 1
    fi
else
    info "Mosquitto not available for validation, continuing..."
fi

# Start Mosquitto
log "Starting Mosquitto daemon..."
exec mosquitto -c /mosquitto/config/mosquitto.conf -v