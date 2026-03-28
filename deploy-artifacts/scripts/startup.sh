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

# Ensure required Docker network exists before compose startup
ensure_docker_network() {
    local network_name="${COMPOSE_NETWORK_NAME:-hydroespinaca}"

    if docker network inspect "$network_name" >/dev/null 2>&1; then
        info "Docker network '$network_name' already exists"
        return 0
    fi

    warn "Docker network '$network_name' not found. Creating it..."
    docker network create "$network_name" >/dev/null
    log "✓ Docker network '$network_name' created"
}

# =================================================
# FRONTEND BUILD FUNCTION
# Development: Build shared on host (required for bind mounts)
# Production: Build shared in Docker (no host dependencies)
# =================================================
build_shared_dependencies() {
    log "=== Shared Dependencies Build ==="

    # In production, shared is built inside Docker
    if [ "$ENVIRONMENT" = "Production" ]; then
        info "Production mode: Shared package will be built inside Docker"
        log "=== Configuration Complete ==="
        return 0
    fi

    # In development, we need shared built on host for bind mounts
    log "Development mode: Building shared package on host..."

    # Navigate to project root (script is in deploy-artifacts/scripts/)
    cd "$(dirname "$0")/../.."
    info "Current directory: $(pwd)"

    # Check if pnpm is available
    if ! command -v pnpm &> /dev/null; then
        error "pnpm is NOT installed on host!"
        error ""
        error "For DEVELOPMENT mode, you need pnpm installed because:"
        error "  1. web-app-dev uses bind mounts (live code reload)"
        error "  2. The host code needs packages/shared/dist/ compiled"
        error ""
        error "Options:"
        error "  A) Install pnpm on host (recommended for development):"
        error "     npm install -g pnpm"
        error "     OR: curl -fsSL https://get.pnpm.io/install.sh | sh -"
        error ""
        error "  B) Switch to Production mode (builds everything in Docker):"
        error "     Edit .env and set: ENVIRONMENT=Production"
        error ""
        exit 1
    fi

    # Install dependencies if needed
    log "Installing dependencies..."
    if ! pnpm install --frozen-lockfile 2>/dev/null; then
        warn "Failed to install with frozen-lockfile, trying regular install..."
        pnpm install || {
            error "Failed to install dependencies!"
            exit 1
        }
    fi

    # Build shared package
    log "Building @hydroespinaca/shared package..."
    if ! pnpm --filter @hydroespinaca/shared build; then
        error "Failed to build shared package!"
        error "Try running manually: pnpm --filter @hydroespinaca/shared build"
        exit 1
    fi

    # Verify dist folder was created
    if [ ! -d "packages/shared/dist" ]; then
        error "Build succeeded but dist folder not found!"
        exit 1
    fi

    log "✓ Shared package built successfully at packages/shared/dist/"
    log "=== Shared Dependencies Build Complete ==="
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

# =================================================
# DEVELOPMENT ENVIRONMENT
# HTTP only, no TLS, serves built frontend via nginx
# =================================================
start_development() {
    log "Starting Development Environment..."
    log "- HTTP only (no TLS)"
    log "- MQTT on port 1883 (internal)"
    log "- WebSocket on port 9001 (no TLS)"
    log "- Nginx configuration: HTTP proxy mode"
    log "- Frontend: Next.js server with HMR (Hot Module Replacement)"

    # Ensure compose network exists for services
    ensure_docker_network

    # ===== STEP 1: Build Shared Dependencies =====
    build_shared_dependencies

    # Ensure nginx template directory exists
    mkdir -p deploy-artifacts/nginx

    # Export profile and build target for container environment
    export COMPOSE_PROFILE=development
    export WEB_TARGET=development
    export NODE_ENV=development

    # Check buildx version and set appropriate build mode
    BUILDX_VERSION=$(docker buildx version 2>/dev/null | grep -oP 'v\K[0-9]+\.[0-9]+' | head -1)
    if [ -n "$BUILDX_VERSION" ] && [ "${BUILDX_VERSION%%.*}" -ge 17 ]; then
        info "Using BuildKit (buildx >= 0.17)"
        export DOCKER_BUILDKIT=1
        export COMPOSE_DOCKER_CLI_BUILD=1
    else
        warn "Buildx version is < 0.17 or not available. Using legacy builder."
        warn "Consider upgrading: docker buildx install"
        export DOCKER_BUILDKIT=0
        unset COMPOSE_DOCKER_CLI_BUILD
    fi

    # ===== STEP 2: Start Docker Services =====
    log "Starting Docker services..."
    COMPOSE_FILE=docker-compose.yml docker compose --profile development up -d

    log "Development environment started successfully!"
    log "Services available at:"
    log "- Frontend: http://$DOMAIN (proxied to Next.js dev server)"
    log "- API: http://api.$DOMAIN/api"
    log "- MQTT: mqtt:1883 (internal Docker network only)"
    log "- WebSocket: mqtt:9001 (internal Docker network only)"
    log ""
    info "Note: Frontend is served by Next.js dev server with HMR enabled"
}

# =================================================
# PRODUCTION ENVIRONMENT
# HTTPS with Let's Encrypt, serves built frontend via nginx
# =================================================
start_production() {
    log "Starting Production Environment..."
    log "- HTTPS with Let's Encrypt certificates"
    log "- MQTT TLS on port 8883"
    log "- WebSocket TLS on port 9002"
    log "- Nginx configuration: HTTPS with SSL termination and reverse proxy"
    log "- Automatic certificate renewal with hooks"
    log "- Frontend: Next.js production server"

    # Ensure compose network exists for services
    ensure_docker_network

    # ===== STEP 1: Build Shared Dependencies =====
    build_shared_dependencies

    # Check if certificates directory exists
    if ! docker volume ls | grep -q certbot_certs; then
        log "Creating certificate volumes..."
        docker volume create certbot_certs
        docker volume create certbot_www
        docker volume create nginx_logs
    fi

    # Ensure nginx template directory exists
    mkdir -p deploy-artifacts/nginx

    # Export profile and build target for container environment
    export COMPOSE_PROFILE=production
    export WEB_TARGET=production
    export NODE_ENV=production

    # Check buildx version and set appropriate build mode
    BUILDX_VERSION=$(docker buildx version 2>/dev/null | grep -oP 'v\K[0-9]+\.[0-9]+' | head -1)
    if [ -n "$BUILDX_VERSION" ] && [ "${BUILDX_VERSION%%.*}" -ge 17 ]; then
        info "Using BuildKit (buildx >= 0.17)"
        export DOCKER_BUILDKIT=1
        export COMPOSE_DOCKER_CLI_BUILD=1
    else
        warn "Buildx version is < 0.17 or not available. Using legacy builder."
        warn "Consider upgrading: docker buildx install"
        export DOCKER_BUILDKIT=0
        unset COMPOSE_DOCKER_CLI_BUILD
    fi

    # ===== STEP 2: Start nginx in certificate-only mode =====
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

    # ===== STEP 3: Generate certificates =====
    log "Running pre-certificate diagnostics..."
    run_certbot_diagnostics

    # Generate initial certificates if needed
    log "Checking and generating certificates..."
    if ! COMPOSE_FILE=docker-compose.yml docker compose --profile production run --rm certbot; then
        error "Certificate generation failed. Running detailed diagnostics..."
        run_detailed_diagnostics
        exit 1
    fi

    # ===== STEP 4: Restart nginx with full configuration =====
    log "Certificates ready. Restarting nginx-proxy with full configuration..."
    export CERTBOT_ONLY=false
    docker stop nginx-proxy
    sleep 2
    COMPOSE_FILE=docker-compose.yml docker compose --profile production up -d nginx-proxy

    # Wait for nginx to be ready with full config
    sleep 5

    # ===== STEP 5: Start all remaining services =====
    log "Starting remaining production services..."
    COMPOSE_FILE=docker-compose.yml docker compose --profile production up -d

    # Show status
    log "Production environment started successfully!"
    log "Services available at:"
    log "- Frontend: https://$DOMAIN (proxied to Next.js server)"
    log "- API: https://api.$DOMAIN/api"
    log "- MQTT TLS: mqtt.$DOMAIN:8883"
    log "- MQTT WebSocket TLS: mqtt.$DOMAIN:9002"
    log ""
    log "Certificate renewal: Automatic every 12h with nginx reload hooks"
    info "Note: Frontend is served by Next.js production server with all features enabled"
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
        log "Certificate Status (mounted in Mosquitto):"
        # Muestra los certificados que realmente usa Mosquitto
        if [ -d "/srv/mqtt/certs" ]; then
            ls -la /srv/mqtt/certs
        else
            warn "MQTT certificates directory not found at /srv/mqtt/certs"
        fi
    fi

    # Show frontend container status
    log "Frontend Container Status:"
    if docker ps | grep -q web-app; then
        docker ps | grep web-app
        log "Frontend is running inside Docker container 'web-app'"
    else
        warn "Frontend container 'web-app' not found or not running"
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
    "build")
        # Rebuild Docker images (includes shared dependencies)
        log "Rebuilding Docker images..."
        info "This will rebuild all images including shared dependencies"

        if [ "$ENVIRONMENT" = "Production" ]; then
            export COMPOSE_PROFILE=production
            export WEB_TARGET=production
            export NODE_ENV=production
        else
            export COMPOSE_PROFILE=development
            export WEB_TARGET=development
            export NODE_ENV=development
        fi

        COMPOSE_FILE=docker-compose.yml docker compose --profile "$COMPOSE_PROFILE" build
        log "Build complete. Restart services to apply changes: $0 restart"
        ;;
    "refresh-frontend"|"frontend")
        # Refresh frontend only - rebuild Docker image (includes shared package build)
        log "=== Refreshing Frontend Only ==="
        log "Backend services will remain running"
        log "Shared package will be rebuilt inside Docker during image build"

        # Determine the correct profile and container name based on ENVIRONMENT
        if [ "$ENVIRONMENT" = "Production" ]; then
            export COMPOSE_PROFILE=production
            export WEB_TARGET=production
            export NODE_ENV=production
            FRONTEND_CONTAINER="web-app"
            log "Using production frontend container: $FRONTEND_CONTAINER"
        else
            export COMPOSE_PROFILE=development
            export WEB_TARGET=development
            export NODE_ENV=development
            FRONTEND_CONTAINER="web-app-dev"
            log "Using development frontend container: $FRONTEND_CONTAINER"
        fi

        # Rebuild and restart only the appropriate web container
        log "Step 1/2: Rebuilding $FRONTEND_CONTAINER Docker image..."
        COMPOSE_FILE=docker-compose.yml docker compose --profile "$COMPOSE_PROFILE" build "$FRONTEND_CONTAINER"

        log "Step 2/2: Restarting $FRONTEND_CONTAINER container..."
        COMPOSE_FILE=docker-compose.yml docker compose --profile "$COMPOSE_PROFILE" up -d --no-deps --force-recreate "$FRONTEND_CONTAINER"

        log "=== Frontend Refresh Complete ==="
        log "Frontend container '$FRONTEND_CONTAINER' restarted. Backend services unchanged."
        log "Check logs with: $0 logs $FRONTEND_CONTAINER"
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|renew|logs [service]|build|refresh-frontend}"
        echo ""
        echo "Commands:"
        echo "  start            - Start services based on ENVIRONMENT setting"
        echo "  stop             - Stop all services"
        echo "  restart          - Restart all services"
        echo "  status           - Show service and certificate status"
        echo "  renew            - Renew SSL certificates"
        echo "  logs [service]   - Show logs (optionally for specific service)"
        echo "  build            - Build shared dependencies (without restarting services)"
        echo "  refresh-frontend - Rebuild and restart ONLY the frontend (backend stays running)"
        echo ""
        echo "Aliases:"
        echo "  frontend         - Same as refresh-frontend"
        echo ""
        echo "Environment: $ENVIRONMENT"
        echo "TLS Enabled: $USE_TLS"
        exit 1
        ;;
esac
