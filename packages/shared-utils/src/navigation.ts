import type { NavigationService, WebRoutes } from '@hydroespinaca/shared-types';

// Web Routes Constants
export const WEB_ROUTES: WebRoutes = {
  PUBLIC: '/',
  LOGIN: '/login',
  PROTECTED: '/protected',
  DASHBOARD: '/dashboard',
};

// Create Navigation Service for Web
// Note: This should be used with React Router's useNavigate hook for SPA navigation
export const createWebNavigationService = (navigateFn?: (path: string) => void): NavigationService => {
  const navigate = (path: string) => {
    if (navigateFn) {
      // Use React Router navigate function if available
      navigateFn(path);
    } else if (typeof window !== 'undefined') {
      // Fallback to window.location for full page navigation
      window.location.href = path;
    }
  };

  const redirect = (path: string) => {
    if (typeof window !== 'undefined') {
      window.location.href = path;
    }
  };

  const goBack = () => {
    if (typeof window !== 'undefined') {
      window.history.back();
    }
  };

  return { navigate, redirect, goBack };
};

// Create Navigation Service for Mobile (placeholder)
// This will be implemented with React Navigation in mobile-specific code
export const createMobileNavigationService = (navigation: any): NavigationService => {
  const navigate = (routeName: string) => {
    if (navigation?.navigate) {
      navigation.navigate(routeName);
    }
  };

  const redirect = (routeName: string) => {
    if (navigation?.reset) {
      navigation.reset({
        index: 0,
        routes: [{ name: routeName }],
      });
    } else if (navigation?.navigate) {
      navigation.navigate(routeName);
    }
  };

  const goBack = () => {
    if (navigation?.goBack) {
      navigation.goBack();
    }
  };

  return { navigate, redirect, goBack };
};

// Platform-agnostic navigation helpers
export const getRouteForPlatform = (route: keyof WebRoutes, platform: 'web' | 'mobile') => {
  if (platform === 'web') {
    return WEB_ROUTES[route];
  }
  
  // Mobile route mapping
  const mobileRoutes = {
    PUBLIC: 'Public',
    LOGIN: 'Login', 
    PROTECTED: 'Home',
    DASHBOARD: 'Home',
  };
  
  return mobileRoutes[route];
};