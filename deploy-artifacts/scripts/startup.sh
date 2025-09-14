#!/bin/bash

# =================================================
# HydroEspinaca Startup Script
# Environment-based Docker Compose management
# =================================================

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[$(date)] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] ERROR: $1${NC}"
}

info() {
    echo -e "${BLUE}[$(date)] INFO: $1${NC}"
}

# Load environment variables
if [ -f ".env" ]; then
    source .env
else
    error ".env file not found!"
    exit 1
fi

ENVIRONMENT=${ENVIRONMENT:-Development}
USE_TLS=${USE_TLS:-false}

info "Starting HydroEspinaca in $ENVIRONMENT mode with TLS=$USE_TLS"

# Validate required variables
if [ -z "$DOMAIN" ] || [ -z "$ADMIN_EMAIL" ]; then
    error "DOMAIN and ADMIN_EMAIL must be set in .env file"
    exit 1
fi

# Function to start development environment
start_development() {
    log "Starting Development Environment..."
    log "- HTTP only (no TLS)"
    log "- MQTT on port 1883 (internal)"
    log "- WebSocket on port 9001 (no TLS)"
    log "- Nginx configuration: HTTP proxy mode"
    
    # Ensure nginx template directory exists
    mkdir -p deploy-artifacts/nginx
    
    # Export profile for container environment
    export COMPOSE_PROFILE=development
    
    COMPOSE_FILE=docker-compose.yml docker compose --profile development up -d
    
    log "Development environment started successfully!"
    log "Services available at:"
    log "- API: http://api.$DOMAIN"
    log "- MQTT: mqtt:1883 (internal Docker network only)"
    log "- WebSocket: mqtt:9001 (internal Docker network only)"
}

# Function to start production environment
start_production() {
    log "Starting Production Environment..."
    log "- HTTPS with Let's Encrypt certificates"
    log "- MQTT TLS on port 8883"
    log "- WebSocket TLS on port 9002"
    log "- Nginx configuration: HTTPS with SSL termination"
    log "- Automatic certificate renewal with hooks"
    
    # Check if certificates directory exists
    if ! docker volume ls | grep -q certbot_certs; then
        log "Creating certificate volumes..."
        docker volume create certbot_certs
        docker volume create certbot_www
        docker volume create nginx_logs
    fi
    
    # Ensure nginx template directory exists
    mkdir -p deploy-artifacts/nginx
    
    # Export profile for container environment
    export COMPOSE_PROFILE=production
    
    # Generate initial certificates if needed
    log "Checking and generating certificates..."
    COMPOSE_FILE=docker-compose.yml docker compose --profile production run --rm certbot
    
    # Start all production services
    COMPOSE_FILE=docker-compose.yml docker compose --profile production up -d
    
    # Show status
    log "Production environment started successfully!"
    log "Services available at:"
    log "- API: https://api.$DOMAIN"
    log "- MQTT TLS: mqtt.$DOMAIN:8883"
    log "- MQTT WebSocket TLS: mqtt.$DOMAIN:9002"
    log ""
    log "Certificate renewal: Automatic every 12h with nginx reload hooks"
}

# Function to stop all services
stop_services() {
    log "Stopping all HydroEspinaca services..."

    # Detener todos los contenedores del perfil actual, eliminar redes, volúmenes e imágenes asociadas
    COMPOSE_FILE=docker-compose.yml docker compose --profile "$COMPOSE_PROFILE" down --rmi all --volumes --remove-orphans

    # Limpieza extra por si hay contenedores colgados
    docker ps -aq | xargs -r docker rm -f
    docker volume ls -q | xargs -r docker volume rm -f

    log "All services and related resources stopped"
}


# Function to show status
show_status() {
    log "HydroEspinaca Services Status:"
    COMPOSE_FILE=docker-compose.yml docker compose ps
    echo
    
    if [ "$USE_TLS" = "true" ]; then
        log "Certificate Status:"
        docker run --rm -v certbot_certs:/etc/letsencrypt certbot/certbot certificates
    fi
}

# Function to update certificates
renew_certificates() {
    if [ "$USE_TLS" = "true" ]; then
        log "Renewing certificates..."
        COMPOSE_FILE=docker-compose.yml docker compose exec certbot-renew certbot renew --quiet
        log "Certificates renewed"
    else
        warn "TLS is disabled. No certificates to renew."
    fi
}

# Main script logic
case "${1:-start}" in
    "start")
        if [ "$ENVIRONMENT" = "Production" ]; then
            start_production
        else
            start_development
        fi
        ;;
    "stop")
        stop_services
        ;;
    "restart")
        stop_services
        sleep 2
        if [ "$ENVIRONMENT" = "Production" ]; then
            start_production
        else
            start_development
        fi
        ;;
    "status")
        show_status
        ;;
    "renew")
        renew_certificates
        ;;
    "logs")
        COMPOSE_FILE=docker-compose.yml docker compose logs -f ${2:-}
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|renew|logs [service]}"
        echo ""
        echo "Commands:"
        echo "  start   - Start services based on ENVIRONMENT setting"
        echo "  stop    - Stop all services"
        echo "  restart - Restart all services"
        echo "  status  - Show service and certificate status"
        echo "  renew   - Renew SSL certificates"
        echo "  logs    - Show logs (optionally for specific service)"
        echo ""
        echo "Environment: $ENVIRONMENT"
        echo "TLS Enabled: $USE_TLS"
        exit 1
        ;;
esac