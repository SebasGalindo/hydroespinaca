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
      '@shared-ui': path.resolve(__dirname, '../../packages/shared-ui/src'),
    },
  },
})