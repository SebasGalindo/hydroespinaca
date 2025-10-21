const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, '../..');
const sharedRoot = path.resolve(workspaceRoot, 'packages/shared');

const config = getDefaultConfig(projectRoot);

// Extensiones compatibles
config.resolver.sourceExts = [
  'native.tsx',
  'native.ts',
  'tsx',
  'ts',
  'jsx',
  'js',
  'json',
];

// Resolver prioridad
config.resolver.resolverMainFields = ['react-native', 'browser', 'main'];

// Alias
config.resolver.alias = {
  '@hydroespinaca/shared': sharedRoot,
  'react-native-svg': require.resolve('react-native-svg'),
};

// Rutas a observar
config.watchFolders = [workspaceRoot, sharedRoot];

// Para que Metro resuelva correctamente dependencias dentro de shared
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules'),
];

module.exports = config;
