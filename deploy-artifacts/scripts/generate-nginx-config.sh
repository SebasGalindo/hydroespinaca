#!/bin/sh

# =================================================
# Nginx Configuration Generator
# Generates nginx.conf from template based on environment
# =================================================

set -e

# Load environment variables
if [ -f "/app/.env" ]; then
    source /app/.env
elif [ -f "/.env" ]; then
    source /.env
fi

# Set defaults
USE_TLS=${USE_TLS:-false}
ENVIRONMENT=${ENVIRONMENT:-Development}
DOMAIN=${DOMAIN:-hydroespinaca.online}
API_SUBDOMAIN=${API_SUBDOMAIN:-api}
MQTT_SUBDOMAIN=${MQTT_SUBDOMAIN:-mqtt}
FRONTEND_SUBDOMAIN=${FRONTEND_SUBDOMAIN:-www}
ADMIN_EMAIL=${ADMIN_EMAIL:-admin@hydroespinaca.online}

# Derived variables
export API_DOMAIN="${API_SUBDOMAIN}.${DOMAIN}"
export MQTT_DOMAIN="${MQTT_SUBDOMAIN}.${DOMAIN}"
export FRONTEND_DOMAIN="${FRONTEND_SUBDOMAIN}.${DOMAIN}"

echo "[INFO] Generating Nginx config for $ENVIRONMENT environment (TLS: $USE_TLS)"
echo "[DEBUG] CERTBOT_ONLY variable: '${CERTBOT_ONLY:-NOT_SET}'"

