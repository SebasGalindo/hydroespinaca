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

# Function to run basic diagnostics before certificate generation
run_certbot_diagnostics() {
    log "=== PRE-CERTIFICATE DIAGNOSTICS ==="
    
    log "1. Checking nginx-proxy container status:"
    docker ps | grep nginx-proxy || warn "nginx-proxy container not found"
    
    log "2. Checking if ports are exposed:"
    netstat -tlnp | grep ":80\|:443" || warn "Ports 80/443 not found in netstat"
    
    log "3. Testing local HTTP access:"
    if curl -I -m 5 http://localhost:80 >/dev/null 2>&1; then
        log "✅ Local HTTP access working"
    else
        warn "❌ Local HTTP access failed"
    fi
    
    log "4. Testing domain HTTP access locally:"
    if curl -I -m 5 -H "Host: ${API_DOMAIN}" http://localhost:80 >/dev/null 2>&1; then
        log "✅ Domain HTTP access working locally"
    else
        warn "❌ Domain HTTP access failed locally"
    fi
    
    log "=== END PRE-CERTIFICATE DIAGNOSTICS ==="
}

# Function to run detailed diagnostics when certificate generation fails
run_detailed_diagnostics() {
    error "=== DETAILED CERTIFICATE FAILURE DIAGNOSTICS ==="
    
    error "1. nginx-proxy logs (last 20 lines):"
    docker logs nginx-proxy | tail -20 || error "Could not get nginx logs"
    
    error "2. nginx configuration:"
    docker exec nginx-proxy head -100 /etc/nginx/nginx.conf | tail -30 || error "Could not get nginx config"
    
    error "3. Certbot webroot directory:"
    docker run --rm -v hydroespinaca_certbot_www:/check alpine ls -la /check || error "Could not check certbot volume"
    
    error "4. Testing challenge path from nginx container:"
    docker exec nginx-proxy ls -la /var/www/certbot/.well-known/acme-challenge/ || error "Challenge directory not accessible"
    
    error "5. Network connectivity test:"
    docker exec nginx-proxy wget -O- --timeout=5 http://localhost/.well-known/acme-challenge/test 2>&1 | head -5 || error "Network test failed"
    
    error "=== END DETAILED DIAGNOSTICS ==="
    error "Please check the above output for issues and ensure:"
    error "  - Oracle Cloud Security Groups allow inbound traffic on port 80"
    error "  - Cloudflare DNS is set to 'DNS only' (not proxied)"
    error "  - Domain ${API_DOMAIN} points to this server's public IP"
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
API_DOMAIN="${API_SUBDOMAIN:-api}.${DOMAIN}"

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
    
    # Use legacy Docker builder to avoid buildx issues
    export DOCKER_BUILDKIT=0
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
    
    # Use legacy Docker builder to avoid buildx issues
    export DOCKER_BUILDKIT=0
    
    # Build frontend static files first
    log "Building frontend for production..."
    COMPOSE_FILE=docker-compose.yml docker compose --profile production run --rm frontend-build
    
    # Start nginx-proxy in certificate-only mode first (no backend dependencies)
    log "Starting nginx-proxy in certificate-only mode..."
    export CERTBOT_ONLY=true
    log "DEBUG: CERTBOT_ONLY is set to: $CERTBOT_ONLY"
    COMPOSE_FILE=docker-compose.yml CERTBOT_ONLY=true docker compose --profile production up -d nginx-proxy
    
    # Wait for nginx-proxy to be ready
    log "Waiting for nginx-proxy to be ready..."
    sleep 10
    
    # Verify nginx is responding on port 80
    if ! curl -f -s http://localhost:80 >/dev/null 2>&1; then
        warn "nginx-proxy may not be ready yet, waiting additional time..."
        sleep 15
    fi
    
    # Run diagnostic before attempting certificate generation
    log "Running pre-certificate diagnostics..."
    run_certbot_diagnostics
    
    # Generate initial certificates if needed
    log "Checking and generating certificates..."
    if ! COMPOSE_FILE=docker-compose.yml docker compose --profile production run --rm certbot; then
        error "Certificate generation failed. Running detailed diagnostics..."
        run_detailed_diagnostics
        exit 1
    fi
    
    # Certificates generated successfully - now restart nginx with full configuration
    log "Certificates ready. Restarting nginx-proxy with full configuration..."
    export CERTBOT_ONLY=false
    docker stop nginx-proxy
    sleep 2
    COMPOSE_FILE=docker-compose.yml docker compose --profile production up -d nginx-proxy
    
    # Wait for nginx to be ready with full config
    sleep 5
    
    # Start all remaining production services
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

    COMPOSE_FILE=docker-compose.yml docker compose --profile "$COMPOSE_PROFILE" down --rmi all --remove-orphans

    docker ps -aq | xargs -r docker rm -f

    log "All services stopped. Certificate volumes (certbot_certs, certbot_www, nginx_logs) were preserved."
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