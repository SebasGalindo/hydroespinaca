import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import typescript from '@rollup/plugin-typescript';
import dts from 'rollup-plugin-dts';
import fs from 'fs';
import path from 'path';

const pkg = JSON.parse(fs.readFileSync('./package.json', 'utf8'));

// Source and dist directories
const srcDir = 'src';
const distDir = 'dist';

// Modules to build (besides main index)
const modules = ['hooks', 'store', 'utils', 'types', 'api', 'icons'];

// External dependencies for both platforms
const commonExternal = [
  ...Object.keys(pkg.dependencies || {}),
  ...Object.keys(pkg.peerDependencies || {}),
  'react/jsx-runtime',
];

// Web-specific externals (exclude React Native packages)
const webExternal = [
  ...commonExternal,
  'react-native',
  'react-native-svg',
  'react-native-web',
  'expo-secure-store',
  '@react-native-async-storage/async-storage',
];

// Native-specific externals
const nativeExternal = [
  ...commonExternal,
  'react-native-svg',
  'expo-secure-store',
  '@react-native-async-storage/async-storage',
];

// Generate Rollup configurations
const buildConfigs = [];

/**
 * Build JavaScript bundles (CJS + ESM) for WEB
 */
function createWebJsConfig(moduleName) {
  const isMainIndex = moduleName === 'index';
  let inputFile = isMainIndex
    ? path.join(srcDir, 'index.web.ts')
    : path.join(srcDir, moduleName, 'index.web.ts');

  // Fallback to regular index.ts if web-specific doesn't exist
  if (!fs.existsSync(inputFile)) {
    inputFile = isMainIndex
      ? path.join(srcDir, 'index.ts')
      : path.join(srcDir, moduleName, 'index.ts');

    if (!fs.existsSync(inputFile)) {
      return null; // Skip if neither exists
    }
  }

  const outputBase = isMainIndex
    ? path.join(distDir, 'index.web')
    : path.join(distDir, moduleName, 'index.web');

  return {
    input: inputFile,
    external: webExternal,
    plugins: [
      resolve({
        browser: true,
        preferBuiltins: false,
        extensions: ['.web.ts', '.web.tsx', '.ts', '.tsx', '.js', '.jsx']
      }),
      commonjs(),
      typescript({
        tsconfig: './tsconfig.web.json',
        declaration: false,
        outDir: distDir,
      })
    ],
    output: [
      {
        file: `${outputBase}.js`,
        format: 'cjs',
        sourcemap: true,
        exports: 'named',
      },
      {
        file: `${outputBase}.esm.js`,
        format: 'esm',
        sourcemap: true,
      },
    ],
  };
}

/**
 * Build JavaScript bundles (CJS + ESM) for NATIVE
 */
function createNativeJsConfig(moduleName) {
  const isMainIndex = moduleName === 'index';
  let inputFile = isMainIndex
    ? path.join(srcDir, 'index.native.ts')
    : path.join(srcDir, moduleName, 'index.native.ts');

  // Fallback to regular index.ts if native-specific doesn't exist
  if (!fs.existsSync(inputFile)) {
    inputFile = isMainIndex
      ? path.join(srcDir, 'index.ts')
      : path.join(srcDir, moduleName, 'index.ts');

    if (!fs.existsSync(inputFile)) {
      return null; // Skip if neither exists
    }
  }

  const outputBase = isMainIndex
    ? path.join(distDir, 'index.native')
    : path.join(distDir, moduleName, 'index.native');

  return {
    input: inputFile,
    external: nativeExternal,
    plugins: [
      resolve({
        preferBuiltins: false,
        extensions: ['.native.ts', '.native.tsx', '.ts', '.tsx', '.js', '.jsx']
      }),
      commonjs(),
      typescript({
        tsconfig: './tsconfig.native.json',
        declaration: false,
        outDir: distDir,
      })
    ],
    output: [
      {
        file: `${outputBase}.js`,
        format: 'cjs',
        sourcemap: true,
        exports: 'named',
      },
      {
        file: `${outputBase}.esm.js`,
        format: 'esm',
        sourcemap: true,
      },
    ],
  };
}

/**
 * Build TypeScript declarations (.d.ts) for WEB
 */
function createWebDtsConfig(moduleName) {
  const isMainIndex = moduleName === 'index';
  let inputFile = isMainIndex
    ? path.join(srcDir, 'index.web.ts')
    : path.join(srcDir, moduleName, 'index.web.ts');

  // Fallback to regular index.ts if web-specific doesn't exist
  if (!fs.existsSync(inputFile)) {
    inputFile = isMainIndex
      ? path.join(srcDir, 'index.ts')
      : path.join(srcDir, moduleName, 'index.ts');

    if (!fs.existsSync(inputFile)) {
      return null;
    }
  }

  const outputFile = isMainIndex
    ? path.join(distDir, 'index.web.d.ts')
    : path.join(distDir, moduleName, 'index.d.ts');

  return {
    input: inputFile,
    output: {
      file: outputFile,
      format: 'esm',
    },
    plugins: [dts()],
    external: webExternal,
  };
}

/**
 * Build TypeScript declarations (.d.ts) for NATIVE
 */
function createNativeDtsConfig(moduleName) {
  const isMainIndex = moduleName === 'index';
  let inputFile = isMainIndex
    ? path.join(srcDir, 'index.native.ts')
    : path.join(srcDir, moduleName, 'index.native.ts');

  // Fallback to regular index.ts if native-specific doesn't exist
  if (!fs.existsSync(inputFile)) {
    inputFile = isMainIndex
      ? path.join(srcDir, 'index.ts')
      : path.join(srcDir, moduleName, 'index.ts');

    if (!fs.existsSync(inputFile)) {
      return null;
    }
  }

  const outputFile = isMainIndex
    ? path.join(distDir, 'index.native.d.ts')
    : path.join(distDir, moduleName, 'index.native.d.ts');

  return {
    input: inputFile,
    output: {
      file: outputFile,
      format: 'esm',
    },
    plugins: [dts()],
    external: nativeExternal,
  };
}

// Build configurations for all modules
const allModules = ['index', ...modules];

for (const mod of allModules) {
  // Web JS builds
  const webJsConfig = createWebJsConfig(mod);
  if (webJsConfig) buildConfigs.push(webJsConfig);

  // Native JS builds
  const nativeJsConfig = createNativeJsConfig(mod);
  if (nativeJsConfig) buildConfigs.push(nativeJsConfig);

  // Web TypeScript definitions
  const webDtsConfig = createWebDtsConfig(mod);
  if (webDtsConfig) buildConfigs.push(webDtsConfig);

  // Native TypeScript definitions
  const nativeDtsConfig = createNativeDtsConfig(mod);
  if (nativeDtsConfig) buildConfigs.push(nativeDtsConfig);
}

export default buildConfigs;
