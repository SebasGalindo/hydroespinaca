const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, '../..');

const config = getDefaultConfig(projectRoot);

// Agregar extensiones native-specific manteniendo los defaults de Expo
config.resolver.sourceExts = [
  'native.tsx',
  'native.ts',
  ...config.resolver.sourceExts,
];

// Resolver prioridad - asegura que se use 'react-native' field primero
config.resolver.resolverMainFields = ['react-native', 'browser', 'main'];

// Watch folders - incluye workspace root además de los defaults de Expo
config.watchFolders = [
  ...config.watchFolders,
  workspaceRoot,
];

// Node modules paths - busca en proyecto y workspace root
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules'),
];

module.exports = config;
