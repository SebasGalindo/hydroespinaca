const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

// Get the default Metro config
const config = getDefaultConfig(__dirname);

// Add monorepo support
const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, '../..');

// Watch all files within the monorepo
config.watchFolders = [workspaceRoot];

// Let Metro know where to resolve packages and in what order
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules'),
];

// Add platform-specific extensions for React Native
config.resolver.platforms = ['native', 'android', 'ios', 'web'];

// Resolve .native.tsx files first for React Native
config.resolver.sourceExts = [
  'native.tsx',
  'native.ts', 
  'native.jsx',
  'native.js',
  ...config.resolver.sourceExts,
];

// Add alias support for monorepo packages
config.resolver.alias = {
  '@hydroespinaca/shared-ui': path.resolve(workspaceRoot, 'packages/shared-ui/src'),
  '@hydroespinaca/shared-hooks': path.resolve(workspaceRoot, 'packages/shared-hooks/src'),
  '@hydroespinaca/shared-utils': path.resolve(workspaceRoot, 'packages/shared-utils/src'),
  '@hydroespinaca/shared-types': path.resolve(workspaceRoot, 'packages/shared-types/src'),
};

module.exports = config;