FROM nginx:1.25-alpine

# Install gettext for envsubst and bash for scripts
RUN apk add --no-cache gettext bash

# Copy nginx configuration generation script
COPY deploy-artifacts/scripts/generate-nginx-config.sh /usr/local/bin/generate-nginx-config.sh
RUN chmod +x /usr/local/bin/generate-nginx-config.sh

# Create necessary directories
RUN mkdir -p /var/www/certbot/.well-known/acme-challenge && \
    chmod -R 755 /var/www/certbot

# Default command: generate config and start nginx
CMD ["sh", "-c", "/usr/local/bin/generate-nginx-config.sh && nginx -g 'daemon off;'"]