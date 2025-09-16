#!/bin/bash

# =================================================
# Certbot Deploy Hook for Mosquitto Certificates
# Copies certificates with proper permissions for Mosquitto
# =================================================

set -e

# Configuration
TARGET_DIR="/etc/mosquitto/certs"
MQTT_DOMAIN="${MQTT_DOMAIN:-mqtt.hydroespinaca.online}"
MOSQUITTO_UID=1883
MOSQUITTO_GID=1883

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[$(date)] CERTBOT-HOOK: $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] CERTBOT-HOOK: WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] CERTBOT-HOOK: ERROR: $1${NC}"
}

# Only process if this is for our MQTT domain
if [ "${RENEWED_DOMAINS}" = "${MQTT_DOMAIN}" ] || [[ "${RENEWED_DOMAINS}" == *"${MQTT_DOMAIN}"* ]]; then
    log "Processing certificate renewal for ${MQTT_DOMAIN}"
    
    # Create target directory
    mkdir -p "${TARGET_DIR}"
    
    # Copy certificates with proper permissions
    if [ -f "/etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem" ]; then
        cp "/etc/letsencrypt/live/${MQTT_DOMAIN}/fullchain.pem" "${TARGET_DIR}/"
        log "Copied fullchain.pem"
    else
        error "fullchain.pem not found"
        exit 1
    fi
    
    if [ -f "/etc/letsencrypt/live/${MQTT_DOMAIN}/privkey.pem" ]; then
        cp "/etc/letsencrypt/live/${MQTT_DOMAIN}/privkey.pem" "${TARGET_DIR}/"
        log "Copied privkey.pem"
    else
        error "privkey.pem not found"
        exit 1
    fi
    
    # Set proper ownership and permissions
    chown ${MOSQUITTO_UID}:${MOSQUITTO_GID} "${TARGET_DIR}"/*.pem
    chmod 644 "${TARGET_DIR}/fullchain.pem"
    chmod 600 "${TARGET_DIR}/privkey.pem"  # Private key should be more restrictive
    
    log "Set proper permissions for Mosquitto certificates"
    log "Certificate deployment completed for ${MQTT_DOMAIN}"
    
    # Restart Mosquitto container if running
    if command -v docker >/dev/null 2>&1; then
        if docker ps --format "table {{.Names}}" | grep -q "^mqtt$"; then
            log "Restarting Mosquitto container..."
            docker restart mqtt || warn "Failed to restart Mosquitto container"
        else
            warn "Mosquitto container not running"
        fi
    fi
else
    log "Certificate renewal not for ${MQTT_DOMAIN}, skipping Mosquitto deployment"
fi