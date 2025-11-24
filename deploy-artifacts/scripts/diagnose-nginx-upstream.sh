#!/bin/bash

# =================================================
# Nginx Upstream Diagnostic Script
# Diagnoses connection issues between nginx and backend services
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

echo ""
info "=========================================="
info "  Nginx Upstream Diagnostic"
info "=========================================="
echo ""

# Load environment variables
if [ -f ".env" ]; then
    source .env
    log "Loaded .env configuration"
elif [ -f ".env.production" ]; then
    source .env.production
    log "Loaded .env.production configuration"
else
    warn "No .env file found, using defaults"
fi

ENVIRONMENT=${ENVIRONMENT:-Development}
COMPOSE_PROFILE=${COMPOSE_PROFILE:-development}

info "Current configuration:"
echo "  Environment: $ENVIRONMENT"
echo "  Profile: $COMPOSE_PROFILE"
echo ""

# Check Docker network
info "Checking Docker network 'hydroespinaca'..."
if docker network inspect hydroespinaca >/dev/null 2>&1; then
    log "Network 'hydroespinaca' exists"

    # List containers in the network
    CONTAINERS=$(docker network inspect hydroespinaca --format '{{range .Containers}}{{.Name}} {{end}}')
    if [ -n "$CONTAINERS" ]; then
        log "Containers in network: $CONTAINERS"
    else
        warn "No containers found in network 'hydroespinaca'"
    fi
else
    error "Network 'hydroespinaca' not found!"
    echo ""
    echo "Create it with:"
    echo "  docker network create hydroespinaca"
    exit 1
fi
echo ""

# Check which frontend service should be running
if [ "$COMPOSE_PROFILE" = "production" ] || [ "$ENVIRONMENT" = "Production" ]; then
    EXPECTED_FRONTEND="web-app"
    EXPECTED_PORT="3000"
else
    EXPECTED_FRONTEND="web-app-dev"
    EXPECTED_PORT="3000"
fi

info "Expected frontend service: $EXPECTED_FRONTEND:$EXPECTED_PORT"
echo ""

# Check if nginx-proxy is running
info "Checking nginx-proxy..."
if docker ps --format '{{.Names}}' | grep -q "^nginx-proxy$"; then
    log "nginx-proxy container is running"

    # Check nginx-proxy network connections
    NGINX_NETWORKS=$(docker inspect nginx-proxy --format '{{range $k, $v := .NetworkSettings.Networks}}{{$k}} {{end}}')
    log "nginx-proxy networks: $NGINX_NETWORKS"

    # Try to resolve frontend from nginx
    info "Testing DNS resolution from nginx-proxy..."
    if docker exec nginx-proxy nslookup $EXPECTED_FRONTEND 2>&1 | grep -q "Address"; then
        RESOLVED_IP=$(docker exec nginx-proxy nslookup $EXPECTED_FRONTEND 2>&1 | grep "Address" | tail -1 | awk '{print $2}')
        log "$EXPECTED_FRONTEND resolves to: $RESOLVED_IP"
    else
        error "Cannot resolve $EXPECTED_FRONTEND from nginx-proxy!"
        echo ""
        echo "Possible causes:"
        echo "  1. $EXPECTED_FRONTEND container is not running"
        echo "  2. $EXPECTED_FRONTEND is not in the same network as nginx-proxy"
        echo "  3. Wrong profile is being used"
    fi

    # Test connectivity
    info "Testing connectivity from nginx-proxy to $EXPECTED_FRONTEND:$EXPECTED_PORT..."
    if docker exec nginx-proxy sh -c "timeout 3 nc -zv $EXPECTED_FRONTEND $EXPECTED_PORT" 2>&1 | grep -q "open\|succeeded"; then
        log "Successfully connected to $EXPECTED_FRONTEND:$EXPECTED_PORT"
    else
        error "Cannot connect to $EXPECTED_FRONTEND:$EXPECTED_PORT!"
        echo ""
        echo "Possible causes:"
        echo "  1. $EXPECTED_FRONTEND container is not healthy"
        echo "  2. Port $EXPECTED_PORT is not exposed"
        echo "  3. Service is not ready yet"
    fi
else
    error "nginx-proxy container is not running!"
    echo ""
    echo "Start it with:"
    if [ "$COMPOSE_PROFILE" = "production" ]; then
        echo "  docker-compose --profile production up -d nginx-proxy"
    else
        echo "  docker-compose --profile development up -d nginx-proxy"
    fi
fi
echo ""

