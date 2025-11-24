#!/bin/bash

# =================================================
# Setup Verification Script
# Verifies prerequisites before starting services
# =================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[✓] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[⚠] $1${NC}"
}

error() {
    echo -e "${RED}[✗] $1${NC}"
}

info() {
    echo -e "${BLUE}[ℹ] $1${NC}"
}

# Load environment variables
if [ -f ".env" ]; then
    source .env
elif [ -f ".env.production" ]; then
    source .env.production
else
    error "No .env file found. Copy .env.example to .env and configure it."
    exit 1
fi

echo ""
info "=========================================="
info "  HydroEspinaca Setup Verification"
info "=========================================="
echo ""

# Check 1: Docker network
info "Checking Docker network..."
if docker network inspect hydroespinaca >/dev/null 2>&1; then
    log "Docker network 'hydroespinaca' exists"
else
    error "Docker network 'hydroespinaca' not found"
    echo ""
    echo "Create it with:"
    echo "  docker network create hydroespinaca"
    exit 1
fi

# Check 2: Environment configuration
info "Checking environment configuration..."
log "Environment: ${ENVIRONMENT:-Development}"
log "Profile: ${COMPOSE_PROFILE:-development}"

# Check profile consistency
if [ "$ENVIRONMENT" = "Production" ] && [ "$COMPOSE_PROFILE" != "production" ]; then
    error "Profile mismatch! ENVIRONMENT=Production but COMPOSE_PROFILE=$COMPOSE_PROFILE"
    echo "  Set COMPOSE_PROFILE=production in your .env file"
    exit 1
fi

if [ "$ENVIRONMENT" = "Development" ] && [ "$COMPOSE_PROFILE" != "development" ]; then
    warn "Profile mismatch: ENVIRONMENT=Development but COMPOSE_PROFILE=$COMPOSE_PROFILE"
fi

# Check which frontend service will be used
if [ "$COMPOSE_PROFILE" = "production" ]; then
    log "Frontend service: web-app (production build)"
else
    log "Frontend service: web-app-dev (development with HMR)"
fi

# Check 3: TLS configuration
if [ "$USE_TLS" = "true" ]; then
    info "Checking TLS configuration..."
    log "TLS is enabled"

    if [ -z "$CLOUDFLARE_API_TOKEN" ] || [ "$CLOUDFLARE_API_TOKEN" = "your_cloudflare_api_token" ]; then
        error "CLOUDFLARE_API_TOKEN is not configured in .env"
        echo ""
        echo "To configure:"
        echo "  1. Get token from https://dash.cloudflare.com/profile/api-tokens"
        echo "  2. Add to .env: CLOUDFLARE_API_TOKEN=your_token_here"
        exit 1
    else
        log "Cloudflare API token is configured"
    fi

    if [ -z "$ADMIN_EMAIL" ] || [ "$ADMIN_EMAIL" = "admin@example.com" ]; then
        warn "ADMIN_EMAIL should be updated to a valid email address"
    else
        log "Admin email: $ADMIN_EMAIL"
    fi
else
    info "TLS is disabled (USE_TLS=false)"
fi

# Check 4: Domain configuration
info "Checking domain configuration..."
log "Domain: ${DOMAIN:-not set}"
log "API: ${API_SUBDOMAIN:-api}.${DOMAIN:-not set}"
log "MQTT: ${MQTT_SUBDOMAIN:-mqtt}.${DOMAIN:-not set}"
log "Frontend: ${FRONTEND_SUBDOMAIN:-www}.${DOMAIN:-not set}"

# Check 5: Database configuration
info "Checking database configuration..."
if [ -z "$MONGO_CONNECTION_STRING" ] || [[ "$MONGO_CONNECTION_STRING" == *"<username>"* ]]; then
    warn "MongoDB connection string needs to be configured"
else
    log "MongoDB connection string is configured"
fi

# Check 6: Service secrets
info "Checking service secrets..."
SECRETS_OK=true

if [ "$BFF_SERVICE_CLIENT_SECRET" = "your_bff_secret" ]; then
    warn "BFF_SERVICE_CLIENT_SECRET should be changed"
    SECRETS_OK=false
fi

if [ "$SENSOR_SERVICE_CLIENT_SECRET" = "your_sensor_secret" ]; then
    warn "SENSOR_SERVICE_CLIENT_SECRET should be changed"
    SECRETS_OK=false
fi

if [ "$ACTUATOR_SERVICE_CLIENT_SECRET" = "your_actuator_secret" ]; then
    warn "ACTUATOR_SERVICE_CLIENT_SECRET should be changed"
    SECRETS_OK=false
fi

if [ "$SECRETS_OK" = true ]; then
    log "Service secrets appear to be configured"
fi

echo ""
info "=========================================="
log "Verification completed!"
info "=========================================="
echo ""

if [ "$ENVIRONMENT" = "Production" ]; then
    info "Production mode detected. To start services:"
    echo ""
    if [ "$USE_TLS" = "true" ]; then
        echo "  # First time (generate certificates):"
        echo "  export CERTBOT_ONLY=true"
        echo "  docker-compose --profile production up nginx-proxy certbot"
        echo ""
        echo "  # After certificates are generated:"
        echo "  export CERTBOT_ONLY=false"
        echo "  docker-compose --profile production up -d"
    else
        echo "  docker-compose --profile production up -d"
    fi
else
    info "Development mode. To start services:"
    echo ""
    echo "  docker-compose --profile development up -d"
fi

echo ""
