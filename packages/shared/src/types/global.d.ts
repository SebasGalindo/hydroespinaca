// Global type definitions for cross-platform shared package

// ============================================
// Vite Environment Variables
// ============================================
interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_MODE?: string;
  readonly MODE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

// ============================================
// Browser APIs (for conditional usage)
// ============================================
// Minimal type definitions for browser APIs used in platform-agnostic code
// These work even when lib: ["DOM"] is not included (React Native builds)

// Navigator for React Native detection
declare const navigator: {
  readonly product?: string;
  readonly userAgent?: string;
} | undefined;

// Window for browser detection
declare const window: {
  readonly localStorage?: Storage;
  location?: {
    href?: string;
  };
} | undefined;

// Document for browser detection
declare const document: unknown | undefined;

// localStorage global (fallback for direct access)
declare const localStorage: Storage | undefined;

// Storage interface (minimal)
interface Storage {
  readonly length: number;
  clear(): void;
  getItem(key: string): string | null;
  key(index: number): string | null;
  removeItem(key: string): void;
  setItem(key: string, value: string): void;
}

// ============================================
// Node.js Process Environment
// ============================================
declare namespace NodeJS {
  interface ProcessEnv {
    readonly NODE_ENV?: string;
    readonly NEXT_PUBLIC_API_URL?: string;
    readonly EXPO_PUBLIC_API_URL?: string;
  }
}