# Check if expected frontend is running
info "Checking $EXPECTED_FRONTEND..."
if docker ps --format '{{.Names}}' | grep -q "^$EXPECTED_FRONTEND$"; then
    log "$EXPECTED_FRONTEND container is running"

    # Check health status
    HEALTH_STATUS=$(docker inspect $EXPECTED_FRONTEND --format '{{.State.Health.Status}}' 2>/dev/null || echo "no healthcheck")
    if [ "$HEALTH_STATUS" = "healthy" ]; then
        log "$EXPECTED_FRONTEND is healthy"
    elif [ "$HEALTH_STATUS" = "unhealthy" ]; then
        error "$EXPECTED_FRONTEND is unhealthy!"
        echo ""
        echo "Check logs with:"
        echo "  docker logs $EXPECTED_FRONTEND"
    elif [ "$HEALTH_STATUS" = "starting" ]; then
        warn "$EXPECTED_FRONTEND healthcheck is still starting..."
    else
        warn "$EXPECTED_FRONTEND has no healthcheck configured"
    fi

    # Check networks
    FRONTEND_NETWORKS=$(docker inspect $EXPECTED_FRONTEND --format '{{range $k, $v := .NetworkSettings.Networks}}{{$k}} {{end}}')
    log "$EXPECTED_FRONTEND networks: $FRONTEND_NETWORKS"

    # Check if both nginx and frontend share a network
    if echo "$NGINX_NETWORKS" | grep -q "hydroespinaca" && echo "$FRONTEND_NETWORKS" | grep -q "hydroespinaca"; then
        log "Both nginx-proxy and $EXPECTED_FRONTEND are in 'hydroespinaca' network ✓"
    else
        error "Network mismatch detected!"
        echo "  nginx-proxy networks: $NGINX_NETWORKS"
        echo "  $EXPECTED_FRONTEND networks: $FRONTEND_NETWORKS"
    fi
else
    error "$EXPECTED_FRONTEND container is not running!"
    echo ""
    echo "Possible causes:"
    echo "  1. Wrong profile - you need to use: --profile $COMPOSE_PROFILE"
    echo "  2. Service failed to start - check logs"
    echo "  3. Service is defined but not started"
    echo ""
    echo "Check running containers:"
    echo "  docker ps --format 'table {{.Names}}\t{{.Status}}'"
    echo ""
    echo "Start the correct profile:"
    if [ "$COMPOSE_PROFILE" = "production" ]; then
        echo "  docker-compose --profile production up -d"
    else
        echo "  docker-compose --profile development up -d"
    fi
fi
echo ""

# Check bff-service (required by nginx)
info "Checking bff-service..."
if docker ps --format '{{.Names}}' | grep -q "^bff-service$"; then
    log "bff-service container is running"

    HEALTH_STATUS=$(docker inspect bff-service --format '{{.State.Health.Status}}' 2>/dev/null || echo "no healthcheck")
    if [ "$HEALTH_STATUS" = "healthy" ]; then
        log "bff-service is healthy"
    else
        warn "bff-service health status: $HEALTH_STATUS"
    fi
else
    error "bff-service container is not running!"
fi
echo ""

# Summary
info "=========================================="
info "  Diagnostic Summary"
info "=========================================="
echo ""

if docker ps --format '{{.Names}}' | grep -q "^nginx-proxy$" && \
   docker ps --format '{{.Names}}' | grep -q "^$EXPECTED_FRONTEND$" && \
   docker exec nginx-proxy nslookup $EXPECTED_FRONTEND >/dev/null 2>&1; then
    log "✓ All checks passed!"
    log "nginx-proxy should be able to connect to $EXPECTED_FRONTEND"
    echo ""
    echo "If you're still seeing errors, check nginx logs:"
    echo "  docker logs nginx-proxy -f"
else
    error "⚠ Issues detected"
    echo ""
    echo "Recommended actions:"
    echo "  1. Ensure you're using the correct profile:"
    echo "     export COMPOSE_PROFILE=$COMPOSE_PROFILE"
    echo ""
    echo "  2. Restart services with correct profile:"
    if [ "$COMPOSE_PROFILE" = "production" ]; then
        echo "     docker-compose --profile production down"
        echo "     docker-compose --profile production up -d"
    else
        echo "     docker-compose --profile development down"
        echo "     docker-compose --profile development up -d"
    fi
    echo ""
    echo "  3. Check logs for errors:"
    echo "     docker logs nginx-proxy"
    echo "     docker logs $EXPECTED_FRONTEND"
fi
echo ""
