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
      // Platform-specific resolvers for web - use source files but with stubs
      '@hydroespinaca/shared-ui': path.resolve(__dirname, '../../packages/shared-ui/src'),
      '@hydroespinaca/shared-hooks': path.resolve(__dirname, '../../packages/shared-hooks/dist/index.mjs'),
      '@hydroespinaca/shared-utils': path.resolve(__dirname, '../../packages/shared-utils/dist/index.mjs'),
      '@hydroespinaca/shared-types': path.resolve(__dirname, '../../packages/shared-types/dist/index.mjs'),
      // Legacy alias support
      '@shared-ui': path.resolve(__dirname, '../../packages/shared-ui/src'),
      // Explicitly stub React Native packages for web builds
      'react-native': path.resolve(__dirname, './src/stubs/react-native.ts'),
      'expo-secure-store': path.resolve(__dirname, './src/stubs/expo-secure-store.ts'),
      '@react-native-async-storage/async-storage': path.resolve(__dirname, './src/stubs/async-storage.ts'),
    },
    // Resolve .web.tsx files first for web platform
    extensions: ['.web.tsx', '.web.ts', '.tsx', '.ts', '.jsx', '.js'],
  },
  define: {
    // Define platform constants
    __DEV__: JSON.stringify(process.env.NODE_ENV === 'development'),
    // Add global to prevent React Native errors
    global: 'globalThis',
  },
  optimizeDeps: {
    // Force specific dependencies to be pre-bundled
    include: [
      'react',
      'react-dom',
      'react-router-dom'
    ],
    exclude: [
      // Exclude all React Native related packages
      'react-native',
      '@react-native-async-storage/async-storage', 
      'expo-secure-store',
      '@react-native-masked-view/masked-view',
      '@react-navigation/native',
      '@react-navigation/native-stack',
      'react-native-safe-area-context',
      'react-native-screens',
      'expo',
      'expo-status-bar',
      'react-native-web'
    ]
  }
})