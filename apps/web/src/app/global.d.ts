// Global type definitions for Next.js web app
declare global {
  // React Native __DEV__ global (always undefined in web, but needed for
  // cross-platform shared package sources compiled via tsconfig paths)
  // eslint-disable-next-line no-var
  var __DEV__: boolean | undefined;

  namespace NodeJS {
    interface ProcessEnv {
      NEXT_RUNTIME?: 'nodejs' | 'edge';
      NODE_ENV: 'development' | 'production' | 'test';
      NEXT_PUBLIC_API_URL?: string;
    }
  }

  // Vite environment variables (for compatibility with shared package)
  interface ImportMetaEnv {
    readonly VITE_API_URL?: string;
    readonly VITE_MODE?: string;
    readonly MODE?: string;
  }

  interface ImportMeta {
    readonly env: ImportMetaEnv;
  }

  // Browser APIs are already available via lib: ["DOM"]
  // but we ensure they're properly typed for conditional checks
  interface Navigator {
    readonly product?: string;
  }
}

export { };