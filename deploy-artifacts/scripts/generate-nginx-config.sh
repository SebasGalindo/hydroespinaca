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
ADMIN_EMAIL=${ADMIN_EMAIL:-admin@hydroespinaca.online}

# Derived variables
export API_DOMAIN="${API_SUBDOMAIN}.${DOMAIN}"

echo "[INFO] Generating Nginx config for $ENVIRONMENT environment (TLS: $USE_TLS)"
echo "[DEBUG] CERTBOT_ONLY variable: '${CERTBOT_ONLY:-NOT_SET}'"

# Configure behavior based on environment
if [ "$USE_TLS" = "true" ] && [ "$ENVIRONMENT" = "Production" ]; then
    echo "[INFO] Configuring for Production with TLS"
    
    # Check if this is certificate-only mode (no backend services)
    if [ "${CERTBOT_ONLY:-false}" = "true" ]; then
        echo "[INFO] Certificate-only mode: using minimal nginx config"
        
        # Use completely separate template for certbot
        envsubst '${API_DOMAIN}' \
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
        
        # Upstreams: only backend services for production
        export NGINX_UPSTREAMS="
            # Upstream for BFF Service
            upstream bff_backend {
                server bff-service:8080;
                keepalive 32;
            }"
        
        # HTTP config: serve frontend files and API in HTTP for challenges and fallback
        export NGINX_HTTP_CONFIG="
            # API routes to BFF Service (fallback HTTP)
            location /api/ {
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
            
            # Frontend routes - serve static files for production
            location / {
                root /var/www/frontend;
                index index.html;
                try_files \$uri \$uri/ /index.html;
                
                # Cache static assets
                location ~* \\.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)\$ {
                    expires 1y;
                    add_header Cache-Control \"public, immutable\";
                }
            }"
    fi
    
    # Redirect config: empty - no redirect, allow HTTP access for challenges
    export NGINX_REDIRECT_CONFIG=""
    
    # HTTPS server block
    export NGINX_HTTPS_SERVER="
    # HTTPS Server (Production)
    server {
        listen 443 ssl;
        http2 on;
        server_name ${API_DOMAIN};
        
        # SSL Configuration
        ssl_certificate /etc/letsencrypt/live/${API_DOMAIN}/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/${API_DOMAIN}/privkey.pem;
        
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
        
        # Frontend routes - serve static files from volume
        location / {
            root /var/www/frontend;
            index index.html;
            try_files \$uri \$uri/ /index.html;
            
            # Cache static assets
            location ~* \\.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)\$ {
                expires 1y;
                add_header Cache-Control \"public, immutable\";
            }
        }
        
        # Health check endpoint
        location /health {
            access_log off;
            return 200 \"healthy\\n\";
            add_header Content-Type text/plain;
        }
    }"
    
else
    echo "[INFO] Configuring for Development (HTTP only)"
    
    # Upstreams: include all services for development
    export NGINX_UPSTREAMS="
        # Upstream for BFF Service
        upstream bff_backend {
            server bff-service:8080;
            keepalive 32;
        }
        
        # Upstream for Frontend Service (development only)
        upstream frontend_backend {
            server frontend:3000;
            keepalive 32;
        }"
    
    # HTTP config: proxy directly
    export NGINX_HTTP_CONFIG="
            # API routes to BFF Service
            location /api/ {
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
            
            # Frontend routes
            location / {
                proxy_pass http://frontend_backend;
                proxy_set_header Host \$host;
                proxy_set_header X-Real-IP \$remote_addr;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_set_header X-Forwarded-Proto \$scheme;
                
                # WebSocket support for HMR
                proxy_http_version 1.1;
                proxy_set_header Upgrade \$http_upgrade;
                proxy_set_header Connection \"upgrade\";
                
                # Timeouts
                proxy_connect_timeout 60s;
                proxy_send_timeout 60s;
                proxy_read_timeout 60s;
            }"
    
    # Redirect config: empty (no redirect)
    export NGINX_REDIRECT_CONFIG=""
    
    # HTTPS server: empty (no HTTPS)
    export NGINX_HTTPS_SERVER=""
fi

# Generate the final nginx.conf
envsubst '${API_DOMAIN} ${NGINX_UPSTREAMS} ${NGINX_HTTP_CONFIG} ${NGINX_REDIRECT_CONFIG} ${NGINX_HTTPS_SERVER}' \
    < /etc/nginx/templates/nginx.conf.tpl > /etc/nginx/nginx.conf

echo "[INFO] Nginx configuration generated successfully"
echo "[INFO] Configuration preview:"
echo "=========================="
head -20 /etc/nginx/nginx.conf
echo "=========================="

# Test nginx configuration
nginx -t

echo "[INFO] Nginx configuration is valid"