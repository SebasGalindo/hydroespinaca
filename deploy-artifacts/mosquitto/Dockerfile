FROM eclipse-mosquitto:2.0

# Install dependencies for configuration generation and health checks
RUN apk add --no-cache gettext netcat-openbsd bash

# Copy mosquitto startup script
COPY deploy-artifacts/scripts/startup-mosquitto.sh /usr/local/bin/startup-mosquitto.sh
RUN chmod +x /usr/local/bin/startup-mosquitto.sh

# Set the entrypoint to our startup script
ENTRYPOINT ["startup-mosquitto.sh"]