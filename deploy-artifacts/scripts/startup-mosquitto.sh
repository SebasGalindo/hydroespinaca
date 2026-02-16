#!/bin/sh

# =================================================
# Mosquitto Startup Script with Profile Support
# HydroEspinaca Project
# =================================================

set -e

# -----------------------------
# Colors for output
# -----------------------------
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

log()    { echo -e "${GREEN}[$(date)] MOSQUITTO-STARTUP: $1${NC}"; }
info()   { echo -e "${BLUE}[$(date)] MOSQUITTO-STARTUP: $1${NC}"; }
warn()   { echo -e "${YELLOW}[$(date)] MOSQUITTO-STARTUP: $1${NC}"; }
error()  { echo -e "${RED}[$(date)] MOSQUITTO-STARTUP: $1${NC}"; }

# -----------------------------
# Load environment variables
# -----------------------------
USE_TLS=${USE_TLS:-false}
ENVIRONMENT=${ENVIRONMENT:-Development}
DOMAIN=${DOMAIN:-hydroespinaca.online}
MQTT_SUBDOMAIN=${MQTT_SUBDOMAIN:-mqtt}
COMPOSE_PROFILE=${COMPOSE_PROFILE:-development}

MQTT_DOMAIN="${MQTT_SUBDOMAIN}.${DOMAIN}"

info "Starting Mosquitto with profile: $COMPOSE_PROFILE"
info "Environment: $ENVIRONMENT (TLS: $USE_TLS)"

# -----------------------------
# Profile-specific configuration
# -----------------------------
case "$COMPOSE_PROFILE" in
  production)
    info "Production profile detected - full TLS configuration"

    # Check certificates only in production
    if [ "$USE_TLS" = "true" ]; then
        log "Checking TLS certificates..."
        MISSING_CERTS=0
        for cert_file in fullchain.pem privkey.pem; do
            cert_path="/etc/mosquitto/certs/$cert_file"
            if [ -f "$cert_path" ]; then
                info "✓ Found: $cert_path"
            else
                error "✗ Missing: $cert_path"
                MISSING_CERTS=1
            fi
        done

        if [ $MISSING_CERTS -eq 1 ]; then
            warn "Certificates missing, falling back to development profile"
            export COMPOSE_PROFILE="development"
        fi
    fi

    export MQTT_TLS_LISTENERS="
# External MQTT listener with TLS (for ESP32 nodes)
listener 8883 0.0.0.0
protocol mqtt
certfile /etc/mosquitto/certs/fullchain.pem
keyfile /etc/mosquitto/certs/privkey.pem

# WebSocket TLS listener (production - TLS)
listener 9002 0.0.0.0
protocol websockets
certfile /etc/mosquitto/certs/fullchain.pem
keyfile /etc/mosquitto/certs/privkey.pem"

    export MQTT_WEBSOCKET_DEV=""
    ;;

  development)
    info "Development profile detected - minimal configuration (no TLS)"
    export MQTT_WEBSOCKET_DEV="
# WebSocket listener (development - no TLS)
listener 9001 0.0.0.0
protocol websockets"
    export MQTT_TLS_LISTENERS=""
    info "Skipping TLS certificate check in development"
    ;;

  *)
    warn "Unknown profile '$COMPOSE_PROFILE', defaulting to development"
    export MQTT_WEBSOCKET_DEV="
# WebSocket listener (development - no TLS)
listener 9001 0.0.0.0
protocol websockets"
    export MQTT_TLS_LISTENERS=""
    ;;
esac

# -----------------------------
# Template check
# -----------------------------
TEMPLATE_PATH="/mosquitto/config/mosquitto.conf.tpl"
if [ ! -f "$TEMPLATE_PATH" ]; then
    error "Template file not found: $TEMPLATE_PATH"
    if [ -f "/scripts/mosquitto.conf.tpl" ]; then
        warn "Using fallback template from /scripts/"
        cp /scripts/mosquitto.conf.tpl "$TEMPLATE_PATH"
    else
        error "No template available, aborting..."
        exit 1
    fi
fi

# -----------------------------
# Generate final mosquitto.conf
# -----------------------------
log "Generating mosquitto.conf from template..."
envsubst '${MQTT_WEBSOCKET_DEV} ${MQTT_TLS_LISTENERS}' < "$TEMPLATE_PATH" > /mosquitto/config/mosquitto.conf
log "Mosquitto configuration generated for profile: $COMPOSE_PROFILE"

