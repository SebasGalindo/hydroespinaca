# =================================================
# Nginx Configuration Template for HydroEspinaca
# Variables are substituted by envsubst
# =================================================

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    # Logging
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';
    
    access_log /var/log/nginx/access.log main;
    error_log /var/log/nginx/error.log warn;
    
    # Basic settings
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 16M;
    
    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;
    
    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    
    # Upstream for BFF Service
    upstream bff_backend {
        server bff-service:8080;
        keepalive 32;
    }
    
    # Upstream for Frontend Service
    upstream frontend_backend {
        server frontend:3000;
        keepalive 32;
    }
    
    # HTTP Server
    server {
        listen 80;
        server_name ${API_DOMAIN};
        
        # Certbot challenge location (always available)
        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }
        
        # Main location blocks - behavior depends on environment
        ${NGINX_HTTP_CONFIG}
        
        # Production mode: redirect to HTTPS  
        ${NGINX_REDIRECT_CONFIG}
    }
    
    # HTTPS Server (Production only)
    ${NGINX_HTTPS_SERVER}
}