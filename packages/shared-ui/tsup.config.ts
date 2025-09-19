import { defineConfig } from 'tsup'

export default defineConfig({
  entry: 'src/index.ts',
  format: ['esm', 'cjs'],
  dts: false, // Temporalmente deshabilitado para evitar problemas con react-native
  clean: true,
  external: ['react', 'react-dom', 'react-native'],
  splitting: false,
  sourcemap: true
})