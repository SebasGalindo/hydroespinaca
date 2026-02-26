import path from 'path';
import { fileURLToPath } from 'url';

// ESM equivalent of __dirname
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Standalone output mode for Docker production deployment
  // Generates a minimal self-contained build with only required dependencies
  output: 'standalone',

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
  webpack: (config, { dev, webpack }) => {
    config.resolve.alias = {
      ...config.resolve.alias,
      '@': path.resolve(__dirname, 'src'),
      'react-native-svg': false,
      'react-native': false,
      'expo-secure-store': false,
      '@react-native-async-storage/async-storage': false,
    };

    // Define __DEV__ global for cross-platform shared package compatibility
    config.plugins.push(
      new webpack.DefinePlugin({
        __DEV__: JSON.stringify(dev),
      })
    );

    return config;
  },
};

export default nextConfig;