FROM nginx:1.25-alpine

# Install gettext for envsubst and bash for scripts
RUN apk add --no-cache gettext bash

# Copy nginx configuration generation script
COPY deploy-artifacts/scripts/generate-nginx-config.sh /usr/local/bin/generate-nginx-config.sh
RUN chmod +x /usr/local/bin/generate-nginx-config.sh

# Security Note (Sonar docker:S6471): Nginx requires root for privileged port binding
# - Master process runs as root to bind to privileged ports 80 (HTTP) and 443 (HTTPS)
# - Worker processes automatically run as 'nginx' user (configured in nginx.conf via 'user nginx;')
# - This is the official nginx architecture and follows industry-standard security practices
# - Root is required for:
#   1. Binding to ports < 1024 (80, 443) - Linux kernel restriction
#   2. Reading SSL/TLS certificates from /etc/letsencrypt
#   3. Managing worker processes with reduced privileges
# - All HTTP request handling occurs in non-privileged worker processes
# Reference: http://nginx.org/en/docs/ngx_core_module.html#user
# Reviewed and accepted as standard nginx deployment pattern

# Default command: generate config and start nginx with retry logic
CMD ["sh", "-c", "\
    /usr/local/bin/generate-nginx-config.sh && \
    # Test configuration - retry up to 10 times if upstreams not ready \
    for i in 1 2 3 4 5 6 7 8 9 10; do \
        if nginx -t 2>&1; then \
            echo '[INFO] Nginx configuration is valid, starting nginx...'; \
            break; \
        else \
            echo \"[WARN] Nginx config test failed (attempt $i/10), waiting for upstreams...\"; \
            sleep 3; \
        fi; \
        if [ $i -eq 10 ]; then \
            echo '[ERROR] Failed to start nginx after 10 attempts'; \
            exit 1; \
        fi; \
    done && \
    nginx -g 'daemon off;' \
"]