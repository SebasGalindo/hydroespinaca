import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    allowedHosts: process.env.NODE_ENV === 'production' 
      ? (process.env.VITE_ALLOWED_HOSTS ? process.env.VITE_ALLOWED_HOSTS.split(',') : 'all')
      : 'all'
  },
  resolve: {
    alias: {
      // Platform-specific resolvers for web
      '@hydroespinaca/shared-ui': path.resolve(__dirname, '../../packages/shared-ui/src'),
      '@hydroespinaca/shared-hooks': path.resolve(__dirname, '../../packages/shared-hooks/src'),
      '@hydroespinaca/shared-utils': path.resolve(__dirname, '../../packages/shared-utils/src'),
      '@hydroespinaca/shared-types': path.resolve(__dirname, '../../packages/shared-types/src'),
      // Legacy alias support
      '@shared-ui': path.resolve(__dirname, '../../packages/shared-ui/src'),
    },
    // Resolve .web.tsx files first for web platform
    extensions: ['.web.tsx', '.web.ts', '.tsx', '.ts', '.jsx', '.js'],
  },
})