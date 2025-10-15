declare namespace NodeJS {
  interface ProcessEnv {
    NODE_ENV: 'development' | 'dev' | 'production' | 'test';
    NEXT_PUBLIC_API_URL?: string;
    EXPO_PUBLIC_API_URL?: string;
    VITE_API_URL?: string;
    VITE_MODE?: string;
  }
}
