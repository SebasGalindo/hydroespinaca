// Default utils export (re-exports from web version for compatibility)
// Platform-specific builds use index.web.ts or index.native.ts
export * from './formatters';
export * from './dateHelpers';
export * from './authHelpers';
export * from './apiConfig';
export * from './authFetch';
export * from './variableAlerts';

// For backward compatibility with direct imports
// Note: SessionStorage import should use platform-specific entry points
export { secureStorage, SessionStorage } from './secureStorage.web';