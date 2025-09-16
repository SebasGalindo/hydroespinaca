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
    if [ "$USE_TLS" = "true" ]; then
        info "Checking TLS certificates for production mode..."
        info "Expected certificate path: /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem"
        
        # Debug: List volume mount points
        info "Volume mount debugging:"
        ls -la /etc/letsencrypt/ || warn "/etc/letsencrypt directory not found"
        ls -la /etc/letsencrypt/live/ || warn "/etc/letsencrypt/live directory not found"
        ls -la /etc/letsencrypt/live/${MQTT_DOMAIN}/ || warn "Certificate directory for ${MQTT_DOMAIN} not found"
        
        # Check each required certificate file
        for cert_file in fullchain.pem cert.pem privkey.pem; do
            cert_path="/etc/letsencrypt/live/${MQTT_DOMAIN}/${cert_file}"
            if [ -f "$cert_path" ]; then
                info "✓ Found: $cert_path"
                ls -la "$cert_path"
            else
                error "✗ Missing: $cert_path"
            fi
        done
        
        # If any certificate is missing, use fallback or exit
        if [ ! -f "/etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem" ]; then
            error "Production mode requires TLS certificates but they don't exist"
            error "Available directories in /etc/letsencrypt/live/:"
            ls -la /etc/letsencrypt/live/ || error "Cannot list /etc/letsencrypt/live/"
            warn "Falling back to development mode for this startup..."
            export COMPOSE_PROFILE="development"
        fi
    fi
    
    # Production: Use dynamic template with TLS
    export MQTT_WEBSOCKET_DEV=""
    export MQTT_TLS_LISTENERS="
# External MQTT listener with TLS (for ESP32 nodes)
listener 8883 0.0.0.0
protocol mqtt
certfile /etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem
keyfile /etc/letsencrypt/live/${MQTT_DOMAIN}/privkey.pem

# WebSocket TLS listener (production - with TLS)
listener 9002 0.0.0.0
protocol websockets
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

info "Skipping pre-validation - mosquitto will validate on startup"

# Start Mosquitto (PID 1)
log "Starting Mosquitto daemon..."
log "Current user: $(whoami)"
log "Current UID: $(id)"

# Test certificate access first
log "Testing certificate access..."
if [ -r "/etc/letsencrypt/live/mqtt.hydroespinaca.online/privkey.pem" ]; then
    log "✓ Can read privkey.pem"
else
    error "✗ Cannot read privkey.pem"
    ls -la /etc/letsencrypt/archive/mqtt.hydroespinaca.online/
fi

log "Starting mosquitto with detailed error output..."
mosquitto -c /mosquitto/config/mosquitto.conf -v 2>&1 || {
    error "Mosquitto failed with exit code $?"
    error "Last 10 lines of mosquitto log:"
    tail -10 /mosquitto/log/mosquitto.log 2>/dev/null || error "No mosquitto.log found"
    error "Checking write permissions on log directory:"
    ls -la /mosquitto/log/ || error "Cannot access log directory"
    ls -la /mosquitto/data/ || error "Cannot access data directory"
    sleep 30  # Prevent rapid restart
    exit 1
}