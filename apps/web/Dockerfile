# =================================================
# Multi-stage Dockerfile for Next.js Web App
# =================================================

# -----------------------------
# Development Stage
# Hot-reloading with Turbopack
# -----------------------------
FROM node:20-alpine AS development

# Install pnpm and curl (for healthcheck)
RUN npm install -g pnpm@10.16.1 && \
    apk add --no-cache curl

# Set working directory to monorepo root
WORKDIR /workspace

# Copy package.json files and pnpm workspace config
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY tsconfig.base.json ./
COPY tools/ ./tools/

# Copy package.json files for all workspaces
COPY apps/web/package.json ./apps/web/
COPY packages/ ./packages/

# Install dependencies for all workspaces
RUN pnpm install --frozen-lockfile

# Copy source code
COPY apps/web/ ./apps/web/

# Set working directory to web app
WORKDIR /workspace/apps/web

# Expose Next.js dev server port
EXPOSE 3000

# Start Next.js dev server with Turbopack on all interfaces
# Script already includes --hostname 0.0.0.0
CMD ["pnpm", "run", "dev"]

# -----------------------------
# Dependencies Stage
# Separate stage for dependency installation
# -----------------------------
FROM node:20-alpine AS deps

# Install pnpm
RUN npm install -g pnpm@10.16.1

# Set working directory to monorepo root
WORKDIR /workspace

# Copy package.json files and pnpm workspace config
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY tsconfig.base.json ./
COPY tools/ ./tools/

# Copy package.json files for all workspaces
COPY apps/web/package.json ./apps/web/
COPY packages/shared/package.json ./packages/shared/

# Install dependencies with frozen lockfile (production + dev)
RUN pnpm install --frozen-lockfile

# -----------------------------
# Builder Stage
# Build Next.js app and shared package
# -----------------------------
FROM node:20-alpine AS builder

# Install pnpm
RUN npm install -g pnpm@10.16.1

# Set working directory to monorepo root
WORKDIR /workspace

# Copy dependencies from deps stage
COPY --from=deps /workspace/node_modules ./node_modules
COPY --from=deps /workspace/apps/web/node_modules ./apps/web/node_modules
COPY --from=deps /workspace/packages/shared/node_modules ./packages/shared/node_modules

# Copy package.json files
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY tsconfig.base.json ./
COPY tools/ ./tools/
COPY apps/web/package.json ./apps/web/
COPY packages/shared/package.json ./packages/shared/

# Copy source code for shared package
COPY packages/shared/ ./packages/shared/

# Build shared package first
RUN pnpm --filter @hydroespinaca/shared build

# Copy web app source code
COPY apps/web/ ./apps/web/

# Set working directory to web app
WORKDIR /workspace/apps/web

# Build Next.js application for production
# Output: .next/ directory with optimized production build
RUN pnpm run build

# -----------------------------
# Production Runner Stage
# Minimal image with only production artifacts
# -----------------------------
FROM node:20-alpine AS runner

# Set working directory
WORKDIR /app

# Install dumb-init and curl for proper signal handling and healthchecks
RUN apk add --no-cache dumb-init curl

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy standalone server and dependencies from builder
# Next.js standalone mode creates a minimal self-contained server
COPY --from=builder --chown=nextjs:nodejs /workspace/apps/web/.next/standalone ./

# Copy static assets
COPY --from=builder --chown=nextjs:nodejs /workspace/apps/web/.next/static ./apps/web/.next/static
COPY --from=builder --chown=nextjs:nodejs /workspace/apps/web/public ./apps/web/public

# Switch to non-root user
USER nextjs

# Expose Next.js production server port
EXPOSE 3000

# Set environment to production
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

# Use dumb-init to handle signals properly (PID 1)
ENTRYPOINT ["dumb-init", "--"]

# Start Next.js production server using standalone server
# The standalone output includes a minimal Node.js server at apps/web/server.js
CMD ["node", "apps/web/server.js"]
