#!/bin/bash

# =================================================
# Certificate Validation and Generation Script
# HydroEspinaca Project
# =================================================

set -e

# Load environment variables
if [ -f "/app/.env" ]; then
    source /app/.env
elif [ -f "/.env" ]; then
    source /.env
fi

DOMAIN=${DOMAIN:-hydroespinaca.online}
API_SUBDOMAIN=${API_SUBDOMAIN:-api}
MQTT_SUBDOMAIN=${MQTT_SUBDOMAIN:-mqtt}
FRONTEND_SUBDOMAIN=${FRONTEND_SUBDOMAIN:-www}

# Derived domains
API_DOMAIN="${API_SUBDOMAIN}.${DOMAIN}"
MQTT_DOMAIN="${MQTT_SUBDOMAIN}.${DOMAIN}"
FRONTEND_DOMAIN="${FRONTEND_SUBDOMAIN}.${DOMAIN}"
EMAIL=${ADMIN_EMAIL:-admin@hydroespinaca.online}

CERTS_DIR="/etc/letsencrypt/live"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Check if certificate exists and is valid
check_cert() {
    local domain=$1
    local cert_path="$CERTS_DIR/$domain/fullchain.pem"
    
    if [ ! -f "$cert_path" ]; then
        warn "Certificate for $domain does not exist"
        return 1
    fi
    
    # Check if certificate expires in less than 30 days
    if openssl x509 -checkend 2592000 -noout -in "$cert_path" >/dev/null 2>&1; then
        log "Certificate for $domain is valid and expires in more than 30 days"
        return 0
    else
        warn "Certificate for $domain expires within 30 days or is invalid"
        return 1
    fi
}

# Generate certificate using certbot with DNS-01 challenge
generate_cert() {
    local domain=$1

    log "Generating certificate for $domain using DNS-01 challenge..."

    # Create Cloudflare credentials file if not exists
    if [ ! -f "/etc/letsencrypt/cloudflare.ini" ]; then
        log "Creating Cloudflare credentials file..."
        mkdir -p /etc/letsencrypt
        echo "dns_cloudflare_api_token = ${CLOUDFLARE_API_TOKEN}" > /etc/letsencrypt/cloudflare.ini
        chmod 600 /etc/letsencrypt/cloudflare.ini
        log "Cloudflare credentials file created"
    fi

    certbot certonly \
        --dns-cloudflare \
        --dns-cloudflare-credentials /etc/letsencrypt/cloudflare.ini \
        --email "$EMAIL" \
        --agree-tos \
        --no-eff-email \
        --force-renewal \
        -d "$domain" \
        --non-interactive

    if [ $? -eq 0 ]; then
        log "Certificate generated successfully for $domain"
        return 0
    else
        error "Failed to generate certificate for $domain"
        return 1
    fi
}

# Main certificate check and generation logic
main() {
    log "Starting certificate validation..."
    
    # Check if USE_TLS is enabled
    if [ "$USE_TLS" != "true" ]; then
        log "TLS is disabled (USE_TLS=$USE_TLS). Skipping certificate validation."
        exit 0
    fi

    log "Using DNS-01 challenge method with Cloudflare"

    # Generate wildcard certificate that covers all subdomains
    # This single certificate will work for:
    # - hydroespinaca.online
    # - *.hydroespinaca.online (www, api, mqtt, etc.)
    log "Checking wildcard certificate for ${DOMAIN} and *.${DOMAIN}"

    if ! check_cert "$DOMAIN"; then
        log "Generating wildcard certificate..."

        # Create Cloudflare credentials file if not exists
        if [ ! -f "/etc/letsencrypt/cloudflare.ini" ]; then
            log "Creating Cloudflare credentials file..."
            mkdir -p /etc/letsencrypt
            echo "dns_cloudflare_api_token = ${CLOUDFLARE_API_TOKEN}" > /etc/letsencrypt/cloudflare.ini
            chmod 600 /etc/letsencrypt/cloudflare.ini
            log "Cloudflare credentials file created"
        fi

        # Generate wildcard certificate
        certbot certonly \
            --dns-cloudflare \
            --dns-cloudflare-credentials /etc/letsencrypt/cloudflare.ini \
            --email "$EMAIL" \
            --agree-tos \
            --no-eff-email \
            --force-renewal \
            -d "$DOMAIN" \
            -d "*.${DOMAIN}" \
            --non-interactive

        if [ $? -eq 0 ]; then
            log "Wildcard certificate generated successfully"
        else
            error "Failed to generate wildcard certificate"
            return 1
        fi
    else
        log "Wildcard certificate is valid"
    fi

    log "Certificate validation completed"
}

# Handle script arguments
case "${1:-}" in
    "check")
        if [ -n "$2" ]; then
            check_cert "$2"
        else
            error "Usage: $0 check <domain>"
            exit 1
        fi
        ;;
    "generate")
        if [ -n "$2" ]; then
            generate_cert "$2"
        else
            error "Usage: $0 generate <domain>"
            exit 1
        fi
        ;;
    "renew")
        log "Renewing all certificates using DNS-01..."
        # Create Cloudflare credentials file if not exists
        if [ ! -f "/etc/letsencrypt/cloudflare.ini" ]; then
            log "Creating Cloudflare credentials file..."
            mkdir -p /etc/letsencrypt
            echo "dns_cloudflare_api_token = ${CLOUDFLARE_API_TOKEN}" > /etc/letsencrypt/cloudflare.ini
            chmod 600 /etc/letsencrypt/cloudflare.ini
        fi
        certbot renew --dns-cloudflare --dns-cloudflare-credentials /etc/letsencrypt/cloudflare.ini --quiet
        ;;
    *)
        main
        ;;
esac