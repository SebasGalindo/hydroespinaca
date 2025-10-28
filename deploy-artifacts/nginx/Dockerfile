FROM nginx:1.25-alpine

# Install gettext for envsubst and bash for scripts
RUN apk add --no-cache gettext bash

# Copy nginx configuration generation script
COPY deploy-artifacts/scripts/generate-nginx-config.sh /usr/local/bin/generate-nginx-config.sh
RUN chmod +x /usr/local/bin/generate-nginx-config.sh

# Create necessary directories
RUN mkdir -p /var/www/certbot/.well-known/acme-challenge && \
    chmod -R 755 /var/www/certbot

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

# Default command: generate config and start nginx
CMD ["sh", "-c", "/usr/local/bin/generate-nginx-config.sh && nginx -g 'daemon off;'"]