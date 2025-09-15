# Multi-stage Dockerfile for frontend
# Development stage
FROM node:20-alpine as development

# Install pnpm
RUN npm install -g pnpm@10.12.1

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
RUN npm install -g pnpm@10.12.1

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

# Build for production
RUN pnpm run build

# Command to copy built files to the shared volume location
CMD ["sh", "-c", "cp -r dist/* /var/www/frontend/ && echo 'Frontend build completed and copied to volume'"]

# Production stage with nginx
FROM nginx:1.25-alpine as production

# Copy built assets from build stage
COPY --from=build /workspace/apps/frontend/dist /usr/share/nginx/html

# Copy custom nginx config if needed
COPY apps/frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
