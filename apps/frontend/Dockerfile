# Multi-stage Dockerfile for frontend
# Development stage
FROM node:20-alpine as development

# Install pnpm
RUN npm install -g pnpm@10.16.1

# Set working directory to the root of the monorepo
WORKDIR /workspace

# Copy package.json files and pnpm workspace config
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY tsconfig.base.json ./

# Copy package.json files for all workspaces
COPY apps/frontend/package.json ./apps/frontend/
COPY packages/ ./packages/

# Install dependencies for all workspaces
RUN pnpm install --frozen-lockfile

# Copy source code
COPY apps/frontend/ ./apps/frontend/

# Set working directory to frontend app
WORKDIR /workspace/apps/frontend

EXPOSE 3000
CMD ["pnpm", "run", "dev", "--", "--host", "0.0.0.0"]

# Production build stage
FROM node:20-alpine as build

# Install pnpm
RUN npm install -g pnpm@10.16.1

# Set working directory to the root of the monorepo
WORKDIR /workspace

# Copy package.json files and pnpm workspace config
COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY tsconfig.base.json ./

# Copy package.json files for all workspaces
COPY apps/frontend/package.json ./apps/frontend/
COPY packages/ ./packages/

# Install dependencies for all workspaces
RUN pnpm install --frozen-lockfile

# Build workspace packages in dependency order
# 1. Build shared-types (no dependencies)
RUN pnpm --filter @hydroespinaca/shared-types build

# 2. Build shared-utils (depends on shared-types)
RUN pnpm --filter @hydroespinaca/shared-utils build

# 3. Build shared-hooks (depends on shared-types and shared-utils)
RUN pnpm --filter @hydroespinaca/shared-hooks build

# 4. Build shared-ui (no internal dependencies)
RUN pnpm --filter @hydroespinaca/shared-ui build

# Copy source code
COPY apps/frontend/ ./apps/frontend/

# Set working directory to frontend app
WORKDIR /workspace/apps/frontend

# Build for production
RUN pnpm run build

# Default command - the script will override this
CMD ["echo", "Frontend build completed"]

# Production stage with nginx
FROM nginx:1.25-alpine as production

# Copy built assets from build stage
COPY --from=build /workspace/apps/frontend/dist /usr/share/nginx/html

# Copy custom nginx config if needed
COPY apps/frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