# Preview first 30 lines in development
if [ "$ENVIRONMENT" = "Development" ]; then
    log "Configuration preview (first 30 lines):"
    echo "=========================="
    head -30 /mosquitto/config/mosquitto.conf
    echo "=========================="
fi

info "Launching Mosquitto..."

# -----------------------------
# Ensure pwfile exists (development convenience)
# -----------------------------
PWFILE_PATH="/mosquitto/config/pwfile"
if [ ! -f "$PWFILE_PATH" ]; then
    warn "pwfile not found at $PWFILE_PATH"

    if [ "$COMPOSE_PROFILE" = "development" ]; then
        if command -v mosquitto_passwd >/dev/null 2>&1; then
            # Collect users to add (only when both user and pass are set)
            ADDED_USERS=0

            if [ -n "${SENSOR_MQTT_USER:-}" ] && [ -n "${SENSOR_MQTT_PASS:-}" ]; then
                mosquitto_passwd -b -c "$PWFILE_PATH" "$SENSOR_MQTT_USER" "$SENSOR_MQTT_PASS"
                ADDED_USERS=1
            fi

            if [ -n "${ACTUATOR_MQTT_USER:-}" ] && [ -n "${ACTUATOR_MQTT_PASS:-}" ]; then
                if [ $ADDED_USERS -eq 0 ]; then
                    mosquitto_passwd -b -c "$PWFILE_PATH" "$ACTUATOR_MQTT_USER" "$ACTUATOR_MQTT_PASS"
                    ADDED_USERS=1
                else
                    mosquitto_passwd -b "$PWFILE_PATH" "$ACTUATOR_MQTT_USER" "$ACTUATOR_MQTT_PASS"
                fi
            fi

            if [ -n "${FUZZY_MQTT_USER:-}" ] && [ -n "${FUZZY_MQTT_PASS:-}" ]; then
                if [ $ADDED_USERS -eq 0 ]; then
                    mosquitto_passwd -b -c "$PWFILE_PATH" "$FUZZY_MQTT_USER" "$FUZZY_MQTT_PASS"
                    ADDED_USERS=1
                else
                    mosquitto_passwd -b "$PWFILE_PATH" "$FUZZY_MQTT_USER" "$FUZZY_MQTT_PASS"
                fi
            fi

            if [ -n "${ESP32_MQTT_USER:-}" ] && [ -n "${ESP32_MQTT_PASS:-}" ]; then
                if [ $ADDED_USERS -eq 0 ]; then
                    mosquitto_passwd -b -c "$PWFILE_PATH" "$ESP32_MQTT_USER" "$ESP32_MQTT_PASS"
                    ADDED_USERS=1
                else
                    mosquitto_passwd -b "$PWFILE_PATH" "$ESP32_MQTT_USER" "$ESP32_MQTT_PASS"
                fi
            fi

            if [ $ADDED_USERS -eq 0 ]; then
                error "No MQTT users were provided via environment variables; cannot generate pwfile."
                error "Set SENSOR_MQTT_USER/PASS, ACTUATOR_MQTT_USER/PASS, FUZZY_MQTT_USER/PASS, etc. in .env (or create deploy-artifacts/mosquitto/config/pwfile manually)."
                exit 1
            fi

            # Try to set ownership for mosquitto if user exists
            if id mosquitto >/dev/null 2>&1; then
                chown mosquitto:mosquitto "$PWFILE_PATH" || true
            fi
            chmod 600 "$PWFILE_PATH" || true

            log "Generated pwfile for development at $PWFILE_PATH"
        else
            error "mosquitto_passwd not found in image; cannot generate pwfile automatically."
            exit 1
        fi
    else
        error "pwfile is missing and profile is not development; refusing to start with allow_anonymous=false"
        exit 1
    fi
fi

mosquitto -c /mosquitto/config/mosquitto.conf -v 2>&1 || {
    error "Mosquitto failed with exit code $?"
    error "Last 10 lines of mosquitto log:"
    tail -10 /mosquitto/log/mosquitto.log 2>/dev/null || error "No mosquitto.log found"
    sleep 30
    exit 1
}
