import { defineConfig } from 'tsup'

export default defineConfig({
  entry: {
    index: 'src/index.ts',
    'hooks/index': 'src/hooks/index.ts',
    'components/index': 'src/components/index.ts'
  },
  format: ['esm', 'cjs'],
  dts: {
    // Disable composite mode for dts build
    compilerOptions: {
      composite: false
    }
  },
  clean: true,
  external: ['react', 'react-dom'],
  splitting: false,
  sourcemap: true
})