# Configure behavior based on environment
if [ "$USE_TLS" = "true" ] && [ "$ENVIRONMENT" = "Production" ]; then
    echo "[INFO] Configuring for Production with TLS"
    
    # Check if this is certificate-only mode (no backend services)
    if [ "${CERTBOT_ONLY:-false}" = "true" ]; then
        echo "[INFO] Certificate-only mode: using minimal nginx config"
        
        # Use completely separate template for certbot
        envsubst '${DOMAIN} ${API_DOMAIN} ${MQTT_DOMAIN} ${FRONTEND_DOMAIN}' \
            < /etc/nginx/templates/nginx-certbot.conf.tpl > /etc/nginx/nginx.conf
        
        echo "[INFO] Minimal nginx configuration generated for certificate validation"
        echo "[INFO] Configuration preview:"
        echo "=========================="
        head -30 /etc/nginx/nginx.conf
        echo "=========================="
        
        # Test nginx configuration
        nginx -t
        echo "[INFO] Minimal nginx configuration is valid"
        exit 0
    else
        echo "[INFO] Full production mode: complete nginx config"
        
        # DNS Resolver for Docker (allows nginx to start even if backends not ready)
        export NGINX_RESOLVER="resolver 127.0.0.11 valid=10s ipv6=off;"

        # Upstreams with dynamic DNS resolution
        # Using variables forces nginx to re-resolve DNS on each request
        # This prevents startup failures when backends aren't ready yet
        export NGINX_UPSTREAMS="
            # Upstream for BFF Service
            upstream bff_backend {
                server bff-service:8080 max_fails=3 fail_timeout=30s;
                keepalive 32;
            }

            # Upstream for Next.js Frontend (production)
            upstream frontend_backend {
                server web-app:3000 max_fails=3 fail_timeout=30s;
                keepalive 32;
            }

            # Upstream for MQTT WebSockets
            upstream mqtt_websocket_backend {
                server mqtt:9002 max_fails=3 fail_timeout=30s;
                keepalive 32;
            }"
        
        # HTTP config: proxy to services in HTTP for challenges and fallback
        export NGINX_HTTP_CONFIG="
            # API routes to BFF Service (fallback HTTP)
            location /api/ {
                # CORS is handled by ASP.NET Core application

                proxy_pass http://bff_backend/;
                proxy_set_header Host \$host;
                proxy_set_header X-Real-IP \$remote_addr;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto \$scheme;

                # WebSocket support
                proxy_http_version 1.1;
                proxy_set_header Upgrade \$http_upgrade;
                proxy_set_header Connection \"upgrade\";

                # Timeouts
                proxy_connect_timeout 60s;
                proxy_send_timeout 60s;
                proxy_read_timeout 60s;
            }

            # Next.js frontend - proxy to Next.js server
            location / {
                proxy_pass http://frontend_backend;
                proxy_set_header Host \$host;
                proxy_set_header X-Real-IP \$remote_addr;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto \$scheme;

                # WebSocket support
                proxy_http_version 1.1;
                proxy_set_header Upgrade \$http_upgrade;
                proxy_set_header Connection \"upgrade\";

                # Timeouts
                proxy_connect_timeout 60s;
                proxy_send_timeout 60s;
                proxy_read_timeout 60s;
            }"
    fi
    
    # Redirect config: empty - no redirect, allow HTTP access for challenges
    export NGINX_REDIRECT_CONFIG=""
    
    # HTTPS server blocks - separate server for each domain
    # All domains use the wildcard certificate (covers ${DOMAIN} and *.${DOMAIN})
    export NGINX_HTTPS_SERVER="
    # =================================================
    # MAIN FRONTEND SERVER (hydroespinaca.online)
    # =================================================
    server {
        listen 443 ssl;
        http2 on;
        server_name ${DOMAIN};

        # SSL Configuration - using wildcard certificate
        ssl_certificate /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
        
        # SSL Security
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;
        ssl_session_cache shared:SSL:10m;
        ssl_session_timeout 10m;
        
        # HSTS
        add_header Strict-Transport-Security \"max-age=31536000; includeSubDomains\" always;

        # Next.js frontend - proxy to Next.js server
        location / {
            proxy_pass http://frontend_backend;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;

            # WebSocket support
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection \"upgrade\";

            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        # Health check endpoint
        location /health {
            access_log off;
            return 200 \"healthy\\n\";
            add_header Content-Type text/plain;
        }
    }
    
    # =================================================
    # WWW FRONTEND SERVER (www.hydroespinaca.online)
    # =================================================
    server {
        listen 443 ssl;
        http2 on;
        server_name ${FRONTEND_DOMAIN};

        # SSL Configuration - using wildcard certificate
        ssl_certificate /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
        
        # SSL Security
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;
        ssl_session_cache shared:SSL:10m;
        ssl_session_timeout 10m;
        
        # HSTS
        add_header Strict-Transport-Security \"max-age=31536000; includeSubDomains\" always;

        # Next.js frontend - proxy to Next.js server
        location / {
            proxy_pass http://frontend_backend;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;

            # WebSocket support
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection \"upgrade\";

            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }
    }

    # =================================================
    # API SERVER (api.hydroespinaca.online)
    # =================================================
    server {
        listen 443 ssl;
        http2 on;
        server_name ${API_DOMAIN};

        # SSL Configuration - using wildcard certificate
        ssl_certificate /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
        
        # SSL Security
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;
        ssl_session_cache shared:SSL:10m;
        ssl_session_timeout 10m;
        
        # HSTS
        add_header Strict-Transport-Security \"max-age=31536000; includeSubDomains\" always;
        
        # Rate limiting
        limit_req zone=api_limit burst=20 nodelay;
        
        # API routes to BFF Service
        location /api/ {
            # CORS is handled by ASP.NET Core application

            proxy_pass http://bff_backend/;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;
            proxy_set_header X-Forwarded-Host \$host;
            
            # WebSocket support
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection \"upgrade\";
            
            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }
        
        # Deny all other requests to API domain
        location / {
            return 404;
        }
    }
    
    # =================================================
    # MQTT WEBSOCKET SERVER (mqtt.hydroespinaca.online)
    # =================================================
    server {
        listen 443 ssl;
        http2 on;
        server_name ${MQTT_DOMAIN};

        # SSL Configuration - using wildcard certificate
        ssl_certificate /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
        
        # SSL Security
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;
        ssl_session_cache shared:SSL:10m;
        ssl_session_timeout 10m;
        
        # HSTS
        add_header Strict-Transport-Security \"max-age=31536000; includeSubDomains\" always;
        
        # MQTT WebSocket routes
        location /mqtt {
            proxy_pass http://mqtt_websocket_backend/;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;
            
            # WebSocket support
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection \"upgrade\";
            
            # Timeouts for long-lived connections
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 300s;
        }
        
        # Deny all other requests to MQTT domain
        location / {
            return 404;
        }
    }"
    
else
    echo "[INFO] Configuring for Development (HTTP only)"

    # DNS Resolver for Docker (allows nginx to start even if backends not ready)
    export NGINX_RESOLVER="resolver 127.0.0.11 valid=10s ipv6=off;"

    # Upstreams with dynamic DNS resolution (development)
    # Using max_fails and fail_timeout for resilience
    export NGINX_UPSTREAMS="
        # Upstream for BFF Service
        upstream bff_backend {
            server bff-service:8080 max_fails=3 fail_timeout=30s;
            keepalive 32;
        }

        # Upstream for Next.js Frontend (Development with HMR)
        upstream frontend_backend {
            server web-app-dev:3000 max_fails=3 fail_timeout=30s;
            keepalive 32;
        }"

    # HTTP config: proxy to Next.js server for development
    export NGINX_HTTP_CONFIG="
            # API routes to BFF Service
            location /api/ {
                # CORS is handled by ASP.NET Core application

                proxy_pass http://bff_backend/;
                proxy_set_header Host \$host;
                proxy_set_header X-Real-IP \$remote_addr;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto \$scheme;

                # WebSocket support
                proxy_http_version 1.1;
                proxy_set_header Upgrade \$http_upgrade;
                proxy_set_header Connection \"upgrade\";

                # Timeouts
                proxy_connect_timeout 60s;
                proxy_send_timeout 60s;
                proxy_read_timeout 60s;
            }

            # Next.js frontend - proxy to Next.js server
            location / {
                proxy_pass http://frontend_backend;
                proxy_set_header Host \$host;
                proxy_set_header X-Real-IP \$remote_addr;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto \$scheme;

                # WebSocket support for Next.js HMR in development
                proxy_http_version 1.1;
                proxy_set_header Upgrade \$http_upgrade;
                proxy_set_header Connection \"upgrade\";

                # Timeouts
                proxy_connect_timeout 60s;
                proxy_send_timeout 60s;
                proxy_read_timeout 60s;

                # Buffering
                proxy_buffering off;
            }"
    
    # Redirect config: empty (no redirect)
    export NGINX_REDIRECT_CONFIG=""
    
    # HTTPS server: empty (no HTTPS)
    export NGINX_HTTPS_SERVER=""
fi

# Generate the final nginx.conf
envsubst '${DOMAIN} ${API_DOMAIN} ${MQTT_DOMAIN} ${FRONTEND_DOMAIN} ${NGINX_RESOLVER} ${NGINX_UPSTREAMS} ${NGINX_HTTP_CONFIG} ${NGINX_REDIRECT_CONFIG} ${NGINX_HTTPS_SERVER}' \
    < /etc/nginx/templates/nginx.conf.tpl > /etc/nginx/nginx.conf

echo "[INFO] Nginx configuration generated successfully"
echo "[INFO] Configuration preview:"
echo "=========================="
head -20 /etc/nginx/nginx.conf
echo "=========================="

# Test nginx configuration
nginx -t

echo "[INFO] Nginx configuration is valid"