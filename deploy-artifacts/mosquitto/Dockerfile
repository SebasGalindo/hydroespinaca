FROM eclipse-mosquitto:latest

# Install dependencies for configuration generation and health checks
RUN apk add --no-cache gettext netcat-openbsd bash

# Copy mosquitto startup script
COPY deploy-artifacts/scripts/startup-mosquitto.sh /usr/local/bin/startup-mosquitto.sh
RUN chmod +x /usr/local/bin/startup-mosquitto.sh

# Security Note (Sonar docker:S6471): Root required for Let's Encrypt certificate access
# - Certificates in /etc/letsencrypt must remain owned by root with 600/700 permissions
# - Mosquitto needs to read these certificates for TLS/SSL functionality
# - The startup script (startup-mosquitto.sh) drops privileges to 'mosquitto' user after certificate loading
# - This approach is safer than copying certs (would require frequent sync) or chmod (reduces security)
# Reviewed and accepted as necessary privileged operation with documented privilege drop
USER root

# Set the entrypoint to our startup script
ENTRYPOINT ["startup-mosquitto.sh"]