import { defineConfig } from 'tsup'

export default defineConfig({
  entry: 'src/index.ts',
  format: ['esm', 'cjs'],
  dts: false, // Disabled due to React Native dependencies
  clean: true,
  external: ['react', 'react-dom', 'react-native'],
  splitting: false,
  sourcemap: true,
  skipNodeModulesBundle: true,
})