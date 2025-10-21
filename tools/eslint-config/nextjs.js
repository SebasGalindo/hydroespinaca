module.exports = {
  extends: [
    './base.js',
    'next/core-web-vitals',
    'next/typescript'
  ],
  plugins: [
    'react',
    'react-hooks'
  ],
  env: {
    browser: true,
    es2022: true,
    node: true
  },
  parserOptions: {
    ecmaFeatures: {
      jsx: true
    },
    ecmaVersion: 2022,
    sourceType: 'module'
  },
  rules: {
    // React específicas
    'react/react-in-jsx-scope': 'off', // Next.js no requiere importar React
    'react/prop-types': 'off', // Usamos TypeScript
    'react/jsx-uses-react': 'off',
    'react/jsx-uses-vars': 'error',
    'react/jsx-key': 'error',
    'react/jsx-no-duplicate-props': 'error',
    'react/jsx-no-undef': 'error',
    'react/no-children-prop': 'error',
    'react/no-danger-with-children': 'error',
    'react/no-deprecated': 'error',
    'react/no-direct-mutation-state': 'error',
    'react/no-find-dom-node': 'error',
    'react/no-is-mounted': 'error',
    'react/no-render-return-value': 'error',
    'react/no-string-refs': 'error',
    'react/no-unescaped-entities': 'error',
    'react/no-unknown-property': 'error',
    'react/require-render-return': 'error',
    'react/self-closing-comp': 'error',
    
    // React Hooks
    'react-hooks/rules-of-hooks': 'error',
    'react-hooks/exhaustive-deps': 'warn',
    
    // Next.js específicas
    '@next/next/no-img-element': 'error',
    '@next/next/no-page-custom-font': 'error'
  },
  settings: {
    react: {
      version: 'detect'
    }
  }
};