import path from 'path';
import { fileURLToPath } from 'url';

// ESM equivalent of __dirname
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Default Next.js output for flexible deployment
  // Can be served via standalone server or through nginx proxy
  // Supports all Next.js features including dynamic routes

  turbopack: {
    rules: {
      '*.svg': {
        loaders: ['@svgr/webpack'],
        as: '*.js',
      },
    },
  },
  transpilePackages: ['@hydroespinaca/shared'],
  // Solución temporal para React 19 + Next.js 15 prerendering error
  experimental: {
    ppr: false, // Deshabilitar Partial Prerendering
  },
  // Configuración para evitar errores de prerendering
  eslint: {
    ignoreDuringBuilds: false,
  },
  typescript: {
    ignoreBuildErrors: false,
  },
 webpack: (config) => {
    config.resolve.alias = {
      ...config.resolve.alias,
      '@': path.resolve(__dirname, 'src'),
      'react-native-svg': false,
      'react-native': false,
      'expo-secure-store': false,
      '@react-native-async-storage/async-storage': false,
    };
    return config;
  },
};

export default nextConfig;