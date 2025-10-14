// Authentication Types
export interface LoginRequest {
  Email: string;
  Password: string;
}

export interface MobileLoginResponse {
  sessionId: string;
  csrfToken: string;
  message?: string;
}

export interface WebLoginResponse {
  message?: string;
}

export interface UserSession {
  userId: string;
  userRole: string;
}

export interface Session {
  sessionId: string | null; // null for web (HttpOnly cookie), string for mobile
  csrfToken: string | null; // null for web (cookie), string for mobile
  userId?: string; // User ID from backend
  userRole?: string; // User role from backend
}

export interface AuthState {
  session: Session | null;
  isLoading: boolean;
  error: string | null;
}

export interface UseAuthReturn {
  session: Session | null;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  isAuthenticated: boolean;
}

export type Platform = 'web' | 'mobile';

export interface AuthConfig {
  platform: Platform;
  apiBaseUrl?: string;
}
