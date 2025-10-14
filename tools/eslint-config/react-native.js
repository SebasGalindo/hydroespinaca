module.exports = {
  extends: [
    './base.js',
    '@react-native'
  ],
  plugins: [
    'react',
    'react-hooks',
    'react-native'
  ],
  env: {
    'react-native/react-native': true
  },
  parserOptions: {
    ecmaFeatures: {
      jsx: true
    },
    ecmaVersion: 2022,
    sourceType: 'module'
  },
  rules: {
    // React específicas - Optimizadas para React 18+
    'react/react-in-jsx-scope': 'off', // No necesario con jsx-runtime
    'react/prop-types': 'off', // Usamos TypeScript
    'react/jsx-uses-react': 'off', // No necesario con jsx-runtime
    'react/jsx-uses-vars': 'error',
    'react/jsx-key': 'error',
    'react/jsx-no-duplicate-props': 'error',
    'react/jsx-no-undef': 'error',
    'react/no-children-prop': 'error',
    'react/function-component-definition': ['error', {
      'namedComponents': 'function-declaration',
      'unnamedComponents': 'arrow-function'
    }],
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
    
    // React Native específicas
    'react-native/no-unused-styles': 'error',
    'react-native/split-platform-components': 'error',
    'react-native/no-inline-styles': 'warn',
    'react-native/no-color-literals': 'warn',
    'react-native/no-raw-text': 'error'
  },
  settings: {
    react: {
      version: 'detect'
    }
  }
};