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

// Remove alias to allow proper resolution through index.native.ts files
// This ensures Metro resolves packages through their main entry points
// which will use index.native.ts for React Native

// Add debugging for module resolution
config.resolver.resolverMainFields = ['react-native', 'browser', 'main'];

// SUPER AGGRESSIVE BLOCK: Completely block any .web files from being included
config.resolver.blockList = [
  // Block ALL .web files in the entire project - more specific patterns
  /.*\.web\.(js|jsx|ts|tsx)$/,
  /.*\.web\.js$/,
  /.*\.web\.jsx$/,
  /.*\.web\.ts$/,
  /.*\.web\.tsx$/,
  // Specifically block the problematic LoginForm.web.tsx
  /.*LoginForm\.web\.tsx$/,
  /.*\/LoginForm\.web\.tsx$/,
  /packages\/shared-ui\/src\/LoginForm\.web\.tsx$/,
  // Block any file in shared-ui that has .web in the name  
  /.*shared-ui.*\.web\.(js|jsx|ts|tsx)$/,
  /packages\/shared-ui\/.*\.web\.(js|jsx|ts|tsx)$/,
  // Block Input.web.tsx specifically
  /.*Input\.web\.tsx$/,
  /.*\/Input\.web\.tsx$/,
  /packages\/shared-ui\/src\/Input\.web\.tsx$/,
];

// Block web files and force native resolution
const originalResolveRequest = config.resolver.resolveRequest;
config.resolver.resolveRequest = (context, moduleName, platform) => {
  // Block any .web file resolution
  if (moduleName.includes('.web') || 
      moduleName.includes('LoginForm.web') || 
      moduleName.includes('Input.web') ||
      moduleName.endsWith('.web.tsx') ||
      moduleName.endsWith('.web.ts') ||
      moduleName.endsWith('.web.jsx') ||
      moduleName.endsWith('.web.js') ||
      (moduleName.includes('LoginForm') && platform !== 'native' && platform !== 'android' && platform !== 'ios')) {
    throw new Error(`Blocked web file: ${moduleName}`);
  }
  
  // Force LoginForm to always resolve to .native version
  if (moduleName.includes('LoginForm') && !moduleName.includes('.native')) {
    const nativeModuleName = moduleName.replace('LoginForm', 'LoginForm.native');
    moduleName = nativeModuleName;
  }
  
  // Call original resolver
  if (originalResolveRequest) {
    return originalResolveRequest(context, moduleName, platform);
  }
  return context.resolveRequest(context, moduleName, platform);
};

// FORCE all shared packages to use src files directly, bypassing dist/
config.resolver.alias = {
  // Force main package imports to use src directly
  '@hydroespinaca/shared-ui': path.resolve(workspaceRoot, 'packages/shared-ui/src/index.native.ts'),
  '@hydroespinaca/shared-hooks': path.resolve(workspaceRoot, 'packages/shared-hooks/src/index.ts'),
  '@hydroespinaca/shared-utils': path.resolve(workspaceRoot, 'packages/shared-utils/src/index.native.ts'),
  '@hydroespinaca/shared-types': path.resolve(workspaceRoot, 'packages/shared-types/src/index.ts'),
  
  // Specific component overrides
  '@hydroespinaca/shared-ui/LoginForm': path.resolve(workspaceRoot, 'packages/shared-ui/src/LoginForm.native.tsx'),
  '@hydroespinaca/shared-ui/Button': path.resolve(workspaceRoot, 'packages/shared-ui/src/Button.native.tsx'),
  '@hydroespinaca/shared-ui/Input': path.resolve(workspaceRoot, 'packages/shared-ui/src/Input.native.tsx'),
  
  // Force secure storage to use native version
  '@hydroespinaca/shared-utils/secureStorage': path.resolve(workspaceRoot, 'packages/shared-utils/src/secureStorage.native.ts'),
};

module.exports = config;