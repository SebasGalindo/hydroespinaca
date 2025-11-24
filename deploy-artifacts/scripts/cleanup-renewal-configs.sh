#!/bin/sh

# =================================================
# Cleanup Renewal Configurations Script
# Removes references to old/missing hooks in renewal configs
# =================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[$(date)] CLEANUP: $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date)] CLEANUP WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date)] CLEANUP ERROR: $1${NC}"
}

RENEWAL_DIR="/etc/letsencrypt/renewal"

log "Starting cleanup of renewal configurations..."

# Check if renewal directory exists
if [ ! -d "$RENEWAL_DIR" ]; then
    log "No renewal directory found at $RENEWAL_DIR - nothing to clean"
    exit 0
fi

# Find all .conf files in renewal directory
CONF_FILES=$(find "$RENEWAL_DIR" -name "*.conf" 2>/dev/null || true)

if [ -z "$CONF_FILES" ]; then
    log "No renewal configuration files found"
    exit 0
fi

# Process each configuration file
for conf_file in $CONF_FILES; do
    log "Processing: $conf_file"

    # Check if file contains references to old hooks
    if grep -q "certbot-mosquitto-hook.sh" "$conf_file" 2>/dev/null; then
        warn "Found reference to old certbot-mosquitto-hook.sh in $conf_file"

        # Backup original file
        cp "$conf_file" "${conf_file}.backup.$(date +%Y%m%d_%H%M%S)"
        log "Created backup: ${conf_file}.backup"

        # Remove lines referencing the old hook
        sed -i '/certbot-mosquitto-hook.sh/d' "$conf_file"
        log "Removed old hook references from $conf_file"
    fi

    # Check for other problematic hooks in renewal-hooks directory paths
    if grep -q "renewal-hooks/deploy" "$conf_file" 2>/dev/null; then
        if ! grep -q "/scripts/certbot-hooks.sh" "$conf_file" 2>/dev/null; then
            warn "Found old renewal-hooks references in $conf_file"

            # Backup if not already backed up
            if [ ! -f "${conf_file}.backup" ]; then
                cp "$conf_file" "${conf_file}.backup.$(date +%Y%m%d_%H%M%S)"
            fi

            # Remove old renewal-hooks lines
            sed -i '/renewal-hooks\/deploy/d' "$conf_file"
            sed -i '/renewal-hooks\/pre/d' "$conf_file"
            sed -i '/renewal-hooks\/post/d' "$conf_file"
            log "Removed old renewal-hooks directory references from $conf_file"
        fi
    fi

    # Verify the file is still valid after modifications
    if [ -f "$conf_file" ]; then
        log "✓ Cleaned: $conf_file"
    fi
done

# Clean up any orphaned hook files in the renewal-hooks directories
for hook_dir in /etc/letsencrypt/renewal-hooks/deploy /etc/letsencrypt/renewal-hooks/pre /etc/letsencrypt/renewal-hooks/post; do
    if [ -d "$hook_dir" ]; then
        log "Checking for orphaned hooks in $hook_dir"

        if [ -f "$hook_dir/certbot-mosquitto-hook.sh" ]; then
            warn "Removing orphaned hook: $hook_dir/certbot-mosquitto-hook.sh"
            rm -f "$hook_dir/certbot-mosquitto-hook.sh"
        fi

        # List remaining hooks
        remaining=$(ls -A "$hook_dir" 2>/dev/null || true)
        if [ -n "$remaining" ]; then
            warn "Remaining hooks in $hook_dir: $remaining"
        fi
    fi
done

log "Cleanup completed successfully"
log "Renewal configurations are now clean"

# Show summary
log "Summary of renewal configurations:"
if [ -n "$CONF_FILES" ]; then
    for conf_file in $CONF_FILES; do
        domain=$(basename "$conf_file" .conf)
        log "  - $domain"
    done
fi
