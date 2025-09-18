export interface LoginRequest {
  Email: string;
  Password: string;
}

export interface MobileLoginResponse {
  SessionId: string;
  CsrfToken: string;
  Message?: string;
}

export interface WebLoginResponse {
  Message?: string;
  SessionId?: string;
  CsrfToken?: string;
}

export interface Session {
  sessionId: string | null; // null para web (HttpOnly cookie), string para mobile
  csrfToken: string;
  userId?: string; // User ID from backend
  role?: string; // User role from backend
}

export interface SessionInfo {
  user?: any; // User data from backend
  isValid: boolean;
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