# Dockerfile for frontend
FROM node:20-alpine

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
