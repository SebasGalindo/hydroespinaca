module.exports = {
  extends: [
    './base.js'
  ],
  env: {
    node: true,
    es2022: true
  },
  parserOptions: {
    ecmaVersion: 2022,
    sourceType: 'module'
  },
  rules: {
    // Node.js específicas
    'no-console': 'off', // En Node.js está bien usar console
    'no-process-exit': 'error',
    'no-process-env': 'off',
    
    // Imports/Exports para Node.js
    'import/no-dynamic-require': 'error',
    'import/no-nodejs-modules': 'off',
    
    // Async/Await
    'require-await': 'error',
    'no-return-await': 'error',
    
    // Error handling
    'handle-callback-err': 'error',
    'no-mixed-requires': 'error',
    'no-new-require': 'error',
    'no-path-concat': 'error'
  }
};