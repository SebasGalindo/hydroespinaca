# Dockerfile for fuzzy-service
# Clean Architecture implementation with secure dependencies
FROM python:3.12.11-slim

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY software-project/fuzzy-service/requirements.txt .

# Install Python dependencies
# Security: Using --no-cache-dir to avoid caching potentially vulnerable packages
# Security: Pinning versions in requirements.txt to prevent supply chain attacks
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY software-project/fuzzy-service/ .

# Create non-root user for security
RUN useradd --create-home --shell /bin/bash fuzzy && \
    chown -R fuzzy:fuzzy /app
USER fuzzy

# Expose port
EXPOSE 8000

# Install curl for health check
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER fuzzy

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Run application
CMD ["uvicorn", "FuzzyService.Api.main:app", "--host", "0.0.0.0", "--port", "8000"]
