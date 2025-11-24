# =================================================
# Minimal Nginx Configuration for Certbot Challenges
# No backend dependencies - only for certificate generation
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
    
    # HTTP Server - minimal for health checks (DNS-01 does not require HTTP challenges)
    server {
        listen 80;
        server_name ${DOMAIN} ${FRONTEND_DOMAIN} ${API_DOMAIN} ${MQTT_DOMAIN};

        # Default response for all requests
        location / {
            return 200 'DNS-01 challenge server - nginx is ready';
            add_header Content-Type text/plain;
        }

        # Health check endpoint
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}