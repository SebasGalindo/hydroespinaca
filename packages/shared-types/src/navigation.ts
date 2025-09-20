// Navigation Types

// Web Router Types
export interface WebRoutes {
  PUBLIC: '/';
  LOGIN: '/login';
  PROTECTED: '/protected';
  DASHBOARD: '/dashboard';
}

// Mobile Navigation Types
export type RootStackParamList = {
  Public: undefined;
  Login: undefined;
  Home: undefined;
  Dashboard: undefined;
};

// Shared Navigation Service Interface
export interface NavigationService {
  navigate: (path: string) => void;
  redirect: (path: string) => void;
  goBack?: () => void;
}

// Route Configuration
export interface RouteConfig {
  path: string;
  component: string;
  protected?: boolean;
  title?: string;
}