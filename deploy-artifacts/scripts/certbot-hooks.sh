#!/bin/bash

# =================================================
# Certbot Hooks Script
# Handles certificate deployment and post-renewal actions
# =================================================

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[$(date)] CERTBOT-HOOK: $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] CERTBOT-HOOK WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] CERTBOT-HOOK ERROR: $1${NC}"
}

# Deploy hook: executed when certificates are successfully renewed
deploy_hook() {
    log "Certificate renewal successful for domain: $RENEWED_DOMAINS"
    log "Certificate path: $RENEWED_LINEAGE"
    
    # Check if nginx container is running
    if docker ps --format "table {{.Names}}" | grep -q "nginx-proxy"; then
        log "Reloading nginx configuration..."
        if docker exec nginx-proxy nginx -s reload; then
            log "Nginx reloaded successfully"
        else
            error "Failed to reload nginx"
            return 1
        fi
    else
        warn "Nginx container not running, skipping reload"
    fi
    
    # Update MQTT broker if certificates were renewed for MQTT domain
    if echo "$RENEWED_DOMAINS" | grep -q "mqtt\."; then
        log "MQTT certificates renewed, restarting MQTT broker..."
        if docker restart mqtt 2>/dev/null; then
            log "MQTT broker restarted successfully"
        else
            warn "Failed to restart MQTT broker or container not running"
        fi
    fi
    
    # Set proper permissions for certificates
    if [ -n "$RENEWED_LINEAGE" ] && [ -d "$RENEWED_LINEAGE" ]; then
        log "Setting certificate permissions..."
        chmod 644 "$RENEWED_LINEAGE"/*.pem
        chmod 600 "$RENEWED_LINEAGE"/privkey.pem
    fi
    
    log "Deploy hook completed successfully"
}

# Pre hook: executed before attempting renewal
pre_hook() {
    log "Starting certificate renewal process..."
    log "Ensuring webroot directory exists..."
    mkdir -p /var/www/certbot
    chmod 755 /var/www/certbot
}

# Post hook: executed after attempting renewal (success or failure)
post_hook() {
    log "Certificate renewal process completed"
    
    # Clean up any temporary files if needed
    if [ -d "/tmp/certbot" ]; then
        rm -rf /tmp/certbot
        log "Cleaned up temporary files"
    fi
}

# Handle script arguments
case "${1:-deploy}" in
    "deploy")
        deploy_hook
        ;;
    "pre")
        pre_hook
        ;;
    "post")
        post_hook
        ;;
    *)
        echo "Usage: $0 {deploy|pre|post}"
        echo ""
        echo "Hooks:"
        echo "  deploy - Execute when certificates are successfully renewed"
        echo "  pre    - Execute before attempting renewal"
        echo "  post   - Execute after renewal attempt (success or failure)"
        exit 1
        ;;
esac